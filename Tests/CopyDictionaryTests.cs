using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithDictionary()
        {
            var instance = TestInstance<ClassWithDictionary>();
            instance.Dictionary = new Dictionary<int, int> { [42] = 100, [84] = 200 };

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            Assert.Equal(instance.Dictionary[42], copy.Dictionary[42]);
            Assert.Equal(instance.Dictionary[84], copy.Dictionary[84]);

            Assert.NotSame(instance.Dictionary, copy.Dictionary);
            Assert.NotSame(instance.Dictionary[42], copy.Dictionary[42]);
            Assert.NotSame(instance.Dictionary[84], copy.Dictionary[84]);
        }

        [Fact]
        public void TestClassWithDictionaryNull()
        {
            var instance = TestInstance<ClassWithDictionary>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Dictionary);
        }

        [Fact]
        public void TestClassWithDictionaryString()
        {
            var instance = TestInstance<ClassWithDictionaryString>();
            instance.Dictionary = new Dictionary<string, string> { ["Hello"] = "World", ["One"] = "Two", ["Three"] = null };

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            Assert.Equal(instance.Dictionary["Hello"], copy.Dictionary["Hello"]);
            Assert.Equal(instance.Dictionary["One"], copy.Dictionary["One"]);
            Assert.Null(instance.Dictionary["Three"]);

            Assert.NotSame(instance.Dictionary, copy.Dictionary);
            Assert.NotSame(instance.Dictionary["Hello"], copy.Dictionary["Hello"]);
            Assert.NotSame(instance.Dictionary["One"], copy.Dictionary["One"]);
        }

        [Fact]
        public void TestClassWithDictionaryObject()
        {
            var someKey1 = CreateRandomSomeKey();
            var someKey2 = CreateRandomSomeKey();
            var someKey3 = CreateRandomSomeKey();

            var instance = TestInstance<ClassWithDictionaryObject>();

            dynamic dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(TestType<SomeKey>(), TestType<SomeObject>()));
            instance.Dictionary = dictionary;
            instance.Dictionary[someKey1] = CreateSomeObject();
            instance.Dictionary[someKey2] = CreateSomeObject();
            instance.Dictionary[someKey3] = null;

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            AssertCopyOfSomeClass(instance.Dictionary[someKey1], copy.Dictionary[someKey1]);
            AssertCopyOfSomeClass(instance.Dictionary[someKey2], copy.Dictionary[someKey2]);
            Assert.Null(copy.Dictionary[someKey3]);

            Assert.NotSame(instance.Dictionary, copy.Dictionary);
            Assert.NotSame(instance.Dictionary[someKey1], copy.Dictionary[someKey1]);
            Assert.NotSame(instance.Dictionary[someKey2], copy.Dictionary[someKey2]);

            var instanceKey1 = System.Linq.Enumerable.First(instance.Dictionary.Keys);
            var copyKey1 = System.Linq.Enumerable.First(copy.Dictionary.Keys);
            Assert.Equal(instanceKey1, copyKey1);
            Assert.NotSame(instanceKey1, copyKey1);
        }

        [Fact]
        public void TestClassWithDictionaryInstance()
        {
            var someKey1 = CreateRandomSomeKey();
            var someKey2 = CreateRandomSomeKey();
            var someKey3 = CreateRandomSomeKey();

            var instance = TestInstance<ClassWithDictionaryInstance>();

            dynamic dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(TestType<SomeKey>(), TestType<SomeObject>()));
            instance.Dictionary = dictionary;
            instance.Dictionary[someKey1] = CreateSomeObject();
            instance.Dictionary[someKey2] = CreateSomeObject();
            instance.Dictionary[someKey3] = null;

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            AssertCopyOfSomeClass(instance.Dictionary[someKey1], copy.Dictionary[someKey1]);
            AssertCopyOfSomeClass(instance.Dictionary[someKey2], copy.Dictionary[someKey2]);
            Assert.Null(copy.Dictionary[someKey3]);

            Assert.NotSame(instance.Dictionary, copy.Dictionary);
            Assert.NotSame(instance.Dictionary[someKey1], copy.Dictionary[someKey1]);
            Assert.NotSame(instance.Dictionary[someKey2], copy.Dictionary[someKey2]);
        }
    }
}