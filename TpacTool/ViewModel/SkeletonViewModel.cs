using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using TpacTool.Lib;
using TpacTool.Properties;

namespace TpacTool
{
	public class SkeletonViewModel : ViewModelBase
	{
		public static readonly Uri Uri_Page_BoneBody = new Uri("SkeletonBodyPage.xaml", UriKind.Relative);

		public static readonly Uri Uri_Page_D6Joint = new Uri("SkeletonD6JointPage.xaml", UriKind.Relative);

		public static readonly Uri Uri_Page_IKJoint = new Uri("SkeletonIKJointPage.xaml", UriKind.Relative);

		private Skeleton _humanSkeleton;

		private Skeleton _horseSkeleton;

		private List<Skeleton> _skeletons = new List<Skeleton>();

		private SaveFileDialog _saveFileDialog;

		private Skeleton _selectedSkeleton;

		private List<SkeletonUserData.Body> _skeletonBones;

		private SkeletonUserData.Body _selectedBone;

		private List<SkeletonUserData.D6JointConstraint> _skeletonD6Joints;

		private SkeletonUserData.D6JointConstraint _selectedD6Joint;

		private List<SkeletonUserData.IKConstraint> _skeletonIKJoints;

		private SkeletonUserData.IKConstraint _selectedIKJoint;

		private SkeletonUserData _copiedUserData;

		private Uri _currentPageUri;

		public SkeletalAnimation CopiedSkeletalAnimation { get; set; }

		private List<SkeletalAnimation> _allHumanSkeletalAnimations;

		private Dictionary<AnimationClip, SkeletalAnimation> _allHumanAnimationClips;

		public Skeleton Asset { get; private set; }

		public Skeleton SelectedSkeleton
		{
			get => _selectedSkeleton;
			private set
			{
				_selectedSkeleton = value;
				if (_selectedSkeleton != null)
				{
					SkeletonBones = _selectedSkeleton.UserData.Data.Bodies;
					SkeletonD6Joints = _selectedSkeleton.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
					SkeletonIKJoints = _selectedSkeleton.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
				}
				else
				{
					SkeletonBones = null;
					SkeletonD6Joints = null;
					SkeletonIKJoints = null;
				}
				RaisePropertyChanged(nameof(SelectedSkeleton));
				RaisePropertyChanged(nameof(SkeletonBones));
			}
		}

		public IEnumerable<SkeletonUserData.Body> SkeletonBones
		{
			get => _skeletonBones;
			private set
			{
				if (value != null)
					_skeletonBones = value.ToList();
				else
					_skeletonBones = null;
				RaisePropertyChanged(nameof(SkeletonBones));
			}
		}

		public SkeletonUserData.Body SelectedBone
		{
			get => _selectedBone;
			set
			{
				_selectedBone = value;
				RaisePropertyChanged(nameof(SelectedBone));
			}
		}

		public IEnumerable<SkeletonUserData.D6JointConstraint> SkeletonD6Joints
		{
			get => _skeletonD6Joints;
			private set
			{
				if (value != null)
					_skeletonD6Joints = value.ToList();
				else
					_skeletonD6Joints = null;
				RaisePropertyChanged(nameof(SkeletonD6Joints));
			}
		}

		public SkeletonUserData.D6JointConstraint SelectedD6Joint
		{
			get => _selectedD6Joint;
			set
			{
				_selectedD6Joint = value;
				RaisePropertyChanged(nameof(SelectedD6Joint));
			}
		}

		public IEnumerable<SkeletonUserData.IKConstraint> SkeletonIKJoints
		{
			get => _skeletonIKJoints;
			private set
			{
				if (value != null)
					_skeletonIKJoints = value.ToList();
				else
					_skeletonIKJoints = null;
				RaisePropertyChanged(nameof(SkeletonIKJoints));
			}
		}

		public SkeletonUserData.IKConstraint SelectedIKJoint
		{
			get => _selectedIKJoint;
			set
			{
				_selectedIKJoint = value;
				RaisePropertyChanged(nameof(SelectedIKJoint));
			}
		}

