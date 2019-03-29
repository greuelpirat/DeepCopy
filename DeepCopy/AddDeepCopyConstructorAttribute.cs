using System;

namespace DeepCopy
{
    /// <summary>
    /// Add a DeepCopy to the class
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class)]
    public class AddDeepCopyConstructorAttribute : Attribute { }
}