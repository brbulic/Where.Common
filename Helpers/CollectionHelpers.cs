using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace Where
{
	/// <summary>
	/// Helpers, et al.
	/// </summary>
	public partial class Utils
	{

		/// <summary>
		/// Extension method that checks if a collction is null or empty
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <returns></returns>
		public static bool CollectionNullOrEmpty<T>(this ICollection<T> collection) where T : class
		{
			if (collection == null)
				return true;

			return collection.Count == 0;
		}

		/// <summary>
		/// Runs an Async operation to add the elements from a source collection to an ObservableCollection that is bound to a ListBox.
		/// </summary>
		/// <typeparam name="T">Type in collection</typeparam>
		/// <param name="source">Source collection holding the data</param>
		/// <param name="destination">Data bound destination ObservableCollection </param>
		/// <param name="sleep">Time in miliseconds that the thread will sleep after adding <b>sleepAfter</b> elements</param>
		/// <param name="sleepAfter">Elements to add before sleeping for a <b>sleep</b> amount of time.</param>
		public static void AddElementsToObservableCollection<T>(IEnumerable<T> source, ObservableCollection<T> destination, int sleep = 35, int sleepAfter = 0)
		{
			//BackgroundWorkerDefault.QueueSimple(state =>
			//                                            {
			//                                                var enumerable = (IEnumerable<T>)state;
			//                                                var currentElement = 0;
			//                                                foreach (var element in enumerable)
			//                                                {
			//                                                    var dumb = element;
			//                                                    Deployment.Current.Dispatcher.BeginInvoke(() => destination.Add(dumb));
			//                                                    currentElement++;

			//                                                    if (sleepAfter > 0)
			//                                                    {
			//                                                        if (currentElement % sleepAfter == 0)
			//                                                            Thread.Sleep(sleep);
			//                                                    }
			//                                                    else
			//                                                    {
			//                                                        Thread.Sleep(sleep);
			//                                                    }
			//                                                }
			//                                            }, source);


			ThreadPool.QueueUserWorkItem(state =>
			{
				var enumerable = (IEnumerable<T>)state;
				var currentElement = 0;
				foreach (var element in enumerable)
				{
					var dumb = element;
					Deployment.Current.Dispatcher.BeginInvoke(() => destination.Add(dumb));
					currentElement++;

					if (sleepAfter > 0)
					{
						if (currentElement % sleepAfter == 0)
							Thread.Sleep(sleep);
					}
					else
					{
						Thread.Sleep(sleep);
					}
				}


			}, source);
		}
	}

}