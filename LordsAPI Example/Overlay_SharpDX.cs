﻿// Copyright (c) 2016-2017 DirectX_Renderer - DoxCode - https://github.com/DoxCode
//
// DxRender - xDasEinhorn
//
// The overlay window use SHARPDX as wrapper of the DirectX API.
// Used version 4.01, of:
//
// SharpDX, SharpDX.Desktop, SharpDX.Direct2D1, SharpDX.Direct3D11, SharpDX.DXGI, SharpDX.Mathematics
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN

using System;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using Factory = SharpDX.Direct2D1.Factory;
using FontFactory = SharpDX.DirectWrite.Factory;
using Format = SharpDX.DXGI.Format;
using SharpDX;
using SharpDX.DirectWrite;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using LordsAPI_Example;
using LordsAPI;

namespace DirectX_Renderer
{
    public partial class Overlay_SharpDX : Form
    {
        public WindowRenderTarget device;
        private HwndRenderTargetProperties renderProperties;
        private SolidColorBrush solidColorBrush;
        private Factory factory;

        //text fonts to test DirectX direct draw text
        private TextFormat font;
        private FontFactory fontFactory;
        private const string fontFamily = "Arial";
        private const float fontSize = 25.0f;

        private IntPtr handle;
        private Thread threadDX = null;
        //DllImports
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref int[] pMargins);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr handle);

        //Styles
        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        public static IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WM_ACTIVATE = 6;
        private const int WA_INACTIVE = 0;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATEANDEAT = 0x0004;
        private Process game = null;
        public void Exit(object sender, EventArgs e)
        {
            this.Close();
        }
        private Overlay_SharpDX overlay_;
        public Overlay_SharpDX(Process process)
        {
            overlay_ = this;
            game = process;
            process.Exited += Exit;
            this.handle = Handle;
            SetWindowLong(Handle, -8, process.Handle);
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            OnResize(null);

            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
        }
        public void UpdatePos()
        {
            Utils.ProcessUtils.RECT screen = LordsAPI.Utils.ProcessUtils.GetProgrammRect(game);
            this.Width = screen.Right - 32;
            this.Height = screen.Bottom - 32;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(screen.Left + 6, screen.Top + 0);
        }
        // Remember change the values of the form in the designer.
        private void Overlay_SharpDX_Load(object sender, EventArgs e)
        {
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            UpdatePos();

            this.DoubleBuffered = true; // reduce the flicker
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.TopMost = true;
            this.Visible = true;
            var exStyle = User32.GetWindowLong(this.Handle, User32.GWL_EXSTYLE);
            exStyle |= User32.WS_EX_LAYERED;
            exStyle |= User32.WS_EX_TRANSPARENT;
            User32.SetWindowLong(this.Handle, User32.GWL_EXSTYLE, (IntPtr)exStyle);
            factory = new Factory();
            fontFactory = new FontFactory();
            renderProperties = new HwndRenderTargetProperties()
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(this.Width, this.Height),
                PresentOptions = PresentOptions.None
            };

            //Init DirectX
            device = new WindowRenderTarget(factory, new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), renderProperties);

            // if you want use DirectX direct renderer, you can use this brush and fonts.
            // of course you can change this as you want.
            solidColorBrush = new SolidColorBrush(device, new SharpDX.Mathematics.Interop.RawColor4(255, 0, 0, 255));
            font = new TextFormat(fontFactory, fontFamily, fontSize);


            threadDX = new Thread(new ParameterizedThreadStart(_loop_DXThread));
            
            threadDX.Priority = ThreadPriority.Highest;
            threadDX.IsBackground = true;
            threadDX.Start();

            device.BeginDraw();
            device.Clear(new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0));
            device.EndDraw();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int[] marg = new int[] { 0, 0, Width, Height };
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }

        private void _loop_DXThread(object sender)
        {
            while (true)
            {
                if (Utils.ProcessUtils.IsActivate(LordsMobileAPI.Settings.GetProcess))
                {
                    Thread.Sleep(500);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Used to not show up the form in alt-tab window. 
        /// Tested on Windows 7 - 64bit and Windows 10 64bit
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams pm = base.CreateParams;
                pm.ExStyle |= 0x80;
                pm.ExStyle |= WS_EX_TOPMOST; // make the form topmost
                pm.ExStyle |= WS_EX_NOACTIVATE; // prevent the form from being activated
                return pm;
            }
        }

        /// <summary>
        /// Makes the form unable to gain focus at all time, 
        /// which should prevent lose focus
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATEANDEAT;
                return;
            }
            if (m.Msg == WM_ACTIVATE)
            {
                if (((int)m.WParam & 0xFFFF) != WA_INACTIVE)
                    if (m.LParam != IntPtr.Zero)
                        SetActiveWindow(m.LParam);
                    else
                        SetActiveWindow(IntPtr.Zero);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}