		public Uri CurrentPageUri
		{
			get => _currentPageUri;
			set
			{
				if (value != null)
				{
					_currentPageUri = value;
					RaisePropertyChanged(nameof(CurrentPageUri));
				}
			}
		}

		public string AnimationExportPrefix { get; set; } = "prefix_";

		public bool CanPasteProperties => _copiedUserData != null;

		public bool CanGenerateWithCopiedFrame => CopiedSkeletalAnimation != null;

		public ICommand ChangeSkeletonCommand { get; private set; }

		public ICommand SaveTPACCommand { get; private set; }

		public ICommand SaveTPACAsCommand { get; private set; }

		public ICommand ExportSingleAssetCommand { get; private set; }

		private VistaFolderBrowserDialog _folderBrowserDialog;

		private SaveFileDialog _exportSaveFileDialog;

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

		public ICommand CopyPropertiesCommand { get; private set; }

		public ICommand PastePropertiesCommand { get; private set; }

		public ICommand GenerateHumanAnimationsCommand { get; private set; }

		public ICommand GenerateHumanAnimationsWithCopiedFrameCommand { get; private set; }

		public ICommand SaveHumanSkeletonCommand { get; private set; }

		public ICommand AddBoneBodyCommand { get; private set; }

		public ICommand RemoveBoneBodyCommand { get; private set; }

		public ICommand AddD6JointCommand { get; private set; }

		public ICommand RemoveD6JointCommand { get; private set; }

		public ICommand AddIKJointCommand { get; private set; }

		public ICommand RemoveIKJointCommand { get; private set; }

		public ICommand SelectBonesPageCommand { get; private set; }

		public ICommand SelectD6JointsPageCommand { get; private set; }

		public ICommand SelectIKJointsPageCommand { get; private set; }

		public static readonly Guid UpdateSkeletonListEvent = ModelViewModel.UpdateSkeletonListEvent;

		public SkeletonViewModel()
		{
			if (!IsInDesignMode)
			{
				CurrentPageUri = Uri_Page_BoneBody;
				_allHumanSkeletalAnimations = new List<SkeletalAnimation>();
				_allHumanAnimationClips = new Dictionary<AnimationClip, SkeletalAnimation>();
				_saveFileDialog = new SaveFileDialog();
				_saveFileDialog.CreatePrompt = false;
				_saveFileDialog.OverwritePrompt = true;
				_saveFileDialog.AddExtension = true;
				_saveFileDialog.DefaultExt = "tpac";
				_saveFileDialog.Title = "Select path for created packages";

				_folderBrowserDialog = new VistaFolderBrowserDialog();

				_exportSaveFileDialog = new SaveFileDialog();
				_exportSaveFileDialog.CreatePrompt = false;
				_exportSaveFileDialog.OverwritePrompt = true;
				_exportSaveFileDialog.AddExtension = true;
				_exportSaveFileDialog.Filter = "TPAC (*.tpac)|*.tpac";
				_exportSaveFileDialog.FilterIndex = 1;
				_exportSaveFileDialog.Title = Resources.SaveFileDialog_SelectSaveFile;

				ChangeSkeletonCommand = new RelayCommand<string>(arg =>
				{
					if (Enum.TryParse<SkeletonType>(arg, ignoreCase: true, out var _))
					{
					}
					RaisePropertyChanged(nameof(IsSkeletonHuman));
					RaisePropertyChanged(nameof(IsSkeletonHorse));
					RaisePropertyChanged(nameof(IsSkeletonOther));
					RaisePropertyChanged(nameof(IsSkeletonOtherAndRigged));
				});
				SaveTPACCommand = new RelayCommand(SaveTPAC);
				SaveTPACAsCommand = new RelayCommand(SaveTPACAs);
				ExportSingleAssetCommand = new RelayCommand(ExportSingleAsset);
				CopyPropertiesCommand = new RelayCommand(CopyJoints);
				PastePropertiesCommand = new RelayCommand(PasteJoints);
				GenerateHumanAnimationsCommand = new RelayCommand(GenerateHumanAnimations);
				GenerateHumanAnimationsWithCopiedFrameCommand = new RelayCommand(GenerateHumanAnimationsWithCopiedFrame);
				SaveHumanSkeletonCommand = new RelayCommand(SaveHumanSkeleton);
				AddBoneBodyCommand = new RelayCommand(AddBoneBody);
				RemoveBoneBodyCommand = new RelayCommand(RemoveBoneBody);
				AddD6JointCommand = new RelayCommand(AddD6Joint);
				RemoveD6JointCommand = new RelayCommand(RemoveD6Joint);
				AddIKJointCommand = new RelayCommand(AddIKJoint);
				RemoveIKJointCommand = new RelayCommand(RemoveIKJoint);
				SelectBonesPageCommand = new RelayCommand(() => { CurrentPageUri = Uri_Page_BoneBody; });
				SelectD6JointsPageCommand = new RelayCommand(() => { CurrentPageUri = Uri_Page_D6Joint; });
				SelectIKJointsPageCommand = new RelayCommand(() => { CurrentPageUri = Uri_Page_IKJoint; });

				MessengerInstance.Register<AssetItem>(this, AssetTreeViewModel.AssetSelectedEvent, OnSelectAsset);
				MessengerInstance.Register<object>(this, MainViewModel.CleanupEvent, _ =>
				{
					SelectedSkeleton = null;
					Asset = null;
					RaisePropertyChanged(nameof(SelectedSkeleton));
					RaisePropertyChanged(nameof(Asset));
				});
				MessengerInstance.Register<IEnumerable<Skeleton>>(this, UpdateSkeletonListEvent, UpdateSkeletonsAndAnimations);
			}
		}

