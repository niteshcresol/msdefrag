using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MSDefrag
{
    class DrawingWrapper
    {
        [DllImport("GDI32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("GDI32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("GDI32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("GDI32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("GDI32.dll")]
        public static extern int SelectObject(IntPtr hdc, IntPtr hgdiobj);
    }
}
