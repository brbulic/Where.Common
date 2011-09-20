using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Where
{
	public partial class Utils
	{
		/// <summary>
		/// Returns all VisualTree elements of a type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="panel"></param>
		/// <returns></returns>
		public static IList<T> GetAllVisualTreeElementsOfType<T>(this DependencyObject panel) where T : DependencyObject
		{
			var elements = new List<T>();

			var childrenCount = VisualTreeHelper.GetChildrenCount(panel);

			if (childrenCount == 1)
				elements.AddRange(GetAllVisualTreeElementsOfType<T>(VisualTreeHelper.GetChild(panel, 0)));
			else if (childrenCount > 1)
				for (var i = 0; i < childrenCount; i++)
				{
					var child = VisualTreeHelper.GetChild(panel, i);

					if (child is T)
					{
						elements.Add(child as T);
					}
					else
						elements.AddRange(child.GetAllVisualTreeElementsOfType<T>());
				}

			return elements;

		}
	}
}