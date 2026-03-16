using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using TpacTool.Lib;

namespace TpacTool
{
	public class AssetTreeViewModel : ViewModelBase
	{
		public static readonly Guid AssetSelectedEvent = Guid.NewGuid();

		public static readonly Guid RefreshEvent = Guid.NewGuid();

		private bool _isPackageMode;

		private string _filterText = string.Empty;

		public List<AssetViewModel> PackageNodes { private set; get; }

		public List<AssetViewModel> AssetNodes { private set; get; }

		public ICommand ClearFilterCommand { private set; get; }

		public ICommand OpenInExplorerCommand { private set; get; }

		public ICommand CopyPathCommand { private set; get; }

		public string FilterText
		{
			set
			{
				if (_filterText != value)
				{
					_filterText = value;
					RaisePropertyChanged("FilterText");

					UpdateAssetTree();
				}
			}
			get
			{
				return _filterText;
			}
		}

		public bool IsPackageMode
		{
			set
			{
				_isPackageMode = value;
				UpdateAssetTree();
			}
			get => _isPackageMode;
		}

		private void UpdateAssetTree()
		{
			IEnumerable<AssetViewModel> list = null;

			if (_isPackageMode)
				list = PackageNodes;
			else
				list = AssetNodes;

			if (!string.IsNullOrWhiteSpace(_filterText))
				list = list.AsParallel().AsOrdered().Where(vm => vm.Filter(_filterText)).AsSequential();
			else
			{
				foreach (var assetViewModel in list)
				{
					assetViewModel.ClearFilter();
				}
			}

			TreeItemSource = list;
			RaisePropertyChanged("TreeItemSource");
		}

		public override void Cleanup()
		{
			PackageNodes.Clear();
			AssetNodes.Clear();
			FilterText = string.Empty;
			base.Cleanup();
		}

		public IEnumerable<AssetViewModel> TreeItemSource { private set; get; }

		private string _selectedFilePath;

		public string SelectedFilePath
		{
			get => _selectedFilePath;
			private set
			{
				_selectedFilePath = value;
				RaisePropertyChanged("SelectedFilePath");
			}
		}

		public AssetTreeViewModel(AssetManager manager, Guid typeGuid)
		{
			ClearFilterCommand = new RelayCommand(() => { FilterText = string.Empty; });
			OpenInExplorerCommand = new RelayCommand(OpenInExplorer, () => !string.IsNullOrEmpty(SelectedFilePath));
			CopyPathCommand = new RelayCommand(CopyPath, () => !string.IsNullOrEmpty(SelectedFilePath));

			MessengerInstance.Register<object>(this, RefreshEvent, _ => UpdateAssetTree());

			PackageNodes = new List<AssetViewModel>();
			AssetNodes = new List<AssetViewModel>();
			var tempList = new List<AssetViewModel>();

			foreach (var package in manager.LoadedPackages)
			{
				//var name = package.File.Name;
				bool hasAsset = false;
				var packageNode = new AssetViewModel.Package(this, package);
				foreach (var asset in package.Items)
				{
					if (asset.Type == typeGuid)
					{
						AssetViewModel assetNode = null;
						assetNode = new AssetViewModel.Item(this, asset);
						/*if (typeGuid == Texture.TYPE_GUID)	
							assetNode = new TextureTreeNode() { Asset = asset };
						else
							assetNode = new AssetTreeNode() { Asset = asset };*/
						AssetNodes.Add(assetNode);
						tempList.Add(assetNode);
						hasAsset = true;
					}
				}

				if (hasAsset)
				{
					tempList.Sort((left, right) =>
						{
							return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
						});
					packageNode.AddRange(tempList);
					PackageNodes.Add(packageNode);
				}
				tempList.Clear();
			}

			PackageNodes.Sort((left, right) =>
				{
					return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
				});
			AssetNodes.Sort((left, right) =>
				{
					return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
				});

			IsPackageMode = true;
		}

		public void SelectAsset(AssetItem assetItem)
		{
			SelectedFilePath = assetItem?.FilePath;
			MessengerInstance.Send(assetItem, AssetSelectedEvent);
		}

		private void OpenInExplorer()
		{
			if (!string.IsNullOrEmpty(SelectedFilePath))
			{
				try
				{
					if (File.Exists(SelectedFilePath))
					{
						Process.Start("explorer.exe", "/select,\"" + SelectedFilePath + "\"");
					}
					else
					{
						var directory = Path.GetDirectoryName(SelectedFilePath);
						if (Directory.Exists(directory))
						{
							Process.Start("explorer.exe", directory);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void CopyPath()
		{
			if (!string.IsNullOrEmpty(SelectedFilePath))
			{
				Clipboard.SetText(SelectedFilePath);
			}
		}
	}
}