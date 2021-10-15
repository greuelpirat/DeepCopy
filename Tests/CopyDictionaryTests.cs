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
            var type = TestType<ClassWithDictionary>();
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Dictionary = new Dictionary<int, int> { [42] = 100, [84] = 200 };

            var copy = Activator.CreateInstance(type, instance);
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
            var type = TestType<ClassWithDictionary>();
            dynamic instance = Activator.CreateInstance(type);

            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Dictionary);
        }

        [Fact]
        public void TestClassWithDictionaryString()
        {
            var type = TestType<ClassWithDictionaryString>();
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Dictionary = new Dictionary<string, string> { ["Hello"] = "World", ["One"] = "Two", ["Three"] = null };

            var copy = Activator.CreateInstance(type, instance);
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
            var someKey1 = CreateSomeKey();
            var someKey2 = CreateSomeKey();
            var someKey3 = CreateSomeKey();

            var type = TestType<ClassWithDictionaryObject>();
            dynamic instance = Activator.CreateInstance(type);

            dynamic dictionary = Activator.CreateInstance(typeof(Dictionary<,>)
                .MakeGenericType(TestType<SomeKey>(), TestType<SomeObject>()));
            Assert.NotNull(instance);
            instance.Dictionary = dictionary;
            instance.Dictionary[someKey1] = CreateSomeObject();
            instance.Dictionary[someKey2] = CreateSomeObject();
            instance.Dictionary[someKey3] = null;

            var copy = Activator.CreateInstance(type, instance);
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
            var someKey1 = CreateSomeKey();
            var someKey2 = CreateSomeKey();
            var someKey3 = CreateSomeKey();

            var type = TestType<ClassWithDictionaryInstance>();
            dynamic instance = Activator.CreateInstance(type);

            dynamic dictionary = Activator.CreateInstance(typeof(Dictionary<,>)
                .MakeGenericType(TestType<SomeKey>(), TestType<SomeObject>()));
            Assert.NotNull(instance);
            instance.Dictionary = dictionary;
            instance.Dictionary[someKey1] = CreateSomeObject();
            instance.Dictionary[someKey2] = CreateSomeObject();
            instance.Dictionary[someKey3] = null;

            var copy = Activator.CreateInstance(type, instance);
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