using System.Globalization;
using System.Diagnostics;
using System.Text;
using Terminal.Gui;

var catalog = UnicodeCatalog.Build();
var filtered = new List<GlyphEntry>(catalog.Entries);

Application.Init();

var top = Application.Top;
var window = new Window("Unicode Browser")
{
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill() - 1
};

var filterLabel = new Label("Filter")
{
    X = 1,
    Y = 0
};

var filterField = new TextField("")
{
    X = Pos.Right(filterLabel) + 1,
    Y = 0,
    Width = 24
};

var collectionLabel = new Label("Collection")
{
    X = Pos.Right(filterField) + 2,
    Y = 0
};

var collectionOptions = catalog.Collections.Select(static item => item.Label).ToList();
var collectionButton = new Button("")
{
    X = Pos.Right(collectionLabel) + 1,
    Y = 0,
    Width = 28
};

var planeLabelHeader = new Label("Plane")
{
    X = 1,
    Y = 1
};

var planeOptions = catalog.Planes.Select(static item => item.Label).ToList();
var planeButton = new Button("")
{
    X = Pos.Right(planeLabelHeader) + 1,
    Y = 1,
    Width = 24
};

var statsLabel = new Label("")
{
    X = Pos.Right(planeButton) + 2,
    Y = 1,
    Width = 36
};

var copyStatusLabel = new Label("")
{
    X = Pos.Right(statsLabel) + 1,
    Y = 1,
    Width = Dim.Fill() - 1
};

var glyphList = new ListView(filtered)
{
    X = 0,
    Y = 3,
    Width = 48,
    Height = Dim.Fill()
};

var detailsFrame = new FrameView("Glyph Details")
{
    X = Pos.Right(glyphList),
    Y = 3,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var glyphLabel = new Label("")
{
    X = 1,
    Y = 0,
    Width = Dim.Fill() - 2
};

var codePointLabel = new Label("")
{
    X = 1,
    Y = 2,
    Width = Dim.Fill() - 2
};

var sourceLabel = new Label("")
{
    X = 1,
    Y = 4,
    Width = Dim.Fill() - 2
};

var planeLabel = new Label("")
{
    X = 1,
    Y = 6,
    Width = Dim.Fill() - 2
};

var sampleLabel = new Label("")
{
    X = 1,
    Y = 8,
    Width = Dim.Fill() - 2
};

var notesView = new TextView()
{
    X = 1,
    Y = 10,
    Width = Dim.Fill() - 2,
    Height = Dim.Fill() - 3,
    ReadOnly = true,
    WordWrap = true
};

detailsFrame.Add(glyphLabel, codePointLabel, sourceLabel, planeLabel, sampleLabel, notesView);

var selectedCollectionIndex = 0;
var selectedPlaneIndex = 0;

var menu = new MenuBar(new[]
{
    new MenuBarItem("_File", new[]
    {
        new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.Q)
    }),
    new MenuBarItem("_Copy", new[]
    {
        new MenuItem("_Glyph", "", CopyGlyph, null, null, Key.CtrlMask | Key.Y),
        new MenuItem("Code _Point", "", CopyCodePoint, null, null, Key.CtrlMask | Key.U),
        new MenuItem("_Glyph + Code Point", "", CopyCombined, null, null, Key.CtrlMask | Key.G)
    }),
    new MenuBarItem("_Help", new[]
    {
        new MenuItem("_About", "", ShowAbout)
    })
});

var statusBar = new StatusBar(new[]
{
    new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop()),
    new StatusItem(Key.F5, "~F5~ Reset", ResetFilter),
    new StatusItem(Key.CtrlMask | Key.Y, "~^Y~ Copy glyph", CopyGlyph),
    new StatusItem(Key.CtrlMask | Key.U, "~^U~ Copy code", CopyCodePoint),
    new StatusItem(Key.Enter, "~Enter~ Details", ShowDetailsForSelection)
});

glyphList.SelectedItemChanged += _ => RefreshDetails();
glyphList.OpenSelectedItem += _ => ShowDetailsForSelection();
filterField.TextChanged += _ => ApplyFilter();
collectionButton.Clicked += () => ShowPicker("Collection", collectionOptions, selectedCollectionIndex, index =>
{
    selectedCollectionIndex = index;
    ApplyFilter();
});
planeButton.Clicked += () => ShowPicker("Plane", planeOptions, selectedPlaneIndex, index =>
{
    selectedPlaneIndex = index;
    ApplyFilter();
});

