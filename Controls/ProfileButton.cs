using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Where.Controls
{
	public class ProfileButton : Button
	{
		public string Subtext
		{
			get { return (string)GetValue(SubtextProperty); }
			set { SetValue(SubtextProperty, value); }
		}



		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(ICommand), typeof(ProfileButton), new PropertyMetadata(null, OnCommandChanged));

		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (ProfileButton)d;

			var newCommand = e.NewValue as ICommand;
			if (newCommand != null)
			{

				newCommand.CanExecute(button.CommandParameter);
				button.IsEnabled = newCommand.CanExecute(button.CommandParameter);
			}
			else
			{
				var oldCommand = e.OldValue as ICommand;
				if (oldCommand != null)
					oldCommand.CanExecuteChanged -= button.ExecuteChanged;
			}

		}

		private void ExecuteChanged(object sender, EventArgs e)
		{
			IsEnabled = Command.CanExecute(CommandParameter);
		}

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CommandParameter.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.Register("CommandParameter", typeof(object), typeof(ProfileButton), new PropertyMetadata(null));


		// Using a DependencyProperty as the backing store for Subtext.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SubtextProperty =
			DependencyProperty.Register("Subtext", typeof(string), typeof(ProfileButton), new PropertyMetadata(null));

		public ProfileButton()
		{
			DefaultStyleKey = typeof(ProfileButton);
		}

		protected override void OnClick()
		{
			base.OnClick();
			if (Command != null)
			{
				Command.Execute(CommandParameter);
			}
		}



	}
}
