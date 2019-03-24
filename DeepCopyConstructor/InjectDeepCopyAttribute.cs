using System;

namespace DeepCopyConstructor
{
    /// <summary>
    /// Injects deep copy functionality to an existing copy constructor
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Constructor)]
    public class InjectDeepCopyAttribute : Attribute { }
}