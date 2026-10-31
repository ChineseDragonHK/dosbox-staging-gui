using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DosGameLauncher;

internal static class WindowAspectLock
{
    const double GameAspect = 4.0 / 3.0;
    const uint EventObjectLocationChange = 0x800B;
    const uint WinEventOutOfContext = 0x0000;
    const int ObjidWindow = 0;
    const int GwlStyle = -16;
    const int GwlExStyle = -20;
    const int WsCaption = 0x00C00000;
    const uint SwpNoMove = 0x0002;
    const uint SwpNoZOrder = 0x0004;
    const uint SwpNoActivate = 0x0010;

    static readonly List<Lock> Active = [];

    public static void Follow(Process process)
    {
        var gate = new Lock(process);
        Active.Add(gate);
        gate.Start();
    }

    sealed class Lock
    {
        readonly Process _process;
        readonly WinEventProc _callback;
        IntPtr _hook;
        int _lastClientWidth;
        int _lastClientHeight;
        bool _adjusting;

        public Lock(Process process)
        {
            _process = process;
            _callback = OnEvent;
        }

        public void Start()
        {
            _hook = SetWinEventHook(
                EventObjectLocationChange,
                EventObjectLocationChange,
                IntPtr.Zero,
                _callback,
                (uint)_process.Id,
                0,
                WinEventOutOfContext);
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => Stop();
        }

        void Stop()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
            Active.Remove(this);
        }

        void OnEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            try
            {
                Adjust(hwnd, idObject, idChild);
            }
            catch (Exception)
            {
                // A failed resize correction must not take down the launcher.
            }
        }

        void Adjust(IntPtr hwnd, int idObject, int idChild)
        {
            if (_adjusting || idObject != ObjidWindow || idChild != 0 || GetParent(hwnd) != IntPtr.Zero)
                return;
            if (IsIconic(hwnd) || IsZoomed(hwnd))
                return;

            var style = (int)GetWindowLongPtr(hwnd, GwlStyle);
            if ((style & WsCaption) == 0)
                return;
            if (!GetClientRect(hwnd, out var client))
                return;

            var width = client.Right - client.Left;
            var height = client.Bottom - client.Top;
            if (width < 160 || height < 120)
                return;

            int targetWidth;
            int targetHeight;
            if (_lastClientWidth == 0)
            {
                if (width / (double)height > GameAspect)
                    targetWidth = (int)Math.Round(height * GameAspect);
                else
                    targetWidth = width;
                targetHeight = (int)Math.Round(targetWidth / GameAspect);
            }
            else if (Math.Abs(width - _lastClientWidth) >= Math.Abs(height - _lastClientHeight))
            {
                targetWidth = width;
                targetHeight = (int)Math.Round(width / GameAspect);
            }
            else
            {
                targetHeight = height;
                targetWidth = (int)Math.Round(height * GameAspect);
            }

            if (Math.Abs(targetWidth - width) <= 2 && Math.Abs(targetHeight - height) <= 2)
            {
                _lastClientWidth = width;
                _lastClientHeight = height;
                return;
            }

            var outer = new Rect { Left = 0, Top = 0, Right = targetWidth, Bottom = targetHeight };
            AdjustWindowRectEx(ref outer, style, false, (int)GetWindowLongPtr(hwnd, GwlExStyle));
            _adjusting = true;
            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                outer.Right - outer.Left,
                outer.Bottom - outer.Top,
                SwpNoMove | SwpNoZOrder | SwpNoActivate);
            _lastClientWidth = targetWidth;
            _lastClientHeight = targetHeight;
            _adjusting = false;
        }
    }

    delegate void WinEventProc(
        IntPtr hook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime);

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    static extern bool AdjustWindowRectEx(ref Rect rect, int style, bool menu, int exStyle);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
