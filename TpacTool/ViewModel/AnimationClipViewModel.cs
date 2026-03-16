using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using CommonServiceLocator;
using Ookii.Dialogs.Wpf;
using TpacTool.Lib;
using TpacTool.Properties;

namespace TpacTool
{
	public class AnimationClipViewModel : ViewModelBase
	{
		private static AnimationClip _copiedClip = null;

		private SaveFileDialog _saveFileDialog;

		private AnimationClip _selectedClip;

		public AnimationClip SelectedClip
		{
			set
			{
				_selectedClip = value;
				RaisePropertyChanged(nameof(ClipExists));
				RaisePropertyChanged(nameof(ShowExportButton));
				RaisePropertyChanged(nameof(ClipName));
				RaisePropertyChanged(nameof(ClipDuration));
				RaisePropertyChanged(nameof(ClipSource1));
				RaisePropertyChanged(nameof(ClipSource2));
				RaisePropertyChanged(nameof(ClipParam1));
				RaisePropertyChanged(nameof(ClipParam2));
				RaisePropertyChanged(nameof(ClipParam3));
				RaisePropertyChanged(nameof(ClipPriority));
				RaisePropertyChanged(nameof(ClipAnimationName));
				RaisePropertyChanged(nameof(ClipAnimationResolvedName));
				RaisePropertyChanged(nameof(ClipStepPoints));
				RaisePropertyChanged(nameof(ClipSoundCode));
				RaisePropertyChanged(nameof(ClipVoiceCode));
				RaisePropertyChanged(nameof(ClipFacialAnimationId));
				RaisePropertyChanged(nameof(ClipBlendsWithAction));
				RaisePropertyChanged(nameof(ClipContinueWithAction));
				RaisePropertyChanged(nameof(ClipLeftHandPose));
				RaisePropertyChanged(nameof(ClipRightHandPose));
				RaisePropertyChanged(nameof(ClipCombatParameterId));
				RaisePropertyChanged(nameof(ClipBlendInPeriod));
				RaisePropertyChanged(nameof(ClipBlendOutPeriod));
				RaisePropertyChanged(nameof(ClipDoNotInterpolate));
				RaisePropertyChanged(nameof(ClipGeneratedIndex));
				RaisePropertyChanged(nameof(ClipUnknownClipName));
				RaisePropertyChanged(nameof(ClipSource1Name));
				RaisePropertyChanged(nameof(ClipSource2Name));
				RaisePropertyChanged(nameof(ClipFlags));
				RaisePropertyChanged(nameof(ClipUsages));
			}
			get => _selectedClip;
		}

		public bool ClipExists => _selectedClip != null;

		public bool ShowExportButton
		{
			get
			{
				if (_selectedClip == null)
					return false;
				var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
				var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(_selectedClip));
				return source.Any(p => p.Items.Count > 1);
			}
		}

		public string ClipName => _selectedClip?.Name ?? string.Empty;

		public string ClipDuration => _selectedClip != null ? _selectedClip.Duration.ToString("F3") : string.Empty;

		public string ClipSource1 => _selectedClip != null ? _selectedClip.Source1.ToString("F3") : string.Empty;

		public string ClipSource2 => _selectedClip != null ? _selectedClip.Source2.ToString("F3") : string.Empty;

		public string ClipParam1 => _selectedClip != null ? _selectedClip.Param1.ToString("F3") : string.Empty;

		public string ClipParam2 => _selectedClip != null ? _selectedClip.Param2.ToString("F3") : string.Empty;

		public string ClipParam3 => _selectedClip != null ? _selectedClip.Param3.ToString("F3") : string.Empty;

		public string ClipPriority => _selectedClip != null ? _selectedClip.Priority.ToString() : string.Empty;

		public string ClipAnimationName => _selectedClip != null && _selectedClip.Animation != Guid.Empty
			? _selectedClip.Animation.ToString() : string.Empty;

		public string ClipAnimationResolvedName
		{
			get
			{
				if (_selectedClip == null || _selectedClip.Animation == Guid.Empty)
					return string.Empty;
				var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
				var animation = mainVm.AssetManager.LoadedAssets.FirstOrDefault(a => a.Guid == _selectedClip.Animation);
				return animation?.Name ?? string.Empty;
			}
		}

		public string ClipStepPoints => _selectedClip != null ? _selectedClip.StepPoints.ToString() : string.Empty;

		public string ClipSoundCode => _selectedClip?.SoundCode ?? string.Empty;

		public string ClipVoiceCode => _selectedClip?.VoiceCode ?? string.Empty;

		public string ClipFacialAnimationId => _selectedClip?.FacialAnimationId ?? string.Empty;

		public string ClipBlendsWithAction => _selectedClip?.BlendsWithAction ?? string.Empty;

