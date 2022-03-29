using System;

namespace DeepCopy
{
    /// <summary>
    /// Add a DeepCopy to the class
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AddDeepCopyConstructorAttribute : Attribute
    {
        /// <summary>
        /// If a copy constructor already exists on the target type,
        /// it can be overwritten if this property is set to <c>true</c>  
        /// </summary>
        public bool Overwrite { get; set; }
    }
}