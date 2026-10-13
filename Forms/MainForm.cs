using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KawaSysWF.Database;
using KawaSysWF.UI;
using KawaSysWF.Forms.Ventas;
using KawaSysWF.Forms.Compras;
using KawaSysWF.Forms.Proveedores;
using KawaSysWF.Forms.Finanzas;
using KawaSysWF.Forms.Sistema;
using KawaSysWF.Forms.Facturacion;

namespace KawaSysWF.Forms
{
    public class MainForm : Form
    {
        public Usuario CurrentUser { get; }
        private Panel sidebar = null!, contentArea = null!;
        private Label lblPageTitle = null!;
        private Button? _activeNav;

        private static readonly Color NavIdleBack = AppTheme.Primary;
        private static readonly Color NavIdleFore = Color.FromArgb(200, 200, 220);
        private static readonly Color NavHoverBack = Color.FromArgb(40, 40, 70);
        private static readonly Color NavActiveBack = Color.FromArgb(55, 60, 95);

        [DllImport("user32.dll")]
        private static extern int ShowScrollBar(IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
        private const int SB_BOTH = 3;
        private static void HideScrollBars(Control c)
        {
            if (c.IsHandleCreated) ShowScrollBar(c.Handle, SB_BOTH, false);
        }

        public MainForm(Usuario user)
        {
            CurrentUser = user;
            Text = $"KawaSys — {user.NombreCompleto} [{user.Rol.ToUpper()}]";
            Size = new Size(1280, 780);
            MinimumSize = new Size(1024, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.LightBg;
            Font = AppTheme.FontNormal;
            if (AppTheme.AppIcon != null) Icon = AppTheme.AppIcon;
            Build();
        }

        private void Build()
        {
            sidebar = new Panel
            {
                Width = 220, Dock = DockStyle.Left,
                BackColor = AppTheme.Primary
            };
            BuildSidebar();

            var mainArea = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg };

            // Topbar
            var topbar = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = AppTheme.White, Padding = new Padding(16, 0, 16, 0) };
            topbar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 220, 230)), 0, 49, topbar.Width, 49);

            lblPageTitle = new Label
            {
                Text = "Dashboard", Font = AppTheme.FontBold, ForeColor = AppTheme.Primary,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            topbar.Controls.Add(lblPageTitle);

            var btnSalir = new Button
            {
                Text = "⏻  Salir", Width = 90, Height = 30, Left = 1100, Top = 10,
                BackColor = Color.FromArgb(254, 226, 226), ForeColor = Color.Crimson,
                FlatStyle = FlatStyle.Flat, Font = AppTheme.FontSmall, Cursor = Cursors.Hand, Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.Click += (s, e) => { Application.Restart(); };
            topbar.Controls.Add(btnSalir);

            contentArea = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.LightBg, Padding = new Padding(20) };

            mainArea.Controls.Add(contentArea);
            mainArea.Controls.Add(topbar);
            Controls.Add(mainArea);
            Controls.Add(sidebar);

            ShowDashboard();
        }

        private void BuildSidebar()
        {
            sidebar.Controls.Clear();
            int y = 0;

            // Brand (clickeable → Dashboard)
            var brand = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = Color.FromArgb(15, 15, 35), Padding = new Padding(10), Cursor = Cursors.Hand };
            EventHandler irAlDashboard = (s, e) => ShowDashboard();
            brand.Click += irAlDashboard;
            var tipBrand = new ToolTip();
            tipBrand.SetToolTip(brand, "Ir al Dashboard");
            try
            {
                var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    var pb = new PictureBox { Width = 60, Height = 55, Left = 10, Top = 12, SizeMode = PictureBoxSizeMode.Zoom, Cursor = Cursors.Hand };
                    pb.Image = Image.FromFile(logoPath);
                    pb.Click += irAlDashboard;
                    tipBrand.SetToolTip(pb, "Ir al Dashboard");
                    brand.Controls.Add(pb);
                }
            }
            catch { }
            var lblBrand = new Label
            {
                Text = "KawaSys", Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White, Left = 75, Top = 15, AutoSize = true, Cursor = Cursors.Hand
            };
            var lblSub = new Label
            {
                Text = "GESTIÓN", Font = new Font("Segoe UI", 7, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 180), Left = 78, Top = 40, AutoSize = true, Cursor = Cursors.Hand
            };
            lblBrand.Click += irAlDashboard;
            lblSub.Click += irAlDashboard;
            tipBrand.SetToolTip(lblBrand, "Ir al Dashboard");
            brand.Controls.AddRange(new Control[] { lblBrand, lblSub });
            sidebar.Controls.Add(brand);

            // Scroll panel for nav items (con scroll por rueda, sin barras visibles)
            var navScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            navScroll.Layout += (s, e) => HideScrollBars(navScroll);
            navScroll.Scroll += (s, e) => HideScrollBars(navScroll);
            navScroll.ControlAdded += (s, e) => HideScrollBars(navScroll);
            navScroll.MouseEnter += (s, e) => HideScrollBars(navScroll);

            int ny = 10;
            void AddSection(string title)
            {
                ny += 10;
                var lbl = new Label
                {
                    Text = title.ToUpper(), Left = 16, Top = ny, AutoSize = true,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(100, 100, 140)
                };
                navScroll.Controls.Add(lbl);
                ny += 26;
            }

            void AddNavItem(string icon, string text, Action onClick, bool allowed = true)
            {
                if (!allowed) return;
                var btn = new Button
                {
                    Text = $"  {icon}  {text}", Left = 8, Top = ny, Width = 190, Height = 38,
                    BackColor = NavIdleBack, ForeColor = NavIdleFore,
                    FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                    Font = AppTheme.FontSmall, Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                // barra de acento a la izquierda cuando está activo
                btn.Paint += (s, e) =>
                {
                    if (_activeNav == btn)
                        e.Graphics.FillRectangle(new SolidBrush(AppTheme.Accent), 0, 0, 4, btn.Height);
                };
                btn.MouseEnter += (s, e) => { if (_activeNav != btn) { btn.BackColor = NavHoverBack; btn.ForeColor = Color.White; } };
                btn.MouseLeave += (s, e) => { if (_activeNav != btn) { btn.BackColor = NavIdleBack; btn.ForeColor = NavIdleFore; } };
                btn.Click += (s, e) =>
                {
                    SetActiveNav(btn);
                    onClick();
                    lblPageTitle.Text = text;
                };
                navScroll.Controls.Add(btn);
                ny += 46;
            }

            bool isVentas  = CurrentUser.Rol == "ventas"  || CurrentUser.Rol == "sistema";
            bool isCompras = CurrentUser.Rol == "compras" || CurrentUser.Rol == "sistema";
            bool isFinanzas= CurrentUser.Rol == "finanzas"|| CurrentUser.Rol == "sistema";
            bool isSistema = CurrentUser.Rol == "sistema";

            AddNavItem("🏠", "Dashboard", ShowDashboard);

            if (isVentas)
            {
                AddSection("VENTAS");
                AddNavItem("🛒", "Registrar Venta", () => ShowForm(new RegistrarVentaForm(CurrentUser)));
                AddNavItem("📋", "Consultar Ventas", () => ShowForm(new ConsultarVentasForm(CurrentUser)));
                AddNavItem("⚠", "Modelos Faltantes", () => ShowForm(new FaltantesForm(CurrentUser)));
                AddNavItem("📝", "Pedidos de Clientes", () => ShowForm(new PedidosClienteForm(CurrentUser)));
                AddNavItem("📤", "Envíos a Central", () => ShowForm(new EnviosForm(CurrentUser)));
            }

            if (isCompras)
            {
                AddSection("COMPRAS");
                AddNavItem("🛍", "Solicitudes Compra", () => ShowForm(new SolicitudesCompraForm(CurrentUser)));
                AddNavItem("📄", "Cotizaciones", () => ShowForm(new CotizacionesForm(CurrentUser)));
                AddNavItem("🧾", "Cot. Recibidas", () => ShowForm(new CotizacionesRecibidasForm(CurrentUser)));
                AddNavItem("📦", "Órdenes de Compra", () => ShowForm(new OrdenesCompraForm(CurrentUser)));
                AddNavItem("🚚", "Recepción", () => ShowForm(new RecepcionForm(CurrentUser)));
            }

            if (isCompras)
            {
                AddSection("PROVEEDORES");
                AddNavItem("🏭", "Lista de Proveedores", () => ShowForm(new ProveedoresForm(CurrentUser)));
                AddNavItem("🔍", "Buscar Proveedor", () => ShowForm(new BuscarProveedorForm(CurrentUser)));
            }

            if (isFinanzas)
            {
                AddSection("FINANZAS");
                AddNavItem("✅", "Aprobaciones", () => ShowForm(new AprobacionesForm(CurrentUser)));
                AddNavItem("📊", "Historial", () => ShowForm(new HistorialOperacionesForm(CurrentUser)));
                AddNavItem("📈", "Reporte Mensual", () => ShowForm(new ReporteMensualForm(CurrentUser)));
            }

            if (isVentas || isFinanzas)
            {
                AddSection("FACTURACIÓN Y COBROS");
                AddNavItem("🧾", "Facturas", () => ShowForm(new FacturasForm(CurrentUser)));
                AddNavItem("💵", "Cobros y Pendientes", () => ShowForm(new CobrosForm(CurrentUser)), isFinanzas);
                AddNavItem("🗓", "Facturación Mensual", () => ShowForm(new FacturacionMensualForm(CurrentUser)), isFinanzas);
            }

            AddSection("CLIENTES");
            AddNavItem("👤", "Clientes Mayoristas", () => ShowForm(new ClientesForm(CurrentUser)));

            if (isSistema)
            {
                AddSection("ADMINISTRACIÓN");
                AddNavItem("👥", "Usuarios", () => ShowForm(new UsuariosForm(CurrentUser)));
                AddNavItem("📍", "Sucursales", () => ShowForm(new SucursalesForm(CurrentUser)));
            }

            // User info at bottom
            var userPanel = new Panel
            {
                Height = 52, Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(15, 15, 35), Padding = new Padding(12, 8, 8, 8)
            };
            var avatar = new Label
            {
                Text = $"{CurrentUser.Nombre[0]}{CurrentUser.Apellido[0]}",
                Width = 34, Height = 34, Left = 12, Top = 9,
                BackColor = AppTheme.Accent, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblUname = new Label { Text = CurrentUser.NombreCompleto, Left = 52, Top = 10, AutoSize = true, ForeColor = Color.White, Font = AppTheme.FontSmall };
            var lblUrole = new Label { Text = CurrentUser.Rol.ToUpper(), Left = 52, Top = 28, AutoSize = true, ForeColor = Color.FromArgb(150, 150, 180), Font = new Font("Segoe UI", 7) };

            // Botón cerrar sesión / cambiar usuario
            var menu = new ContextMenuStrip();
            menu.Items.Add("🔄  Cambiar de usuario", null, (s, e) => CambiarUsuario());
            menu.Items.Add("⏻  Cerrar sesión y salir", null, (s, e) => CerrarSesion());
            var btnLogout = new Button
            {
                Text = "⏻", Width = 32, Height = 32, Left = 178, Top = 10,
                BackColor = Color.FromArgb(30, 30, 55), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            var tip = new ToolTip();
            tip.SetToolTip(btnLogout, "Cerrar sesión / Cambiar de usuario");
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = AppTheme.Accent;
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.FromArgb(30, 30, 55);
            btnLogout.Click += (s, e) => menu.Show(btnLogout, new Point(0, btnLogout.Height));

            userPanel.Controls.AddRange(new Control[] { avatar, lblUname, lblUrole, btnLogout });

            sidebar.Controls.Add(navScroll);
            sidebar.Controls.Add(userPanel);
        }

        private void CambiarUsuario()
        {
            Application.Restart();
        }

        private void CerrarSesion()
        {
            if (MessageBox.Show("¿Cerrar sesión y salir de KawaSys?", "Cerrar sesión",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void SetActiveNav(Button btn)
        {
            // restaurar el anterior
            if (_activeNav != null)
            {
                _activeNav.BackColor = NavIdleBack;
                _activeNav.ForeColor = NavIdleFore;
                _activeNav.Invalidate();
            }
            // marcar el nuevo como activo (más claro + barra de acento)
            _activeNav = btn;
            btn.BackColor = NavActiveBack;
            btn.ForeColor = Color.White;
            btn.Invalidate();
        }

        private void ShowForm(Form f)
        {
            contentArea.Controls.Clear();
            f.TopLevel = false;
            f.FormBorderStyle = FormBorderStyle.None;
            f.Dock = DockStyle.Fill;
            f.BackColor = AppTheme.LightBg;
            contentArea.Controls.Add(f);
            f.Show();
        }

        public void ShowDashboard()
        {
            lblPageTitle.Text = "Dashboard";
            ShowForm(new DashboardPanel(CurrentUser));
        }
    }
}
