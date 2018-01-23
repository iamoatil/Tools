using AForge.Controls;
using ProjectExtend.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace XLY.SF.Project.CameraView
{
    /// <summary>
    /// 摄像头播放器控件
    /// 当播放器为拍照时，显示默认的照片；否则显示摄像头的内容
    /// </summary>
    class CameraPlayer:Border,IDisposable,INotifyPropertyChanged
    {
        public CameraPlayer()
        {
            this.Loaded += (o, e) =>
              {
                  _timer = new System.Threading.Timer(state =>
                  {
                      double y = 0;
                      int count = 0;

                      for (int i = 0; i < 20; i++)
                      {
                          Point offset = new Point();
                          this.Dispatcher.Invoke(new Action(() =>
                          {
                              Window window = Window.GetWindow(this);
                              offset = this.TranslatePoint(new Point(), window);                              
                          }));
                          if (y == offset.Y
                              && y < 70)
                          {
                              count++;
                              if (count > 4)
                              {
                                  break;
                              }
                              continue;
                          }
                          count = 0;
                          y = offset.Y;
                          Thread.Sleep(100);
                      }

                      this.Dispatcher.Invoke(new Action(() => { IsInitialized2 = InnerInitialize(); }));
                      _timer.Dispose();
                  }, null, 0, 1000);
              };
        }
        private System.Threading.Timer _timer;

        /// <summary>
        /// 视频播放器
        /// </summary>
        private VideoSourcePlayer _videoSourcePlayer;

        /// <summary>
        /// 提示控件
        /// </summary>
        private System.Windows.Forms.Label _tipsLabel;        

        /// <summary>
        /// winForm空间的Host
        /// </summary>
        private WinFormHost _winFormHost;

        /// <summary>
        /// 当前摄像头设备
        /// </summary>
        private CameraDevice _currentCameraDevice;

        /// <summary>
        /// 是否刷新设备停止
        /// </summary>
        private bool _isRefreshStop;
        
        /// <summary>
        /// 是否已经完成初始化
        /// </summary>
        public bool IsInitialized2 { get; private set; }

        /// <summary>
        /// 摄像头的Tips
        /// </summary>
        public string Tips
        {
            get { return _tips; }
            set
            {
                if (!IsInitialized2)
                {
                    return;
                }
                _tips = value;
                _tipsLabel.Text = _tips;
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
            List<System.Windows.Forms.Control> list = new List<System.Windows.Forms.Control>();

            //当前标签
            System.Windows.Forms.Panel tipsPanel = new System.Windows.Forms.Panel();
            tipsPanel.Size = new System.Drawing.Size(54, 22);
            tipsPanel.BackColor =System.Drawing.Color.Black;
            tipsPanel.Left = 535;
            tipsPanel.Top = 15;
            list.Add(tipsPanel);

            _tipsLabel = new System.Windows.Forms.Label();
            System.Drawing.Font font = new System.Drawing.Font(_tipsLabel.Font.FontFamily, 16,System.Drawing.FontStyle.Bold);
            _tipsLabel.Font = font;
            _tipsLabel.Text = "正面";
            _tipsLabel.ForeColor = System.Drawing.Color.White;
            tipsPanel.Controls.Add(_tipsLabel);

            //摄像头实时图像控件
            _videoSourcePlayer = new VideoSourcePlayer();
            _videoSourcePlayer.Dock = DockStyle.Fill;
            list.Add(_videoSourcePlayer);
            
            _winFormHost = new WinFormHost(this, list);

            CameraDeviceManager cameraDeviceManager = new CameraDeviceManager();
            Task.Run((() => { while (!_isRefreshStop) { RefreshDevice(cameraDeviceManager); Thread.Sleep(100); } }));

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
                if (cameraDeviceManager.DefaultCameraDevice != null)
                {
                    _currentCameraDevice = cameraDeviceManager.DefaultCameraDevice;
                    
                    AppThread.Instance.Invoke(()=>
                    {
                        Start();
                    });                    
                }
                else
                {
                    _currentCameraDevice = null;
                }
            }

            //刷新设备的运行状态
            if (_winFormHost != null)
            {
                AppThread.Instance.Invoke(() =>
                {
                    _winFormHost.IsVisible = (_currentCameraDevice != null);
                });
            }
        }

        /// <summary>
        /// 摄像头开始捕获
        /// </summary>
        public void Start()
        {
            if(_currentCameraDevice != null
                && !_currentCameraDevice.IsConnectedToPlayer)
            {
                _currentCameraDevice.ConnnectDevice(_videoSourcePlayer);
                _videoSourcePlayer.Start();
            }
        }

        /// <summary>
        /// 摄像头停止捕获
        /// </summary>
        public void Stop()
        {
            _isRefreshStop = true;

            if (_currentCameraDevice != null
               && _currentCameraDevice.IsConnectedToPlayer)
            {
                _videoSourcePlayer.Stop();
                _currentCameraDevice.DisconnnectDevice(_videoSourcePlayer);
            }           
        }

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="dir"></param>
        public void TakePhoto(string path)
        {
            string dir = Path.GetDirectoryName(path);
            //修整目录
            if(!dir.EndsWith("\\"))
            {
                dir += "\\";
            }
            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                if ((_videoSourcePlayer != null) && (_videoSourcePlayer.IsRunning))
                {
                    BitmapSource image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                _videoSourcePlayer.GetCurrentVideoFrame().GetHbitmap(),
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    MemoryStream ms = new MemoryStream();
                    encoder.Save(ms);
                    // 剪切图片

                    System.Drawing.Image initImage = System.Drawing.Image.FromStream(ms, true);

                    //对象实例化
                    System.Drawing.Bitmap pickedImage = new System.Drawing.Bitmap((int)image.PixelWidth, (int)image.PixelHeight);
                    System.Drawing.Graphics pickedG = System.Drawing.Graphics.FromImage(pickedImage);
                    //设置质量
                    pickedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    pickedG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    //定位
                    System.Drawing.Rectangle fromR = new System.Drawing.Rectangle(0, 0, (int)image.PixelWidth, (int)image.PixelHeight);
                    System.Drawing.Rectangle toR = new System.Drawing.Rectangle(0, 0, (int)image.PixelWidth, (int)image.PixelHeight);
                    //画图
                    pickedG.DrawImage(initImage, toR, fromR, System.Drawing.GraphicsUnit.Pixel);

                    pickedImage.Save(path);

                    // 释放资源 

                    ms.Close();
                    pickedImage.Dispose();
                    pickedG.Dispose();

                }
            }
            catch (Exception ex)
            {
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
