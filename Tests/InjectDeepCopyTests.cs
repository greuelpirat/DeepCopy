using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassCopyConstructor()
        {
            var type = GetTestType(typeof(ClassWithCopyConstructor));
            dynamic instance = Activator.CreateInstance(type);
            instance.Integer = 3;
            instance.Integers = new List<int> { 1, 5, 7 };
            dynamic specialObject = Activator.CreateInstance(GetTestType(typeof(ClassWithNoCopyConstructor)));
            instance.SpecialObject = specialObject;
            instance.SpecialObject.List = new List<int> { 3, 2, 1 };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(3, copy.Integer);
            Assert.Equal(3, instance.SpecialObject.List.Count);

            Assert.NotNull(copy.Integers);
            Assert.NotNull(copy.SpecialObject.List);
            Assert.NotNull(copy.SpecialObject.List);

            Assert.NotSame(instance.Integers, copy.Integers);
            Assert.NotSame(instance.SpecialObject, copy.SpecialObject);
            Assert.NotSame(instance.SpecialObject.List, copy.SpecialObject.List);

            Assert.Equal(new List<int> { 1, 5, 7 }, copy.Integers);
            Assert.Equal(new List<int> { 1, 2, 3, 3, 2, 1 }, copy.SpecialObject.List);
        }
    }
}