		public string ClipContinueWithAction => _selectedClip?.ContinueWithAction ?? string.Empty;

		public string ClipLeftHandPose => _selectedClip != null ? _selectedClip.LeftHandPose.ToString() : string.Empty;

		public string ClipRightHandPose => _selectedClip != null ? _selectedClip.RightHandPose.ToString() : string.Empty;

		public string ClipCombatParameterId => _selectedClip?.CombatParameterId ?? string.Empty;

		public string ClipBlendInPeriod => _selectedClip != null ? _selectedClip.BlendInPeriod.ToString("F3") : string.Empty;

		public string ClipBlendOutPeriod => _selectedClip != null ? _selectedClip.BlendOutPeriod.ToString("F3") : string.Empty;

		public string ClipDoNotInterpolate => _selectedClip != null ? _selectedClip.DoNotInterpolate.ToString() : string.Empty;

		public string ClipGeneratedIndex => _selectedClip != null ? _selectedClip.GeneratedIndex.ToString() : string.Empty;

		public string ClipUnknownClipName => _selectedClip?.UnknownClipName ?? string.Empty;

		public string ClipSource1Name => _selectedClip?.ClipSource1Name ?? string.Empty;

		public string ClipSource2Name => _selectedClip?.ClipSource2Name ?? string.Empty;

		public List<string> ClipFlags => _selectedClip?.Flags ?? new List<string>();

		public List<AnimationClip.ClipUsage> ClipUsages => _selectedClip?.ClipUsages ?? new List<AnimationClip.ClipUsage>();

		public bool CanPasteProperties => _copiedClip != null;

		public ICommand CopyPropertiesCommand { get; private set; }

		public ICommand PastePropertiesCommand { get; private set; }

		public ICommand SaveTPACCommand { get; private set; }

		public ICommand SaveTPACAsCommand { get; private set; }

		public ICommand ExportSingleAssetCommand { get; private set; }

		private VistaFolderBrowserDialog _folderBrowserDialog;

		private SaveFileDialog _exportSaveFileDialog;

		public AnimationClipViewModel()
		{
			if (!IsInDesignMode)
			{
				_saveFileDialog = new SaveFileDialog();
				_saveFileDialog.CreatePrompt = false;
				_saveFileDialog.OverwritePrompt = true;
				_saveFileDialog.AddExtension = true;
				_saveFileDialog.Filter = "TPAC (*.tpac)|*.tpac";
				_saveFileDialog.FilterIndex = 1;
				_saveFileDialog.Title = Resources.SaveFileDialog_SelectSaveFile;

				_folderBrowserDialog = new VistaFolderBrowserDialog();

				_exportSaveFileDialog = new SaveFileDialog();
				_exportSaveFileDialog.CreatePrompt = false;
				_exportSaveFileDialog.OverwritePrompt = true;
				_exportSaveFileDialog.AddExtension = true;
				_exportSaveFileDialog.Filter = "TPAC (*.tpac)|*.tpac";
				_exportSaveFileDialog.FilterIndex = 1;
				_exportSaveFileDialog.Title = Resources.SaveFileDialog_SelectSaveFile;

				CopyPropertiesCommand = new RelayCommand(CopyClip);
				PastePropertiesCommand = new RelayCommand(PasteClip);
				SaveTPACCommand = new RelayCommand(SaveTPAC);
				SaveTPACAsCommand = new RelayCommand(SaveTPACAs);
				ExportSingleAssetCommand = new RelayCommand(ExportSingleAsset);

				MessengerInstance.Register<AssetItem>(this, AssetTreeViewModel.AssetSelectedEvent, asset =>
				{
					if (asset is AnimationClip clip)
						SelectedClip = clip;
					else
						SelectedClip = null;
				});

				MessengerInstance.Register<object>(this, MainViewModel.CleanupEvent, _ =>
				{
					SelectedClip = null;
					_copiedClip = null;
					RaisePropertyChanged(nameof(CanPasteProperties));
				});
			}
		}

		private void CopyClip()
		{
			if (_selectedClip != null)
			{
				_copiedClip = _selectedClip;
				RaisePropertyChanged(nameof(CanPasteProperties));
			}
		}

