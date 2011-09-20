using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Where.Common.Controls
{
    public class SimpleTile : ContentControl
    {
        private static readonly Brush DefaultBrush = Application.Current.Resources["PhoneAccentBrush"] as Brush;


        public ImageSource BackgroundImage
        {
            get { return (ImageSource)GetValue(BackgroundImageProperty); }
            set { SetValue(BackgroundImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundImageProperty =
            DependencyProperty.Register("BackgroundImage", typeof(ImageSource), typeof(SimpleTile), new PropertyMetadata(null));


        public SimpleTile()
        {
            DefaultStyleKey = typeof(SimpleTile);

        }

        private Image _currentImageReference;
        
        private static void GetImageSourceImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                image.Source = null;
                image.ImageFailed -= GetImageSourceImageFailed;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _currentImageReference = (Image)GetTemplateChild("ImagePresenter");
            _currentImageReference.ImageOpened += GetImageSourceImageOpened;
            _currentImageReference.ImageFailed += GetImageSourceImageFailed;
        }

        private void GetImageSourceImageOpened(object sender, RoutedEventArgs e)
        {
            Background = null;
            var image = sender as Image;

            if (image != null)
            {
                image.ImageOpened -= GetImageSourceImageOpened;
            }
        }
    }

}
