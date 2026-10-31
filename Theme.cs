namespace DosGameLauncher;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(32, 30, 28);
    public static readonly Color Panel = Color.FromArgb(43, 39, 35);
    public static readonly Color ListBg = Color.FromArgb(28, 26, 24);
    public static readonly Color Selected = Color.FromArgb(68, 54, 38);
    public static readonly Color Card = Color.FromArgb(58, 52, 46);
    public static readonly Color Line = Color.FromArgb(96, 82, 66);
    public static readonly Color Text = Color.FromArgb(245, 240, 230);
    public static readonly Color Muted = Color.FromArgb(176, 164, 148);
    public static readonly Color Accent = Color.FromArgb(224, 160, 64);
    public static readonly Color AccentHover = Color.FromArgb(236, 184, 96);
    public static readonly Color AccentPressed = Color.FromArgb(196, 136, 48);
    public static readonly Color AccentText = Color.FromArgb(36, 26, 12);
    public static readonly Color Danger = Color.FromArgb(214, 120, 96);
    public static readonly Color Ok = Color.FromArgb(138, 186, 122);

    public static Font FontOf(float size, bool bold = false) =>
        new("Microsoft JhengHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);

    public static Button MakeButton(string text, bool primary, int height = 40)
    {
        var button = new Button
        {
            Text = text,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = FontOf(primary ? 12f : 10.5f, primary),
            BackColor = primary ? Accent : Card,
            ForeColor = primary ? AccentText : Text,
            Margin = new Padding(0),
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Line;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(72, 64, 56);
        button.FlatAppearance.MouseDownBackColor = primary ? AccentPressed : Color.FromArgb(48, 42, 36);
        return button;
    }

    public static void StyleInput(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = ListBg;
        box.ForeColor = Text;
        box.Font = FontOf(11f);
    }

    public static void StyleChoice(ComboBox box)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.Font = FontOf(10.5f);
        box.BackColor = ListBg;
        box.ForeColor = Text;
        box.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleNumeric(NumericUpDown box)
    {
        box.BackColor = ListBg;
        box.ForeColor = Text;
        box.Font = FontOf(11f);
        box.BorderStyle = BorderStyle.FixedSingle;
    }
}
