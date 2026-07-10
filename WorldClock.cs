using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WorldClockWidget
{
    public class City
    {
        public string Key;
        public string Zh;
        public string En;
        public string TzId;
        public TimeZoneInfo Tz;
        public string TimeStr = "--:--:--";
        public string SubStr = "";

        public string DisplayName(bool zh)
        {
            return zh ? Zh : En;
        }
    }

    public class CatItem
    {
        public string Key;
        public string Zh;
        public string En;
        public string TzId;

        public CatItem(string key, string zh, string en, string tz)
        {
            Key = key;
            Zh = zh;
            En = en;
            TzId = tz;
        }

        public override string ToString()
        {
            return Zh + " / " + En + "   -   " + TzId;
        }
    }

    public class ThemeDef
    {
        public string Id;
        public string Zh;
        public string En;
        public Color Bg;
        public Color Card;
        public Color Fg;
        public Color Accent;
        public Color Mute;
        public Color Line;

        public ThemeDef(string id, string zh, string en, Color bg, Color card, Color fg, Color accent, Color mute, Color line)
        {
            Id = id;
            Zh = zh;
            En = en;
            Bg = bg;
            Card = card;
            Fg = fg;
            Accent = accent;
            Mute = mute;
            Line = line;
        }
    }

    public class MainForm : Form
    {
        readonly List<City> cities = new List<City>();
        Timer timer;
        ContextMenuStrip menu;
        NotifyIcon trayIcon;
        string hdrZone = "本地";
        string hdrTime = "00:00:00";
        string hdrDate = "----";
        Font fZone, fTime, fDate, fName, fCity, fSub;

        readonly string appDir, cfgPath, citiesPath;
        readonly string runName = "WorldClockWidget";

        const string APP_VERSION = "1.0.1";
        const int DEFAULT_WIDTH = 260, PAD = 18, ROWH = 34;
        const int SEP_Y = 112, ROWS_Y = 122, RADIUS = 18;

        string language = "zh";
        string themeId = "catppuccin";
        bool use24Hour = true;
        bool showSeconds = true;
        bool lockedPosition = false;
        bool clickThrough = false;
        string fontSize = "normal";
        int widgetWidth = DEFAULT_WIDTH;
        byte alpha = 235;
        IntPtr desktopHost = IntPtr.Zero;
        string lastPinState = "";
        DateTime lastPinLog = DateTime.MinValue;

        static readonly ThemeDef[] Themes = new ThemeDef[]
        {
            new ThemeDef(
                "catppuccin", "柔夜", "Catppuccin",
                Color.FromArgb(24, 24, 37), Color.FromArgb(30, 30, 46),
                Color.FromArgb(205, 214, 244), Color.FromArgb(137, 180, 250),
                Color.FromArgb(127, 132, 156), Color.FromArgb(49, 50, 68)),
            new ThemeDef(
                "graphite", "石墨", "Graphite",
                Color.FromArgb(28, 30, 34), Color.FromArgb(38, 41, 47),
                Color.FromArgb(235, 238, 242), Color.FromArgb(126, 203, 180),
                Color.FromArgb(151, 161, 173), Color.FromArgb(68, 73, 82)),
            new ThemeDef(
                "nord", "极夜", "Nord",
                Color.FromArgb(36, 41, 51), Color.FromArgb(46, 52, 64),
                Color.FromArgb(236, 239, 244), Color.FromArgb(136, 192, 208),
                Color.FromArgb(180, 190, 205), Color.FromArgb(76, 86, 106)),
            new ThemeDef(
                "daylight", "日间", "Daylight",
                Color.FromArgb(246, 248, 250), Color.FromArgb(255, 255, 255),
                Color.FromArgb(31, 35, 40), Color.FromArgb(9, 105, 218),
                Color.FromArgb(87, 96, 106), Color.FromArgb(208, 215, 222))
        };

        static readonly CatItem[] Catalog = new CatItem[]
        {
            new CatItem("utc", "UTC", "UTC", "UTC"),
            new CatItem("honolulu", "檀香山", "Honolulu", "Hawaiian Standard Time"),
            new CatItem("anchorage", "安克雷奇", "Anchorage", "Alaskan Standard Time"),
            new CatItem("vancouver", "温哥华", "Vancouver", "Pacific Standard Time"),
            new CatItem("seattle", "西雅图", "Seattle", "Pacific Standard Time"),
            new CatItem("sanfrancisco", "旧金山", "San Francisco", "Pacific Standard Time"),
            new CatItem("losangeles", "洛杉矶", "Los Angeles", "Pacific Standard Time"),
            new CatItem("lasvegas", "拉斯维加斯", "Las Vegas", "Pacific Standard Time"),
            new CatItem("phoenix", "凤凰城", "Phoenix", "US Mountain Standard Time"),
            new CatItem("denver", "丹佛", "Denver", "Mountain Standard Time"),
            new CatItem("mexicocity", "墨西哥城", "Mexico City", "Central Standard Time (Mexico)"),
            new CatItem("dallas", "达拉斯", "Dallas", "Central Standard Time"),
            new CatItem("houston", "休斯敦", "Houston", "Central Standard Time"),
            new CatItem("chicago", "芝加哥", "Chicago", "Central Standard Time"),
            new CatItem("toronto", "多伦多", "Toronto", "Eastern Standard Time"),
            new CatItem("miami", "迈阿密", "Miami", "Eastern Standard Time"),
            new CatItem("washingtondc", "华盛顿", "Washington DC", "Eastern Standard Time"),
            new CatItem("newyork", "纽约", "New York", "Eastern Standard Time"),
            new CatItem("montreal", "蒙特利尔", "Montreal", "Eastern Standard Time"),
            new CatItem("bogota", "波哥大", "Bogota", "SA Pacific Standard Time"),
            new CatItem("lima", "利马", "Lima", "SA Pacific Standard Time"),
            new CatItem("santiago", "圣地亚哥", "Santiago", "Pacific SA Standard Time"),
            new CatItem("buenosaires", "布宜诺斯艾利斯", "Buenos Aires", "Argentina Standard Time"),
            new CatItem("saopaulo", "圣保罗", "Sao Paulo", "E. South America Standard Time"),
            new CatItem("riodejaneiro", "里约热内卢", "Rio de Janeiro", "E. South America Standard Time"),
            new CatItem("reykjavik", "雷克雅未克", "Reykjavik", "Greenwich Standard Time"),
            new CatItem("dublin", "都柏林", "Dublin", "GMT Standard Time"),
            new CatItem("london", "伦敦", "London", "GMT Standard Time"),
            new CatItem("lisbon", "里斯本", "Lisbon", "GMT Standard Time"),
            new CatItem("madrid", "马德里", "Madrid", "Romance Standard Time"),
            new CatItem("paris", "巴黎", "Paris", "Romance Standard Time"),
            new CatItem("brussels", "布鲁塞尔", "Brussels", "Romance Standard Time"),
            new CatItem("amsterdam", "阿姆斯特丹", "Amsterdam", "W. Europe Standard Time"),
            new CatItem("berlin", "柏林", "Berlin", "W. Europe Standard Time"),
            new CatItem("zurich", "苏黎世", "Zurich", "W. Europe Standard Time"),
            new CatItem("rome", "罗马", "Rome", "W. Europe Standard Time"),
            new CatItem("vienna", "维也纳", "Vienna", "W. Europe Standard Time"),
            new CatItem("stockholm", "斯德哥尔摩", "Stockholm", "W. Europe Standard Time"),
            new CatItem("oslo", "奥斯陆", "Oslo", "W. Europe Standard Time"),
            new CatItem("copenhagen", "哥本哈根", "Copenhagen", "W. Europe Standard Time"),
            new CatItem("warsaw", "华沙", "Warsaw", "Central European Standard Time"),
            new CatItem("prague", "布拉格", "Prague", "Central Europe Standard Time"),
            new CatItem("budapest", "布达佩斯", "Budapest", "Central Europe Standard Time"),
            new CatItem("athens", "雅典", "Athens", "GTB Standard Time"),
            new CatItem("helsinki", "赫尔辛基", "Helsinki", "FLE Standard Time"),
            new CatItem("istanbul", "伊斯坦布尔", "Istanbul", "Turkey Standard Time"),
            new CatItem("johannesburg", "约翰内斯堡", "Johannesburg", "South Africa Standard Time"),
            new CatItem("cairo", "开罗", "Cairo", "Egypt Standard Time"),
            new CatItem("nairobi", "内罗毕", "Nairobi", "E. Africa Standard Time"),
            new CatItem("riyadh", "利雅得", "Riyadh", "Arab Standard Time"),
            new CatItem("jerusalem", "耶路撒冷", "Jerusalem", "Israel Standard Time"),
            new CatItem("moscow", "莫斯科", "Moscow", "Russian Standard Time"),
            new CatItem("dubai", "迪拜", "Dubai", "Arabian Standard Time"),
            new CatItem("tehran", "德黑兰", "Tehran", "Iran Standard Time"),
            new CatItem("karachi", "卡拉奇", "Karachi", "Pakistan Standard Time"),
            new CatItem("newdelhi", "新德里", "New Delhi", "India Standard Time"),
            new CatItem("mumbai", "孟买", "Mumbai", "India Standard Time"),
            new CatItem("kolkata", "加尔各答", "Kolkata", "India Standard Time"),
            new CatItem("dhaka", "达卡", "Dhaka", "Bangladesh Standard Time"),
            new CatItem("yangon", "仰光", "Yangon", "Myanmar Standard Time"),
            new CatItem("bangkok", "曼谷", "Bangkok", "SE Asia Standard Time"),
            new CatItem("hanoi", "河内", "Hanoi", "SE Asia Standard Time"),
            new CatItem("jakarta", "雅加达", "Jakarta", "SE Asia Standard Time"),
            new CatItem("kualalumpur", "吉隆坡", "Kuala Lumpur", "Singapore Standard Time"),
            new CatItem("manila", "马尼拉", "Manila", "Singapore Standard Time"),
            new CatItem("taipei", "台北", "Taipei", "Taipei Standard Time"),
            new CatItem("beijing", "北京", "Beijing", "China Standard Time"),
            new CatItem("shanghai", "上海", "Shanghai", "China Standard Time"),
            new CatItem("shenzhen", "深圳", "Shenzhen", "China Standard Time"),
            new CatItem("hongkong", "香港", "Hong Kong", "China Standard Time"),
            new CatItem("singapore", "新加坡", "Singapore", "Singapore Standard Time"),
            new CatItem("tokyo", "东京", "Tokyo", "Tokyo Standard Time"),
            new CatItem("osaka", "大阪", "Osaka", "Tokyo Standard Time"),
            new CatItem("seoul", "首尔", "Seoul", "Korea Standard Time"),
            new CatItem("perth", "珀斯", "Perth", "W. Australia Standard Time"),
            new CatItem("brisbane", "布里斯班", "Brisbane", "E. Australia Standard Time"),
            new CatItem("adelaide", "阿德莱德", "Adelaide", "Cen. Australia Standard Time"),
            new CatItem("melbourne", "墨尔本", "Melbourne", "AUS Eastern Standard Time"),
            new CatItem("sydney", "悉尼", "Sydney", "AUS Eastern Standard Time"),
            new CatItem("wellington", "惠灵顿", "Wellington", "New Zealand Standard Time"),
            new CatItem("auckland", "奥克兰", "Auckland", "New Zealand Standard Time")
        };

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        const int WM_NCLBUTTONDOWN = 0xA1, HT_CAPTION = 0x2;
        const int WM_ERASEBKGND = 0x0014;

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr FindWindow(string cls, string win);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")] static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")] static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr val);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr val);
        [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll", SetLastError = true)] static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);
        [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] struct SIZE { public int Cx, Cy; }
        [StructLayout(LayoutKind.Sequential)]
        struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

        static readonly IntPtr HWND_TOP = IntPtr.Zero;
        const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        const long WS_CHILD = 0x40000000L, WS_POPUP = 0x80000000L;
        const long WS_EX_LAYERED = 0x00080000L, WS_EX_NOACTIVATE = 0x08000000L, WS_EX_TOOLWINDOW = 0x00000080L, WS_EX_TRANSPARENT = 0x00000020L;
        const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10, SWP_FRAMECHANGED = 0x20;
        const uint LWA_COLORKEY = 0x1, LWA_ALPHA = 0x2, WM_SPAWN_WORKERW = 0x052C, SMTO_NORMAL = 0x0;
        const int ULW_ALPHA = 0x2;
        const byte AC_SRC_OVER = 0x0, AC_SRC_ALPHA = 0x1;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)WS_EX_NOACTIVATE;
                cp.ExStyle |= (int)WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public MainForm()
        {
            appDir = Path.GetDirectoryName(Application.ExecutablePath);
            cfgPath = Path.Combine(appDir, "settings.txt");
            citiesPath = Path.Combine(appDir, "cities.txt");

            ApplyFonts();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            try { Icon = new Icon(Path.Combine(appDir, "clock.ico")); } catch { }

            LoadSettings();
            ApplyFonts();
            ApplyTheme();
            RebuildMenu();
            SetupTrayIcon();

            MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && !lockedPosition)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            ReloadCities();
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate { Tick(); };
            timer.Start();
            Tick();
        }

        bool IsZh()
        {
            return language != "en";
        }

        string T(string zh, string en)
        {
            return IsZh() ? zh : en;
        }

        string TimeFormat()
        {
            if (use24Hour) return showSeconds ? "HH:mm:ss" : "HH:mm";
            return showSeconds ? "h:mm:ss tt" : "h:mm tt";
        }

        float FontScale()
        {
            if (fontSize == "compact") return 0.9f;
            if (fontSize == "large") return 1.12f;
            return 1.0f;
        }

        void ApplyFonts()
        {
            float s = FontScale();
            bool daylight = String.Equals(CurrentTheme().Id, "daylight", StringComparison.OrdinalIgnoreCase);
            if (fZone != null) fZone.Dispose();
            if (fTime != null) fTime.Dispose();
            if (fDate != null) fDate.Dispose();
            if (fName != null) fName.Dispose();
            if (fCity != null) fCity.Dispose();
            if (fSub != null) fSub.Dispose();

            fZone = new Font("Segoe UI", 8.5f * s);
            fTime = new Font(daylight ? "Segoe UI Semibold" : "Segoe UI Semilight", 30f * s);
            fDate = new Font("Segoe UI", 9.5f * s);
            fName = new Font("Segoe UI", 11f * s);
            fCity = new Font("Consolas", 12.5f * s, FontStyle.Bold);
            fSub  = new Font("Segoe UI", 7.5f * s);
        }

        int WidgetWidth()
        {
            return Math.Max(230, Math.Min(420, widgetWidth));
        }

        ThemeDef CurrentTheme()
        {
            foreach (ThemeDef t in Themes)
                if (String.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase)) return t;
            return Themes[0];
        }

        void ApplyTheme()
        {
            ThemeDef t = CurrentTheme();
            BackColor = t.Bg;
            ForeColor = t.Fg;
            Invalidate();
            RenderLayeredWindow();
        }

        void RebuildMenu()
        {
            ContextMenuStrip old = menu;
            menu = BuildMenu();
            ContextMenuStrip = menu;
            if (trayIcon != null) trayIcon.ContextMenuStrip = menu;
            if (old != null) old.Dispose();
        }

        void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "WorldClock " + APP_VERSION;
            try { trayIcon.Icon = Icon; } catch { }
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += delegate
            {
                ResetPosition();
                EnsurePinned();
                RenderLayeredWindow();
            };
        }

        static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);
        }

        static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr val)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, val) : SetWindowLongPtr32(hWnd, nIndex, val);
        }

        static IntPtr FindIconHost()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
            {
                IntPtr unused;
                SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out unused);

                IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero) return defView;
            }

            IntPtr host = IntPtr.Zero;
            EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                IntPtr defView = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    host = defView;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return host;
        }

        void ApplyDesktopChildStyles()
        {
            long style = GetWindowLongPtr(Handle, GWL_STYLE).ToInt64();
            style = (style & ~WS_POPUP) | WS_CHILD;
            SetWindowLongPtr(Handle, GWL_STYLE, new IntPtr(style));

            long ex = GetWindowLongPtr(Handle, GWL_EXSTYLE).ToInt64();
            ex |= WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            if (clickThrough) ex |= WS_EX_TRANSPARENT;
            else ex &= ~WS_EX_TRANSPARENT;
            SetWindowLongPtr(Handle, GWL_EXSTYLE, new IntPtr(ex));

            SetWindowPos(Handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedRegion();
            EnsurePinned();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = new IntPtr(1);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            RenderLayeredWindow();
        }

        void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            SetWindowRgn(Handle, IntPtr.Zero, true);
            RenderLayeredWindow();
        }

        static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(d, d));
            GraphicsPath path = new GraphicsPath();
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d - 1;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d - 1;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        void EnsurePinned()
        {
            IntPtr host = FindIconHost();
            IntPtr currentParent = GetParent(Handle);
            if (host != IntPtr.Zero && currentParent == host)
            {
                desktopHost = host;
                return;
            }

            IntPtr ret = IntPtr.Zero;
            IntPtr after = IntPtr.Zero;
            int err = 0;
            if (host != IntPtr.Zero)
            {
                Point loc = Location;
                ApplyDesktopChildStyles();
                ret = SetParent(Handle, host);
                err = Marshal.GetLastWin32Error();
                after = GetParent(Handle);
                desktopHost = after;
                Location = loc;
                ApplyRoundedRegion();
                RaiseAboveIcons();
                ApplyAlpha();
            }

            string state = "host=" + host + " ret=" + ret + " err=" + err + " after=" + after;
            if (state != lastPinState || host == IntPtr.Zero || after == IntPtr.Zero || (DateTime.Now - lastPinLog).TotalMinutes > 10)
            {
                lastPinState = state;
                lastPinLog = DateTime.Now;
                try
                {
                    File.AppendAllText(Path.Combine(appDir, "pin.log"),
                        DateTime.Now.ToString("HH:mm:ss") + " " + state + Environment.NewLine);
                }
                catch { }
            }
        }

        void RaiseAboveIcons()
        {
            if (GetParent(Handle) != IntPtr.Zero)
                SetWindowPos(Handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        void EnsureAboveIcons()
        {
            if (GetParent(Handle) == IntPtr.Zero) return;
            RECT r;
            if (!GetWindowRect(Handle, out r)) return;
            POINT c;
            c.X = (r.L + r.R) / 2;
            c.Y = (r.T + r.B) / 2;
            IntPtr top = WindowFromPoint(c);
            uint pidTop;
            uint pidMine;
            GetWindowThreadProcessId(top, out pidTop);
            GetWindowThreadProcessId(Handle, out pidMine);
            if (pidTop != pidMine) RaiseAboveIcons();
        }

        void ApplyAlpha()
        {
            long ex = GetWindowLongPtr(Handle, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(Handle, GWL_EXSTYLE, new IntPtr(ex | WS_EX_LAYERED));
            RenderLayeredWindow();
        }

        void ReloadCities()
        {
            cities.Clear();
            cities.AddRange(LoadCities());
            ClientSize = new Size(WidgetWidth(), ROWS_Y + cities.Count * ROWH + 12);
            ApplyRoundedRegion();
            Tick();
            Invalidate();
            RenderLayeredWindow();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawClock(e.Graphics, false);
        }

        void DrawClock(Graphics g, bool transparentCanvas)
        {
            ThemeDef t = CurrentTheme();
            g.Clear(transparentCanvas ? Color.Transparent : t.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            using (GraphicsPath shell = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), RADIUS))
            using (SolidBrush bg = new SolidBrush(t.Bg))
                g.FillPath(bg, shell);

            bool daylight = String.Equals(t.Id, "daylight", StringComparison.OrdinalIgnoreCase);
            if (daylight) g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (daylight)
            {
                DrawText(g, hdrZone, fZone, t.Mute, new RectangleF(PAD, 13, WidgetWidth() - PAD * 2, 18), StringAlignment.Near, StringAlignment.Near, false);
                DrawText(g, hdrTime, fTime, t.Fg, new RectangleF(PAD - 4, 22, WidgetWidth() - PAD * 2 + 8, 58), StringAlignment.Near, StringAlignment.Near, false);
                DrawText(g, hdrDate, fDate, t.Accent, new RectangleF(PAD, 84, WidgetWidth() - PAD * 2, 18), StringAlignment.Near, StringAlignment.Near, false);
            }
            else
            {
                TextRenderer.DrawText(g, hdrZone, fZone, new Point(PAD, 14), t.Mute, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, hdrTime, fTime, new Point(PAD - 3, 26), t.Fg, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, hdrDate, fDate, new Point(PAD, 84), t.Accent, TextFormatFlags.NoPadding);
            }

            using (Pen pen = new Pen(t.Line))
                g.DrawLine(pen, PAD, SEP_Y, WidgetWidth() - PAD, SEP_Y);

            int y = ROWS_Y;
            int rightW = 150;
            foreach (City c in cities)
            {
                if (daylight)
                {
                    RectangleF nameRect = new RectangleF(PAD, y, WidgetWidth() - PAD * 2 - rightW + 40, ROWH);
                    DrawText(g, c.DisplayName(IsZh()), fName, t.Fg, nameRect, StringAlignment.Near, StringAlignment.Center, true);

                    RectangleF timeRect = new RectangleF(WidgetWidth() - PAD - rightW, y - 1, rightW, 22);
                    DrawText(g, c.TimeStr, fCity, t.Fg, timeRect, StringAlignment.Far, StringAlignment.Near, false);

                    RectangleF subRect = new RectangleF(WidgetWidth() - PAD - rightW, y + 18, rightW, 15);
                    DrawText(g, c.SubStr, fSub, t.Mute, subRect, StringAlignment.Far, StringAlignment.Near, false);
                }
                else
                {
                    Rectangle nameRect = new Rectangle(PAD, y, WidgetWidth() - PAD * 2 - rightW + 40, ROWH);
                    TextRenderer.DrawText(g, c.DisplayName(IsZh()), fName, nameRect, t.Fg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

                    Rectangle timeRect = new Rectangle(WidgetWidth() - PAD - rightW, y + 1, rightW, 20);
                    TextRenderer.DrawText(g, c.TimeStr, fCity, timeRect, t.Fg,
                        TextFormatFlags.Right | TextFormatFlags.NoPadding);

                    Rectangle subRect = new Rectangle(WidgetWidth() - PAD - rightW, y + 19, rightW, 14);
                    TextRenderer.DrawText(g, c.SubStr, fSub, subRect, t.Mute,
                        TextFormatFlags.Right | TextFormatFlags.NoPadding);
                }

                y += ROWH;
            }
        }


        void DrawText(Graphics g, string text, Font font, Color color, RectangleF bounds, StringAlignment align, StringAlignment lineAlign, bool ellipsis)
        {
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = align;
                format.LineAlignment = lineAlign;
                format.FormatFlags = StringFormatFlags.NoWrap;
                if (ellipsis) format.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(text, font, brush, bounds, format);
            }
        }

        void RenderLayeredWindow()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

            using (Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    DrawClock(g, true);

                IntPtr screenDc = GetDC(IntPtr.Zero);
                IntPtr memDc = IntPtr.Zero;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr oldBitmap = IntPtr.Zero;

                try
                {
                    memDc = CreateCompatibleDC(screenDc);
                    hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                    oldBitmap = SelectObject(memDc, hBitmap);

                    POINT dst = new POINT { X = Left, Y = Top };
                    SIZE size = new SIZE { Cx = Width, Cy = Height };
                    POINT src = new POINT { X = 0, Y = 0 };
                    BLENDFUNCTION blend = new BLENDFUNCTION
                    {
                        BlendOp = AC_SRC_OVER,
                        BlendFlags = 0,
                        SourceConstantAlpha = alpha,
                        AlphaFormat = AC_SRC_ALPHA
                    };

                    UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, ULW_ALPHA);
                }
                finally
                {
                    if (oldBitmap != IntPtr.Zero && memDc != IntPtr.Zero) SelectObject(memDc, oldBitmap);
                    if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                    if (memDc != IntPtr.Zero) DeleteDC(memDc);
                    if (screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }

        void Tick()
        {
            EnsurePinned();
            EnsureAboveIcons();

            DateTime utc = DateTime.UtcNow;
            DateTime local = DateTime.Now;
            hdrTime = local.ToString(TimeFormat(), CultureInfo.GetCultureInfo("en-US"));
            hdrDate = IsZh()
                ? local.ToString("yyyy年M月d日 dddd", CultureInfo.GetCultureInfo("zh-CN"))
                : local.ToString("ddd, MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
            hdrZone = T("本地 - ", "Local - ") + TimeZoneInfo.Local.DisplayName.Split(')')[0].TrimStart('(');

            foreach (City c in cities)
            {
                TimeZoneInfo tz = c.Tz ?? TimeZoneInfo.Local;
                DateTime t = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                c.TimeStr = t.ToString(TimeFormat(), CultureInfo.GetCultureInfo("en-US"));

                string day = "";
                if (t.Date > local.Date) day = T("明天 ", "Tomorrow ");
                else if (t.Date < local.Date) day = T("昨天 ", "Yesterday ");

                TimeSpan off = tz.GetUtcOffset(utc);
                string sign = off < TimeSpan.Zero ? "-" : "+";
                int totalMinutes = Math.Abs((int)off.TotalMinutes);
                c.SubStr = day + "UTC" + sign + (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");
            }
            Invalidate();
            RenderLayeredWindow();
        }

        ContextMenuStrip BuildMenu()
        {
            ThemeDef t = CurrentTheme();
            ContextMenuStrip m = new ContextMenuStrip();
            m.BackColor = t.Card;
            m.ForeColor = t.Fg;

            ToolStripMenuItem version = new ToolStripMenuItem("WorldClock v" + APP_VERSION);
            version.Enabled = false;
            m.Items.Add(version);
            m.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem pick = new ToolStripMenuItem(T("选择城市...", "Choose Cities..."));
            pick.Click += delegate { if (ShowCityPicker()) ReloadCities(); };
            m.Items.Add(pick);

            ToolStripMenuItem converter = new ToolStripMenuItem(T("时间换算器...", "Time Converter..."));
            converter.Click += delegate { ShowTimeConverter(); };
            m.Items.Add(converter);

            ToolStripMenuItem lang = new ToolStripMenuItem(T("语言", "Language"));
            AddLanguageItem(lang, "zh", "简体中文");
            AddLanguageItem(lang, "en", "English");
            m.Items.Add(lang);

            ToolStripMenuItem theme = new ToolStripMenuItem(T("主题", "Theme"));
            foreach (ThemeDef th in Themes)
            {
                ThemeDef selected = th;
                ToolStripMenuItem it = new ToolStripMenuItem(IsZh() ? th.Zh : th.En);
                it.Checked = String.Equals(themeId, th.Id, StringComparison.OrdinalIgnoreCase);
                it.Click += delegate
                {
                    themeId = selected.Id;
                    ApplyFonts();
                    ApplyTheme();
                    SaveSettings();
                    BeginInvoke(new MethodInvoker(RebuildMenu));
                };
                theme.DropDownItems.Add(it);
            }
            m.Items.Add(theme);

            ToolStripMenuItem timeMode = new ToolStripMenuItem(T("时间制式", "Time Format"));
            ToolStripMenuItem time24 = new ToolStripMenuItem(T("24 小时制", "24-hour"));
            time24.Checked = use24Hour;
            time24.Click += delegate
            {
                use24Hour = true;
                Tick();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            ToolStripMenuItem time12 = new ToolStripMenuItem(T("12 小时制", "12-hour"));
            time12.Checked = !use24Hour;
            time12.Click += delegate
            {
                use24Hour = false;
                Tick();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            timeMode.DropDownItems.Add(time24);
            timeMode.DropDownItems.Add(time12);
            m.Items.Add(timeMode);

            ToolStripMenuItem display = new ToolStripMenuItem(T("显示", "Display"));
            ToolStripMenuItem seconds = new ToolStripMenuItem(T("显示秒数", "Show Seconds"));
            seconds.Checked = showSeconds;
            seconds.Click += delegate
            {
                showSeconds = !showSeconds;
                Tick();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            display.DropDownItems.Add(seconds);

            ToolStripMenuItem font = new ToolStripMenuItem(T("字体大小", "Font Size"));
            AddFontSizeItem(font, "compact", T("紧凑", "Compact"));
            AddFontSizeItem(font, "normal", T("标准", "Normal"));
            AddFontSizeItem(font, "large", T("大号", "Large"));
            display.DropDownItems.Add(font);

            ToolStripMenuItem width = new ToolStripMenuItem(T("宽度", "Width"));
            AddWidthItem(width, 260, T("标准", "Normal"));
            AddWidthItem(width, 310, T("宽", "Wide"));
            AddWidthItem(width, 360, T("超宽", "Extra Wide"));
            display.DropDownItems.Add(width);
            m.Items.Add(display);

            ToolStripMenuItem behavior = new ToolStripMenuItem(T("行为", "Behavior"));
            ToolStripMenuItem locked = new ToolStripMenuItem(T("锁定位置", "Lock Position"));
            locked.Checked = lockedPosition;
            locked.Click += delegate
            {
                lockedPosition = !lockedPosition;
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            behavior.DropDownItems.Add(locked);

            ToolStripMenuItem passThrough = new ToolStripMenuItem(T("点击穿透", "Click Through"));
            passThrough.Checked = clickThrough;
            passThrough.Click += delegate
            {
                clickThrough = !clickThrough;
                ApplyDesktopChildStyles();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            behavior.DropDownItems.Add(passThrough);

            ToolStripMenuItem reset = new ToolStripMenuItem(T("重置位置", "Reset Position"));
            reset.Click += delegate
            {
                ResetPosition();
                SaveSettings();
                RenderLayeredWindow();
            };
            behavior.DropDownItems.Add(reset);
            m.Items.Add(behavior);

            ToolStripMenuItem opa = new ToolStripMenuItem(T("透明度", "Opacity"));
            foreach (int v in new int[] { 100, 92, 85, 75, 60 })
            {
                int pv = v;
                ToolStripMenuItem it = new ToolStripMenuItem(v + "%");
                it.Checked = alpha == (byte)(pv * 255 / 100);
                it.Click += delegate
                {
                    alpha = (byte)(pv * 255 / 100);
                    ApplyAlpha();
                    SaveSettings();
                    BeginInvoke(new MethodInvoker(RebuildMenu));
                };
                opa.DropDownItems.Add(it);
            }
            m.Items.Add(opa);

            ToolStripMenuItem startup = new ToolStripMenuItem(T("开机自启", "Start with Windows"));
            startup.Checked = IsAutoStart();
            startup.Click += delegate { SetAutoStart(!IsAutoStart()); startup.Checked = IsAutoStart(); };
            m.Items.Add(startup);

            m.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exit = new ToolStripMenuItem(T("退出", "Exit"));
            exit.Click += delegate { Close(); };
            m.Items.Add(exit);

            StyleMenuItems(m.Items, t);
            return m;
        }

        void AddLanguageItem(ToolStripMenuItem parent, string id, string text)
        {
            ToolStripMenuItem it = new ToolStripMenuItem(text);
            it.Checked = String.Equals(language, id, StringComparison.OrdinalIgnoreCase);
            it.Click += delegate
            {
                language = id;
                Tick();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            parent.DropDownItems.Add(it);
        }

        void AddFontSizeItem(ToolStripMenuItem parent, string id, string text)
        {
            ToolStripMenuItem it = new ToolStripMenuItem(text);
            it.Checked = String.Equals(fontSize, id, StringComparison.OrdinalIgnoreCase);
            it.Click += delegate
            {
                fontSize = id;
                ApplyFonts();
                ReloadCities();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            parent.DropDownItems.Add(it);
        }

        void AddWidthItem(ToolStripMenuItem parent, int width, string text)
        {
            ToolStripMenuItem it = new ToolStripMenuItem(text);
            it.Checked = widgetWidth == width;
            it.Click += delegate
            {
                widgetWidth = width;
                ReloadCities();
                EnsureVisibleOnAnyScreen();
                SaveSettings();
                BeginInvoke(new MethodInvoker(RebuildMenu));
            };
            parent.DropDownItems.Add(it);
        }

        void ResetPosition()
        {
            Rectangle scr = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(scr.Right - WidgetWidth() - 24, scr.Top + 24);
            EnsureVisibleOnAnyScreen();
            RenderLayeredWindow();
        }

        void EnsureVisibleOnAnyScreen()
        {
            Rectangle bounds = new Rectangle(Left, Top, Math.Max(Width, WidgetWidth()), Math.Max(Height, 80));
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds)) return;
            }

            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(scr.Right - WidgetWidth() - 24, scr.Top + 24);
        }

        void StyleMenuItems(ToolStripItemCollection items, ThemeDef t)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = t.Card;
                item.ForeColor = t.Fg;
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                if (mi != null) StyleMenuItems(mi.DropDownItems, t);
            }
        }

        bool ShowCityPicker()
        {
            HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (City c in LoadCities())
                selected.Add(c.Key);

            ThemeDef t = CurrentTheme();
            Form dlg = new Form();
            dlg.Text = T("选择城市", "Choose Cities");
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;
            dlg.ClientSize = new Size(380, 460);
            dlg.BackColor = t.Bg;
            dlg.ForeColor = t.Fg;
            dlg.Font = new Font("Segoe UI", 9.5F);
            try { dlg.Icon = Icon; } catch { }

            Label tip = new Label();
            tip.Text = T("搜索、勾选并排序要显示的城市。", "Search, select, and order cities.");
            tip.Dock = DockStyle.Top;
            tip.Height = 30;
            tip.ForeColor = t.Mute;
            tip.TextAlign = ContentAlignment.MiddleLeft;
            tip.Padding = new Padding(12, 0, 0, 0);

            TextBox search = new TextBox();
            search.Dock = DockStyle.Top;
            search.Height = 28;
            search.BorderStyle = BorderStyle.FixedSingle;
            search.BackColor = t.Card;
            search.ForeColor = t.Fg;
            search.Font = new Font("Segoe UI", 10F);

            CheckedListBox clb = new CheckedListBox();
            clb.Dock = DockStyle.Fill;
            clb.BackColor = t.Card;
            clb.ForeColor = t.Fg;
            clb.BorderStyle = BorderStyle.None;
            clb.CheckOnClick = true;
            clb.IntegralHeight = false;
            clb.Font = new Font("Segoe UI", 10F);
            List<CatItem> pickerItems = new List<CatItem>(Catalog);
            List<string> existingOrder = new List<string>();
            foreach (City c in LoadCities()) existingOrder.Add(c.Key);
            pickerItems.Sort(delegate(CatItem a, CatItem b)
            {
                int ai = existingOrder.IndexOf(a.Key);
                int bi = existingOrder.IndexOf(b.Key);
                if (ai < 0) ai = 10000 + Array.IndexOf(Catalog, a);
                if (bi < 0) bi = 10000 + Array.IndexOf(Catalog, b);
                return ai.CompareTo(bi);
            });
            Action refreshList = delegate
            {
                foreach (object obj in clb.CheckedItems)
                    selected.Add(((CatItem)obj).Key);

                string q = search.Text.Trim();
                clb.BeginUpdate();
                clb.Items.Clear();
                foreach (CatItem it in pickerItems)
                {
                    if (q.Length == 0 ||
                        it.Zh.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        it.En.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        it.TzId.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        clb.Items.Add(it, selected.Contains(it.Key));
                }
                clb.EndUpdate();
            };
            clb.ItemCheck += delegate(object sender, ItemCheckEventArgs e)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    CatItem it = clb.Items[e.Index] as CatItem;
                    if (it == null) return;
                    if (clb.GetItemChecked(e.Index)) selected.Add(it.Key);
                    else selected.Remove(it.Key);
                }));
            };
            search.TextChanged += delegate { refreshList(); };
            refreshList();

            Panel panel = new Panel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 50;
            panel.BackColor = t.Bg;

            Button up = new Button();
            up.Text = T("上移", "Up");
            up.Width = 62;
            up.Height = 30;
            up.Left = 8;
            up.Top = 10;
            up.FlatStyle = FlatStyle.Flat;
            up.BackColor = t.Card;
            up.ForeColor = t.Fg;
            up.FlatAppearance.BorderColor = t.Line;
            up.Click += delegate
            {
                if (clb.SelectedItem == null) return;
                CatItem it = (CatItem)clb.SelectedItem;
                int idx = pickerItems.IndexOf(it);
                if (idx <= 0) return;
                pickerItems.RemoveAt(idx);
                pickerItems.Insert(idx - 1, it);
                refreshList();
                clb.SelectedItem = it;
            };

            Button down = new Button();
            down.Text = T("下移", "Down");
            down.Width = 62;
            down.Height = 30;
            down.Left = 74;
            down.Top = 10;
            down.FlatStyle = FlatStyle.Flat;
            down.BackColor = t.Card;
            down.ForeColor = t.Fg;
            down.FlatAppearance.BorderColor = t.Line;
            down.Click += delegate
            {
                if (clb.SelectedItem == null) return;
                CatItem it = (CatItem)clb.SelectedItem;
                int idx = pickerItems.IndexOf(it);
                if (idx < 0 || idx >= pickerItems.Count - 1) return;
                pickerItems.RemoveAt(idx);
                pickerItems.Insert(idx + 1, it);
                refreshList();
                clb.SelectedItem = it;
            };

            Button ok = new Button();
            ok.Text = T("确定", "OK");
            ok.DialogResult = DialogResult.OK;
            ok.Width = 90;
            ok.Height = 30;
            ok.Left = 185;
            ok.Top = 10;
            ok.FlatStyle = FlatStyle.Flat;
            ok.BackColor = t.Accent;
            ok.ForeColor = t.Bg;
            ok.FlatAppearance.BorderSize = 0;

            Button cancel = new Button();
            cancel.Text = T("取消", "Cancel");
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Width = 90;
            cancel.Height = 30;
            cancel.Left = 280;
            cancel.Top = 10;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.BackColor = t.Card;
            cancel.ForeColor = t.Fg;
            cancel.FlatAppearance.BorderColor = t.Line;

            panel.Controls.Add(up);
            panel.Controls.Add(down);
            panel.Controls.Add(ok);
            panel.Controls.Add(cancel);
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;
            dlg.Controls.Add(clb);
            dlg.Controls.Add(panel);
            dlg.Controls.Add(search);
            dlg.Controls.Add(tip);

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                dlg.Dispose();
                return false;
            }

            List<string> lines = new List<string>();
            lines.Add("# Generated by Choose Cities. Format: display name = Windows time zone ID");
            foreach (CatItem it in pickerItems)
            {
                if (selected.Contains(it.Key))
                    lines.Add((IsZh() ? it.Zh : it.En) + " = " + it.TzId);
            }
            if (lines.Count == 1) lines.Add("UTC = UTC");
            try { File.WriteAllLines(citiesPath, lines, System.Text.Encoding.UTF8); } catch { }
            dlg.Dispose();
            return true;
        }

        void ShowTimeConverter()
        {
            ThemeDef t = CurrentTheme();
            Form dlg = new Form();
            dlg.Text = T("时间换算器", "Time Converter");
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.FormBorderStyle = FormBorderStyle.Sizable;
            dlg.MinimumSize = new Size(560, 460);
            dlg.ClientSize = new Size(620, 520);
            dlg.ShowInTaskbar = false;
            dlg.BackColor = t.Bg;
            dlg.ForeColor = t.Fg;
            dlg.Font = new Font("Segoe UI", 9.5F);
            try { dlg.Icon = Icon; } catch { }

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 118;
            top.Padding = new Padding(12);
            top.BackColor = t.Bg;

            Label sourceLabel = new Label();
            sourceLabel.Text = T("基准城市", "Base City");
            sourceLabel.Left = 12;
            sourceLabel.Top = 12;
            sourceLabel.Width = 110;
            sourceLabel.Height = 22;
            sourceLabel.ForeColor = t.Mute;

            ComboBox source = new ComboBox();
            source.DropDownStyle = ComboBoxStyle.DropDownList;
            source.Left = 12;
            source.Top = 36;
            source.Width = 250;
            source.BackColor = t.Card;
            source.ForeColor = t.Fg;
            foreach (CatItem item in Catalog) source.Items.Add(item);
            source.DisplayMember = null;
            source.SelectedItem = FindCatalog(TimeZoneInfo.Local.DisplayName, TimeZoneInfo.Local.Id);
            if (source.SelectedItem == null)
            {
                foreach (CatItem item in Catalog)
                {
                    if (String.Equals(item.TzId, TimeZoneInfo.Local.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        source.SelectedItem = item;
                        break;
                    }
                }
            }
            if (source.SelectedItem == null && source.Items.Count > 0) source.SelectedIndex = 0;

            Label dateLabel = new Label();
            dateLabel.Text = T("日期", "Date");
            dateLabel.Left = 282;
            dateLabel.Top = 12;
            dateLabel.Width = 110;
            dateLabel.Height = 22;
            dateLabel.ForeColor = t.Mute;

            DateTimePicker date = new DateTimePicker();
            date.Left = 282;
            date.Top = 36;
            date.Width = 145;
            date.Format = DateTimePickerFormat.Custom;
            date.CustomFormat = "yyyy-MM-dd";
            date.Value = DateTime.Now;

            Label timeLabel = new Label();
            timeLabel.Text = T("时间", "Time");
            timeLabel.Left = 445;
            timeLabel.Top = 12;
            timeLabel.Width = 110;
            timeLabel.Height = 22;
            timeLabel.ForeColor = t.Mute;

            DateTimePicker time = new DateTimePicker();
            time.Left = 445;
            time.Top = 36;
            time.Width = 130;
            time.Format = DateTimePickerFormat.Custom;
            time.CustomFormat = use24Hour ? "HH:mm:ss" : "h:mm:ss tt";
            time.ShowUpDown = true;
            time.Value = DateTime.Now;

            CheckBox allCities = new CheckBox();
            allCities.Left = 12;
            allCities.Top = 78;
            allCities.Width = 240;
            allCities.Height = 24;
            allCities.Text = T("显示全部内置城市", "Show all built-in cities");
            allCities.ForeColor = t.Fg;
            allCities.BackColor = t.Bg;

            Button now = new Button();
            now.Text = T("现在", "Now");
            now.Left = 282;
            now.Top = 76;
            now.Width = 70;
            now.Height = 28;
            now.FlatStyle = FlatStyle.Flat;
            now.BackColor = t.Card;
            now.ForeColor = t.Fg;
            now.FlatAppearance.BorderColor = t.Line;

            Button close = new Button();
            close.Text = T("关闭", "Close");
            close.Left = 505;
            close.Top = 76;
            close.Width = 70;
            close.Height = 28;
            close.FlatStyle = FlatStyle.Flat;
            close.BackColor = t.Accent;
            close.ForeColor = t.Bg;
            close.FlatAppearance.BorderSize = 0;
            close.Click += delegate { dlg.Close(); };

            top.Controls.Add(sourceLabel);
            top.Controls.Add(source);
            top.Controls.Add(dateLabel);
            top.Controls.Add(date);
            top.Controls.Add(timeLabel);
            top.Controls.Add(time);
            top.Controls.Add(allCities);
            top.Controls.Add(now);
            top.Controls.Add(close);

            ListView results = new ListView();
            results.Dock = DockStyle.Fill;
            results.View = View.Details;
            results.FullRowSelect = true;
            results.GridLines = false;
            results.HideSelection = false;
            results.BackColor = t.Card;
            results.ForeColor = t.Fg;
            results.BorderStyle = BorderStyle.None;
            results.Columns.Add(T("城市", "City"), 170);
            results.Columns.Add(T("日期", "Date"), 150);
            results.Columns.Add(T("时间", "Time"), 120);
            results.Columns.Add("UTC", 80);
            results.Columns.Add(T("时区", "Time Zone"), 240);

            Action update = delegate
            {
                CatItem src = source.SelectedItem as CatItem;
                if (src == null) return;

                TimeZoneInfo srcTz;
                try { srcTz = TimeZoneInfo.FindSystemTimeZoneById(src.TzId); }
                catch { return; }

                DateTime srcLocal = new DateTime(
                    date.Value.Year, date.Value.Month, date.Value.Day,
                    time.Value.Hour, time.Value.Minute, time.Value.Second);
                DateTime utc = TimeZoneInfo.ConvertTimeToUtc(srcLocal, srcTz);

                List<City> targets = new List<City>();
                if (allCities.Checked)
                {
                    foreach (CatItem item in Catalog)
                    {
                        try
                        {
                            targets.Add(new City
                            {
                                Key = item.Key,
                                Zh = item.Zh,
                                En = item.En,
                                TzId = item.TzId,
                                Tz = TimeZoneInfo.FindSystemTimeZoneById(item.TzId)
                            });
                        }
                        catch { }
                    }
                }
                else
                {
                    targets.AddRange(LoadCities());
                }

                results.BeginUpdate();
                results.Items.Clear();
                foreach (City c in targets)
                {
                    TimeZoneInfo tz = c.Tz ?? TimeZoneInfo.Local;
                    DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                    TimeSpan off = tz.GetUtcOffset(utc);
                    string sign = off < TimeSpan.Zero ? "-" : "+";
                    int totalMinutes = Math.Abs((int)off.TotalMinutes);
                    string offset = "UTC" + sign + (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");

                    ListViewItem row = new ListViewItem(c.DisplayName(IsZh()));
                    row.SubItems.Add(IsZh()
                        ? converted.ToString("yyyy-MM-dd dddd", CultureInfo.GetCultureInfo("zh-CN"))
                        : converted.ToString("ddd, MMM d, yyyy", CultureInfo.GetCultureInfo("en-US")));
                    row.SubItems.Add(converted.ToString(TimeFormat(), CultureInfo.GetCultureInfo("en-US")));
                    row.SubItems.Add(offset);
                    row.SubItems.Add(tz.Id);
                    if (String.Equals(c.TzId, src.TzId, StringComparison.OrdinalIgnoreCase))
                        row.BackColor = t.Bg;
                    results.Items.Add(row);
                }
                results.EndUpdate();
            };

            source.SelectedIndexChanged += delegate { update(); };
            date.ValueChanged += delegate { update(); };
            time.ValueChanged += delegate { update(); };
            allCities.CheckedChanged += delegate { update(); };
            now.Click += delegate
            {
                DateTime n = DateTime.Now;
                date.Value = n;
                time.Value = n;
                update();
            };

            dlg.Controls.Add(results);
            dlg.Controls.Add(top);
            dlg.Shown += delegate { update(); };
            dlg.Show(this);
        }

        List<City> LoadCities()
        {
            List<City> list = new List<City>();
            if (!File.Exists(citiesPath)) WriteDefaultCities();
            try
            {
                foreach (string raw in File.ReadAllLines(citiesPath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;

                    string name = line.Substring(0, i).Trim();
                    string tzid = line.Substring(i + 1).Trim();
                    CatItem cat = FindCatalog(name, tzid);
                    City city = new City();
                    city.Key = cat != null ? cat.Key : name;
                    city.Zh = cat != null ? cat.Zh : name;
                    city.En = cat != null ? cat.En : name;
                    city.TzId = tzid;
                    try { city.Tz = TimeZoneInfo.FindSystemTimeZoneById(tzid); }
                    catch
                    {
                        city.Tz = TimeZoneInfo.Local;
                        city.Zh += " (无效)";
                        city.En += " (invalid)";
                    }
                    list.Add(city);
                }
            }
            catch { }

            if (list.Count == 0)
                list.Add(new City { Key = "utc", Zh = "UTC", En = "UTC", TzId = "UTC", Tz = TimeZoneInfo.Utc });
            return list;
        }

        CatItem FindCatalog(string name, string tzid)
        {
            foreach (CatItem it in Catalog)
            {
                if (String.Equals(name, it.Zh, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(name, it.En, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(name, it.Zh + " / " + it.En, StringComparison.OrdinalIgnoreCase))
                    return it;
            }

            foreach (CatItem it in Catalog)
            {
                if (String.Equals(tzid, it.TzId, StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(name, it.Key, StringComparison.OrdinalIgnoreCase))
                    return it;
            }
            return null;
        }

        void WriteDefaultCities()
        {
            string[] def = new string[]
            {
                "# One city per line. Format: display name = Windows time zone ID",
                "北京 = China Standard Time",
                "东京 = Tokyo Standard Time",
                "新加坡 = Singapore Standard Time",
                "迪拜 = Arabian Standard Time",
                "伦敦 = GMT Standard Time",
                "纽约 = Eastern Standard Time",
                "洛杉矶 = Pacific Standard Time"
            };
            try { File.WriteAllLines(citiesPath, def, System.Text.Encoding.UTF8); } catch { }
        }

        bool IsAutoStart()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    return k != null && k.GetValue(runName) != null;
            }
            catch { return false; }
        }

        void SetAutoStart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k == null) return;
                    if (on) k.SetValue(runName, "\"" + Application.ExecutablePath + "\"");
                    else if (k.GetValue(runName) != null) k.DeleteValue(runName);
                }
            }
            catch { }
        }

        void LoadSettings()
        {
            StartPosition = FormStartPosition.Manual;
            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(scr.Right - WidgetWidth() - 24, scr.Top + 24);
            try
            {
                if (!File.Exists(cfgPath)) return;
                foreach (string line in File.ReadAllLines(cfgPath))
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string k = line.Substring(0, idx).Trim();
                    string v = line.Substring(idx + 1).Trim();
                    if (k == "X") Left = int.Parse(v);
                    else if (k == "Y") Top = int.Parse(v);
                    else if (k == "Alpha") alpha = byte.Parse(v);
                    else if (k == "Language") language = v == "en" ? "en" : "zh";
                    else if (k == "Theme") themeId = v;
                    else if (k == "Use24Hour") use24Hour = v != "False" && v != "false" && v != "0";
                    else if (k == "ShowSeconds") showSeconds = v != "False" && v != "false" && v != "0";
                    else if (k == "LockedPosition") lockedPosition = v == "True" || v == "true" || v == "1";
                    else if (k == "ClickThrough") clickThrough = v == "True" || v == "true" || v == "1";
                    else if (k == "FontSize") fontSize = v;
                    else if (k == "WidgetWidth") widgetWidth = int.Parse(v);
                }
                widgetWidth = WidgetWidth();
                EnsureVisibleOnAnyScreen();
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                File.WriteAllLines(cfgPath, new string[]
                {
                    "X=" + Left,
                    "Y=" + Top,
                    "Alpha=" + alpha,
                    "Language=" + language,
                    "Theme=" + themeId,
                    "Use24Hour=" + use24Hour,
                    "ShowSeconds=" + showSeconds,
                    "LockedPosition=" + lockedPosition,
                    "ClickThrough=" + clickThrough,
                    "FontSize=" + fontSize,
                    "WidgetWidth=" + WidgetWidth()
                });
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
            base.OnFormClosing(e);
        }

        [STAThread]
        static void Main()
        {
            bool isNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "WorldClockWidget_SingleInstance", out isNew))
            {
                if (!isNew) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
