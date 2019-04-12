using System;

namespace DeepCopy
{
    /// <summary>
    /// Copy reference instead of deep copy
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DeepCopyByReferenceAttribute : Attribute { }
}