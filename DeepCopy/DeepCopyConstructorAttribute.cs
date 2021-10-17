using System;

namespace DeepCopy
{
    /// <summary>
    ///     Injects deep copy functionality to an existing copy constructor
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Constructor)]
    public class DeepCopyConstructorAttribute : Attribute { }
}