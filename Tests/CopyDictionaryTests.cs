using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void CopyClassWithDictionary()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithDictionary");
            dynamic instance = Activator.CreateInstance(type);
            instance.Dictionary = new Dictionary<int, int> { [42] = 100, [84] = 200 };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            Assert.Equal(instance.Dictionary[42], copy.Dictionary[42]);
            Assert.Equal(instance.Dictionary[84], copy.Dictionary[84]);

            Assert.False(ReferenceEquals(instance.Dictionary, copy.Dictionary));
            Assert.False(ReferenceEquals(instance.Dictionary[42], copy.Dictionary[84]));
            Assert.False(ReferenceEquals(instance.Dictionary[42], copy.Dictionary[84]));
        }
    }
}