		public bool IsSkeletonHuman => SelectedSkeleton?.UserData?.Data?.Usage == SkeletonUserData.USAGE_HUMAN;

		public bool IsSkeletonHorse => SelectedSkeleton?.UserData?.Data?.Usage == SkeletonUserData.USAGE_HORSE;

		public bool IsSkeletonOther => SelectedSkeleton?.UserData?.Data?.Usage == SkeletonUserData.USAGE_OTHER;

		public bool IsSkeletonOtherAndRigged => IsSkeletonOther && SelectedSkeleton?.Definition?.Data?.Bones?.Count > 0;

		private void UpdateSkeletonsAndAnimations(IEnumerable<Skeleton> skeletons)
		{
			var assetManager = ServiceLocator.Current.GetInstance<MainViewModel>().AssetManager;
			foreach (var skeleton in skeletons)
			{
				if (skeleton.Name == "human_skeleton")
				{
					_humanSkeleton = skeleton;
				}
				else if (skeleton.Name == "horse_skeleton")
				{
					_horseSkeleton = skeleton;
				}
			}
			if (_humanSkeleton == null)
			{
				return;
			}
			foreach (var item in assetManager.LoadedAssets.OfType<SkeletalAnimation>())
			{
				if (!(item.Skeleton != _humanSkeleton.Guid))
				{
					Console.WriteLine("Found Skeletal Animation: " + item.Name);
					_allHumanSkeletalAnimations.Add(item);
				}
			}
			foreach (var item2 in assetManager.LoadedAssets.OfType<AnimationClip>())
			{
				if (item2.GeneratedIndex == -1)
				{
					var asset = assetManager.GetAsset<SkeletalAnimation>(item2.Animation);
					if (asset != null && !(asset.Skeleton != _humanSkeleton.Guid))
					{
						Console.WriteLine("Found Animation Clip: " + item2.Name + " for Skeletal Animation: " + asset.Name);
						_allHumanAnimationClips.Add(item2, asset);
					}
				}
			}
		}

