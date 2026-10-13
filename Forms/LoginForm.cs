using System;
using System.Drawing;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;

namespace KawaSysWF.Forms
{
    public class LoginForm : Form
    {
        public Usuario? LoggedUser { get; private set; }

        private TextBox txtUser = null!, txtPass = null!;
        private Label lblError = null!;

        public LoginForm()
        {
            Text = "KawaSys — Iniciar Sesión";
            Size = new Size(420, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = AppTheme.Primary;
            Font = AppTheme.FontNormal;
            if (AppTheme.AppIcon != null) Icon = AppTheme.AppIcon;
            Build();
        }

        private void Build()
        {
            // Card central
            var card = new Panel
            {
                Size = new Size(340, 430),
                BackColor = AppTheme.White,
                Left = (ClientSize.Width - 340) / 2,
                Top = 60,
                Padding = new Padding(30)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 230)), 0, 0, card.Width - 1, card.Height - 1);
            };

            // Logo
            try
            {
                var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    var pb = new PictureBox { Width = 100, Height = 80, Left = 120, Top = 20, SizeMode = PictureBoxSizeMode.Zoom };
                    pb.Image = Image.FromFile(logoPath);
                    card.Controls.Add(pb);
                }
            }
            catch { }

            int y = 110;
            var lblTitle = new Label
            {
                Text = "KawaSys", Font = AppTheme.FontTitle, ForeColor = AppTheme.Primary,
                Left = 0, Top = y, Width = 340, TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblTitle);

            y += 32;
            var lblSub = new Label
            {
                Text = "KAWA S.R.L. — Sistema de Gestión",
                Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
                Left = 0, Top = y, Width = 340, TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblSub);

            y += 30;
            lblError = new Label
            {
                Text = "", ForeColor = Color.Crimson, Font = AppTheme.FontSmall,
                Left = 20, Top = y, Width = 300, Height = 18, TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblError);

            y += 22;
            card.Controls.Add(new Label { Text = "Usuario", Left = 20, Top = y, Font = AppTheme.FontBold, ForeColor = AppTheme.TextDark, AutoSize = true });
            y += 22;
            txtUser = new TextBox { Left = 20, Top = y, Width = 300, Height = 28, Font = AppTheme.FontNormal, BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(txtUser);

            y += 38;
            card.Controls.Add(new Label { Text = "Contraseña", Left = 20, Top = y, Font = AppTheme.FontBold, ForeColor = AppTheme.TextDark, AutoSize = true });
            y += 22;
            txtPass = new TextBox { Left = 20, Top = y, Width = 268, Height = 28, Font = AppTheme.FontNormal, PasswordChar = '●', BorderStyle = BorderStyle.FixedSingle };
            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };
            card.Controls.Add(txtPass);

            var btnEye = new Button
            {
                Text = "👁", Left = 290, Top = y, Width = 30, Height = 28,
                BackColor = Color.FromArgb(245, 245, 248), ForeColor = AppTheme.TextDark,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand,
                TabStop = false
            };
            btnEye.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 210);
            btnEye.Click += (s, e) =>
            {
                if (txtPass.PasswordChar == '●') { txtPass.PasswordChar = '\0'; btnEye.Text = "🙈"; }
                else { txtPass.PasswordChar = '●'; btnEye.Text = "👁"; }
                txtPass.Focus();
            };
            card.Controls.Add(btnEye);

            y += 44;
            var btnLogin = new Button
            {
                Text = "Iniciar Sesión", Left = 20, Top = y, Width = 300, Height = 40,
                BackColor = AppTheme.Accent, ForeColor = AppTheme.White,
                FlatStyle = FlatStyle.Flat, Font = AppTheme.FontBold, Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => DoLogin();
            card.Controls.Add(btnLogin);

            y += 52;
            var hint = new Label
            {
                Text = "admin/admin123 · ventas/ventas123 · compras/compras123 · finanzas/finanzas123",
                Font = new Font("Segoe UI", 7.5f), ForeColor = AppTheme.TextMuted,
                Left = 10, Top = y, Width = 320, Height = 30, TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(hint);

            Controls.Add(card);
        }

        private void DoLogin()
        {
            lblError.Text = "";
            var user = Repository.Login(txtUser.Text.Trim(), txtPass.Text);
            if (user == null)
            {
                lblError.Text = "Usuario o contraseña incorrectos";
                txtPass.Clear();
                return;
            }
            LoggedUser = user;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
