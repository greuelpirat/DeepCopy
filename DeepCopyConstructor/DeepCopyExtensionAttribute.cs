using System;

namespace DeepCopyConstructor
{
    /// <inheritdoc />
    /// <summary>
    /// Mark an Method-Extension as a deep copy method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DeepCopyExtensionAttribute : Attribute
    {
        /// <summary>
        /// If inheritance is enabled, than the deep copy will create the copy from the same type as the target.
        /// </summary>
        public bool Inheritance { get; set; } = true;
    }
}