using ProjectExtend.Context;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace XLY.SF.Project.CameraView
{
    class WinFormHost
    {
        Window _wpfWindow;
        FrameworkElement _placementTarget;
        Form _formWindow; // the top-level window holding the WebBrowser control
        Control _uiElement;
        DispatcherOperation _repositionCallback;

        /// <summary>
        /// 是否现实
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
                if (_formWindow != null)
                {
                    _formWindow.Visible = _isVisible;
                }
            }
        }
        private bool _isVisible;
        public WinFormHost(FrameworkElement placementTarget, List<Control> controlList)
        {
            _placementTarget = placementTarget;
            _uiElement = controlList[0];
            Window owner = Window.GetWindow(placementTarget);
            if(owner == null)
            {
                return;
            }
            _wpfWindow = owner;

            _formWindow = new Form();
            _formWindow.Opacity = _wpfWindow.Opacity;
            _formWindow.ShowInTaskbar = false;
            _formWindow.FormBorderStyle = FormBorderStyle.None;
            _formWindow.TransparencyKey = System.Drawing.Color.Black;

            foreach (var item in controlList)
            {
                _formWindow.Controls.Add(item);
            }
            
            _wpfWindow.LocationChanged += delegate { OnSizeLocationChanged(); };
            _placementTarget.SizeChanged += delegate { OnSizeLocationChanged(); };           

            if (_wpfWindow.IsVisible)
            {
                InitialShow();
            }
            else
            {
                _wpfWindow.SourceInitialized += delegate
                {
                    InitialShow();
                };
            }

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(UIElement.OpacityProperty, typeof(Window));
            dpd.AddValueChanged(_wpfWindow, delegate { _formWindow.Opacity = _wpfWindow.Opacity; });

            _formWindow.FormClosing += delegate { _wpfWindow.Close(); };
            OnSizeLocationChanged();
        }

        void InitialShow()
        {
            NativeWindow owner = new NativeWindow();
            owner.AssignHandle(((HwndSource)HwndSource.FromVisual(_wpfWindow)).Handle);
            _formWindow.Show(owner);
            owner.ReleaseHandle();
        }

        void OnSizeLocationChanged()
        {
            // To reduce flicker when transparency is applied without DWM composition, 
            // do resizing at lower priority.
            if (_repositionCallback == null)
                _repositionCallback = _wpfWindow.Dispatcher.BeginInvoke(Reposition, DispatcherPriority.Input);
        }

        public void Reposition()
        {
            _repositionCallback = null;

            Point offset = _placementTarget.TranslatePoint(new Point(), _wpfWindow);
            Point size = new Point(_placementTarget.ActualWidth, _placementTarget.ActualHeight);
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual(_wpfWindow);
            CompositionTarget ct = hwndSource.CompositionTarget;
            offset = ct.TransformToDevice.Transform(offset);
            size = ct.TransformToDevice.Transform(size);

            Win32.POINT screenLocation = new Win32.POINT(offset);
            Win32.ClientToScreen(hwndSource.Handle, ref screenLocation);
            Win32.POINT screenSize = new Win32.POINT(size);

            Win32.MoveWindow(_formWindow.Handle, screenLocation.X, screenLocation.Y, screenSize.X, screenSize.Y, true);
            //_form.SetBounds(screenLocation.X, screenLocation.Y, screenSize.X, screenSize.Y);
            //_form.Update();
        }
    }
}
