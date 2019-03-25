using System;

namespace DeepCopyConstructor
{
    /// <inheritdoc />
    /// <summary>
    /// Mark an Method-Extension as a deep copy method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DeepCopyExtensionAttribute : Attribute { }
}