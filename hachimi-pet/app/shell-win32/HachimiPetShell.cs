using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace HachimiPetShell
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); }
            catch
            {
                try { SetProcessDPIAware(); } catch { }
            }

            int port = 17890;
            string envPort = Environment.GetEnvironmentVariable("HACHIMI_PET_PORT");
            if (string.IsNullOrEmpty(envPort)) envPort = Environment.GetEnvironmentVariable("AGY_WHALE_PORT");
            int parsedEnvPort;
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out parsedEnvPort) && parsedEnvPort > 0)
            {
                port = parsedEnvPort;
            }

            string targetUrl = string.Format("http://127.0.0.1:{0}/", port);
            string trayIconPath = null;
            int ownerPid = 0;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i] ?? "";
                if (a.StartsWith("--port="))
                {
                    int p;
                    if (int.TryParse(a.Substring("--port=".Length), out p) && p > 0) port = p;
                }
                else if (a.StartsWith("--url="))
                {
                    targetUrl = a.Substring("--url=".Length);
                }
                else if (a.StartsWith("--icon="))
                {
                    trayIconPath = a.Substring("--icon=".Length);
                }
                else if (a.StartsWith("--owner-pid="))
                {
                    int op;
                    if (int.TryParse(a.Substring("--owner-pid=".Length), out op) && op > 0) ownerPid = op;
                }
            }

            bool createdNew;
            using (Mutex singleInstance = new Mutex(true, string.Format("Global\\HachimiPetWebViewShell_{0}", port), out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new OverlayForm(port, targetUrl, trayIconPath, ownerPid));
            }
        }
    }

    internal sealed class NiigoColorTable : ProfessionalColorTable
    {
        private static readonly Color Bg = Color.FromArgb(21, 24, 38);
        private static readonly Color Border = Color.FromArgb(58, 66, 102);

        public override Color ToolStripDropDownBackground { get { return Bg; } }
        public override Color ImageMarginGradientBegin { get { return Bg; } }
        public override Color ImageMarginGradientMiddle { get { return Bg; } }
        public override Color ImageMarginGradientEnd { get { return Bg; } }
        public override Color MenuBorder { get { return Border; } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(40, 47, 78); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(40, 47, 78); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(40, 47, 78); } }
        public override Color MenuItemPressedGradientBegin { get { return Color.FromArgb(48, 56, 92); } }
        public override Color MenuItemPressedGradientEnd { get { return Color.FromArgb(48, 56, 92); } }
        public override Color SeparatorDark { get { return Color.FromArgb(44, 50, 78); } }
        public override Color SeparatorLight { get { return Color.Transparent; } }
    }

    internal sealed class NiigoMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color BgColor = Color.FromArgb(21, 24, 38);
        private static readonly Color HoverFill = Color.FromArgb(41, 47, 78);
        private static readonly Color HoverBorder = Color.FromArgb(105, 118, 190);
        private static readonly Color DangerHoverFill = Color.FromArgb(72, 30, 48);
        private static readonly Color DangerHoverBorder = Color.FromArgb(225, 95, 130);
        private static readonly Color AccentBar = Color.FromArgb(148, 168, 255);
        private static readonly Color DangerAccentBar = Color.FromArgb(251, 113, 133);
        private static readonly Color TextPrimary = Color.FromArgb(242, 244, 255);
        private static readonly Color TextMuted = Color.FromArgb(142, 154, 192);
        private static readonly Color TextHeader = Color.FromArgb(196, 181, 253);
        private static readonly Color TextDanger = Color.FromArgb(253, 164, 175);
        private static readonly Color SepColor = Color.FromArgb(46, 52, 82);
        private static readonly Color BorderColor = Color.FromArgb(62, 70, 110);

        public NiigoMenuRenderer() : base(new NiigoColorTable())
        {
            RoundedEdges = false;
        }

        private static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush br = new SolidBrush(BgColor))
            {
                e.Graphics.FillRectangle(br, e.AffectedBounds);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Keep unified dark canvas without default Win98 light margin strip
            using (SolidBrush br = new SolidBrush(BgColor))
            {
                e.Graphics.FillRectangle(br, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using (Pen pen = new Pen(BorderColor, 1f))
            {
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.ContentRectangle.Height / 2;
            using (Pen pen = new Pen(SepColor, 1f))
            {
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            string tag = e.Item.Tag as string;

            if (tag == "header")
            {
                return;
            }

            if (e.Item.Selected && e.Item.Enabled)
            {
                bool isDanger = (tag == "danger");
                RectangleF pill = new RectangleF(6f, 1.5f, e.Item.Width - 12f, e.Item.Height - 3f);
                using (GraphicsPath gp = CreateRoundedRect(pill, 6.5f))
                {
                    using (SolidBrush br = new SolidBrush(isDanger ? DangerHoverFill : HoverFill))
                    {
                        g.FillPath(br, gp);
                    }
                    using (Pen pen = new Pen(isDanger ? DangerHoverBorder : HoverBorder, 1f))
                    {
                        g.DrawPath(pen, gp);
                    }
                }
                // Subtle glowing accent bar on left edge
                RectangleF bar = new RectangleF(7.5f, 7f, 2.5f, Math.Max(6f, e.Item.Height - 14f));
                using (GraphicsPath bp = CreateRoundedRect(bar, 1.25f))
                using (SolidBrush br = new SolidBrush(isDanger ? DangerAccentBar : AccentBar))
                {
                    g.FillPath(br, bp);
                }
            }

            ToolStripMenuItem mi = e.Item as ToolStripMenuItem;
            if (mi != null)
            {
                if (tag == "toggle")
                {
                    DrawTogglePill(g, e.Item.Width - 40f, (e.Item.Height - 14f) / 2f, mi.Checked);
                }
                else if (tag == "radio" && mi.Checked)
                {
                    DrawCheckGlyph(g, e.Item.Width - 26f, e.Item.Height / 2f);
                }
            }
        }

        private static void DrawTogglePill(Graphics g, float x, float y, bool on)
        {
            RectangleF track = new RectangleF(x, y, 28f, 14f);
            using (GraphicsPath gp = CreateRoundedRect(track, 7f))
            {
                if (on)
                {
                    using (LinearGradientBrush br = new LinearGradientBrush(
                        track,
                        Color.FromArgb(103, 232, 249),
                        Color.FromArgb(167, 139, 250),
                        LinearGradientMode.Horizontal))
                    {
                        g.FillPath(br, gp);
                    }
                }
                else
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(52, 58, 86)))
                    {
                        g.FillPath(br, gp);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(84, 94, 134), 1f))
                    {
                        g.DrawPath(pen, gp);
                    }
                }
            }
            float knobX = on ? (x + 16f) : (x + 2f);
            using (SolidBrush kb = new SolidBrush(on ? Color.White : Color.FromArgb(175, 184, 214)))
            {
                g.FillEllipse(kb, knobX, y + 2f, 10f, 10f);
            }
        }

        private static void DrawCheckGlyph(Graphics g, float cx, float cy)
        {
            using (Pen pen = new Pen(Color.FromArgb(103, 232, 249), 1.9f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new PointF[]
                {
                    new PointF(cx - 4.5f, cy + 0.2f),
                    new PointF(cx - 1.2f, cy + 3.4f),
                    new PointF(cx + 5.0f, cy - 3.4f)
                });
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Handled cleanly via custom right-aligned toggle pill / check glyph
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            string tag = e.Item.Tag as string;

            if (tag == "header")
            {
                e.TextColor = TextHeader;
                Rectangle rect = new Rectangle(14, e.TextRectangle.Y, e.Item.Width - 28, e.TextRectangle.Height);
                TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, rect, TextHeader,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using (Font subFont = new Font("Microsoft YaHei UI", 8.0f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(e.Graphics, "空之世界", subFont, rect, TextMuted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                return;
            }

            if (!e.Item.Enabled)
            {
                e.TextColor = TextMuted;
            }
            else if (tag == "danger" && e.Item.Selected)
            {
                e.TextColor = TextDanger;
            }
            else
            {
                e.TextColor = TextPrimary;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = e.ArrowRectangle.X + e.ArrowRectangle.Width / 2f;
            float cy = e.ArrowRectangle.Y + e.ArrowRectangle.Height / 2f;
            Color clr = e.Item.Selected ? Color.FromArgb(224, 231, 255) : Color.FromArgb(136, 148, 186);
            using (Pen pen = new Pen(clr, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new PointF[]
                {
                    new PointF(cx - 2.2f, cy - 3.8f),
                    new PointF(cx + 2.0f, cy),
                    new PointF(cx - 2.2f, cy + 3.8f)
                });
            }
        }
    }

    public class OverlayForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_EX_TOPMOST = 0x00000008L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private const uint GW_HWNDNEXT = 2;

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWCP_ROUND = 2;

        private const int WM_HOTKEY = 0x0312;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int HOTKEY_ID = 1;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;

        private static readonly HashSet<string> CaptureProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "qq",
            "qqsclauncher",
            "qqscreenshot",
            "tim",
            "wechat",
            "weixin",
            "wechatappex",
            "snipaste",
            "pixpin",
            "sharex",
            "screenclippinghost",
            "snippingtool",
            "fscapture",
            "greenshot",
            "flameshot",
            "picpick",
            "lightshot",
            "wxwork"
        };

        private static readonly HashSet<string> SystemShellClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Progman",
            "WorkerW",
            "NotifyIconOverflowWindow",
            "Windows.UI.Core.CoreWindow",
            "ForegroundStaging",
            "ApplicationManager_DesktopShellWindow",
            "#32768",
            "tooltips_class32"
        };

        private readonly int _port;
        private readonly string _targetUrl;
        private readonly string _trayIconPath;
        private readonly int _ownerPid;
        private readonly uint _myPid;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private WebView2 _webView;
        private NotifyIcon _tray;
        private ContextMenuStrip _trayMenu;
        private Font _menuFont;
        private Font _menuHeaderFont;
        private readonly Dictionary<string, Bitmap> _menuIcons = new Dictionary<string, Bitmap>();
        private IntPtr _trayIconHandle = IntPtr.Zero;
        private System.Windows.Forms.Timer _cursorTimer;
        private bool _webViewReady = false;

        private bool _clickThroughLocked = false;
        private bool _wantedCapture = false;
        private bool _keyboardFocused = false;
        private long _keyboardFocusedAtMs = 0;
        private long _leaseExpiresAtMs = 0;
        private bool? _appliedIgnore = null;
        private bool _appliedAllowActivate = false;

        private bool _yieldedForOverlay = false;
        private long _lastOverlayCheckAtMs = 0;
        private readonly Dictionary<uint, string> _pidNameCache = new Dictionary<uint, string>();
        private long _lastPidCacheClearAtMs = 0;

        private int _lastCursorX = -99999;
        private int _lastCursorY = -99999;
        private long _lastSentAtMs = 0;
        private long _lastOwnerCheckAtMs = 0;

        private string _scheme = "astral";
        private string _roam = "free";
        private bool _soundOn = true;

        public OverlayForm(int port, string targetUrl, string trayIconPath, int ownerPid)
        {
            _port = port;
            _targetUrl = targetUrl;
            _trayIconPath = trayIconPath;
            _ownerPid = ownerPid;
            _myPid = (uint)Process.GetCurrentProcess().Id;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;

            ApplyWorkAreaBounds();

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Color.Transparent;
            Controls.Add(_webView);

            InitTray();
            Load += OnFormLoad;
            FormClosing += OnFormClosing;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)(WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT);
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private void ApplyWorkAreaBounds()
        {
            Screen primary = Screen.PrimaryScreen;
            Rectangle wa = primary.WorkingArea;
            Rectangle full = primary.Bounds;
            int w = wa.Width;
            int h = wa.Height;
            if (wa.X == full.X && wa.Y == full.Y && w == full.Width && h == full.Height)
            {
                h = Math.Max(200, h - 2);
            }
            Bounds = new Rectangle(wa.X, wa.Y, w, h);
        }

        private void ExtendDwmGlass()
        {
            try
            {
                MARGINS m = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(Handle, ref m);
            }
            catch { }
        }

        private static void ApplyRoundedDarkFrame(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { }
            try
            {
                // COLORREF is 0x00BBGGRR for #3E466E (R=62, G=70, B=110)
                int borderColorRef = (110 << 16) | (70 << 8) | 62;
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColorRef, sizeof(int));
            }
            catch { }
        }

        private static long NowMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private void ApplyMouseState()
        {
            if (IsDisposed || !IsHandleCreated) return;
            long now = NowMs();
            bool shouldCapture = !_clickThroughLocked && !_yieldedForOverlay && _wantedCapture && now < _leaseExpiresAtMs && Visible;
            bool shouldIgnore = !shouldCapture;
            bool allowActivate = _keyboardFocused && !_clickThroughLocked && !_yieldedForOverlay && Visible;
            if (_appliedIgnore.HasValue && _appliedIgnore.Value == shouldIgnore && _appliedAllowActivate == allowActivate) return;
            _appliedIgnore = shouldIgnore;
            _appliedAllowActivate = allowActivate;

            long ex = GetWindowLongPtr(Handle, GWL_EXSTYLE).ToInt64();
            ex |= WS_EX_LAYERED;
            if (shouldIgnore)
            {
                ex |= WS_EX_TRANSPARENT;
            }
            else
            {
                ex &= ~WS_EX_TRANSPARENT;
            }
            if (allowActivate)
            {
                ex &= ~WS_EX_NOACTIVATE;
            }
            else
            {
                ex |= WS_EX_NOACTIVATE;
            }
            SetWindowLongPtr(Handle, GWL_EXSTYLE, new IntPtr(ex));
            // CRITICAL: Use SWP_NOZORDER so toggling mouse click-through NEVER raises our overlay above QQ screenshot or other topmost tools!
            SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            ReportHostStateAsync(shouldIgnore);
        }

        private string GetProcessNameCached(uint pid)
        {
            if (pid == 0) return "";
            string cached;
            if (_pidNameCache.TryGetValue(pid, out cached)) return cached;
            string name = "";
            try
            {
                using (Process p = Process.GetProcessById((int)pid))
                {
                    name = p.ProcessName ?? "";
                }
            }
            catch { }
            _pidNameCache[pid] = name;
            return name;
        }

        private static string GetWindowClass(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(128);
            try { GetClassName(hwnd, sb, sb.Capacity); }
            catch { }
            return sb.ToString();
        }

        private bool IsForeignCaptureOrFullscreenOverlay(IntPtr hwnd, Rectangle primaryBounds, Rectangle workArea)
        {
            if (hwnd == IntPtr.Zero || hwnd == Handle) return false;
            if (!IsWindowVisible(hwnd)) return false;

            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0 || pid == _myPid) return false;

            RECT r;
            if (!GetWindowRect(hwnd, out r)) return false;
            int w = r.Right - r.Left;
            int h = r.Bottom - r.Top;
            if (w < 300 || h < 200) return false;

            string cls = GetWindowClass(hwnd);
            if (SystemShellClasses.Contains(cls)) return false;

            long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            bool isTopmost = (exStyle & WS_EX_TOPMOST) != 0;

            double areaRatio = ((double)w * (double)h) / Math.Max(1.0, (double)primaryBounds.Width * (double)primaryBounds.Height);
            bool coversFullMonitor =
                r.Left <= primaryBounds.Left + 4 &&
                r.Top <= primaryBounds.Top + 4 &&
                r.Right >= primaryBounds.Right - 4 &&
                r.Bottom >= primaryBounds.Bottom - 4;

            // 1. Any foreign topmost window covering >= 85% of the screen (QQ/WeChat/Snipaste/PixPin/SnippingTool/fullscreen overlay)
            if (isTopmost && (coversFullMonitor || areaRatio >= 0.85))
            {
                return true;
            }

            // 2. Known screenshot tool process with a topmost overlay covering >= 55% of screen OR explicit capture class name
            if (isTopmost)
            {
                string proc = GetProcessNameCached(pid);
                if (CaptureProcessNames.Contains(proc))
                {
                    if (areaRatio >= 0.55) return true;
                    if (cls.IndexOf("screen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cls.IndexOf("capture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cls.IndexOf("snip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cls.IndexOf("clip", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            // 3. Borderless true-fullscreen window (covering full bounds including taskbar, not a normal maximized window)
            if (coversFullMonitor && !IsZoomed(hwnd))
            {
                long style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
                bool noCaption = (style & WS_CAPTION) != WS_CAPTION;
                string proc = GetProcessNameCached(pid);
                if (CaptureProcessNames.Contains(proc) || (noCaption && workArea.Height < primaryBounds.Height))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DetectActiveCaptureOverlay()
        {
            Rectangle full = Screen.PrimaryScreen.Bounds;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;

            // Check foreground window first (fast path)
            IntPtr fg = GetForegroundWindow();
            if (fg != IntPtr.Zero && IsForeignCaptureOrFullscreenOverlay(fg, full, wa))
            {
                return true;
            }

            // Walk the top of the Z-order band (topmost windows sit at the very front, typically < 15 windows)
            IntPtr cur = GetTopWindow(IntPtr.Zero);
            int checkedCount = 0;
            while (cur != IntPtr.Zero && checkedCount < 24)
            {
                checkedCount++;
                if (cur != Handle && IsWindowVisible(cur))
                {
                    long exStyle = GetWindowLongPtr(cur, GWL_EXSTYLE).ToInt64();
                    if ((exStyle & WS_EX_TOPMOST) == 0)
                    {
                        // Reached below the topmost band
                        break;
                    }
                    if (IsForeignCaptureOrFullscreenOverlay(cur, full, wa))
                    {
                        return true;
                    }
                }
                cur = GetWindow(cur, GW_HWNDNEXT);
            }

            return false;
        }

        private void UpdateOverlayYieldState(long now)
        {
            if (now - _lastOverlayCheckAtMs < 66) return;
            _lastOverlayCheckAtMs = now;

            if (now - _lastPidCacheClearAtMs > 15000)
            {
                _lastPidCacheClearAtMs = now;
                _pidNameCache.Clear();
            }

            bool shouldYield = DetectActiveCaptureOverlay();
            if (shouldYield == _yieldedForOverlay) return;

            _yieldedForOverlay = shouldYield;
            if (_yieldedForOverlay)
            {
                // Drop below all topmost windows (so QQ screenshot canvas, selection box, magnifier & toolbar stay 100% on top!)
                _keyboardFocused = false;
                _appliedIgnore = null;
                ApplyMouseState();
                SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else
            {
                // Restore topmost once the screenshot/fullscreen overlay closes
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                _appliedIgnore = null;
                ApplyMouseState();
            }
        }

        private void ReportHostStateAsync(bool ignoringMouseEvents)
        {
            int port = _port;
            bool locked = _clickThroughLocked;
            bool wanted = _wantedCapture;
            bool yielded = _yieldedForOverlay;
            long leaseRem = Math.Max(0, _leaseExpiresAtMs - NowMs());
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string body = _json.Serialize(new Dictionary<string, object>
                    {
                        { "runtime", "webview2" },
                        { "clickThroughLocked", locked },
                        { "wantedCapture", wanted },
                        { "yieldedForOverlay", yielded },
                        { "ignoringMouseEvents", ignoringMouseEvents },
                        { "leaseRemainingMs", leaseRem }
                    });
                    HttpPostJson(string.Format("http://127.0.0.1:{0}/api/host-state", port), body);
                }
                catch { }
            });
        }

        private void ReportRendererLogAsync(string level, string source, string message)
        {
            int port = _port;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string body = _json.Serialize(new Dictionary<string, object>
                    {
                        { "level", level ?? "error" },
                        { "source", source ?? "webview2-shell" },
                        { "message", message ?? "" }
                    });
                    HttpPostJson(string.Format("http://127.0.0.1:{0}/api/renderer-log", port), body);
                }
                catch { }
            });
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            ExtendDwmGlass();
            ApplyMouseState();
            RegisterHotKey(Handle, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (uint)Keys.W);

            try
            {
                string userDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".hachimi-pet",
                    "webview2-data"
                );
                Directory.CreateDirectory(userDataDir);

                CoreWebView2EnvironmentOptions opts = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataDir, opts);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.DefaultBackgroundColor = Color.Transparent;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

                string preloadScript = BuildPreloadScript(_port);
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(preloadScript);

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _webView.CoreWebView2.WindowCloseRequested += delegate(object s, object ev)
                {
                    ReportRendererLogAsync("warn", "webview2-shell", "Blocked WindowCloseRequested from document/accelerator");
                };
                _webView.CoreWebView2.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs ev)
                {
                    string uri = ev.Uri ?? "";
                    string allowedPrefix = string.Format("http://127.0.0.1:{0}/", _port);
                    if (!uri.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ev.Cancel = true;
                        ReportRendererLogAsync("warn", "webview2-shell", "Blocked external navigation: " + uri);
                    }
                };
                _webView.CoreWebView2.ProcessFailed += delegate(object s, CoreWebView2ProcessFailedEventArgs ev)
                {
                    ReportRendererLogAsync("error", "webview2-shell", "CoreWebView2 ProcessFailed: " + ev.ProcessFailedKind.ToString());
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (!IsDisposed && _webView != null && _webView.CoreWebView2 != null)
                            {
                                _webView.CoreWebView2.Navigate(_targetUrl);
                            }
                        });
                    }
                    catch { }
                };
                _webView.CoreWebView2.NavigationCompleted += delegate(object s, CoreWebView2NavigationCompletedEventArgs ev)
                {
                    if (!ev.IsSuccess)
                    {
                        ReportRendererLogAsync("error", "webview2-shell", "NavigationCompleted failed: " + ev.WebErrorStatus.ToString());
                    }
                    _webViewReady = true;
                    ExtendDwmGlass();
                    _appliedIgnore = null;
                    ApplyMouseState();
                };

                _webView.CoreWebView2.Navigate(_targetUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("25时初音桌宠初始化失败：\n" + ex.Message, "25时初音桌宠", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            FetchSettingsFromBridge();

            _cursorTimer = new System.Windows.Forms.Timer();
            _cursorTimer.Interval = 33;
            _cursorTimer.Tick += OnCursorTick;
            _cursorTimer.Start();
        }

        private static string BuildPreloadScript(int port)
        {
            return @"(function() {
  if (window.petHost) return;
  var cursorListeners = [];
  var commandListeners = [];
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', function(ev) {
      var msg = ev && ev.data;
      if (!msg || typeof msg !== 'object') return;
      if (msg.kind === 'cursor') {
        for (var i = 0; i < cursorListeners.length; i++) {
          try { cursorListeners[i](msg.payload); } catch (e) {}
        }
      } else if (msg.kind === 'command') {
        for (var j = 0; j < commandListeners.length; j++) {
          try { commandListeners[j](msg.payload); } catch (e) {}
        }
      }
    });
  }
  function postToHost(type, data) {
    try {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ type: type, data: data });
      }
    } catch (e) {}
  }
  window.petHost = {
    bridgePort: " + port.ToString() + @",
    setMouseCapture: function(capture) { postToHost('mouseCapture', { capture: !!capture }); },
    focusWindow: function() { postToHost('focusWindow', {}); },
    blurWindow: function() { postToHost('blurWindow', {}); },
    openExternal: function(url) { postToHost('openExternal', { url: url }); },
    quitApp: function() { postToHost('quit', {}); },
    pickDirectory: function() { return Promise.resolve(null); },
    onCursor: function(cb) {
      if (typeof cb !== 'function') return function() {};
      cursorListeners.push(cb);
      return function() {
        var idx = cursorListeners.indexOf(cb);
        if (idx >= 0) cursorListeners.splice(idx, 1);
      };
    },
    onCommand: function(cb) {
      if (typeof cb !== 'function') return function() {};
      commandListeners.push(cb);
      return function() {
        var idx = commandListeners.indexOf(cb);
        if (idx >= 0) commandListeners.splice(idx, 1);
      };
    }
  };
})();";
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                Dictionary<string, object> msg = _json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (msg == null || !msg.ContainsKey("type")) return;
                string type = Convert.ToString(msg["type"]);
                Dictionary<string, object> data = msg.ContainsKey("data") ? msg["data"] as Dictionary<string, object> : null;

                if (type == "mouseCapture")
                {
                    bool capture = data != null && data.ContainsKey("capture") && Convert.ToBoolean(data["capture"]);
                    _wantedCapture = capture;
                    _leaseExpiresAtMs = capture ? (NowMs() + 1600) : 0;
                    ApplyMouseState();
                }
                else if (type == "focusWindow")
                {
                    if (!_yieldedForOverlay && !_clickThroughLocked)
                    {
                        _keyboardFocused = true;
                        _keyboardFocusedAtMs = NowMs();
                        _wantedCapture = true;
                        _leaseExpiresAtMs = NowMs() + 15000;
                        _appliedIgnore = null;
                        ApplyMouseState();
                        try
                        {
                            SetForegroundWindow(Handle);
                            if (_webView != null) _webView.Focus();
                        }
                        catch { }
                    }
                }
                else if (type == "blurWindow")
                {
                    _keyboardFocused = false;
                    _appliedIgnore = null;
                    ApplyMouseState();
                }
                else if (type == "openExternal" && data != null && data.ContainsKey("url"))
                {
                    string u = Convert.ToString(data["url"]);
                    if (!string.IsNullOrEmpty(u) && (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        Process.Start(new ProcessStartInfo(u) { UseShellExecute = true });
                    }
                }
                else if (type == "quit")
                {
                    ExitEverything();
                }
            }
            catch { }
        }

        private void OnCursorTick(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            long now = NowMs();

            UpdateOverlayYieldState(now);
            if (_keyboardFocused && (now - _keyboardFocusedAtMs > 450) && GetForegroundWindow() != Handle)
            {
                _keyboardFocused = false;
            }
            ApplyMouseState();

            if (_ownerPid > 0 && now - _lastOwnerCheckAtMs > 2000)
            {
                _lastOwnerCheckAtMs = now;
                try
                {
                    Process p = Process.GetProcessById(_ownerPid);
                    if (p == null || p.HasExited)
                    {
                        Close();
                        return;
                    }
                }
                catch
                {
                    Close();
                    return;
                }
            }

            if (!_webViewReady || _webView.CoreWebView2 == null) return;

            POINT pt;
            if (!GetCursorPos(out pt)) return;

            double scale = 1.0;
            try
            {
                uint dpi = GetDpiForWindow(Handle);
                if (dpi >= 96) scale = dpi / 96.0;
            }
            catch { }

            int rx = (int)Math.Round((pt.X - Left) / scale);
            int ry = (int)Math.Round((pt.Y - Top) / scale);
            int lw = (int)Math.Round(Width / scale);
            int lh = (int)Math.Round(Height / scale);

            if (rx != _lastCursorX || ry != _lastCursorY || now - _lastSentAtMs > 400)
            {
                _lastCursorX = rx;
                _lastCursorY = ry;
                _lastSentAtMs = now;

                string payload = _json.Serialize(new Dictionary<string, object>
                {
                    { "kind", "cursor" },
                    { "payload", new Dictionary<string, object>
                        {
                            { "x", rx },
                            { "y", ry },
                            { "insideWindow", rx >= 0 && ry >= 0 && rx <= lw && ry <= lh },
                            { "locked", _clickThroughLocked || _yieldedForOverlay }
                        }
                    }
                });
                try { _webView.CoreWebView2.PostWebMessageAsJson(payload); }
                catch { }
            }
        }

        private void SendCommandToWeb(string cmdType)
        {
            if (!_webViewReady || _webView.CoreWebView2 == null) return;
            string payload = _json.Serialize(new Dictionary<string, object>
            {
                { "kind", "command" },
                { "payload", new Dictionary<string, object> { { "type", cmdType } } }
            });
            try { _webView.CoreWebView2.PostWebMessageAsJson(payload); }
            catch { }
        }

        private Bitmap GetMenuIcon(string key, Action<Graphics> draw)
        {
            Bitmap existing;
            if (_menuIcons.TryGetValue(key, out existing)) return existing;
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                draw(g);
            }
            _menuIcons[key] = bmp;
            return bmp;
        }

        private Bitmap GetSwatchIcon(string key, Color c1, Color c2)
        {
            return GetMenuIcon("swatch_" + key, delegate(Graphics g)
            {
                using (LinearGradientBrush br = new LinearGradientBrush(
                    new RectangleF(3f, 3f, 12f, 12f), c1, c2, LinearGradientMode.ForwardDiagonal))
                {
                    g.FillEllipse(br, 3.2f, 3.2f, 11.6f, 11.6f);
                }
                using (Pen pen = new Pen(Color.FromArgb(140, 255, 255, 255), 1f))
                {
                    g.DrawEllipse(pen, 3.2f, 3.2f, 11.6f, 11.6f);
                }
            });
        }

        private void InitTray()
        {
            _menuFont = new Font("Microsoft YaHei UI", 9.2f, FontStyle.Regular);
            _menuHeaderFont = new Font("Microsoft YaHei UI", 8.8f, FontStyle.Bold);

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Renderer = new NiigoMenuRenderer();
            _trayMenu.Font = _menuFont;
            _trayMenu.ShowImageMargin = true;
            _trayMenu.ShowCheckMargin = false;
            _trayMenu.ImageScalingSize = new Size(18, 18);
            _trayMenu.Padding = new Padding(0, 6, 0, 6);
            _trayMenu.MinimumSize = new Size(276, 0);
            _trayMenu.DropShadowEnabled = true;
            _trayMenu.Opening += delegate
            {
                ApplyRoundedDarkFrame(_trayMenu.Handle);
            };

            _tray = new NotifyIcon();
            _tray.Text = "25时初音 · 桌面伴侣 (左键回位 / 右键菜单)";
            _tray.ContextMenuStrip = _trayMenu;

            try
            {
                if (!string.IsNullOrEmpty(_trayIconPath) && File.Exists(_trayIconPath))
                {
                    using (Bitmap raw = new Bitmap(_trayIconPath))
                    using (Bitmap scaled = new Bitmap(raw, new Size(32, 32)))
                    {
                        _trayIconHandle = scaled.GetHicon();
                        _tray.Icon = Icon.FromHandle(_trayIconHandle);
                    }
                }
                else
                {
                    _tray.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _tray.Icon = SystemIcons.Application;
            }

            _tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    SendCommandToWeb("resetPosition");
                }
            };

            RebuildTrayMenu();
            _tray.Visible = true;
        }

        private void FetchSettingsFromBridge()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string txt = HttpGet(string.Format("http://127.0.0.1:{0}/api/state", _port));
                    Dictionary<string, object> root = _json.Deserialize<Dictionary<string, object>>(txt);
                    Dictionary<string, object> st = root != null && root.ContainsKey("settings") ? root["settings"] as Dictionary<string, object> : null;
                    if (st != null && !IsDisposed)
                    {
                        if (st.ContainsKey("scheme")) _scheme = Convert.ToString(st["scheme"]) ?? "astral";
                        if (st.ContainsKey("roam")) _roam = Convert.ToString(st["roam"]) ?? "free";
                        if (st.ContainsKey("soundOn")) _soundOn = Convert.ToBoolean(st["soundOn"]);
                        BeginInvoke((MethodInvoker)delegate { RebuildTrayMenu(); });
                    }
                }
                catch { }
            });
        }

        private void PostSettingsPatch(Dictionary<string, object> patch)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    HttpPostJson(string.Format("http://127.0.0.1:{0}/api/settings", _port), _json.Serialize(patch));
                }
                catch { }
            });
        }

        private ToolStripMenuItem CreateStyledItem(string text, Bitmap icon, string tag, int rightPad)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Image = icon;
            item.Tag = tag;
            item.Font = _menuFont;
            item.Padding = new Padding(4, 6, rightPad, 6);
            return item;
        }

        private void ConfigureSubMenu(ToolStripMenuItem parent)
        {
            ToolStripDropDownMenu drop = parent.DropDown as ToolStripDropDownMenu;
            if (drop != null)
            {
                drop.Renderer = new NiigoMenuRenderer();
                drop.ShowImageMargin = true;
                drop.ShowCheckMargin = false;
                drop.ImageScalingSize = new Size(18, 18);
                drop.Padding = new Padding(0, 5, 0, 5);
                drop.MinimumSize = new Size(218, 0);
                drop.DropShadowEnabled = true;
            }
            parent.DropDownOpening += delegate
            {
                ApplyRoundedDarkFrame(parent.DropDown.Handle);
            };
        }

        private void RebuildTrayMenu()
        {
            if (_trayMenu == null) return;
            _trayMenu.Items.Clear();

            // Subtle Niigo Miku Header Banner
            ToolStripMenuItem headerItem = new ToolStripMenuItem("✦ 25时 · 初音未来");
            headerItem.Tag = "header";
            headerItem.Enabled = false;
            headerItem.Font = _menuHeaderFont;
            headerItem.Padding = new Padding(4, 5, 16, 4);
            _trayMenu.Items.Add(headerItem);
            _trayMenu.Items.Add(new ToolStripSeparator());

            // 1. Reset position
            Bitmap iconPin = GetMenuIcon("pin", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(103, 232, 249), 1.6f))
                {
                    g.DrawEllipse(p, 4.5f, 2.5f, 9f, 9f);
                    g.DrawLine(p, 9f, 11.5f, 9f, 15.8f);
                }
                using (SolidBrush br = new SolidBrush(Color.FromArgb(165, 243, 252)))
                {
                    g.FillEllipse(br, 7.2f, 5.2f, 3.6f, 3.6f);
                }
            });
            ToolStripMenuItem resetItem = CreateStyledItem("召唤回右下角", iconPin, "normal", 20);
            resetItem.Click += delegate { SendCommandToWeb("resetPosition"); };
            _trayMenu.Items.Add(resetItem);

            // 2. Stats Dashboard
            Bitmap iconStats = GetMenuIcon("stats", delegate(Graphics g)
            {
                using (SolidBrush b1 = new SolidBrush(Color.FromArgb(129, 140, 248)))
                using (SolidBrush b2 = new SolidBrush(Color.FromArgb(103, 232, 249)))
                using (SolidBrush b3 = new SolidBrush(Color.FromArgb(196, 181, 253)))
                {
                    g.FillRectangle(b1, 2.5f, 9.5f, 3.2f, 6f);
                    g.FillRectangle(b2, 7.4f, 3.5f, 3.2f, 12f);
                    g.FillRectangle(b3, 12.3f, 6.5f, 3.2f, 9f);
                }
            });
            ToolStripMenuItem statsItem = CreateStyledItem("消耗与费用统计看板", iconStats, "normal", 20);
            statsItem.Click += delegate { SendCommandToWeb("openStats"); };
            _trayMenu.Items.Add(statsItem);

            // 3. Direct Model Price Settings (USD coin icon)
            Bitmap iconPrice = GetMenuIcon("price", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(252, 211, 77), 1.45f))
                {
                    g.DrawEllipse(p, 2.2f, 2.2f, 13.6f, 13.6f);
                }
                using (Font f = new Font("Segoe UI", 8.2f, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(Color.FromArgb(252, 211, 77)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.DrawString("$", f, br, new RectangleF(2.2f, 2.5f, 13.8f, 13.6f), sf);
                }
            });
            ToolStripMenuItem priceItem = CreateStyledItem("模型价格设置", iconPrice, "normal", 20);
            priceItem.Click += delegate { SendCommandToWeb("openPriceSettings"); };
            _trayMenu.Items.Add(priceItem);

            // 4. Color Schemes Submenu
            Bitmap iconPalette = GetMenuIcon("palette", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(244, 114, 182), 1.5f))
                {
                    g.DrawEllipse(p, 2.4f, 2.8f, 13.2f, 12.4f);
                }
                using (SolidBrush d1 = new SolidBrush(Color.FromArgb(103, 232, 249)))
                using (SolidBrush d2 = new SolidBrush(Color.FromArgb(244, 114, 182)))
                using (SolidBrush d3 = new SolidBrush(Color.FromArgb(196, 181, 253)))
                {
                    g.FillEllipse(d1, 5.5f, 5.6f, 2.6f, 2.6f);
                    g.FillEllipse(d2, 9.6f, 5.4f, 2.6f, 2.6f);
                    g.FillEllipse(d3, 6.8f, 9.8f, 2.6f, 2.6f);
                }
            });
            ToolStripMenuItem schemeMenu = CreateStyledItem("切换初音配色", iconPalette, "normal", 26);
            object[][] schemes = new object[][]
            {
                new object[] { "astral", "空之世界 · 银紫原设", Color.FromArgb(216, 206, 238), Color.FromArgb(139, 124, 210) },
                new object[] { "sakura", "樱桃微光 · 柔和粉", Color.FromArgb(251, 207, 232), Color.FromArgb(244, 114, 182) },
                new object[] { "azure", "苍穹微风 · 冰晶蓝", Color.FromArgb(186, 230, 253), Color.FromArgb(56, 189, 248) },
                new object[] { "emerald", "森灵薄荷 · 初音青", Color.FromArgb(167, 243, 208), Color.FromArgb(52, 211, 153) },
                new object[] { "amber", "星屑流光 · 暖香槟", Color.FromArgb(254, 240, 138), Color.FromArgb(251, 191, 36) },
                new object[] { "crimson", "绯夜蔷薇 · 玫瑰红", Color.FromArgb(254, 205, 211), Color.FromArgb(244, 63, 94) },
                new object[] { "arctic", "霜月白雪 · 纯白银", Color.FromArgb(248, 250, 252), Color.FromArgb(148, 163, 184) },
                new object[] { "obsidian", "午夜寂静 · 黑曜灰", Color.FromArgb(100, 116, 139), Color.FromArgb(30, 41, 59) }
            };
            for (int i = 0; i < schemes.Length; i++)
            {
                string id = (string)schemes[i][0];
                string label = (string)schemes[i][1];
                Color c1 = (Color)schemes[i][2];
                Color c2 = (Color)schemes[i][3];
                ToolStripMenuItem item = CreateStyledItem(label, GetSwatchIcon(id, c1, c2), "radio", 32);
                item.Checked = (_scheme == id) || (id == "astral" && _scheme == "gemini");
                item.Click += delegate
                {
                    _scheme = id;
                    PostSettingsPatch(new Dictionary<string, object> { { "scheme", id }, { "outfit", "maid" } });
                    RebuildTrayMenu();
                };
                schemeMenu.DropDownItems.Add(item);
            }
            ConfigureSubMenu(schemeMenu);
            _trayMenu.Items.Add(schemeMenu);

            // 5. Subagent Preview Submenu
            Bitmap iconSpark = GetMenuIcon("spark", delegate(Graphics g)
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(196, 181, 253)))
                {
                    PointF[] star = new PointF[]
                    {
                        new PointF(9f, 2f),
                        new PointF(10.8f, 7.2f),
                        new PointF(16f, 9f),
                        new PointF(10.8f, 10.8f),
                        new PointF(9f, 16f),
                        new PointF(7.2f, 10.8f),
                        new PointF(2f, 9f),
                        new PointF(7.2f, 7.2f)
                    };
                    g.FillPolygon(br, star);
                }
            });
            ToolStripMenuItem subMenu = CreateStyledItem("分身伙伴预览", iconSpark, "normal", 26);

            Bitmap iconPlus = GetMenuIcon("plus", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(110, 231, 183), 1.8f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, 9f, 4f, 9f, 14f);
                    g.DrawLine(p, 4f, 9f, 14f, 9f);
                }
            });
            ToolStripMenuItem spawnItem = CreateStyledItem("召唤一只测试分身", iconPlus, "normal", 18);
            spawnItem.Click += delegate
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { HttpPostJson(string.Format("http://127.0.0.1:{0}/api/subagents", _port), "{}"); }
                    catch { }
                });
            };

            Bitmap iconClear = GetMenuIcon("clear", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(148, 163, 184), 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, 5f, 5f, 13f, 13f);
                    g.DrawLine(p, 13f, 5f, 5f, 13f);
                }
            });
            ToolStripMenuItem clearItem = CreateStyledItem("清空测试分身", iconClear, "normal", 18);
            clearItem.Click += delegate
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { HttpPostJson(string.Format("http://127.0.0.1:{0}/api/subagents", _port), "{\"action\":\"clear\"}"); }
                    catch { }
                });
            };
            subMenu.DropDownItems.Add(spawnItem);
            subMenu.DropDownItems.Add(clearItem);
            ConfigureSubMenu(subMenu);
            _trayMenu.Items.Add(subMenu);

            _trayMenu.Items.Add(new ToolStripSeparator());

            // 6. Toggle items with sleek pill switches on right
            Bitmap iconWalk = GetMenuIcon("walk", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(147, 197, 253), 1.55f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawEllipse(p, 7.6f, 2.3f, 3.2f, 3.2f);
                    g.DrawLine(p, 9.2f, 5.8f, 8.6f, 10.6f);
                    g.DrawLine(p, 6.0f, 8.4f, 12.0f, 7.8f);
                    g.DrawLine(p, 8.6f, 10.6f, 6.2f, 15.2f);
                    g.DrawLine(p, 8.6f, 10.6f, 11.8f, 15.0f);
                }
            });
            ToolStripMenuItem roamItem = CreateStyledItem("允许自由散步", iconWalk, "toggle", 52);
            roamItem.Checked = (_roam == "free");
            roamItem.Click += delegate
            {
                _roam = (_roam == "free") ? "off" : "free";
                PostSettingsPatch(new Dictionary<string, object> { { "roam", _roam } });
                RebuildTrayMenu();
            };
            _trayMenu.Items.Add(roamItem);

            Bitmap iconSound = GetMenuIcon("sound", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(165, 180, 252), 1.5f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawPolygon(p, new PointF[]
                    {
                        new PointF(3.2f, 7.0f),
                        new PointF(6.2f, 7.0f),
                        new PointF(9.6f, 4.2f),
                        new PointF(9.6f, 13.8f),
                        new PointF(6.2f, 11.0f),
                        new PointF(3.2f, 11.0f)
                    });
                    g.DrawArc(p, 9.8f, 6.2f, 4.0f, 5.6f, -55, 110);
                    g.DrawArc(p, 11.2f, 4.6f, 5.0f, 8.8f, -55, 110);
                }
            });
            ToolStripMenuItem soundItem = CreateStyledItem("开启提示音效", iconSound, "toggle", 52);
            soundItem.Checked = _soundOn;
            soundItem.Click += delegate
            {
                _soundOn = !_soundOn;
                PostSettingsPatch(new Dictionary<string, object> { { "soundOn", _soundOn } });
                RebuildTrayMenu();
            };
            _trayMenu.Items.Add(soundItem);

            Bitmap iconGhost = GetMenuIcon("ghost", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(251, 191, 36), 1.5f))
                {
                    g.DrawRectangle(p, 4.2f, 8.0f, 9.6f, 7.0f);
                    g.DrawArc(p, 6.0f, 3.2f, 6.0f, 7.2f, 180, 180);
                }
            });
            ToolStripMenuItem lockItem = CreateStyledItem("鼠标穿透免打扰 (Alt+Shift+W)", iconGhost, "toggle", 52);
            lockItem.Checked = _clickThroughLocked;
            lockItem.Click += delegate
            {
                ToggleClickThroughLock();
            };
            _trayMenu.Items.Add(lockItem);

            _trayMenu.Items.Add(new ToolStripSeparator());

            // 7. Refresh & Exit
            Bitmap iconReload = GetMenuIcon("reload", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(147, 197, 253), 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawArc(p, 3.5f, 3.5f, 11f, 11f, -45, 290);
                    g.DrawLine(p, 11.2f, 3.6f, 14.2f, 4.2f);
                    g.DrawLine(p, 13.6f, 7.2f, 14.2f, 4.2f);
                }
            });
            ToolStripMenuItem reloadItem = CreateStyledItem("刷新桌宠界面", iconReload, "normal", 20);
            reloadItem.Click += delegate
            {
                try
                {
                    if (_webView != null && _webView.CoreWebView2 != null)
                    {
                        _webView.CoreWebView2.Navigate(_targetUrl);
                    }
                }
                catch { }
            };
            _trayMenu.Items.Add(reloadItem);

            Bitmap iconExit = GetMenuIcon("exit", delegate(Graphics g)
            {
                using (Pen p = new Pen(Color.FromArgb(251, 113, 133), 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawArc(p, 3.6f, 4.2f, 10.8f, 10.8f, -52, 284);
                    g.DrawLine(p, 9.0f, 2.4f, 9.0f, 8.2f);
                }
            });
            ToolStripMenuItem exitItem = CreateStyledItem("退出 25时初音", iconExit, "danger", 20);
            exitItem.Click += delegate { ExitEverything(); };
            _trayMenu.Items.Add(exitItem);
        }

        private void ToggleClickThroughLock()
        {
            _clickThroughLocked = !_clickThroughLocked;
            _keyboardFocused = false;
            _wantedCapture = false;
            _leaseExpiresAtMs = 0;
            _appliedIgnore = null;
            ApplyMouseState();
            RebuildTrayMenu();
        }

        private void ExitEverything()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { HttpPostJson(string.Format("http://127.0.0.1:{0}/api/quit", _port), "{}"); }
                catch { }
            });
            BeginInvoke((MethodInvoker)delegate { Close(); });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleClickThroughLock();
            }
            else if (m.Msg == WM_DISPLAYCHANGE)
            {
                ApplyWorkAreaBounds();
                ExtendDwmGlass();
            }
            base.WndProc(ref m);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try { UnregisterHotKey(Handle, HOTKEY_ID); } catch { }
            if (_cursorTimer != null)
            {
                _cursorTimer.Stop();
                _cursorTimer.Dispose();
            }
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            if (_trayIconHandle != IntPtr.Zero)
            {
                try { DestroyIcon(_trayIconHandle); } catch { }
                _trayIconHandle = IntPtr.Zero;
            }
            foreach (KeyValuePair<string, Bitmap> kv in _menuIcons)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            _menuIcons.Clear();
            if (_menuFont != null) { try { _menuFont.Dispose(); } catch { } _menuFont = null; }
            if (_menuHeaderFont != null) { try { _menuHeaderFont.Dispose(); } catch { } _menuHeaderFont = null; }
        }

        private static string HttpGet(string url, int timeoutMs = 2000)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        private static void HttpPostJson(string url, string jsonBody, int timeoutMs = 2000)
        {
            HttpPostJsonWithResponse(url, jsonBody, timeoutMs);
        }

        private static string HttpPostJsonWithResponse(string url, string jsonBody, int timeoutMs = 2000)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.ContentLength = bytes.Length;
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            using (Stream s = req.GetRequestStream())
            {
                s.Write(bytes, 0, bytes.Length);
            }
            using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
