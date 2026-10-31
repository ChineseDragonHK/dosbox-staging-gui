namespace DosGameLauncher;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _pathBox;
    private readonly Label _status;
    private readonly ComboBox _globalCycles;
    private readonly NumericUpDown _globalCyclesValue;

    public string DosboxPath => _pathBox.Text.Trim();

    public string GlobalCpuCycles
    {
        get
        {
            return _globalCycles.SelectedIndex switch
            {
                1 => "auto",
                2 => "3000",
                3 => "max",
                4 => ((int)_globalCyclesValue.Value).ToString(),
                _ => "",
            };
        }
    }

    public SettingsForm(GameLibrary library)
    {
        Text = "DOSBox 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 300);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.FontOf(10.5f);

        var title = new Label
        {
            Text = "DOSBox Staging 的位置",
            AutoSize = true,
            Location = new Point(24, 20),
            Font = Theme.FontOf(12f, bold: true),
            ForeColor = Theme.Text,
        };

        _pathBox = new TextBox
        {
            Location = new Point(24, 56),
            Width = 400,
            Text = library.DosboxPath,
        };
        Theme.StyleInput(_pathBox);

        var browse = Theme.MakeButton("瀏覽", primary: false, height: 32);
        browse.Location = new Point(436, 52);
        browse.Width = 100;
        browse.Click += (_, _) => Browse();

        _status = new Label
        {
            AutoSize = false,
            Location = new Point(24, 96),
            Size = new Size(512, 24),
            Font = Theme.FontOf(9.5f),
        };

        var cyclesTitle = new Label
        {
            Text = "CPU Cycles（全遊戲預設）",
            AutoSize = true,
            Location = new Point(24, 132),
            Font = Theme.FontOf(11f, bold: true),
        };

        var cyclesHint = new Label
        {
            Text = "各遊戲選「自動」速度時使用；單款遊戲可再覆寫。",
            AutoSize = true,
            Location = new Point(24, 156),
            Font = Theme.FontOf(9f),
            ForeColor = Theme.Muted,
        };

        _globalCycles = new ComboBox
        {
            Location = new Point(24, 182),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        Theme.StyleChoice(_globalCycles);
        _globalCycles.Items.AddRange(
        [
            "不指定（交給 DOSBox / 各遊戲設定）",
            "DOSBox 自動（auto）",
            "較慢（3000）",
            "最快（max）",
            "自訂數值…",
        ]);
        _globalCycles.SelectedIndexChanged += (_, _) => RefreshCyclesValueEnabled();

        _globalCyclesValue = new NumericUpDown
        {
            Location = new Point(320, 182),
            Width = 216,
            Minimum = CpuCycles.MinCustom,
            Maximum = CpuCycles.MaxCustom,
            Increment = 1000,
            ThousandsSeparator = true,
        };
        Theme.StyleNumeric(_globalCyclesValue);

        SelectGlobalCycles(library.GlobalCpuCycles);

        var cancel = Theme.MakeButton("取消", primary: false);
        cancel.Location = new Point(328, 238);
        cancel.Width = 100;
        cancel.DialogResult = DialogResult.Cancel;

        var save = Theme.MakeButton("儲存", primary: true);
        save.Location = new Point(436, 238);
        save.Width = 100;
        save.Click += (_, _) => TrySave();

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange(
        [
            title, _pathBox, browse, _status, cyclesTitle, cyclesHint, _globalCycles, _globalCyclesValue, cancel, save,
        ]);
        _pathBox.TextChanged += (_, _) => RefreshStatus();
        RefreshStatus();
        RefreshCyclesValueEnabled();
    }

    void SelectGlobalCycles(string global)
    {
        var norm = CpuCycles.NormalizeGlobal(global);
        if (norm.Length == 0)
            _globalCycles.SelectedIndex = 0;
        else if (norm.Equals("auto", StringComparison.OrdinalIgnoreCase))
            _globalCycles.SelectedIndex = 1;
        else if (norm == "3000")
            _globalCycles.SelectedIndex = 2;
        else if (norm.Equals("max", StringComparison.OrdinalIgnoreCase))
            _globalCycles.SelectedIndex = 3;
        else if (int.TryParse(norm, out var n))
        {
            _globalCycles.SelectedIndex = 4;
            _globalCyclesValue.Value = CpuCycles.ClampCustom(n);
        }
        else
            _globalCycles.SelectedIndex = 0;
    }

    void RefreshCyclesValueEnabled() =>
        _globalCyclesValue.Enabled = _globalCycles.SelectedIndex == 4;

    private void Browse()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "選擇 dosbox.exe",
            Filter = "dosbox.exe|dosbox.exe|執行檔|*.exe",
            CheckFileExists = true,
            FileName = "dosbox.exe",
        };
        var directory = Path.GetDirectoryName(_pathBox.Text);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            dialog.InitialDirectory = directory;

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _pathBox.Text = dialog.FileName;
    }

    private void TrySave()
    {
        if (!File.Exists(DosboxPath))
        {
            MessageBox.Show(this, "這個路徑沒有 dosbox.exe。", "設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void RefreshStatus()
    {
        if (File.Exists(_pathBox.Text.Trim()))
        {
            _status.Text = "已找到 DOSBox Staging。";
            _status.ForeColor = Theme.Ok;
        }
        else
        {
            _status.Text = "這個路徑目前找不到檔案。";
            _status.ForeColor = Theme.Danger;
        }
    }
}
