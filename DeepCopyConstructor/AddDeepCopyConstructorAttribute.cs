using System;

namespace DeepCopyConstructor
{
    /// <summary>
    /// Add a DeepCopyConstructor to the class
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AddDeepCopyConstructorAttribute : Attribute
    {
    }
}