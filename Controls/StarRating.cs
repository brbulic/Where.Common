using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Where.Controls
{
	public class StarRating : ContentControl
	{
		private enum StarRatingType
		{
			Full = 0,
			Half,
			Empty
		} ;

		public StarRating()
		{
			DefaultStyleKey = typeof(StarRating);
		}

		private StackPanel _baseStackPanel;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_baseStackPanel = (StackPanel)GetTemplateChild("StarRatingPanel");
			_baseStackPanel.Orientation = Orientation.Horizontal;
			DrawStars();
		}

		private void DrawStars()
		{

			if (Rating < 0)
				return;

			if (_baseStackPanel == null) return;

			_baseStackPanel.Children.Clear();
			var lower = (int)Math.Floor(Rating);
			var higher = (int)Math.Ceiling(Rating);

			if (Rating >= 6) throw new ArgumentOutOfRangeException("Rating cannot be equal or larger than 6", new Exception());
			for (var i = 0; i < lower; i++)
				_baseStackPanel.Children.Add(CreateImage(StarRatingType.Full, StarSize));
			if (Rating - lower > 0)
				_baseStackPanel.Children.Add(CreateImage(StarRatingType.Half, StarSize));
			//for (var i = higher; i < 5; i++)
			//    _baseStackPanel.Children.Add(CreateImage(StarRatingType.Empty, StarSize));
		}

		private static readonly Thickness Thickness = new Thickness(0);

		private static readonly BitmapImage HalfImage = new BitmapImage(new Uri("/Where.Common;component/Themes/appbar.favs-half.png", UriKind.Relative));
		private static readonly BitmapImage FullImage = new BitmapImage(new Uri("/Where.Common;component/Themes/appbar.favs.png", UriKind.Relative));

		private static Image CreateImage(StarRatingType ratingType, double starsize)
		{
			BitmapImage starUrl;
			switch (ratingType)
			{
				case StarRatingType.Full:
					starUrl = FullImage;
					break;
				case StarRatingType.Half:
					starUrl = HalfImage;
					break;
				default:
					throw new ArgumentOutOfRangeException("ratingType");
			}

			return new Image
							{
								Stretch = Stretch.UniformToFill,
								Margin = Thickness,
								Source = starUrl,
								Height = starsize

							};
		}

		public double Rating
		{
			get { return (double)GetValue(RatingProperty); }
			set { SetValue(RatingProperty, value); }
		}
		public static DependencyProperty RatingProperty = DependencyProperty.Register("Rating", typeof(double), typeof(StarRating), new PropertyMetadata(0.0, (depObj, args) => ((StarRating)depObj).DrawStars()));

		public int StarSize
		{
			get { return (int)GetValue(StarSizeProperty); }
			set { SetValue(StarSizeProperty, value); }
		}
		public static DependencyProperty StarSizeProperty = DependencyProperty.Register("StarSize", typeof(int), typeof(StarRating), new PropertyMetadata(25, (depObj, args) => ((StarRating)depObj).DrawStars()));
	}
}
