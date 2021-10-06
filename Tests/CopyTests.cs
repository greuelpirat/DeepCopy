using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestSomeClass()
        {
            var instance = CreateSomeObject();
            var copy = Activator.CreateInstance(GetTestType(typeof(SomeObject)), instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithObject()
        {
            var type = GetTestType(typeof(ClassWithObject));
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Object = CreateSomeObject();
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }

        [Fact]
        public void TestSomeKey()
        {
            var type = GetTestType(typeof(SomeKey));
            dynamic instance = Activator.CreateInstance(type, 35, 148);
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(35, copy.HighKey);
            Assert.Equal(148, copy.LowKey);
        }

        [Fact]
        public void TestIgnoreDuringDeepCopy()
        {
            var type = GetTestType(typeof(ClassWithIgnoreDuringDeepCopy));
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Integer = 42;
            instance.IntegerIgnored = 84;
            instance.String = "Hello";
            instance.StringIgnored = "World";
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(42, copy.Integer);
            Assert.Equal(default(int), copy.IntegerIgnored);
            Assert.Equal("Hello", copy.String);
            Assert.Null(copy.StringIgnored);
        }

        [Fact]
        public void TestEmptyObject()
        {
            var type = GetTestType(typeof(EmptyObject));
            var instance = Activator.CreateInstance(type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
        }

        [Fact]
        public void TestAutoPropertyInitializer()
        {
            var type = GetTestType(typeof(AutoPropertyInitializerObject));
            dynamic instance = Activator.CreateInstance(type);
            var guid = Guid.NewGuid();
            Assert.NotNull(instance);
            instance.Guid = guid;
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(instance.Guid, copy.Guid);
        }

        [Fact]
        public void TestAutoPropertyConstructorInitializer()
        {
            var type = GetTestType(typeof(AutoPropertyInitializerConstructorObject));
            dynamic instance = Activator.CreateInstance(type);
            var guid = Guid.NewGuid();
            Assert.NotNull(instance);
            instance.Guid = guid;
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(instance.Guid, copy.Guid);
        }
    }
}