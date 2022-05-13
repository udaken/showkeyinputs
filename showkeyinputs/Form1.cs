using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using static showkeyinputs.NativeMethods;

namespace showkeyinputs
{
    public partial class Form1 : Form
    {
        private const int DisplayTime = 2000;
        SafeHookHandle _hhook;
        KeyboardLLHookProc _keyboardLLHookProc;
        List<KeyInfo> _queue = new List<KeyInfo>(100);

        void InstallHook()
        {
            uint dwTid = 0;
            _hhook = SetWindowsHookEx(HookType.WM_KEYBOARD_LL, _keyboardLLHookProc, default, dwTid);
            if (_hhook.IsInvalid)
            {
                throw new Win32Exception();
            }
        }

        void UninstallHook()
        {
            if (_hhook?.IsInvalid == false)
            {
                _hhook.Dispose();
                _hhook = null;
            }
        }

        IntPtr HookCallbak(int code, IntPtr wParam, IntPtr lparam)
        {
            if (code >= HC_ACTION)
            {
                _queue.Add(new KeyInfo(Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lparam)));
                pictureBox1.Invalidate();
            }

            return CallNextHookEx(default, code, wParam, lparam);
        }

        public Form1()
        {
            InitializeComponent();
            Disposed += Form1_Disposed;
            _keyboardLLHookProc = HookCallbak;
            Text = Application.ProductName;

        }

        private void Form1_Disposed(object sender, EventArgs e)
        {
            UninstallHook();
            _font?.Dispose();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
#if DEBUG
            GC.Collect();
#endif
            while (_queue.Count > 0 && _queue[0].Tick + DisplayTime < Environment.TickCount)
            {
                _queue.RemoveAt(0);
                pictureBox1.Invalidate();
            }
        }

        Font _font;
        float? _DpiY;
        Pen _pen = new Pen(SystemBrushes.Control, 2);
        StringBuilder _sb = new StringBuilder();
        static StringFormat _stringFormat = new StringFormat(StringFormatFlags.DisplayFormatControl)
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Center,
        };


        float PixelToPoint(Graphics g, int pixel)
        {
            _DpiY = _DpiY ?? g.DpiY;

            const float PointsPerInch = 72f;
            return (pixel / _DpiY.Value) * PointsPerInch;
        }
        GraphicsPath gp = new GraphicsPath();
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(pictureBox1.BackColor);

            if (_queue.Count > 0)
            {
                (Keys, bool) lastKey = default;
                for (int i = 0; i < _queue.Count; i++)
                {
                    var currentKey = (_queue[i].Key, _queue[i].KeyUp);
                    if (currentKey != lastKey)
                        _sb.Append(_queue[i].KeyText).Append(' ');
                    lastKey = currentKey;
                }

                gp.ClearMarkers();
                gp.Reset();
                gp.AddString(_sb.ToString(), SystemFonts.MessageBoxFont.FontFamily, 0, PixelToPoint(g, pictureBox1.Height / 2), e.ClipRectangle, _stringFormat);

                g.SmoothingMode = SmoothingMode.None;
                g.DrawPath(_pen, gp);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(SystemBrushes.ControlText, gp);
                _sb.Clear();
            }

            //TextRenderer.DrawText(g, Environment.TickCount.ToString(), SystemFonts.MessageBoxFont, e.ClipRectangle, Color.White);
            //if (_font == null)
            //{
            //    _font = new Font(SystemFonts.MessageBoxFont.FontFamily, pictureBox1.Height - 2, FontStyle.Bold, GraphicsUnit.Pixel);
            //}
            //g.DrawString(s, _font, SystemBrushes.ControlText, e.ClipRectangle, _stringFormat);
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            _font?.Dispose();
            _font = null;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            InstallHook();
        }
    }

    enum HookType
    {
        WM_KEYBOARD_LL = 13,
    }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr KeyboardLLHookProc(int code, IntPtr wParam, IntPtr lParam);

    static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern SafeHookHandle SetWindowsHookEx(HookType hookType, KeyboardLLHookProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        internal const int HC_ACTION = 0;

    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct KBDLLHOOKSTRUCT
    {
        public readonly Keys vkCode;
        public readonly uint scanCode;
        public readonly LLKHF flags;
        public readonly uint time;
        public readonly UIntPtr dwExtraInfo;

        public override string ToString()
        {
            return $"{vkCode}, {scanCode}, {flags}, {time}, {dwExtraInfo} ";
        }
    }

    struct KeyInfo
    {
        public KeyInfo(KBDLLHOOKSTRUCT hookStruct)
        {
            Key = hookStruct.vkCode & Keys.KeyCode;
            Modifiers = (hookStruct.vkCode & Keys.Modifiers);
            KeyUp = (hookStruct.flags & LLKHF.LLKHF_UP) != 0;
            Tick = (int)hookStruct.time;

            _keyText = null;
        }
        public readonly Keys Key;
        public readonly Keys Modifiers;
        public readonly bool KeyUp;
        private string _keyText;
        public readonly int Tick;

        public string KeyText
        {
            get
            {
                var keyChar =
                    Keys.D0 <= Key && Key <= Keys.D9 ? ((char)('0' + Key - Keys.D0)).ToString() :
                    0xE9 <= (int)Key && (int)Key <= 0xF5 ? $"Oem({Key:X})" :
                    Key.ToString();
                _keyText = _keyText ?? $"{keyChar}{(KeyUp ? "↑" : "↓")}";
                return _keyText;
            }
        }

        public override string ToString()
        {
            return $"{TimeSpan.FromMilliseconds(Tick):g}: {Key}{(KeyUp ? "↑" : "↓")}";
        }
    }

    [Flags]
    enum LLKHF : uint
    {
        None = 0,
        LLKHF_EXTENDED = 1,
        LLKHF_LOWER_IL_INJECTED = 2,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    sealed class SafeHookHandle : SafeHandle
    {
        public SafeHookHandle() : base(default, false)
        {
        }

        public override bool IsInvalid => handle == default;

        protected override bool ReleaseHandle()
        {
            return UnhookWindowsHookEx(handle);
        }
    }
}
