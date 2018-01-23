using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using XLY.SF.Framework.Core.Base.ViewModel;
using XLY.SF.Project.ViewDomain.MefKeys;

namespace XLY.SF.Project.CameraView
{
    [Export(ExportKeys.TakePhotoViewModel, typeof(ViewModelBase))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CameraViewModel : ViewModelBase
    {
        public CameraViewModel()
        {
            TakePhotoCommand = new RelayCommand<CameraPlayer>(TakePhoto);
            DevLooksManager = new DeviceLooksManager();
            SetCameraPlayerStateCommand = new RelayCommand<CameraPlayer>(SetCameraPlayerState);
        }
        /// <summary>
        /// 设备样貌目录的名称
        /// </summary>
        private const string DeviceLooks = "DeviceLooks";
        public DeviceLooksManager DevLooksManager { get; private set; }

        /// <summary>
        /// 拍照命令
        /// </summary>
        public ICommand TakePhotoCommand { get; private set; }

        /// <summary>
        /// 设置CameraPlayer的装填命令
        /// </summary>
        public ICommand SetCameraPlayerStateCommand { get; private set; }

        /// <summary>
        /// 替换后提示
        /// </summary>
        public string ReplacedTip
        {
            get { return _replacedTip; }
            set
            {
                _replacedTip = value;
                OnPropertyChanged();
            }
        }
        private string _replacedTip;

        protected override void InitLoad(object parameters)
        {
            base.InitLoad(parameters);
            string dir = parameters as string;
           
            if (!string.IsNullOrWhiteSpace(dir))
            {
                dir = Path.GetFullPath(dir);
                if (Directory.Exists(dir))
                {
                    dir = $"{dir}\\{DeviceLooks}\\";
                    DevLooksManager.Initialize(dir);
                    return;
                }
            }
            DevLooksManager.Initialize(Path.GetFullPath(DeviceLooks));
        }

        /// <summary>
        /// 拍照
        /// </summary>
        private void TakePhoto(CameraPlayer player)
        {
            DeviceLooks devLook = DevLooksManager.SelectedItem;
            if(devLook != null)
            {
                string path = devLook.ImagePath;
                if(File.Exists(path))
                {
                    ReplacedTip = devLook.Name + "照片已经被覆盖。";
                }
                player.TakePhoto(path);
                devLook.CheckValidate();
            }
        }
      
        private void SetCameraPlayerState(CameraPlayer player)
        {
            DeviceLooks devLook = DevLooksManager.SelectedItem;
            if (devLook != null)
            {
               // player.Tips = devLook.Name;
            }
        }
    }
}
