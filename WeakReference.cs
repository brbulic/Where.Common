using System;

namespace Where.Common
{

    /// <summary>
    /// References an object by type, still allowing it to be garbage collected
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakReference<T> where T : class
    {
        private readonly WeakReference _originalReference;


        /// <summary>
        /// Initializes a new instance of the WeakReference class, referencing the specified object.
        /// </summary>
        /// <param name="item">Specified object</param>
        public WeakReference(T item)
        {
            _originalReference = new WeakReference(item);
        }

        /// <summary>
        /// Gets indication weather the object has been garbage collected
        /// </summary>
        public bool IsAlive { get { return _originalReference.IsAlive; } }


        /// <summary>
        /// Gets the target referenced by the WeakReference
        /// </summary>
        public T Target
        {
            get
            {
                return (T)_originalReference.Target;
            }
            set
            {
                _originalReference.Target = value;
            }
        }
    }
}
