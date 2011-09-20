using System;
using Newtonsoft.Json;

namespace Where.Common.DataController
{
    /// <summary>
    /// Control serialization parameters of a property monitored by the SuperintendentDataCore
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false) ]
    public sealed class SerializableSharedDataAttribute : Attribute
    {
        /// <summary>
        /// True means that the property won't get serialized
        /// </summary>
        public bool IgnoreSerialization { get; set; }

        /// <summary>
        /// Gets or sets the json converter for a particular property when saving
        /// </summary>
        public JsonConverter Converter { get; set; }

        /// <summary>
        /// Use memory caching, avoid weak referencing and other memory optimisations strategies
        /// </summary>
        public bool CacheToMemory { get; set; }

        /// <summary>
        /// Automatically send a message on changed
        /// </summary>
        public bool AutoSendMessage { get; set; }

        public SerializableSharedDataAttribute()
        {
            IgnoreSerialization = false;
        }
    }
}
