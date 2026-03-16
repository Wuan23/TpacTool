using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TpacTool
{
	public partial class SkeletonPage : Page, IComponentConnector
	{
		public SkeletonPage()
		{
			InitializeComponent();
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
		}
	}
}