window.Add(filterLabel, filterField, collectionLabel, collectionButton, planeLabelHeader, planeButton, statsLabel, copyStatusLabel, glyphList, detailsFrame);
top.Add(menu, window, statusBar);

ResetFilter();
glyphList.SetFocus();
Application.Run();
Application.Shutdown();

void ResetFilter()
{
    filterField.Text = "";
    selectedCollectionIndex = 0;
    selectedPlaneIndex = 0;
    ApplyFilter();
}

void ApplyFilter()
{
    var query = filterField.Text?.ToString()?.Trim() ?? string.Empty;
    var selectedCollection = catalog.Collections[Math.Max(selectedCollectionIndex, 0)];
    var selectedPlane = catalog.Planes[Math.Max(selectedPlaneIndex, 0)];

    filtered = catalog.Filter(query, selectedCollection, selectedPlane);
    glyphList.SetSource(filtered);
    glyphList.SelectedItem = filtered.Count == 0 ? -1 : 0;
    collectionButton.Text = $" {selectedCollection.Label} v";
    planeButton.Text = $" {selectedPlane.Label} v";
    statsLabel.Text = $"{filtered.Count} glyphs  |  {selectedCollection.Label}  |  {selectedPlane.Label}";
    copyStatusLabel.Text = "";
    RefreshDetails();
}

void RefreshDetails()
{
    if (filtered.Count == 0 || glyphList.SelectedItem < 0 || glyphList.SelectedItem >= filtered.Count)
    {
        glyphLabel.Text = "Glyph: no match";
        codePointLabel.Text = "Code point:";
        sourceLabel.Text = "Block:";
        planeLabel.Text = "Plane:";
        sampleLabel.Text = "Rendered:";
        notesView.Text = "Search by glyph, collection, block, or code point.\n\nExamples:\n  arrows\n  technical\n  powerline\n  material\n  2192\n  e0b0\n  f0001";
        return;
    }

    var entry = filtered[glyphList.SelectedItem];
    glyphLabel.Text = $"Glyph: {entry.DisplayGlyph}   Name: {entry.DisplayName}";
    codePointLabel.Text = $"Code point: U+{entry.CodePointHex}";
    sourceLabel.Text = $"Block: {entry.BlockName}   Collection: {entry.CollectionName}";
    planeLabel.Text = $"Plane: {entry.PlaneDescription}";
    sampleLabel.Text = $"Rendered: {entry.DisplayGlyph}";
    notesView.Text = $"{entry.Description}\n\nSearch terms: {entry.SearchSummary}";
}

void ShowDetailsForSelection()
{
    if (filtered.Count == 0 || glyphList.SelectedItem < 0 || glyphList.SelectedItem >= filtered.Count)
    {
        return;
    }

    var entry = filtered[glyphList.SelectedItem];
    MessageBox.Query(
        76,
        18,
        entry.DisplayName,
        $"Glyph: {entry.DisplayGlyph}\nCode point: U+{entry.CodePointHex}\nCollection: {entry.CollectionName}\nBlock: {entry.BlockName}\nPlane: {entry.PlaneDescription}\n\n{entry.Description}",
        "OK");
}

void CopyGlyph()
{
    CopySelection(static entry => entry.Glyph, "glyph");
}

void CopyCodePoint()
{
    CopySelection(static entry => $"U+{entry.CodePointHex}", "code point");
}

void CopyCombined()
{
    CopySelection(static entry => $"{entry.Glyph} U+{entry.CodePointHex} {entry.DisplayName}", "glyph and code point");
}

void CopySelection(Func<GlyphEntry, string> selector, string label)
{
    if (filtered.Count == 0 || glyphList.SelectedItem < 0 || glyphList.SelectedItem >= filtered.Count)
    {
        return;
    }

    var entry = filtered[glyphList.SelectedItem];
    var payload = selector(entry);

    if (TryCopyToClipboard(payload, out var tool, out var error))
    {
        copyStatusLabel.Text = $"Copied {label} via {tool}";
        Application.Refresh();
        return;
    }

    MessageBox.ErrorQuery(
        70,
        10,
        "Clipboard Error",
        $"Could not copy {label} to the clipboard.\n\nTried: clip.exe, powershell.exe, pwsh.exe\n\n{error}",
        "OK");
}

