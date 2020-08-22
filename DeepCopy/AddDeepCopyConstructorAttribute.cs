using System;

namespace DeepCopy
{
    /// <summary>
    /// Add a DeepCopy to the class
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AddDeepCopyConstructorAttribute : Attribute { }
}