		private void OnSelectAsset(AssetItem assetItem)
		{
			if (assetItem is Skeleton skeleton)
			{
				Asset = skeleton;
				SelectedSkeleton = skeleton;
				SelectedBone = Asset.UserData.Data.Bodies.FirstOrDefault();
				SelectedD6Joint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>().FirstOrDefault();
				SelectedIKJoint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>().FirstOrDefault();
				RaisePropertyChanged(nameof(Asset));
				RaisePropertyChanged(nameof(ShowExportButton));
			}
		}

		private void CopyJoints()
		{
			if (SelectedSkeleton != null)
			{
				_copiedUserData = SelectedSkeleton.UserData.Data;
				RaisePropertyChanged(nameof(CanPasteProperties));
			}
		}

		private void PasteJoints()
		{
			if (_copiedUserData == null || Asset == null)
			{
				return;
			}
			var bodies = Asset.UserData.Data.Bodies;
			foreach (var copiedBody in _copiedUserData.Bodies)
			{
				var body = bodies.FirstOrDefault(b => b.BoneName == copiedBody.BoneName);
				if (body != null)
				{
					// 保留BoneName，只复制其他属性
					body.EnableBlend = copiedBody.EnableBlend;
					body.Type = copiedBody.Type;
					body.BodyType = copiedBody.BodyType;
					body.Mass = copiedBody.Mass;
					body.RagdollPosition1 = copiedBody.RagdollPosition1;
					body.RagdollPosition2 = copiedBody.RagdollPosition2;
					body.RagdollRadius = copiedBody.RagdollRadius;
					body.CollisionPosition1 = copiedBody.CollisionPosition1;
					body.CollisionPosition2 = copiedBody.CollisionPosition2;
					body.CollisionMaxRadius = copiedBody.CollisionMaxRadius;
					body.CollisionRadius = copiedBody.CollisionRadius;
				}
			}
			var constraints = Asset.UserData.Data.Constraints;
			foreach (var copiedConstraint in _copiedUserData.Constraints)
			{
				var constraint = constraints.FirstOrDefault(c =>
					c.Bone1 == copiedConstraint.Bone1 &&
					c.Bone2 == copiedConstraint.Bone2 &&
					c.GetType() == copiedConstraint.GetType());
				if (constraint != null)
				{
					// 保留Bone1和Bone2，复制约束的其他所有属性
					var index = constraints.IndexOf(constraint);
					var newConstraint = copiedConstraint;
					newConstraint.Bone1 = constraint.Bone1;
					newConstraint.Bone2 = constraint.Bone2;
					constraints[index] = newConstraint;
				}
				else if (bodies.Exists(b => b.BoneName == copiedConstraint.Bone1) &&
						 bodies.Exists(b => b.BoneName == copiedConstraint.Bone2))
				{
					constraints.Add(copiedConstraint);
				}
			}
			// 创建新的列表引用以触发UI更新
			SkeletonBones = bodies.ToList();
			RaisePropertyChanged(nameof(SkeletonD6Joints));
			RaisePropertyChanged(nameof(SkeletonIKJoints));
			// 同时触发SelectedBone更新以刷新显示
			var tempBone = SelectedBone;
			SelectedBone = null;
			SelectedBone = tempBone;
		}

		private void SaveTPAC()
		{
			if (Asset == null)
			{
				return;
			}
			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(Asset));
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
			if (Asset == null)
			{
				return;
			}
			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(Asset));
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

		private void SaveHumanSkeleton()
		{
			if (Asset == null || _humanSkeleton == null || Asset == _humanSkeleton)
			{
				return;
			}
			var mainVm = ServiceLocator.Current.GetInstance<MainViewModel>();
			var source = mainVm.AssetManager.LoadedPackages.Where(p => p.Items.Contains(_humanSkeleton));
			if (source.Any())
			{
				var assetPackage = source.First();
				_saveFileDialog.FileName = "generated_" + assetPackage.File.Name;
				if (_saveFileDialog.ShowDialog() == true)
				{
					var fileName = _saveFileDialog.FileName;
					_humanSkeleton.Definition.Data.Bones.Clear();
					_humanSkeleton.Definition.Data.Bones.AddRange(Asset.Definition.Data.Bones);
					_humanSkeleton.UserData.Data.Bodies.Clear();
					_humanSkeleton.UserData.Data.Bodies.AddRange(Asset.UserData.Data.Bodies);
					_humanSkeleton.UserData.Data.Constraints.Clear();
					_humanSkeleton.UserData.Data.Constraints.AddRange(Asset.UserData.Data.Constraints);
					assetPackage.Save(fileName);
				}
			}
		}

