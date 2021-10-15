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
            var copy = CopyByConstructor(instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithObject()
        {
            var instance = TestInstance<ClassWithObject>();
            instance.Object = CreateSomeObject();
            var copy = CopyByConstructor(instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }

        [Fact]
        public void TestSomeKey()
        {
            var type = TestType<SomeKey>();
            dynamic instance = Activator.CreateInstance(type, 35, 148);
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(35, copy.HighKey);
            Assert.Equal(148, copy.LowKey);
        }

        [Fact]
        public void TestIgnoreDuringDeepCopy()
        {
            var instance = TestInstance<ClassWithIgnoreDuringDeepCopy>();
            instance.Integer = 42;
            instance.IntegerIgnored = 84;
            instance.String = "Hello";
            instance.StringIgnored = "World";
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Equal(42, copy.Integer);
            Assert.Equal(default(int), copy.IntegerIgnored);
            Assert.Equal("Hello", copy.String);
            Assert.Null(copy.StringIgnored);
        }

        [Fact]
        public void TestEmptyObject()
        {
            var instance = TestInstance<EmptyObject>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
        }

        [Fact]
        public void TestAutoPropertyInitializer()
        {
            var guid = Guid.NewGuid();
            var instance = TestInstance<AutoPropertyInitializerObject>();
            instance.Guid = guid;
            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Guid, copy.Guid);
        }

        [Fact]
        public void TestAutoPropertyConstructorInitializer()
        {
            var guid = Guid.NewGuid();
            var instance = TestInstance<AutoPropertyInitializerConstructorObject>();
            instance.Guid = guid;
            var copy = CopyByConstructor(instance);
            Assert.Equal(guid, copy.Guid);
        }
    }
}