using System;
using AForge.Video;
using AForge.Video.DirectShow;


namespace XLY.SF.Project.CameraView
{
    class CameraDevice
    {
        /// <summary>
        /// 设备
        /// </summary>
        private VideoCaptureDevice _device;

        /// <summary>
        /// NewFrameEventHandler
        /// </summary>
        public event NewFrameEventHandler NewFrame;

        /// <summary>
        /// 是否已经开始了
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// 设备名字
        /// </summary>
        public string Name { get; private set; }

        public CameraDevice(string deviceName)
        {
            Name = deviceName;
        }

        private void CameraDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {            
            if (NewFrame != null)
            {
                NewFrame(sender, eventArgs);
            }
        }

        /// <summary>
        /// 把Player连接到指定的摄像头设备上
        /// </summary>
        /// <param name="device"></param>
        public void ConnnectDevice()
        {
            try
            {
                _device = new VideoCaptureDevice(Name);
            }
            catch (Exception ex)
            {
                return;
            }
            _device.NewFrame -= CameraDevice_NewFrame;
            _device.NewFrame += CameraDevice_NewFrame;
            _device.Start();
            IsStarted = true;
        }

        public void DisconnnectDevice()
        {
            _device.Stop();
            _device.NewFrame -= CameraDevice_NewFrame;
            IsStarted = false;
        }
    }
}