		private void GenerateHumanAnimations()
		{
			GenerateHumanAnimationsAux(false);
		}

		private void GenerateHumanAnimationsWithCopiedFrame()
		{
			if (CopiedSkeletalAnimation != null)
			{
				GenerateHumanAnimationsAux(true);
			}
		}

		private void GenerateHumanAnimationsAux(bool useCopiedFrame)
		{
			if (Asset != null && _humanSkeleton != null)
			{
				if (CopiedSkeletalAnimation == null)
				{
					useCopiedFrame = false;
				}
				string text = "";
				string fileName = "autogenerated.tpac";
				_saveFileDialog.FileName = fileName;
				if (_saveFileDialog.ShowDialog() == true)
				{
					text = Path.GetDirectoryName(_saveFileDialog.FileName);
					fileName = Path.GetFileName(_saveFileDialog.FileName);
					var skeletalAnimationsDictionary = GenerateSkeletalAnimations(text, fileName, useCopiedFrame);
					GenerateAnimationClips(text, skeletalAnimationsDictionary);
					MessageBox.Show("Done Exporting Animations");
				}
			}
		}

		private Dictionary<SkeletalAnimation, SkeletalAnimation> GenerateSkeletalAnimations(string path, string packageName, bool useCopiedFrame)
		{
			var list = new List<int>();
			for (int i = 0; i < Asset.Definition.Data.Bones.Count; i++)
			{
				var customSkeletonBone = Asset.Definition.Data.Bones[i];
				if (!_humanSkeleton.Definition.Data.Bones.Exists(b => b.Name == customSkeletonBone.Name))
				{
					Console.WriteLine("Added Extra Bone: " + customSkeletonBone.Name + " With index " + i);
					list.Add(i);
				}
			}
			var dictionary = new Dictionary<SkeletalAnimation, SkeletalAnimation>();
			foreach (var allHumanSkeletalAnimation in _allHumanSkeletalAnimations)
			{
				var skeletalAnimation = allHumanSkeletalAnimation.Clone() as SkeletalAnimation;
				skeletalAnimation.Guid = Guid.NewGuid();
				skeletalAnimation.Name = AnimationExportPrefix + skeletalAnimation.Name;
				skeletalAnimation.Definition.Data.Name = AnimationExportPrefix + skeletalAnimation.Definition.Data.Name;
				skeletalAnimation.BoneNum = Asset.UserData.Data.Bodies.Count;
				skeletalAnimation.Definition.OwnerGuid = skeletalAnimation.Guid;
				foreach (var item in list)
				{
					var boneAnim = new AnimationDefinitionData.BoneAnim();
					Quaternion identity = Quaternion.Identity;
					identity = !useCopiedFrame
						? Quaternion.CreateFromRotationMatrix(Asset.Definition.Data.Bones[item].RestFrame)
						: CopiedSkeletalAnimation.Definition.Data.BoneAnims[item].RotationFrames[1f].Value;
					foreach (var rotationFrame in skeletalAnimation.Definition.Data.BoneAnims[0].RotationFrames)
					{
						boneAnim.RotationFrames.Add(rotationFrame.Key, new AnimationFrame<Quaternion>(rotationFrame.Value.Time, identity));
					}
					skeletalAnimation.Definition.Data.BoneAnims.Add(boneAnim);
				}
				skeletalAnimation.Definition.Data.RootScaleFrames.Clear();
				skeletalAnimation.Skeleton = Asset.Guid;
				dictionary.Add(allHumanSkeletalAnimation, skeletalAnimation);
			}
			var assetPackage = new AssetPackage(Guid.NewGuid());
			assetPackage.Items.AddRange(dictionary.Values.ToList());
			assetPackage.Save(path + "\\" + packageName);
			return dictionary;
		}

