using System.Diagnostics;
using System.Globalization;

namespace DosGameLauncher;

internal sealed class MainForm : Form
{
    private readonly GameLibrary _library;
    private readonly ListBox _list = new();
    private readonly TextBox _search = new();
    private readonly TextBox _name = new();
    private readonly Label _exePath = new();
    private readonly Label _missing = new();
    private readonly Label _lastPlayed = new();
    private readonly CheckBox _fullscreen = new();
    private readonly ComboBox _shader = new();
    private readonly ComboBox _sync = new();
    private readonly ComboBox _speed = new();
    private readonly NumericUpDown _customCycles = new();
    private readonly Button _keys = Theme.MakeButton("按鍵設定", primary: false, height: 36);
    private readonly Label _cdPath = new();
    private readonly Label _cdMissing = new();
    private readonly Button _cdBrowse = Theme.MakeButton("選擇光碟 1", primary: false, height: 34);
    private readonly Button _cdClear = Theme.MakeButton("清除", primary: false, height: 34);
    private readonly Panel _cdActions = new();
    private readonly Label _cdPath2 = new();
    private readonly Label _cdMissing2 = new();
    private readonly Button _cdBrowse2 = Theme.MakeButton("選擇光碟 2", primary: false, height: 34);
    private readonly Button _cdClear2 = Theme.MakeButton("清除", primary: false, height: 34);
    private readonly Panel _cdActions2 = new();
    private readonly ToolTip _tips = new();
    private readonly Button _play;
    private readonly Button _remove;
    private readonly Button _openFolder;
    private readonly Button _changeExe;
    private readonly TableLayoutPanel _editor = new();
    private readonly Panel _optionsScroll = new();
    private readonly Panel _gameTools = new();
    private readonly Label _empty = new();
    private readonly Label _count = new();
    private readonly Label _dosboxStatus = new();
    private static readonly Font RowTitleFont = Theme.FontOf(12f, bold: true);
    private static readonly Font RowPathFont = Theme.FontOf(8.5f);
    private bool _binding;
    public MainForm()
    {
        _library = LibraryStore.Load();

        Text = "DOS 遊戲櫃";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 760);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.FontOf(10.5f);
        DoubleBuffered = true;
        AllowDrop = true;
        KeyPreview = true;

        if (_library.WindowWidth >= 1000 && _library.WindowHeight >= 760)
            Size = new Size(_library.WindowWidth, _library.WindowHeight);
        else
            Size = new Size(1120, 800);

