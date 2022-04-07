// =============================================================================
//  ImageViewControl.cs
//
//  Written in 2022 by Dairoku Sekiguchi (sekiguchi at acm dot org)
//
//  To the extent possible under law, the author(s) have dedicated all copyright
//  and related and neighboring rights to this software to the public domain worldwide.
//  This software is distributed without any warranty.
//
//  You should have received a copy of the CC0 Public Domain Dedication along with
//  this software. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
// =============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ImageViewControl
{   public partial class ImageViewControl : UserControl
    {
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFO
        {
            public UInt32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public UInt16 biPlanes;
            public UInt16 biBitCount;
            public UInt32 biCompression;
            public UInt32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256*4)]
            public byte[] bmiColors;
        };
        private enum DIB_ColorMode : UInt32
        {
            DIB_RGB_COLORS = 0,
            DIB_PAL_COLORS = 1
        };
        private enum StretchBltMode : Int32
        {
            STRETCH_ANDSCANS    = 1,        // Same as BLACKONWHITE
            STRETCH_ORSCANS     = 2,        // Same as COLORONCOLOR
            STRETCH_DELETESCANS = 3,        // Same as HALFTONE
            STRETCH_HALFTONE    = 4,        // Same as WHITEONBLACK
        };
        enum RasterOperations : UInt32
        {
            BLACKNESS   = 0x00000042,
            CAPTUREBLT  = 0x40000000,
            DSTINVERT   = 0x00550009,
            MERGECOPY   = 0x00C000CA,
            MERGEPAINT  = 0x00BB0226,
            NOTSRCCOPY  = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            PATCOPY     = 0x00F00021,
            PATINVERT   = 0x005A0049,
            PATPAINT    = 0x00FB0A09,
            SRCAND      = 0x008800C6,
            SRCCOPY     = 0x00CC0020,
            SRCERASE    = 0x00440328,
            SRCINVERT   = 0x00660046,
            SRCPAINT    = 0x00EE0086,
            WHITENESS   = 0x00FF0062,
        };

        [DllImport("gdi32.dll")]
        private static extern int SetDIBitsToDevice(IntPtr hdc,
                                                     Int32 xDest,
                                                     Int32 yDest,
                                                     UInt32 w,
                                                     UInt32 h,
                                                     Int32 xSrc,
                                                     Int32 ySrc,
                                                     UInt32 StartScan,
                                                     UInt32 cLines,
                                                     IntPtr lpvBits,
                                                     IntPtr lpbmi,
                                                     DIB_ColorMode ColorUse);
        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, StretchBltMode iStretchMode);
        [DllImport("gdi32.dll")]
        private static extern int StretchDIBits(IntPtr hdc,
                                                Int32 xDest,
                                                Int32 yDest,
                                                Int32 DestWidth,
                                                Int32 DestHeight,
                                                Int32 xSrc,
                                                Int32 ySrc,
                                                Int32 SrcWidth,
                                                Int32 SrcHeight,
                                                IntPtr lpBits,
                                                IntPtr lpbmi,
                                                DIB_ColorMode iUsage,
                                                RasterOperations rop);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRectRgn(Int32 nLeftRect, Int32 nTopRect, Int32 nRightRect, Int32 nBottomRect);
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        const double MOUSE_WHEEL_STEP = 60.0;
        const int SCALE_CALC_COEF = 100;    // = pow(10, 2) = the number of decimals in the image scale is 2

        BITMAPINFO mBitmapInfo;
        byte[] mImageBuffer;
        Size mImageSize;
        Size mDispImageSize;
        Size mDrawArea;
        Rectangle mVisImageRect;
        SolidBrush mBackBrush;
        double mImageScale;

        bool mMousePressed;
        Point mMousePressedPos;
        Point mMousePressedValue;

        public ImageViewControl()
        {
            InitializeComponent();

            //AllocConsole();

            mBitmapInfo = new BITMAPINFO();
            mBitmapInfo.bmiColors = new byte[256 * 4];
            mImageBuffer = null;
            mImageSize.Width = 0;
            mImageSize.Height = 0;
            mDispImageSize.Width = 0;
            mDispImageSize.Height = 0;
            mImageScale = 1.0;

            mVisImageRect = new Rectangle(0, 0, 0, 0);

            // This prevents from drawing the background (to avoid flickering)
            SetStyle(ControlStyles.Opaque, true);
            mBackBrush = new SolidBrush(Color.White);

            mMousePressed = false;
            SetColorMap(0);

            MouseWheel += new System.Windows.Forms.MouseEventHandler(ImageViewControl_MouseWheel);
        }
        public bool AllocateImageBuffer(int inWidth, int inHeight,
            bool inIsColor, bool inIsBottomUp = false, bool inAllocateAlways = false)
        {
            if (mImageBuffer != null && mImageSize.Width == inWidth && mImageSize.Height == inHeight &&
                IsColor() == inIsColor && IsBottomUp() == inIsBottomUp && inAllocateAlways == false)
            {
                return false;
            }

            mImageBuffer = new byte[CalcImageBufferSize(inWidth, inHeight, inIsColor)];
            if (SetBitmapInfo(inWidth, inHeight, inIsColor, inIsBottomUp))
                ImageSizeChanged();
            return true;
        }
        public bool SetImageBuffer(byte[] inImageBuffer, int inWidth, int inHeight, bool inIsColor, bool inIsBottomUp = false)
        {
            if (inImageBuffer == null)
            {
                RemoveImageBuffer();
                return false;
            }
            if (inImageBuffer.Length != CalcImageBufferSize(inWidth, inHeight, inIsColor))
                throw new System.IndexOutOfRangeException("ImageBuffer length didn't match");
            mImageBuffer = inImageBuffer;
            if (SetBitmapInfo(inWidth, inHeight, inIsColor, inIsBottomUp))
                ImageSizeChanged();
            return true;
        }
        public bool SetImageBuffer(byte[] inImageBuffer)
        {
            if (mImageBuffer == null)
                return false;
            if (inImageBuffer.Length != mImageBuffer.Length)
                throw new System.IndexOutOfRangeException("ImageBuffer length didn't match");
            mImageBuffer = inImageBuffer;
            return true;
        }
        public bool CopyImageBuffer(byte[] inImageBuffer, int inWidth, int inHeight, bool inIsColor, bool inIsBottomUp = false)
        {
            if (inImageBuffer.Length != CalcImageBufferSize(inWidth, inHeight, inIsColor))
                throw new System.IndexOutOfRangeException("ImageBuffer length didn't match");
            bool result;
            result = AllocateImageBuffer(inWidth, inHeight, inIsColor, inIsBottomUp, false);
            inImageBuffer.CopyTo(mImageBuffer, 0);
            return result;
        }
        public bool CopyImageBuffer(byte[] inImageBuffer)
        {
            if (mImageBuffer == null)
                return false;
            if (inImageBuffer.Length != mImageBuffer.Length)
                throw new System.IndexOutOfRangeException("ImageBuffer length didn't match");
            inImageBuffer.CopyTo(mImageBuffer, 0);
            return true;
        }
        public void MarkAsImageModified()
        {
            //Invalidate(true);
            Invalidate();
        }
        public void SetColorMap(int inIndex = 0)
        {
            int i;
            if (inIndex == 0)
            {
                for (i = 0; i < 256; i++)
                {
                    mBitmapInfo.bmiColors[i * 4 + 0] = (byte)i; // rgbBlue
                    mBitmapInfo.bmiColors[i * 4 + 1] = (byte)i; // rgbGreen
                    mBitmapInfo.bmiColors[i * 4 + 2] = (byte)i; // rgbRed
                    mBitmapInfo.bmiColors[i * 4 + 3] = 0;       // rgbReserved
                }
                /*mBitmapInfo.bmiColors[0 * 4 + 0] = 0;   // rgbBlue
                mBitmapInfo.bmiColors[0 * 4 + 1] = 0;   // rgbGreen
                mBitmapInfo.bmiColors[0 * 4 + 2] = 0;   // rgbRed
                mBitmapInfo.bmiColors[1 * 4 + 0] = 255; // rgbBlue
                mBitmapInfo.bmiColors[1 * 4 + 1] = 255; // rgbGreen
                mBitmapInfo.bmiColors[1 * 4 + 2] = 255; // rgbRed*/
                return;
            }
        }
        public int GetImageBufferSize()
        {
            if (mImageBuffer == null)
                return 0;
            return mImageBuffer.Length;
        }
        public byte[] GetImageBuffer()
        {
            return mImageBuffer;
        }
        public int GetImageWidth()
        {
            return mImageSize.Width;
        }
        public int GetImageHeight()
        {
            return mImageSize.Height;
        }
        public bool IsColor()
        {
            if (mBitmapInfo.biBitCount == 24)
                return true;
            return false;
        }
        public bool IsBottomUp()
        {
            if (mBitmapInfo.biHeight > 0)
                return true;
            return false;
        }
        protected bool SetBitmapInfo(int inWidth, int inHeight, bool inIsColor, bool inIsBottomUp = false)
        {
            bool isSizeChanged = false;
            if (mImageSize.Width != inWidth || mImageSize.Height != inHeight)
            {
                isSizeChanged = true;
            }
            mImageSize.Width = inWidth;
            mImageSize.Height = inHeight;
            mDispImageSize.Width = (int)(inWidth * mImageScale);
            mDispImageSize.Height = (int)(inHeight * mImageScale);

            mBitmapInfo.biSize          = 40;  // sizeof(BITMAPINFOHEADER)
            mBitmapInfo.biWidth         = mImageSize.Width;
            mBitmapInfo.biPlanes        = 1;
            mBitmapInfo.biCompression   = 0;    // BI_RGB
            mBitmapInfo.biSizeImage     = 0;
            mBitmapInfo.biXPelsPerMeter = 100;
            mBitmapInfo.biYPelsPerMeter = 100;
            //
            if (inIsBottomUp)
                mBitmapInfo.biHeight = mImageSize.Height;
            else
                mBitmapInfo.biHeight = -1 * mImageSize.Height;
            //
            if (inIsColor)
            {
                mBitmapInfo.biBitCount  = 24;
                mBitmapInfo.biClrUsed = 0;
                mBitmapInfo.biClrImportant = 0;
            }
            else
            {
                mBitmapInfo.biBitCount = 8;
                mBitmapInfo.biClrUsed = 256;
                mBitmapInfo.biClrImportant = 256;
            }
            return isSizeChanged;
        }
        // Private
        private void RemoveImageBuffer()
        {
            mImageBuffer = null;
            mImageSize.Width = 0;
            mImageSize.Height = 0;
            mDispImageSize.Width = 0;
            mDispImageSize.Height = 0;
        }
        private int CalcImageBufferSize(int inWidth, int inHeight, bool inIsColor)
        {
            if (inIsColor)
                return inWidth * inHeight * 3;
            return inWidth * inHeight;
        }
        private void ImageSizeChanged()
        {
            UpdateDispRect();
            MarkAsImageModified();
        }
        private void UpdateDispRect()
        {
            mDrawArea.Width = Size.Width;
            mDrawArea.Height = Size.Height;

            bool mPrevHorizVisible = mHorizScrollBar.Visible;
            bool mPrevVertVisible = mVertScrollBar.Visible;

            for (int i = 0; i < 2; i++)
            {
                if (mDispImageSize.Width < mDrawArea.Width)
                {
                    mHorizScrollBar.Visible = false;
                    mDrawArea.Height = Size.Height;
                }
                else
                {
                    mHorizScrollBar.Visible = true;
                    mDrawArea.Height = Size.Height - mHorizScrollBar.Size.Height;
                }
                if (mDispImageSize.Height < mDrawArea.Height)
                {
                    mVertScrollBar.Visible = false;
                    mDrawArea.Width = Size.Width;
                }
                else
                {
                    mVertScrollBar.Visible = true;
                    mDrawArea.Width = Size.Width - mVertScrollBar.Size.Width;
                }
            }
            int horizDelta = 0;
            int vertDelta = 0;
            if (mHorizScrollBar.Visible)
            {
                vertDelta = mHorizScrollBar.Size.Height;
                int value = mHorizScrollBar.Value;
                mHorizScrollBar.Value = 0;
                mHorizScrollBar.Minimum = 0;
                //SetHorzScrollMax((int )((mDispImageSize.Width - mDrawArea.Width) / mImageScale));
                SetHorzScrollMax(mDispImageSize.Width - mDrawArea.Width);
                if (mPrevHorizVisible)
                {
                    if (value > GetHorzScrollMax())
                        mHorizScrollBar.Value = GetHorzScrollMax();
                    else
                        mHorizScrollBar.Value = value;
                }
            }
            else
            {
                mHorizScrollBar.Value = 0;
            }
            if (mVertScrollBar.Visible)
            {
                horizDelta = mVertScrollBar.Size.Width;
                int value = mVertScrollBar.Value;
                mVertScrollBar.Value = 0;
                mVertScrollBar.Minimum = 0;
                //SetVertScrollMax((int )((mDispImageSize.Height - mDrawArea.Height) / mImageScale));
                SetVertScrollMax(mDispImageSize.Height - mDrawArea.Height);
                if (mPrevVertVisible)
                {
                    if (value > GetVertScrollMax())
                        mVertScrollBar.Value = GetVertScrollMax();
                    else
                        mVertScrollBar.Value = value;
                }
            }
            else
            {
                mVertScrollBar.Value = 0;
            }
            mHorizScrollBar.SetBounds(
                mHorizScrollBar.Location.X,
                mHorizScrollBar.Location.Y,
                Size.Width - horizDelta,
                mHorizScrollBar.Size.Height);
            mVertScrollBar.SetBounds(
                mVertScrollBar.Location.X,
                mVertScrollBar.Location.Y,
                mVertScrollBar.Size.Width,
                Size.Height - vertDelta);
            if (mDispImageSize.Width < mDrawArea.Width)
            {
                mVisImageRect.X = (mDrawArea.Width - mDispImageSize.Width) / 2;
                mVisImageRect.Width = mDispImageSize.Width;
            }
            else
            {
                mVisImageRect.X = 0;
                mVisImageRect.Width = mDrawArea.Width;
            }
            if (mDispImageSize.Height < mDrawArea.Height)
            {
                mVisImageRect.Y = (mDrawArea.Height - mDispImageSize.Height) / 2;
                mVisImageRect.Height = mDispImageSize.Height;
            }
            else
            {
                mVisImageRect.Y = 0;
                mVisImageRect.Height = mDrawArea.Height;
            }
        }
        private void ImageViewControl_Paint(object sender, PaintEventArgs e)
        {
            if (mImageBuffer == null)
            {
                e.Graphics.FillRectangle(mBackBrush,
                    0, 0,
                    Size.Width, Size.Height);
                return;
            }

            if (mVisImageRect.Y != 0)
            {
                e.Graphics.FillRectangle(mBackBrush,
                    0, 0,
                    mDrawArea.Width, mVisImageRect.Y);
                e.Graphics.FillRectangle(mBackBrush,
                    0, mVisImageRect.Bottom,
                    mDrawArea.Width, mVisImageRect.Y);
            }
            if (mVisImageRect.X != 0)
            {
                e.Graphics.FillRectangle(mBackBrush,
                    0, mVisImageRect.Y,
                    mVisImageRect.X, mVisImageRect.Height);
                e.Graphics.FillRectangle(mBackBrush,
                    mVisImageRect.Right, mVisImageRect.Y,
                    mVisImageRect.X, mVisImageRect.Height);
            }
            if (mHorizScrollBar.Visible && mVertScrollBar.Visible)
            {
                // Erase bottom/right little corner box
                e.Graphics.FillRectangle(mBackBrush,
                    mHorizScrollBar.Width, mVertScrollBar.Height,
                    mVertScrollBar.Width,  mHorizScrollBar.Height);
            }

            IntPtr hdc = e.Graphics.GetHdc();
            IntPtr hClipRgn = CreateRectRgn(
                mVisImageRect.X, mVisImageRect.Y,
                mVisImageRect.Right,
                mVisImageRect.Bottom);
            SelectClipRgn(hdc, hClipRgn);
            int size = Marshal.SizeOf(mImageBuffer[0]) * mImageBuffer.Length;
            IntPtr lpvBits = Marshal.AllocHGlobal(size);
            Marshal.Copy(mImageBuffer, 0, lpvBits, size);
            IntPtr lpbmi = Marshal.AllocHGlobal(Marshal.SizeOf(mBitmapInfo));
            Marshal.StructureToPtr(mBitmapInfo, lpbmi, false);
            if (mImageScale == 1.0)
            {
                SetDIBitsToDevice(hdc,
                    mVisImageRect.X, mVisImageRect.Y,
                    (uint)mImageSize.Width, (uint)mImageSize.Height,
                    mHorizScrollBar.Value, -1 * mVertScrollBar.Value,
                    0, (uint)mImageSize.Height,
                    lpvBits, lpbmi, DIB_ColorMode.DIB_RGB_COLORS);
            }
            else
            {
                int srcX, srcY, dstX, dstY;
                srcX = (int)(mHorizScrollBar.Value / mImageScale);
                srcY = -1 * (int)(mVertScrollBar.Value / mImageScale);
                if (mVisImageRect.X != 0 || mImageScale < 1.0)
                    dstX = mVisImageRect.X;
                else
                {
                    dstX = (int)(mHorizScrollBar.Value * SCALE_CALC_COEF) % (int)(mImageScale * SCALE_CALC_COEF);
                    dstX = -1 * (dstX / SCALE_CALC_COEF);
                }
                if (mVisImageRect.Y != 0 || mImageScale < 1.0)
                    dstY = mVisImageRect.Y;
                else
                {
                    dstY = (mVertScrollBar.Value * SCALE_CALC_COEF) % (int)(mImageScale * SCALE_CALC_COEF);
                    dstY = -1 * (dstY / SCALE_CALC_COEF);
                }
                //Console.WriteLine("{0}, {1}", mHorizScrollBar.Value, mVertScrollBar.Value);
                //Console.WriteLine("{0}, {1}, {2}, {3}, {4}", srcX, srcY, dstX, dstY, mImageScale);
                SetStretchBltMode(hdc, StretchBltMode.STRETCH_ORSCANS);
                StretchDIBits(hdc,
                    dstX, dstY,
                    mDispImageSize.Width, mDispImageSize.Height,
                    srcX, srcY,
                    mImageSize.Width, mImageSize.Height,
                    lpvBits, lpbmi,
                    DIB_ColorMode.DIB_RGB_COLORS,
                    RasterOperations.SRCCOPY);
            }
            SelectClipRgn(hdc, IntPtr.Zero);
            DeleteObject(hClipRgn);
            e.Graphics.ReleaseHdc(hdc);
        }
        private void ImageViewControl_Resize(object sender, EventArgs e)
        {
            UpdateDispRect();
            MarkAsImageModified();
        }
        private void mScrollBars_Scroll(object sender, ScrollEventArgs e)
        {
            DebugOutScrollBars();
            MarkAsImageModified();
        }
        private void ImageViewControl_MouseDown(object sender, MouseEventArgs e)
        {
            mMousePressed = true;
            mMousePressedPos = e.Location;
            mMousePressedValue.X = mHorizScrollBar.Value;
            mMousePressedValue.Y = mVertScrollBar.Value;
        }

        private void ImageViewControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mMousePressed == false)
                return;

            bool needUpdate = false;
            if (mHorizScrollBar.Visible)
            {
                //int t = mMousePressedValue.X + (int)((mMousePressedPos.X - e.Location.X) / mImageScale);
                int t = mMousePressedValue.X + mMousePressedPos.X - e.Location.X;
                if (t < mHorizScrollBar.Minimum)
                    t = mHorizScrollBar.Minimum;
                if (t > GetHorzScrollMax())
                    t = GetHorzScrollMax();
                mHorizScrollBar.Value = t;
                needUpdate = true;
            }
            if (mVertScrollBar.Visible)
            {
                //int t = mMousePressedValue.Y + (int)((mMousePressedPos.Y - e.Location.Y) / mImageScale);
                int t = mMousePressedValue.Y + mMousePressedPos.Y - e.Location.Y;
                if (t < mVertScrollBar.Minimum)
                    t = mVertScrollBar.Minimum;
                if (t > GetVertScrollMax())
                    t = GetVertScrollMax();
                mVertScrollBar.Value = t;
                needUpdate = true;
            }
            //DebugOutScrollBars();
            if (needUpdate)
                MarkAsImageModified();
        }
        private void ImageViewControl_MouseUp(object sender, MouseEventArgs e)
        {
            mMousePressed = false;
        }
        private void ImageViewControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.X > mDrawArea.Width || e.Y > mDrawArea.Height)
                return;

            double x, y;
            int t;
            if (e.X < mVisImageRect.X || e.X > mVisImageRect.Right)
                x = 0;
            else
                x = (e.X + mHorizScrollBar.Value) / mImageScale;
            if (e.Y < mVisImageRect.Y || e.Y > (mVisImageRect.Bottom))
                y = 0;
            else
                y = (e.Y + mVertScrollBar.Value) / mImageScale;

            double scale = CalcImageScale((int )(e.Delta / MOUSE_WHEEL_STEP));
            SetDispScale(scale);

            if (mHorizScrollBar.Visible)
            {
                t = (int)(x * mImageScale - e.X);
                if (t < 0)
                    t = 0;
                if (t > GetHorzScrollMax())
                    t = GetHorzScrollMax();
                mHorizScrollBar.Value = t;
            }
            if (mVertScrollBar.Visible)
            {
                t = (int)(y * mImageScale - e.Y);
                if (t < 0)
                    t = 0;
                if (t > GetVertScrollMax())
                    t = GetVertScrollMax();
                mVertScrollBar.Value = t;
            }
        }
        private void SetDispScale(double inScale)
        {
            if (inScale < 0.01)
                inScale = 0.01;
            mImageScale = inScale;
            mDispImageSize.Width = (int)(mImageSize.Width * mImageScale);
            mDispImageSize.Height = (int)(mImageSize.Height * mImageScale);

            UpdateDispRect();
            MarkAsImageModified();
        }
        private double CalcImageScale(int inStep)
        {
            double val, scale;

            val = Math.Log10(mImageScale * 100.0) + inStep / 100.0;
            scale = Math.Pow(10, val);

            // 100% snap
            if (Math.Abs(scale - 100.0) <= 1.0)
                scale = 100;
            // 1% limit
            if (scale <= 1.0)
                scale = 1.0;

            // Make the number of decimals to 2
            int t = (int )scale * (SCALE_CALC_COEF / 100);
            scale = (double)t / (double)SCALE_CALC_COEF;
            //Console.WriteLine("Scale = {0}", scale);

            return scale;
        }
        // Why we need the following functions?
        // This is comming the wired behavior of the WinForm's Scrollbar control
        // Check the "Remarks" in the following official document
        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.scrollbar
        private int GetHorzScrollMax()
        {
            int t = mHorizScrollBar.Maximum - mHorizScrollBar.LargeChange + 1;
            if (t < 0)
                t = 0;
            return t;
        }
        private void SetHorzScrollMax(int inValue)
        {
            mHorizScrollBar.Maximum = inValue + mHorizScrollBar.LargeChange - 1;
        }
        private int GetVertScrollMax()
        {
            int t = mVertScrollBar.Maximum - mVertScrollBar.LargeChange + 1;
            if (t < 0)
                t = 0;
            return t;
        }
        private void SetVertScrollMax(int inValue)
        {
            mVertScrollBar.Maximum = inValue + mVertScrollBar.LargeChange - 1;
        }
        private void DebugOutScrollBars()
        {
            //Console.WriteLine("H: {0}, {1}, {2} {3} {4}", mHorizScrollBar.Minimum, mHorizScrollBar.Maximum, mHorizScrollBar.Value, mHorizScrollBar.LargeChange, mHorizScrollBar.Visible);
            //Console.WriteLine("V: {0}, {1}, {2} {3} {4}", mVertScrollBar.Minimum, mVertScrollBar.Maximum, mVertScrollBar.Value, mVertScrollBar.LargeChange, mVertScrollBar.Visible);
        }
    }
}
