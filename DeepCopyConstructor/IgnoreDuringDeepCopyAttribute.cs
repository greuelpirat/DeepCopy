using System;

namespace DeepCopyConstructor
{
    /// <summary>
    /// Marked Property will be skip by DeepCopyConstructor
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreDuringDeepCopyAttribute : Attribute { }
}