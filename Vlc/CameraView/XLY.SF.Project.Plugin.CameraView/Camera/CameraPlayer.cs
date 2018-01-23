using ProjectExtend.Context;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace XLY.SF.Project.CameraView
{
    /// <summary>
    /// 摄像头播放器控件
    /// 当播放器为拍照时，显示默认的照片；否则显示摄像头的内容
    /// </summary>
    class CameraPlayer: System.Windows.Controls.Image, IDisposable,INotifyPropertyChanged
    {
        /// <summary>
        /// 当前图片的bytes
        /// </summary>
        private byte[] _currentBitmapBytes;

        /// <summary>
        /// 需要一帧图像
        /// </summary>
        private bool _needOneFrame = false;

        public CameraPlayer()
        {
            IsInitialized2 = InnerInitialize();
        }

        /// <summary>
        /// 当前摄像头设备
        /// </summary>
        private CameraDevice _currentCameraDevice;

        /// <summary>
        /// 是否刷新设备停止
        /// </summary>
        private bool _isRefreshStop;

        /// <summary>
        /// 是否已经开始
        /// </summary>
        private bool _haveStarted;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                if(_isPlaying == value)
                {
                    return;
                }
                _isPlaying = value;                
                OnPropertyChanged();
            }
        }
        private bool _isPlaying = false;

        /// <summary>
        /// 是否已经完成初始化
        /// </summary>
        public bool IsInitialized2 { get; private set; }

        /// <summary>
        /// Tips
        /// </summary>
        public string Tips
        {
            get { return _tips; }
            set 
            {
                _tips = value;
                OnPropertyChanged();
            }
        }
        private string _tips;


        private bool InnerInitialize()
        {
            if(IsInitialized2)
            {
                return true;
            }

            CameraDeviceManager cameraDeviceManager = new CameraDeviceManager();
            Task.Run((() => { while (!_isRefreshStop) { RefreshDevice(cameraDeviceManager); Thread.Sleep(1000); } }));

            return true;
        }

        /// <summary>
        ///  刷新设备状态
        /// </summary>
        public void RefreshDevice(CameraDeviceManager cameraDeviceManager)
        {
            //刷新设备的连接状态
            bool isChanged = cameraDeviceManager.DetectState();
            if (isChanged)
            {
                //用界面线程来对_currentCameraDevice进行设置，防止设置对_currentCameraDevice设置时的多线程问题
                AppThread.Instance.Invoke(() =>
                {
                    if (cameraDeviceManager.DefaultCameraDevice != null)
                    {
                        _currentCameraDevice = cameraDeviceManager.DefaultCameraDevice;
                        Start();
                    }
                    else
                    {
                        Stop();
                        _currentCameraDevice = null;
                    }
                });
            }
        }

        /// <summary>
        /// 摄像头开始捕获
        /// </summary>
        public void Start()
        {
            if(_currentCameraDevice != null
                && !_currentCameraDevice.IsStarted)
            {
                _haveStarted = true;
                _currentCameraDevice.NewFrame -= _currentCameraDevice_NewFrame;
                _currentCameraDevice.NewFrame += _currentCameraDevice_NewFrame;
                _currentCameraDevice.ConnnectDevice();
            }
        }

        private void _currentCameraDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            IsPlaying = _haveStarted;
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = bitmap.ToBitmapImage();
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { this.Source = bi; }));

                if (_needOneFrame)
                {
                    using (MemoryStream bitmapStream = new MemoryStream())
                    {
                        eventArgs.Frame.Save(bitmapStream, ImageFormat.Bmp);
                        _currentBitmapBytes = bitmapStream.GetBuffer();
                        _needOneFrame = false;
                    }
                }
            }
            catch (Exception exc)
            {
            }
        }

        /// <summary>
        /// 摄像头停止捕获
        /// </summary>
        public void Stop(bool isClosed = false)
        {
            _isRefreshStop = isClosed;

            if (_currentCameraDevice != null
               && _currentCameraDevice.IsStarted)
            {
                _haveStarted = false;
                _currentCameraDevice.DisconnnectDevice();
                _currentCameraDevice.NewFrame -= _currentCameraDevice_NewFrame;               
            }
            IsPlaying = false;
        }

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="dir"></param>
        public void TakePhoto(string path)
        {
            string dir = Path.GetDirectoryName(path);
            //修整目录
            if (!dir.EndsWith("\\"))
            {
                dir += "\\";
            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (_currentCameraDevice == null
              || !_currentCameraDevice.IsStarted)
            {
                return;
            }
            //等待获取最新的图片bytes
            _needOneFrame = true;
            int count = 0;
            while (true)
            {
                if (!_needOneFrame)
                {
                    break;
                }
                Thread.Sleep(10);
                count++;

                if (_currentCameraDevice == null
                || !_currentCameraDevice.IsStarted
                || count > 100)
                {
                    return;
                }
            }
            //把字节数组写入文件
            if (_currentBitmapBytes != null)
            {
                File.WriteAllBytes(path, _currentBitmapBytes);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable
        //是否回收完毕
        bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CameraPlayer()
        {
            Dispose(false);
        }

        //这里的参数表示示是否需要释放那些实现IDisposable接口的托管对象
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return; //如果已经被回收，就中断执行
            }
            if (disposing)
            {
                IsInitialized2 = false;
                //TODO:释放那些实现IDisposable接口的托管对象
            }
            //TODO:释放非托管资源，设置对象为null
            _disposed = true;
        }
        #endregion
    }
    
}