		private void PasteClip()
		{
			if (_copiedClip == null || _selectedClip == null)
				return;

			_selectedClip.Name = _copiedClip.Name;
			_selectedClip.Duration = _copiedClip.Duration;
			_selectedClip.Source1 = _copiedClip.Source1;
			_selectedClip.Source2 = _copiedClip.Source2;
			_selectedClip.Param1 = _copiedClip.Param1;
			_selectedClip.Param2 = _copiedClip.Param2;
			_selectedClip.Param3 = _copiedClip.Param3;
			_selectedClip.Priority = _copiedClip.Priority;
			//_selectedClip.Animation = _copiedClip.Animation;
			_selectedClip.StepPoints = _copiedClip.StepPoints;
			_selectedClip.SoundCode = _copiedClip.SoundCode;
			_selectedClip.VoiceCode = _copiedClip.VoiceCode;
			_selectedClip.FacialAnimationId = _copiedClip.FacialAnimationId;
			_selectedClip.BlendsWithAction = _copiedClip.BlendsWithAction;
			_selectedClip.ContinueWithAction = _copiedClip.ContinueWithAction;
			_selectedClip.LeftHandPose = _copiedClip.LeftHandPose;
			_selectedClip.RightHandPose = _copiedClip.RightHandPose;
			_selectedClip.CombatParameterId = _copiedClip.CombatParameterId;
			_selectedClip.BlendInPeriod = _copiedClip.BlendInPeriod;
			_selectedClip.BlendOutPeriod = _copiedClip.BlendOutPeriod;
			_selectedClip.DoNotInterpolate = _copiedClip.DoNotInterpolate;

			RaisePropertyChanged(nameof(ClipName));
			RaisePropertyChanged(nameof(ClipDuration));
			RaisePropertyChanged(nameof(ClipSource1));
			RaisePropertyChanged(nameof(ClipSource2));
			RaisePropertyChanged(nameof(ClipParam1));
			RaisePropertyChanged(nameof(ClipParam2));
			RaisePropertyChanged(nameof(ClipParam3));
			RaisePropertyChanged(nameof(ClipPriority));
			RaisePropertyChanged(nameof(ClipAnimationName));
			RaisePropertyChanged(nameof(ClipAnimationResolvedName));
			RaisePropertyChanged(nameof(ClipStepPoints));
			RaisePropertyChanged(nameof(ClipSoundCode));
			RaisePropertyChanged(nameof(ClipVoiceCode));
			RaisePropertyChanged(nameof(ClipFacialAnimationId));
			RaisePropertyChanged(nameof(ClipBlendsWithAction));
			RaisePropertyChanged(nameof(ClipContinueWithAction));
			RaisePropertyChanged(nameof(ClipLeftHandPose));
			RaisePropertyChanged(nameof(ClipRightHandPose));
			RaisePropertyChanged(nameof(ClipCombatParameterId));
			RaisePropertyChanged(nameof(ClipBlendInPeriod));
			RaisePropertyChanged(nameof(ClipBlendOutPeriod));
			RaisePropertyChanged(nameof(ClipDoNotInterpolate));
		}

		private void SaveTPAC()
		{
			if (_selectedClip == null)
				return;

			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(_selectedClip));
			if (source.Any())
			{
				var assetPackage = source.First();
				if (assetPackage.File != null)
				{
					if (!ConfirmSave(assetPackage))
						return;
					assetPackage.Save();
					MessengerInstance.Send<object>(null, AssetTreeViewModel.RefreshEvent);
				}
			}
		}

		private void SaveTPACAs()
		{
			if (_selectedClip == null)
				return;

			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(_selectedClip));
			if (source.Any())
			{
				var assetPackage = source.First();
				if (!ConfirmSave(assetPackage))
					return;
				_saveFileDialog.FileName = "generated_" + assetPackage.File?.Name ?? "untitled";
				if (_saveFileDialog.ShowDialog() == true)
				{
					var fileName = _saveFileDialog.FileName;
					assetPackage.Save(fileName);
					MessengerInstance.Send<object>(null, AssetTreeViewModel.RefreshEvent);
				}
			}
		}

		private bool ConfirmSave(AssetPackage assetPackage)
		{
			if (assetPackage.Items.Count > 1)
			{
				var result = MessageBox.Show(
					"此TPAC文件包含多个资源，保存将覆盖所有资源。\n其他资源类型的保存功能可能尚未开发，继续保存可能导致文件损坏！\n建议使用导出功能。\n确认保存吗？",
					"警告",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);
				return result == MessageBoxResult.Yes;
			}
			return true;
		}

		private void ExportSingleAsset()
		{
			if (_selectedClip == null)
				return;

			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(_selectedClip));
			if (source.Any())
			{
				var assetPackage = source.First();
				var suffix = AssetPackage.GetTypeSuffix(_selectedClip.Type);
				_exportSaveFileDialog.FileName = _selectedClip.Name + suffix;
				if (_exportSaveFileDialog.ShowDialog() == true)
				{
					var filePath = _exportSaveFileDialog.FileName;
					assetPackage.ExportSingleAsset(_selectedClip, System.IO.Path.GetDirectoryName(filePath), System.IO.Path.GetFileNameWithoutExtension(filePath));
					MessengerInstance.Send<object>(null, AssetTreeViewModel.RefreshEvent);
				}
			}
		}
	}
}
