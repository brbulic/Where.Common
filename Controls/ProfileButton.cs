using System.Windows;
using System.Windows.Controls.Primitives;

namespace Where.Controls
{
	public class ProfileButton : ButtonBase
	{
		public string Subtext
		{
			get { return (string)GetValue(SubtextProperty); }
			set { SetValue(SubtextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Subtext.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SubtextProperty =
			DependencyProperty.Register("Subtext", typeof(string), typeof(ProfileButton), new PropertyMetadata(null));

		public ProfileButton()
		{
			DefaultStyleKey = typeof(ProfileButton);
		}
	}
}
