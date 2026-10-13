using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace KawaSysWF.UI
{
    public static class AppTheme
    {
        private static Icon? _appIcon;
        public static Icon? AppIcon
        {
            get
            {
                if (_appIcon == null)
                {
                    try
                    {
                        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kawasys.ico");
                        if (File.Exists(path)) _appIcon = new Icon(path);
                    }
                    catch { }
                }
                return _appIcon;
            }
        }

        public static Color Primary    = Color.FromArgb(26, 26, 46);
        public static Color Accent     = Color.FromArgb(233, 69, 96);
        public static Color LightBg    = Color.FromArgb(240, 242, 245);
        public static Color White      = Color.White;
        public static Color TextDark   = Color.FromArgb(30, 30, 30);
        public static Color TextMuted  = Color.FromArgb(100, 100, 120);
        public static Color Success    = Color.FromArgb(34, 197, 94);
        public static Color Warning    = Color.FromArgb(249, 115, 22);
        public static Color Info       = Color.FromArgb(59, 130, 246);

        public static Font FontTitle   = new Font("Segoe UI", 13, FontStyle.Bold);
        public static Font FontBold    = new Font("Segoe UI", 10, FontStyle.Bold);
        public static Font FontNormal  = new Font("Segoe UI", 9);
        public static Font FontSmall   = new Font("Segoe UI", 8);

        public static Button MakeButton(string text, Color? back = null, int w = 130, int h = 36)
        {
            return new Button
            {
                Text = text,
                Width = w, Height = h,
                BackColor = back ?? Accent,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font = FontBold,
                Cursor = Cursors.Hand
            };
        }

        public static Label MakeLabel(string text, bool bold = false, Color? fg = null)
        {
            return new Label
            {
                Text = text,
                Font = bold ? FontBold : FontNormal,
                ForeColor = fg ?? TextDark,
                AutoSize = true
            };
        }

        public static TextBox MakeTextBox(int w = 220, bool multiline = false, int h = 28)
        {
            return new TextBox
            {
                Width = w, Height = multiline ? 70 : h,
                Font = FontNormal,
                Multiline = multiline,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        public static ComboBox MakeCombo(int w = 220)
        {
            return new ComboBox
            {
                Width = w,
                Font = FontNormal,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        public static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = FontNormal,
                GridColor = Color.FromArgb(220, 220, 230),
                Dock = DockStyle.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            g.ColumnHeadersDefaultCellStyle.ForeColor = White;
            g.ColumnHeadersDefaultCellStyle.Font = FontBold;
            g.ColumnHeadersHeight = 34;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            g.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        public static Panel MakeCard(string title, int w = 0, int h = 0)
        {
            var p = new Panel
            {
                BackColor = White,
                Padding = new Padding(0),
            };
            if (w > 0) p.Width = w;
            if (h > 0) p.Height = h;

            var header = new Panel
            {
                Height = 40, Dock = DockStyle.Top,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12, 0, 0, 0)
            };
            var lbl = new Label
            {
                Text = title, AutoSize = false, Dock = DockStyle.Fill,
                Font = FontBold, ForeColor = Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lbl);
            p.Controls.Add(header);
            return p;
        }

        public static Panel MakeStat(string label, string value, Color iconColor)
        {
            var p = new Panel { Width = 160, Height = 80, BackColor = White, Cursor = Cursors.Hand };
            p.Paint += (s, e) => {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220,220,230)), 0, 0, p.Width-1, p.Height-1);
            };
            var icon = new Panel { Width = 46, Height = 46, BackColor = Color.FromArgb(30, iconColor.R, iconColor.G, iconColor.B), Left = 12, Top = 17 };
            icon.Paint += (s, e) => {
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(40, iconColor)), 0, 0, 46, 46);
            };
            var lv = new Label { Text = value, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = iconColor, Left = 68, Top = 12, AutoSize = true };
            var ll = new Label { Text = label, Font = FontSmall, ForeColor = TextMuted, Left = 68, Top = 46, AutoSize = true };
            p.Controls.AddRange(new Control[] { icon, lv, ll });
            return p;
        }
    }
}