void ShowAbout()
{
    MessageBox.Query(
        78,
        18,
        "About",
        "Browse Nerd Fonts and standard Unicode symbol blocks.\n\n" +
        "Filters:\n" +
        "  Search by name, block, collection, code point, or glyph.\n" +
        "  Narrow by collection/block and Unicode plane.\n\n" +
        "Clipboard:\n" +
        "  Ctrl-Y copies the glyph.\n" +
        "  Ctrl-U copies the U+ code point.\n" +
        "  Ctrl-G copies both.\n\n" +
        "Material Design icons live in Supplementary Private Use Area-A.\n" +
        "If they fail to render, the usual cause is terminal or font support for supplementary-plane private-use glyphs rather than the browser data model.",
        "OK");
}

void ShowPicker(string title, IReadOnlyList<string> options, int selectedIndex, Action<int> apply)
{
    var dialog = new Dialog(title, 60, 20);
    var list = new ListView(options.ToList())
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill() - 2
    };
    list.SelectedItem = selectedIndex;

    var ok = new Button("OK", is_default: true);
    var cancel = new Button("Cancel");

    ok.Clicked += () =>
    {
        apply(Math.Max(list.SelectedItem, 0));
        Application.RequestStop(dialog);
    };

    cancel.Clicked += () => Application.RequestStop(dialog);
    list.OpenSelectedItem += _ =>
    {
        apply(Math.Max(list.SelectedItem, 0));
        Application.RequestStop(dialog);
    };

    dialog.Add(list);
    dialog.AddButton(ok);
    dialog.AddButton(cancel);
    Application.Run(dialog);
}

static bool TryCopyToClipboard(string text, out string tool, out string error)
{
    error = "No clipboard command succeeded.";

    foreach (var candidate in ClipboardCandidates(text))
    {
        if (RunClipboardCommand(candidate.FileName, candidate.Arguments, text, out error))
        {
            tool = candidate.FileName;
            return true;
        }
    }

    tool = "";
    return false;
}

static IEnumerable<(string FileName, string Arguments)> ClipboardCandidates(string text)
{
    yield return ("clip.exe", "");
    yield return ("powershell.exe", "-NoProfile -Command Set-Clipboard");
    yield return ("pwsh.exe", "-NoProfile -Command Set-Clipboard");
}

static bool RunClipboardCommand(string fileName, string arguments, string text, out string error)
{
    try
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false)
            }
        };

        process.Start();
        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            error = "";
            return true;
        }

        error = process.StandardError.ReadToEnd();
        if (string.IsNullOrWhiteSpace(error))
        {
            error = $"{fileName} exited with code {process.ExitCode}.";
        }

        return false;
    }
    catch (Exception ex)
    {
        error = ex.Message;
        return false;
    }
}

sealed record FilterOption(string Key, string Label);

sealed record GlyphBlock(
    string CollectionName,
    string BlockName,
    string Description,
    params CodePointRange[] Ranges);

sealed record CodePointRange(
    int Start,
    int End,
    params int[] Excluded);

sealed record GlyphEntry(
    int CodePoint,
    string CodePointHex,
    string Glyph,
    string DisplayGlyph,
    string CollectionName,
    string BlockName,
    string DisplayName,
    string Description,
    string PlaneKey,
    string PlaneDescription,
    string SearchSummary)
{
    public override string ToString()
    {
        return $"{DisplayGlyph} U+{CodePointHex} {DisplayName}";
    }
}

sealed class UnicodeCatalog
{
    public List<GlyphEntry> Entries { get; }
    public List<FilterOption> Collections { get; }
    public List<FilterOption> Planes { get; }

    private UnicodeCatalog(List<GlyphEntry> entries, List<FilterOption> collections, List<FilterOption> planes)
    {
        Entries = entries;
        Collections = collections;
        Planes = planes;
    }