        var header = BuildHeader();
        var body = BuildBody(out _play, out _remove, out _openFolder, out _changeExe);
        var footer = BuildFooter();

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);

        _list.SelectedIndexChanged += (_, _) => ShowSelected();
        _list.DoubleClick += (_, _) => PlaySelected();
        _name.TextChanged += (_, _) => RenameSelected();
        _fullscreen.CheckedChanged += (_, _) => SaveOptions();
        _shader.SelectedIndexChanged += (_, _) => SaveOptions();
        _speed.SelectedIndexChanged += (_, _) =>
        {
            UpdateOptionLocks();
            SaveOptions();
        };
        _customCycles.ValueChanged += (_, _) => SaveOptions();
        _sync.SelectedIndexChanged += (_, _) => SaveOptions();
        _keys.Click += (_, _) => EditKeys();
        _tips.SetToolTip(_keys, "遊戲進行中也可以按 Ctrl+F1。點要改的鍵，按 Add，按下新按鍵，再按 Save。");
        _tips.SetToolTip(_speed, "畫面太快選較慢；要固定 CPU 速度選「自訂 Cycles」。");
        _tips.SetToolTip(_customCycles, "DOSBox Staging 的 cycles 參數。常見 8000～15000；過高（例如 60000）可能讓仙劍等老遊戲戰鬥選單只顯示一部分。");
        _tips.SetToolTip(_cdBrowse, "需要光碟才能進行的遊戲：選 .cue / .iso，掛成 D:。有兩片碟的遊戲（如俠客英雄傳 3）請再選光碟 2。");
        _tips.SetToolTip(_cdBrowse2, "第二片映像與光碟 1 同一個 D: 光碟機；遊戲內用 Ctrl+F4 換片。");
        _play.Click += (_, _) => PlaySelected();
        _remove.Click += (_, _) => RemoveSelected();
        _openFolder.Click += (_, _) => OpenSelectedFolder();
        _changeExe.Click += (_, _) => ChangeExecutable();
        _cdBrowse.Click += (_, _) => PickCdImage(1);
        _cdClear.Click += (_, _) => ClearCdImage(1);
        _cdBrowse2.Click += (_, _) => PickCdImage(2);
        _cdClear2.Click += (_, _) => ClearCdImage(2);
        _search.TextChanged += (_, _) => FillList(SelectedId());
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        KeyDown += OnKeyDown;
        FormClosing += (_, _) => RememberWindow();
        Shown += (_, _) => EnsureDosbox();

        FillList(null);
        UpdateDosboxStatus();

        if (_library.RecoveredFromCorruptFile)
        {
            MessageBox.Show(this,
                "遊戲清單檔壞掉了，已另外備份，這次從空白清單開始。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = Theme.Panel,
            Padding = new Padding(24, 16, 24, 12),
        };

        var title = new Label
        {
            Text = "DOS 遊戲櫃",
            AutoSize = true,
            Location = new Point(24, 18),
            Font = Theme.FontOf(18f, bold: true),
            ForeColor = Theme.Text,
        };
        var subtitle = new Label
        {
            Text = "選一款遊戲就開始，不用打指令",
            AutoSize = true,
            Location = new Point(26, 56),
            Font = Theme.FontOf(9.5f),
            ForeColor = Theme.Muted,
        };

        _search.PlaceholderText = "搜尋遊戲";
        _search.Width = 200;
        _search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Theme.StyleInput(_search);

        var add = Theme.MakeButton("新增遊戲", primary: true, height: 36);
        add.Width = 120;
        add.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        add.Click += (_, _) => AddGames();

        var settings = Theme.MakeButton("設定", primary: false, height: 36);
        settings.Width = 80;
        settings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        settings.Click += (_, _) => EditSettings();

        header.Controls.AddRange([title, subtitle, _search, add, settings]);
        header.Resize += (_, _) =>
        {
            settings.Location = new Point(header.Width - settings.Width - 24, 30);
            add.Location = new Point(settings.Left - add.Width - 8, 30);
            _search.Location = new Point(add.Left - _search.Width - 12, 34);
        };
        header.PerformLayout();
        return header;
    }

    private Control BuildBody(out Button play, out Button remove, out Button openFolder, out Button changeExe)
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Bg,
            Padding = new Padding(20, 16, 20, 8),
        };

        var listFrame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Line,
            Padding = new Padding(1),
            Margin = new Padding(0, 0, 16, 0),
        };
        _list.Dock = DockStyle.Fill;
        _list.BorderStyle = BorderStyle.None;
        _list.BackColor = Theme.ListBg;
        _list.ForeColor = Theme.Text;
        _list.DrawMode = DrawMode.OwnerDrawFixed;
        _list.ItemHeight = 76;
        _list.IntegralHeight = false;
        _list.Font = Theme.FontOf(12f, bold: true);
        _list.DrawItem += DrawGameRow;
        listFrame.Controls.Add(_list);

        var side = new Panel
        {
            Dock = DockStyle.Right,
            Width = 390,
            BackColor = Theme.Panel,
            Padding = new Padding(18, 12, 18, 12),
        };

        _empty.Dock = DockStyle.Fill;
        _empty.AutoSize = false;
        _empty.Padding = new Padding(4, 8, 8, 8);
        _empty.ForeColor = Theme.Muted;
        _empty.Font = Theme.FontOf(11f);
        _empty.Text = "還沒有遊戲。\n\n按右上角「新增遊戲」，選遊戲資料夾裡的 EXE、BAT 或 COM。\n\n也可以把執行檔拖進這個視窗。\n\n中文遊戲若有自己的啟動批次檔，選那個 BAT，它通常會先載入中文系統。";

        Theme.StyleInput(_name);
        _name.Dock = DockStyle.Fill;

        _exePath.AutoSize = false;
        _exePath.Dock = DockStyle.Fill;
        _exePath.ForeColor = Theme.Muted;
        _exePath.Font = Theme.FontOf(9f);

        _missing.AutoSize = false;
        _missing.Dock = DockStyle.Fill;
        _missing.ForeColor = Theme.Danger;
        _missing.Font = Theme.FontOf(9f);
        _missing.Text = "找不到這個檔案";
        _missing.Visible = false;

        _lastPlayed.AutoSize = false;
        _lastPlayed.Dock = DockStyle.Fill;
        _lastPlayed.ForeColor = Theme.Muted;
        _lastPlayed.Font = Theme.FontOf(9f);

        _cdPath.AutoSize = false;
        _cdPath.Dock = DockStyle.Fill;
        _cdPath.ForeColor = Theme.Muted;
        _cdPath.Font = Theme.FontOf(9f);
        _cdPath.Text = "（未設定）";

        _cdMissing.AutoSize = false;
        _cdMissing.Dock = DockStyle.Fill;
        _cdMissing.ForeColor = Theme.Danger;
        _cdMissing.Font = Theme.FontOf(9f);
        _cdMissing.Text = "找不到光碟映像檔";
        _cdMissing.Visible = false;

        _cdBrowse.Width = 120;
        _cdClear.Width = 64;
        _cdActions.Dock = DockStyle.Fill;
        _cdActions.Height = 38;
        _cdActions.BackColor = Theme.Panel;
        var cdFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Panel,
        };
        cdFlow.Controls.AddRange([_cdBrowse, _cdClear]);
        _cdActions.Controls.Add(cdFlow);

        _cdPath2.AutoSize = false;
        _cdPath2.Dock = DockStyle.Fill;
        _cdPath2.ForeColor = Theme.Muted;
        _cdPath2.Font = Theme.FontOf(9f);
        _cdPath2.Text = "（未設定）";

        _cdMissing2.AutoSize = false;
        _cdMissing2.Dock = DockStyle.Fill;
        _cdMissing2.ForeColor = Theme.Danger;
        _cdMissing2.Font = Theme.FontOf(9f);
        _cdMissing2.Text = "找不到第二片光碟映像";
        _cdMissing2.Visible = false;

        _cdBrowse2.Width = 120;
        _cdClear2.Width = 64;
        _cdActions2.Dock = DockStyle.Fill;
        _cdActions2.Height = 38;
        _cdActions2.BackColor = Theme.Panel;
        var cdFlow2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Panel,
        };
        cdFlow2.Controls.AddRange([_cdBrowse2, _cdClear2]);
        _cdActions2.Controls.Add(cdFlow2);

        _fullscreen.Text = "全螢幕開啟";
        _fullscreen.Dock = DockStyle.Fill;
        _fullscreen.ForeColor = Theme.Text;
        _fullscreen.Font = Theme.FontOf(10.5f);

        StyleChoice(_shader);
        _shader.Items.AddRange(ShaderFilters.All);
        StyleChoice(_speed);
        _speed.Items.AddRange(["自動", "較慢（畫面跑太快時）", "最快", "自訂 Cycles…"]);
        _customCycles.Minimum = CpuCycles.MinCustom;
        _customCycles.Maximum = CpuCycles.MaxCustom;
        _customCycles.Increment = 1000;
        _customCycles.ThousandsSeparator = true;
        Theme.StyleNumeric(_customCycles);
        FillChoice(_sync, PictureOptions.SyncModes);

        var quitHint = new Label
        {
            Text = "遊戲中按 Ctrl+F1 可改按鍵，記得按 Save\n"
                + "遊戲要求換光碟：Ctrl+F4（等幾秒再操作）\n"
                + "離開遊戲：Ctrl+F9",
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = Theme.Muted,
            Font = Theme.FontOf(9f),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        (Control Control, int Height)[] rows =
        [
            (Caption("遊戲名稱"), 22),
            (_name, 34),
            (_exePath, 48),
            (_missing, 22),
            (_lastPlayed, 24),
            (Caption("光碟 1（掛載為 D:）"), 22),
            (_cdPath, 40),
            (_cdMissing, 20),
            (_cdActions, 38),
            (Caption("光碟 2（可選，換片）"), 22),
            (_cdPath2, 40),
            (_cdMissing2, 20),
            (_cdActions2, 38),
            (_fullscreen, 30),
            (Caption("濾鏡"), 22),
            (_shader, 32),
            (Caption("速度"), 22),
            (_speed, 32),
            (Caption("Cycles 數值"), 22),
            (_customCycles, 32),
            (Caption("畫面同步"), 22),
            (_sync, 32),
            (_keys, 40),
            (quitHint, 58),
        ];

        _editor.AutoSize = false;
        _editor.ColumnCount = 1;
        _editor.RowCount = rows.Length;
        _editor.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _editor.Margin = new Padding(0);
        _editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var editorHeight = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            _editor.RowStyles.Add(new RowStyle(SizeType.Absolute, rows[i].Height));
            _editor.Controls.Add(rows[i].Control, 0, i);
            editorHeight += rows[i].Height;
        }

        _keys.Dock = DockStyle.Fill;
        _editor.Dock = DockStyle.Top;
        _editor.Height = editorHeight;
        _optionsScroll.Dock = DockStyle.Fill;
        _optionsScroll.AutoScroll = true;
        _optionsScroll.BackColor = Theme.Panel;
        _optionsScroll.Visible = false;
        _optionsScroll.Controls.Add(_editor);
        _optionsScroll.Resize += (_, _) =>
        {
            _editor.Width = Math.Max(200, _optionsScroll.ClientSize.Width - 4);
        };

        play = Theme.MakeButton("開始遊戲", primary: true, height: 48);
        play.Dock = DockStyle.Bottom;
        play.Height = 48;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
        };
        changeExe = Theme.MakeButton("換執行檔", primary: false, height: 34);
        changeExe.Width = 96;
        openFolder = Theme.MakeButton("開資料夾", primary: false, height: 34);
        openFolder.Width = 96;
        remove = Theme.MakeButton("移除", primary: false, height: 34);
        remove.Width = 72;
        actions.Controls.AddRange([changeExe, openFolder, remove]);

        _gameTools.Dock = DockStyle.Bottom;
        _gameTools.Height = 100;
        _gameTools.Visible = false;
        _gameTools.BackColor = Theme.Panel;
        _gameTools.Controls.Add(play);
        _gameTools.Controls.Add(actions);

        side.Controls.Add(_optionsScroll);
        side.Controls.Add(_empty);
        side.Controls.Add(_gameTools);
        _editor.Width = 340;
        body.Controls.Add(listFrame);
        body.Controls.Add(side);
        return body;
    }

    private static void StyleChoice(ComboBox box)
    {
        box.Dock = DockStyle.Fill;
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Font = Theme.FontOf(10.5f);
        box.DropDownWidth = 320;
    }

    private static void FillChoice(ComboBox box, Choice[] choices)
    {
        StyleChoice(box);
        box.Items.AddRange(choices);
    }

    private static void SelectChoice(ComboBox box, string id)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is Choice choice && choice.Id == id)
            {
                box.SelectedIndex = i;
                return;
            }
        }

        if (box.Items.Count > 0)
            box.SelectedIndex = 0;
    }

    private static string ChoiceId(ComboBox box) =>
        box.SelectedItem is Choice choice ? choice.Id : "";

    private void UpdateOptionLocks()
    {
        _customCycles.Enabled = _speed.SelectedIndex == 3;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Theme.Panel,
            Padding = new Padding(24, 0, 24, 0),
        };
        _count.Dock = DockStyle.Left;
        _count.AutoSize = true;
        _count.TextAlign = ContentAlignment.MiddleLeft;
        _count.ForeColor = Theme.Muted;
        _count.Font = Theme.FontOf(9f);
        _count.Padding = new Padding(0, 8, 0, 0);

        _dosboxStatus.Dock = DockStyle.Right;
        _dosboxStatus.AutoSize = true;
        _dosboxStatus.TextAlign = ContentAlignment.MiddleRight;
        _dosboxStatus.Font = Theme.FontOf(9f);
        _dosboxStatus.Padding = new Padding(0, 8, 0, 0);

        footer.Controls.Add(_dosboxStatus);
        footer.Controls.Add(_count);
        return footer;
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AutoSize = false,
        ForeColor = Theme.Muted,
        Font = Theme.FontOf(9f),
        TextAlign = ContentAlignment.BottomLeft,
    };

    private void FillList(string? keepId)
    {
        var filter = _search.Text.Trim();
        var comparer = StringComparer.Create(new CultureInfo("zh-TW"), ignoreCase: true);
        var games = _library.Games
            .Where(game => filter.Length == 0
                || game.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || game.Executable.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(game => game.Name, comparer)
            .ToList();

        foreach (var game in games)
            game.FileMissing = !File.Exists(game.Executable);

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var game in games)
            _list.Items.Add(game);
        _list.EndUpdate();

        _count.Text = filter.Length == 0
            ? $"{_library.Games.Count} 款遊戲"
            : $"顯示 {games.Count} / {_library.Games.Count} 款";

        GameEntry? pick = null;
        if (keepId != null)
            pick = games.FirstOrDefault(game => game.Id == keepId);
        pick ??= games.FirstOrDefault();
        if (pick == null)
        {
            ShowSelected();
            return;
        }

        _list.SelectedItem = pick;
    }

    private void ShowSelected()
    {
        var game = Selected();
        var hasGame = game != null;
        _optionsScroll.Visible = hasGame;
        _gameTools.Visible = hasGame;
        _empty.Visible = !hasGame;
        if (game == null)
            return;

        _binding = true;
        _name.Text = game.Name;
        var direct = LaunchTarget.Resolve(game.Executable);
        _exePath.Text = direct.Equals(game.Executable, StringComparison.OrdinalIgnoreCase)
            ? game.Executable
            : game.Executable + Environment.NewLine + "開始時直接開 " + Path.GetFileName(direct) + "，略過設定畫面";
        game.FileMissing = !File.Exists(game.Executable);
        _missing.Visible = game.FileMissing;
        _lastPlayed.Text = game.LastPlayed is { } played
            ? "上次打開：" + played.ToString("yyyy/MM/dd HH:mm")
            : "還沒打開過";
        PictureOptions.Normalize(game);
        _fullscreen.Checked = game.Fullscreen;
        _shader.SelectedIndex = ShaderIndex(game.Shader);
        SelectChoice(_sync, game.SyncMode);
        UpdateOptionLocks();
        _speed.SelectedIndex = game.Speed switch
        {
            "slow" => 1,
            "max" => 2,
            "custom" => 3,
            _ => 0,
        };
        _customCycles.Value = CpuCycles.ClampCustom(game.CustomCycles);
        BindCdPath(_cdPath, _cdMissing, game.CdImagePath);
        BindCdPath(_cdPath2, _cdMissing2, game.CdImagePath2, optional: true);

        var cdOk = CdPathsOk(game);
        _play.Enabled = !game.FileMissing && cdOk;
        _keys.Text = File.Exists(MapperFiles.PathFor(game.Id)) ? "按鍵設定 · 已自訂" : "按鍵設定";
        _binding = false;
    }

    private void RenameSelected()
    {
        if (_binding)
            return;
        var game = Selected();
        if (game == null)
            return;

        var name = _name.Text.Trim();
        if (name.Length == 0 || name == game.Name)
            return;

        game.Name = name;
        LibraryStore.Save(_library);
        _list.Invalidate();
    }

    private void SaveOptions()
    {
        if (_binding)
            return;
        var game = Selected();
        if (game == null)
            return;

        game.Fullscreen = _fullscreen.Checked;
        if (_shader.SelectedItem is ShaderFilter filter)
            game.Shader = filter.Id;
        game.SyncMode = ChoiceId(_sync);
        PictureOptions.Normalize(game);
        game.Speed = _speed.SelectedIndex switch
        {
            1 => "slow",
            2 => "max",
            3 => "custom",
            _ => "auto",
        };
        game.CustomCycles = CpuCycles.ClampCustom((int)_customCycles.Value);
        LibraryStore.Save(_library);
    }

    private void AddGames()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "選擇遊戲執行檔",
            Filter = "DOS 遊戲 (*.exe;*.bat;*.com)|*.exe;*.bat;*.com|所有檔案|*.*",
            CheckFileExists = true,
            Multiselect = true,
        };
        if (Directory.Exists(_library.LastBrowseDirectory))
            dialog.InitialDirectory = _library.LastBrowseDirectory;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var directory = Path.GetDirectoryName(dialog.FileName);
        if (!string.IsNullOrEmpty(directory))
            _library.LastBrowseDirectory = directory;

        var added = AddExecutables(dialog.FileNames);
        FillList(added?.Id);
        if (added != null && _editor.Visible)
        {
            _name.Focus();
            _name.SelectAll();
        }
    }

    private GameEntry? AddExecutables(IEnumerable<string> paths)
    {
        GameEntry? firstNew = null;
        GameEntry? firstExisting = null;
        var added = 0;
        var skipped = 0;
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
                continue;

            var existing = _library.Games.FirstOrDefault(game =>
                string.Equals(game.Executable, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                skipped++;
                firstExisting ??= existing;
                continue;
            }

            var entry = new GameEntry
            {
                Name = LibraryStore.SuggestName(path),
                Executable = path,
                Fullscreen = true,
                Speed = "auto",
                Shader = ShaderFilters.DefaultId,
            };
            _library.Games.Add(entry);
            added++;
            firstNew ??= entry;
        }

        if (added > 0)
            LibraryStore.Save(_library);
        else if (skipped > 0)
            MessageBox.Show(this, "這些遊戲已經在清單裡。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

        return firstNew ?? firstExisting;
    }

    private static bool IsExecutable(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".com", StringComparison.OrdinalIgnoreCase);
    }

    private static void BindCdPath(Label pathLabel, Label missingLabel, string? value, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            pathLabel.Text = optional ? "（未設定）" : "（未設定，不需光碟可留空）";
            missingLabel.Visible = false;
            return;
        }

        pathLabel.Text = value;
        missingLabel.Visible = !File.Exists(value);
    }

    private static bool CdPathsOk(GameEntry game)
    {
        if (string.IsNullOrWhiteSpace(game.CdImagePath))
            return string.IsNullOrWhiteSpace(game.CdImagePath2);
        if (!File.Exists(game.CdImagePath))
            return false;
        if (string.IsNullOrWhiteSpace(game.CdImagePath2))
            return true;
        return File.Exists(game.CdImagePath2);
    }

    private void PickCdImage(int slot)
    {
        var game = Selected();
        if (game == null)
            return;

        using var dialog = new OpenFileDialog
        {
            Title = slot == 1 ? "選擇光碟映像 1" : "選擇光碟映像 2",
            Filter = "光碟映像 (*.cue;*.iso;*.bin;*.img;*.mds;*.mdf)|*.cue;*.iso;*.bin;*.img;*.mds;*.mdf|所有檔案|*.*",
            CheckFileExists = true,
        };
        var directory = Path.GetDirectoryName(game.Executable);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            dialog.InitialDirectory = directory;
        var current = slot == 1 ? game.CdImagePath : game.CdImagePath2;
        if (!string.IsNullOrWhiteSpace(current))
        {
            var cdDir = Path.GetDirectoryName(current);
            if (!string.IsNullOrEmpty(cdDir) && Directory.Exists(cdDir))
                dialog.InitialDirectory = cdDir;
            dialog.FileName = Path.GetFileName(current);
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (slot == 1)
            game.CdImagePath = dialog.FileName;
        else
            game.CdImagePath2 = dialog.FileName;
        CdMount.NormalizePath(game);
        LibraryStore.Save(_library);
        ShowSelected();
    }

    private void ClearCdImage(int slot)
    {
        var game = Selected();
        if (game == null)
            return;

        if (slot == 1)
        {
            if (string.IsNullOrWhiteSpace(game.CdImagePath))
                return;
            game.CdImagePath = "";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(game.CdImagePath2))
                return;
            game.CdImagePath2 = "";
        }

        LibraryStore.Save(_library);
        ShowSelected();
    }

    private void ChangeExecutable()
    {
        var game = Selected();
        if (game == null)
            return;

        using var dialog = new OpenFileDialog
        {
            Title = "更換執行檔",
            Filter = "DOS 遊戲 (*.exe;*.bat;*.com)|*.exe;*.bat;*.com|所有檔案|*.*",
            CheckFileExists = true,
        };
        var directory = Path.GetDirectoryName(game.Executable);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            dialog.InitialDirectory = directory;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        game.Executable = dialog.FileName;
        LibraryStore.Save(_library);
        ShowSelected();
        _list.Invalidate();
    }

    private void PlaySelected()
    {
        var game = Selected();
        if (game == null)
            return;

        try
        {
            var process = GameRunner.Launch(_library, game);
            if (!game.Fullscreen)
                WindowAspectLock.Follow(process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        game.LastPlayed = DateTime.Now;
        LibraryStore.Save(_library);
        ShowSelected();
    }

    private void RemoveSelected()
    {
        var game = Selected();
        if (game == null)
            return;

        var answer = MessageBox.Show(this,
            $"要把「{game.Name}」從清單移除嗎？\n遊戲檔案不會被刪除。",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _library.Games.Remove(game);
        MapperFiles.Delete(game.Id);
        LibraryStore.Save(_library);
        FillList(null);
    }

    private void EditKeys()
    {
        var game = Selected();
        if (game == null)
            return;

        Process process;
        try
        {
            process = GameRunner.OpenMapper(_library, game);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _keys.Enabled = false;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            if (IsDisposed)
                return;
            BeginInvoke(() =>
            {
                _keys.Enabled = Selected() != null;
                ShowSelected();
            });
        };
    }

    private void OpenSelectedFolder()
    {
        var game = Selected();
        if (game == null)
            return;

        var directory = Path.GetDirectoryName(game.Executable);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            MessageBox.Show(this, "找不到遊戲資料夾。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (File.Exists(game.Executable))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + game.Executable + "\"",
                UseShellExecute = true,
            });
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(directory) { UseShellExecute = true });
    }

    private void EnsureDosbox()
    {
        if (File.Exists(_library.DosboxPath))
            return;

        var found = LibraryStore.DetectDosbox();
        if (File.Exists(found))
        {
            _library.DosboxPath = found;
            LibraryStore.Save(_library);
            UpdateDosboxStatus();
            return;
        }

        MessageBox.Show(this,
            "這台電腦還沒找到 DOSBox Staging。\n請選擇這台電腦上的 dosbox.exe。每個人的安裝位置可以不一樣。",
            Text,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        EditSettings();
    }

    private void EditSettings()
    {
        using var dialog = new SettingsForm(_library);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _library.DosboxPath = dialog.DosboxPath;
        _library.GlobalCpuCycles = dialog.GlobalCpuCycles;
        LibraryStore.Save(_library);
        UpdateDosboxStatus();
    }

    private void UpdateDosboxStatus()
    {
        if (File.Exists(_library.DosboxPath))
        {
            _dosboxStatus.Text = "DOSBox Staging 已就緒";
            _dosboxStatus.ForeColor = Theme.Ok;
        }
        else
        {
            _dosboxStatus.Text = "找不到 DOSBox，請按設定";
            _dosboxStatus.ForeColor = Theme.Danger;
        }
    }

    private void RememberWindow()
    {
        if (WindowState == FormWindowState.Normal)
        {
            _library.WindowWidth = Width;
            _library.WindowHeight = Height;
            LibraryStore.Save(_library);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && ActiveControl != _name && ActiveControl != _search)
        {
            RemoveSelected();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter && ActiveControl == _list)
        {
            PlaySelected();
            e.Handled = true;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
            return;

        var resolved = ResolveDropped(paths).ToArray();
        if (resolved.Length == 0)
            return;

        var added = AddExecutables(resolved);
        FillList(added?.Id);
    }

    private IEnumerable<string> ResolveDropped(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path)
                    .Where(IsExecutable)
                    .OrderBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (files.Length == 0)
                {
                    MessageBox.Show(this, "這個資料夾裡沒有 EXE、BAT 或 COM。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                if (files.Length == 1)
                {
                    yield return files[0];
                    continue;
                }

                var picked = PickExecutable(files);
                if (picked != null)
                    yield return picked;
                continue;
            }

            if (File.Exists(path) && IsExecutable(path))
                yield return path;
        }
    }

    private string? PickExecutable(string[] files)
    {
        using var dialog = new Form
        {
            Text = "這個資料夾有好幾個執行檔",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(460, 340),
            BackColor = Theme.Bg,
            ForeColor = Theme.Text,
            Font = Theme.FontOf(10.5f),
        };
        var hint = new Label
        {
            Text = "選遊戲真正的啟動檔。中文遊戲多半是 BAT。",
            Location = new Point(16, 16),
            AutoSize = true,
            ForeColor = Theme.Muted,
        };
        var list = new ListBox
        {
            Location = new Point(16, 48),
            Size = new Size(428, 220),
            BackColor = Theme.ListBg,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Font = Theme.FontOf(11f),
        };
        foreach (var file in files)
            list.Items.Add(new ExecutableChoice(file));
        var preferred = files.FirstOrDefault(file => file.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)) ?? files[0];
        list.SelectedIndex = Array.FindIndex(files, file => string.Equals(file, preferred, StringComparison.OrdinalIgnoreCase));

        var cancel = Theme.MakeButton("取消", primary: false, height: 36);
        cancel.Location = new Point(236, 284);
        cancel.Width = 96;
        cancel.DialogResult = DialogResult.Cancel;
        var ok = Theme.MakeButton("加入", primary: true, height: 36);
        ok.Location = new Point(344, 284);
        ok.Width = 100;
        ok.DialogResult = DialogResult.OK;

        dialog.Controls.AddRange([hint, list, cancel, ok]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        list.DoubleClick += (_, _) =>
        {
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || list.SelectedItem is not ExecutableChoice choice)
            return null;
        return choice.Path;
    }

    private void DrawGameRow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _list.Items[e.Index] is not GameEntry game)
            return;

        var selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? Theme.Selected : Theme.ListBg);
        e.Graphics.FillRectangle(background, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top, 4, e.Bounds.Height);
        }

        var nameColor = game.FileMissing ? Theme.Danger : Theme.Text;
        var nameBounds = new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 12, e.Bounds.Width - 28, 28);
        var pathBounds = new Rectangle(e.Bounds.X + 16, e.Bounds.Y + 40, e.Bounds.Width - 28, 22);
        TextRenderer.DrawText(e.Graphics, game.Name, RowTitleFont, nameBounds, nameColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, game.Executable, RowPathFont, pathBounds, Theme.Muted,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static int ShaderIndex(string shaderId)
    {
        for (var i = 0; i < ShaderFilters.All.Length; i++)
        {
            if (ShaderFilters.All[i].Id == shaderId)
                return i;
        }
        return 0;
    }

    private GameEntry? Selected() => _list.SelectedItem as GameEntry;

    private string? SelectedId() => Selected()?.Id;
}

internal sealed class ExecutableChoice(string path)
{
    public string Path { get; } = path;
    public override string ToString() => System.IO.Path.GetFileName(Path);
}
