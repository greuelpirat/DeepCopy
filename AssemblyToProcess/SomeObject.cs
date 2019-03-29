using System;
using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class SomeObject
    {
        public int Integer { get; set; }
        public SomeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
    }

    public enum SomeEnum
    {
        Value1,
        Value2,
        Value3
    }

    [AddDeepCopyConstructor]
    public class SomeKey
    {
        public SomeKey(int highKey, int lowKey)
        {
            HighKey = highKey;
            LowKey = lowKey;
        }

        public int HighKey { get; }
        public int LowKey { get; }

        #region equals/hashcode

        protected bool Equals(SomeKey other)
        {
            return HighKey == other.HighKey && LowKey == other.LowKey;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SomeKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (HighKey * 397) ^ LowKey;
            }
        }

        #endregion
    }
}