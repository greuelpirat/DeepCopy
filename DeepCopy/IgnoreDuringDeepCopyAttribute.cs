using System;

namespace DeepCopy
{
    /// <summary>
    /// Marked Property will be skip by DeepCopy
    /// </summary>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreDuringDeepCopyAttribute : Attribute { }
}