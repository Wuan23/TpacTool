using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using CommonServiceLocator;
using TpacTool.IO;
using TpacTool.Lib;
using TpacTool.Properties;

namespace TpacTool
{
	public class TextureViewModel : ViewModelBase
	{
		internal static int _clampMode = 2;
		internal static int _channelMode = 0;

		internal static string CLAMP_NO = Resources.Texture_Preview_Clamp_Full;
		internal static string CLAMP_2048 = "2048";
		internal static string CLAMP_1024 = "1024";
		internal static string CLAMP_512 = "512";

		internal static string CHANNEL_AUTO = Resources.Texture_Preview_Channel_Auto;
		internal static string CHANNEL_RGBA = "RGBA";
		internal static string CHANNEL_RGB = "RGB";
		internal static string CHANNEL_RG = "RG";
		internal static string CHANNEL_R = Resources.Texture_Preview_Channel_RGray;
		internal static string CHANNEL_G = Resources.Texture_Preview_Channel_GGray;
		internal static string CHANNEL_B = Resources.Texture_Preview_Channel_BGray;
		internal static string CHANNEL_A = Resources.Texture_Preview_Channel_Alpha;

		internal static string[] _clampItems = new[]
		{
			CLAMP_NO,
			CLAMP_2048,
			CLAMP_1024,
			CLAMP_512
		};

		internal static string[] _channelItems = new[]
		{
			CHANNEL_AUTO,
			CHANNEL_RGBA,
			CHANNEL_RGB,
			CHANNEL_RG,
			CHANNEL_R,
			CHANNEL_G,
			CHANNEL_B,
			CHANNEL_A
		};

		private SaveFileDialog _saveFileDialog;

		private Texture _asset;

		public string[] ClampItems { private set; get; } = _clampItems;

		public string[] ChannelItems { private set; get; } = _channelItems;

		public int ClampMode
		{
			set
			{
				_clampMode = value;
				MessengerInstance.Send(Asset, OglPreviewViewModel.PreviewTextureEvent);
			}
			get => _clampMode;
		}

		public int ChannelMode
		{
			set
			{
				_channelMode = value;
				MessengerInstance.Send(Asset, OglPreviewViewModel.PreviewTextureEvent);
			}
			get => _channelMode;
		}

		public Texture Asset
		{
			private set
			{
				_asset = value;
				MessengerInstance.Send(Asset, OglPreviewViewModel.PreviewTextureEvent);
			}
			get => _asset;
		}

		public string SuggestedFormat { private set; get; }

		public bool ShowExportButton
		{
			get
			{
				if (Asset == null)
					return false;
				var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
				var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(Asset));
				return source.Any(p => p.Items.Count > 1);
			}
		}

		public bool IsExportable { set; get; } = false;

		public ICommand ExportCommand { private set; get; }

		public ICommand ExportSingleAssetCommand { private set; get; }

		private SaveFileDialog _exportSaveFileDialog;

		public TextureViewModel()
		{
			if (IsInDesignMode)
			{
			}
			else
			{
				_saveFileDialog = new SaveFileDialog();
				_saveFileDialog.CreatePrompt = false;
				_saveFileDialog.OverwritePrompt = true;
				_saveFileDialog.AddExtension = true;
				_saveFileDialog.Filter = "Portable Network Graphics (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds";
				_saveFileDialog.Title = Resources.Texture_Dialog_SelectExportFile;

				MessengerInstance.Register<AssetItem>(this, AssetTreeViewModel.AssetSelectedEvent, OnSelectAsset);
				MessengerInstance.Register<object>(this, MainViewModel.CleanupEvent, unused =>
				{
					Asset = null;
					RaisePropertyChanged("Asset");
				});

				ExportCommand = new RelayCommand(OnExport);

				ExportSingleAssetCommand = new RelayCommand(ExportSingleAsset);

				_exportSaveFileDialog = new SaveFileDialog();
				_exportSaveFileDialog.CreatePrompt = false;
				_exportSaveFileDialog.OverwritePrompt = true;
				_exportSaveFileDialog.AddExtension = true;
				_exportSaveFileDialog.Filter = "TPAC (*.tpac)|*.tpac";
				_exportSaveFileDialog.FilterIndex = 1;
				_exportSaveFileDialog.Title = Resources.SaveFileDialog_SelectSaveFile;
			}
		}

		private void OnSelectAsset(AssetItem assetItem)
		{
			var texture = assetItem as Texture;
			if (texture != null)
			{
				Asset = texture;
				IsExportable = Asset.HasPixelData;
				SuggestedFormat = IsExportable ? string.Format(Resources.Texture_Export_SuggestedFormat, 
												TextureIOUtil.GetSuggestedFormat(Asset.Format)) : string.Empty;
				RaisePropertyChanged("Asset");
				RaisePropertyChanged("IsExportable");
				RaisePropertyChanged("SuggestedFormat");
			}
		}

		private void OnExport()
		{
			if (Asset == null || !IsExportable)
				return;
			_saveFileDialog.FileName = Asset.Name;
			if (_saveFileDialog.ShowDialog().GetValueOrDefault(false))
			{
				var path = _saveFileDialog.FileName;
				if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !Asset.Format.IsSupported())
				{
					MessageBox.Show("Cannot export this texture in png format. Try dds instead.", 
									"Error", MessageBoxButton.OK, MessageBoxImage.Stop);
					return;
				}
				MessengerInstance.Send(string.Format("Export {0} ...", Asset.Name), MainViewModel.StatusEvent);
				TextureExporter.ExportToFile(path, Asset);
				MessengerInstance.Send(string.Format("{0} exported", Asset.Name), MainViewModel.StatusEvent);
			}
		}

		private void ExportSingleAsset()
		{
			if (Asset == null)
				return;

			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(Asset));
			if (source.Any())
			{
				var assetPackage = source.First();
				var suffix = AssetPackage.GetTypeSuffix(Asset.Type);
				_exportSaveFileDialog.FileName = Asset.Name + suffix;
				if (_exportSaveFileDialog.ShowDialog() == true)
				{
					var filePath = _exportSaveFileDialog.FileName;
					assetPackage.ExportSingleAsset(Asset, System.IO.Path.GetDirectoryName(filePath), System.IO.Path.GetFileNameWithoutExtension(filePath));
					MessengerInstance.Send<object>(null, AssetTreeViewModel.RefreshEvent);
				}
			}
		}
	}
}