    public static UnicodeCatalog Build()
    {
        var blocks = new[]
        {
            new GlyphBlock("Nerd Fonts", "Pomicons", "Pomodoro-themed Nerd Fonts icons.", new CodePointRange(0xE000, 0xE00A)),
            new GlyphBlock("Nerd Fonts", "Powerline Symbols", "Classic Powerline separators.", new CodePointRange(0xE0A0, 0xE0A2), new CodePointRange(0xE0B0, 0xE0B3)),
            new GlyphBlock("Nerd Fonts", "Powerline Extra Symbols", "Extra Powerline separators and shell symbols.", new CodePointRange(0xE0A3, 0xE0A3), new CodePointRange(0xE0B4, 0xE0C8), new CodePointRange(0xE0CA, 0xE0CA), new CodePointRange(0xE0CC, 0xE0D7), new CodePointRange(0x2630, 0x2630)),
            new GlyphBlock("Nerd Fonts", "Font Awesome Extension", "Extra Font Awesome-compatible Nerd Fonts glyphs.", new CodePointRange(0xE200, 0xE2A9)),
            new GlyphBlock("Nerd Fonts", "Weather", "Weather-themed Nerd Fonts icons.", new CodePointRange(0xE300, 0xE3E3)),
            new GlyphBlock("Nerd Fonts", "Seti-UI + Custom", "Seti-UI icons plus Nerd Fonts custom additions.", new CodePointRange(0xE5FA, 0xE6B7)),
            new GlyphBlock("Nerd Fonts", "Devicons", "Developer-oriented filetype and tool icons.", new CodePointRange(0xE700, 0xE8EF)),
            new GlyphBlock("Nerd Fonts", "Codicons", "Visual Studio Code icon glyphs.", new CodePointRange(0xEA60, 0xEC1E)),
            new GlyphBlock("Nerd Fonts", "Font Awesome", "Font Awesome glyphs relocated into Nerd Fonts code points.", new CodePointRange(0xED00, 0xF2FF, 0xEE00, 0xEE01, 0xEE02, 0xEE03, 0xEE04, 0xEE05, 0xEE06, 0xEE07, 0xEE08, 0xEE09, 0xEE0A, 0xEE0B)),
            new GlyphBlock("Nerd Fonts", "Progress", "Progress indicator glyphs reserved inside the Nerd Fonts PUA.", new CodePointRange(0xEE00, 0xEE0B)),
            new GlyphBlock("Nerd Fonts", "Font Logos", "Linux distribution and open-source project logos.", new CodePointRange(0xF300, 0xF381)),
            new GlyphBlock("Nerd Fonts", "Octicons", "GitHub Octicons plus a few standard Unicode carryovers.", new CodePointRange(0xF400, 0xF533), new CodePointRange(0x2665, 0x2665), new CodePointRange(0x26A1, 0x26A1)),
            new GlyphBlock("Nerd Fonts", "Material Design Icons", "Material Design icons stored in Supplementary Private Use Area-A. Rendering depends on terminal and font support for supplementary-plane private-use glyphs.", new CodePointRange(0xF0001, 0xF1AF0)),
            new GlyphBlock("Unicode Symbols", "Arrows", "Standard Unicode arrows.", new CodePointRange(0x2190, 0x21FF)),
            new GlyphBlock("Unicode Symbols", "Mathematical Operators", "Standard mathematical operator symbols.", new CodePointRange(0x2200, 0x22FF)),
            new GlyphBlock("Unicode Symbols", "Miscellaneous Technical", "Standard technical and UI-adjacent symbols.", new CodePointRange(0x2300, 0x23FF)),
            new GlyphBlock("Unicode Symbols", "Control Pictures", "Printable representations of control characters.", new CodePointRange(0x2400, 0x243F)),
            new GlyphBlock("Unicode Symbols", "Optical Character Recognition", "OCR marks and scanning symbols.", new CodePointRange(0x2440, 0x245F)),
            new GlyphBlock("Unicode Symbols", "Enclosed Alphanumerics", "Circled numbers and letters.", new CodePointRange(0x2460, 0x24FF)),
            new GlyphBlock("Unicode Symbols", "Box Drawing", "Standard box drawing characters.", new CodePointRange(0x2500, 0x257F)),
            new GlyphBlock("Unicode Symbols", "Block Elements", "Block and shading characters.", new CodePointRange(0x2580, 0x259F)),
            new GlyphBlock("Unicode Symbols", "Geometric Shapes", "Geometric shape symbols.", new CodePointRange(0x25A0, 0x25FF)),
            new GlyphBlock("Unicode Symbols", "Miscellaneous Symbols", "Weather, zodiac, card suit, and utility symbols.", new CodePointRange(0x2600, 0x26FF)),
            new GlyphBlock("Unicode Symbols", "Dingbats", "Decorative and symbol-oriented dingbats.", new CodePointRange(0x2700, 0x27BF)),
            new GlyphBlock("Unicode Symbols", "Supplemental Arrows-A", "Additional standard Unicode arrows.", new CodePointRange(0x27F0, 0x27FF)),
            new GlyphBlock("Unicode Symbols", "Braille Patterns", "Braille cell patterns.", new CodePointRange(0x2800, 0x28FF)),
            new GlyphBlock("Unicode Symbols", "Supplemental Arrows-B", "Advanced and mathematical arrows.", new CodePointRange(0x2900, 0x297F)),
            new GlyphBlock("Unicode Symbols", "Miscellaneous Symbols and Arrows", "Extra arrows and symbol hybrids.", new CodePointRange(0x2B00, 0x2BFF))
        };

        var entries = new List<GlyphEntry>(16000);

        foreach (var block in blocks)
        {
            foreach (var range in block.Ranges)
            {
                var excluded = range.Excluded.Length == 0 ? null : range.Excluded.ToHashSet();

                for (var codePoint = range.Start; codePoint <= range.End; codePoint++)
                {
                    if (excluded is not null && excluded.Contains(codePoint))
                    {
                        continue;
                    }

                    if (!System.Text.Rune.IsValid(codePoint))
                    {
                        continue;
                    }

                    var rune = new System.Text.Rune(codePoint);
                    var hex = codePoint.ToString(codePoint <= 0xFFFF ? "X4" : "X6", CultureInfo.InvariantCulture);
                    var plane = DescribePlane(codePoint);

                    entries.Add(new GlyphEntry(
                        codePoint,
                        hex,
                        rune.ToString(),
                        GetDisplayGlyph(rune.ToString(), plane.Key),
                        block.CollectionName,
                        block.BlockName,
                        $"{block.BlockName} {hex}",
                        block.Description,
                        plane.Key,
                        plane.Description,
                        $"{block.CollectionName} {block.BlockName} {hex} u+{hex} {rune}"));
                }
            }
        }

        entries.Sort(static (left, right) =>
        {
            var byCollection = string.Compare(left.CollectionName, right.CollectionName, StringComparison.Ordinal);
            if (byCollection != 0)
            {
                return byCollection;
            }

            return left.CodePoint.CompareTo(right.CodePoint);
        });

        var collections = new List<FilterOption> { new("all", "All collections") };
        collections.AddRange(
            blocks
                .Select(static block => block.CollectionName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .Select(static name => new FilterOption(name, name)));
        collections.AddRange(
            blocks
                .Select(static block => block.BlockName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .Select(static name => new FilterOption($"block:{name}", $"Block: {name}")));

        var planes = new List<FilterOption>
        {
            new("all", "All planes"),
            new("standard", "Standard Unicode planes"),
            new("bmp-pua", "BMP private use area"),
            new("spua-a", "Supplementary PUA-A")
        };

        return new UnicodeCatalog(entries, collections, planes);
    }

    public List<GlyphEntry> Filter(string query, FilterOption collection, FilterOption plane)
    {
        var normalized = query.Trim();
        var hexCandidate = normalized.StartsWith("U+", StringComparison.OrdinalIgnoreCase)
            ? normalized[2..]
            : normalized;

        var matches = new List<GlyphEntry>();

        foreach (var entry in Entries)
        {
            if (!MatchesCollection(entry, collection))
            {
                continue;
            }

            if (!MatchesPlane(entry, plane))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(normalized) ||
                entry.CollectionName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                entry.BlockName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                entry.DisplayName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                entry.CodePointHex.Contains(hexCandidate, StringComparison.OrdinalIgnoreCase) ||
                entry.Glyph.Contains(normalized, StringComparison.Ordinal))
            {
                matches.Add(entry);
            }
        }

        return matches;
    }

    private static bool MatchesCollection(GlyphEntry entry, FilterOption option)
    {
        if (option.Key == "all")
        {
            return true;
        }

        if (option.Key.StartsWith("block:", StringComparison.Ordinal))
        {
            return string.Equals(entry.BlockName, option.Key[6..], StringComparison.Ordinal);
        }

        return string.Equals(entry.CollectionName, option.Key, StringComparison.Ordinal);
    }

    private static bool MatchesPlane(GlyphEntry entry, FilterOption option)
    {
        return option.Key switch
        {
            "all" => true,
            "standard" => entry.PlaneKey == "standard",
            "bmp-pua" => entry.PlaneKey == "bmp-pua",
            "spua-a" => entry.PlaneKey == "spua-a",
            _ => true
        };
    }

    private static (string Key, string Description) DescribePlane(int codePoint)
    {
        if (codePoint is >= 0xE000 and <= 0xF8FF)
        {
            return ("bmp-pua", "Basic Multilingual Plane private use area");
        }

        if (codePoint is >= 0xF0000 and <= 0xFFFFF)
        {
            return ("spua-a", "Supplementary Private Use Area-A");
        }

        if (codePoint is >= 0x100000 and <= 0x10FFFF)
        {
            return ("spua-b", "Supplementary Private Use Area-B");
        }

        return ("standard", "Standard Unicode plane");
    }

    private static string GetDisplayGlyph(string glyph, string planeKey)
    {
        return planeKey is "spua-a" or "spua-b" ? "[PUA]" : glyph;
    }
}