		private void GenerateAnimationClips(string path, Dictionary<SkeletalAnimation, SkeletalAnimation> skeletalAnimationsDictionary)
		{
			foreach (var allHumanAnimationClip in _allHumanAnimationClips)
			{
				var key = allHumanAnimationClip.Key;
				var value = allHumanAnimationClip.Value;
				var animationClip = key.Clone() as AnimationClip;
				animationClip.Guid = Guid.NewGuid();
				animationClip.Name = AnimationExportPrefix + animationClip.Name;
				if (animationClip.Name.Length > 63)
				{
					int count = animationClip.Name.Length - 63;
					animationClip.Name = animationClip.Name.Remove(animationClip.Name.Length - 1, count);
				}
				if (!skeletalAnimationsDictionary.TryGetValue(value, out var value2))
				{
					throw new Exception("Generated Skeletal Animation not found for Original: " + value.Name);
				}
				animationClip.Animation = value2.Guid;
				if (!string.IsNullOrEmpty(animationClip.BlendsWithAction))
				{
					string text = AnimationExportPrefix + animationClip.BlendsWithAction;
					if (text.Length > 63)
					{
						int count2 = text.Length - 63;
						text = text.Remove(text.Length - 1, count2);
					}
					animationClip.BlendsWithAction = text;
				}
				var assetPackage = new AssetPackage(Guid.NewGuid());
				assetPackage.Items.Add(animationClip);
				assetPackage.Save(path + "\\clips\\" + animationClip.Name + "_anm.tpac");
			}
		}

		private void AddBoneBody()
		{
			var body = new SkeletonUserData.Body();
			body.BoneName = "new_bone_body";
			Asset.UserData.Data.Bodies.Add(body);
			SelectedBone = body;
			SkeletonBones = Asset.UserData.Data.Bodies;
		}

		private void RemoveBoneBody()
		{
			if (Asset.UserData.Data.Bodies.Contains(SelectedBone))
			{
				Asset.UserData.Data.Bodies.Remove(SelectedBone);
				SelectedBone = Asset.UserData.Data.Bodies.FirstOrDefault();
				SkeletonBones = Asset.UserData.Data.Bodies;
			}
		}

		private void AddD6Joint()
		{
			var d6JointConstraint = new SkeletonUserData.D6JointConstraint();
			d6JointConstraint.Bone1 = "write_bone_1_here";
			d6JointConstraint.Bone2 = "write_bone_2_here";
			d6JointConstraint.Name = "new_ik_joint";
			Asset.UserData.Data.Constraints.Add(d6JointConstraint);
			SelectedD6Joint = d6JointConstraint;
			SkeletonD6Joints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
		}

		private void RemoveD6Joint()
		{
			if (Asset.UserData.Data.Constraints.Contains(SelectedD6Joint))
			{
				Asset.UserData.Data.Constraints.Remove(SelectedD6Joint);
				SelectedD6Joint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>().FirstOrDefault();
				SkeletonD6Joints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.D6JointConstraint>();
			}
		}

		private void AddIKJoint()
		{
			var iKConstraint = new SkeletonUserData.IKConstraint();
			iKConstraint.Bone1 = "write_bone_1_here";
			iKConstraint.Bone2 = "write_bone_2_here";
			iKConstraint.Name = "new_ik_joint";
			Asset.UserData.Data.Constraints.Add(iKConstraint);
			SelectedIKJoint = iKConstraint;
			SkeletonIKJoints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
		}

		private void RemoveIKJoint()
		{
			if (Asset.UserData.Data.Constraints.Contains(SelectedIKJoint))
			{
				Asset.UserData.Data.Constraints.Remove(SelectedIKJoint);
				SelectedIKJoint = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>().FirstOrDefault();
				SkeletonIKJoints = Asset.UserData.Data.Constraints.OfType<SkeletonUserData.IKConstraint>();
			}
		}
	}
}