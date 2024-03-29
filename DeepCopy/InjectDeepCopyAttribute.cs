﻿using System;

namespace DeepCopy
{
    /// <summary>
    ///     Injects deep copy functionality to an existing copy constructor
    /// </summary>
    /// <remarks>replaced with <see cref="DeepCopyConstructorAttribute" /></remarks>
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Constructor)]
    [Obsolete("Use [DeepCopyConstructor]")]
    public class InjectDeepCopyAttribute : Attribute { }
}