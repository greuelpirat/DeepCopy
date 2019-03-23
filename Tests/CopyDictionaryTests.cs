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
            var type = TestResult.Assembly.GetType(typeof(ClassWithDictionary).FullName);
            dynamic instance = Activator.CreateInstance(type);
            instance.Dictionary = new Dictionary<int, int> { [42] = 100, [84] = 200 };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            Assert.Equal(instance.Dictionary[42], copy.Dictionary[42]);
            Assert.Equal(instance.Dictionary[84], copy.Dictionary[84]);

            Assert.False(ReferenceEquals(instance.Dictionary, copy.Dictionary));
            Assert.False(ReferenceEquals(instance.Dictionary[42], copy.Dictionary[42]));
            Assert.False(ReferenceEquals(instance.Dictionary[84], copy.Dictionary[84]));
        }

        [Fact]
        public void TestClassWithDictionaryString()
        {
            var type = TestResult.Assembly.GetType(typeof(ClassWithDictionaryString).FullName);
            dynamic instance = Activator.CreateInstance(type);
            instance.Dictionary = new Dictionary<string, string> { ["Hello"] = "World", ["One"] = "Two", ["Three"] = null };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            Assert.Equal(instance.Dictionary["Hello"], copy.Dictionary["Hello"]);
            Assert.Equal(instance.Dictionary["One"], copy.Dictionary["One"]);
            Assert.Null(instance.Dictionary["Three"]);

            Assert.False(ReferenceEquals(instance.Dictionary, copy.Dictionary));
            Assert.False(ReferenceEquals(instance.Dictionary["Hello"], copy.Dictionary["Hello"]));
            Assert.False(ReferenceEquals(instance.Dictionary["One"], copy.Dictionary["One"]));
        }

        [Fact]
        public void TestClassWithDictionaryObject()
        {
            var someClass1 = CreateSomeClassInstance(out var someClassType);
            var someKey1 = CreateSomeKeyInstance(out var someKeyType);
            var someKey2 = CreateSomeKeyInstance(out _);
            var someKey3 = CreateSomeKeyInstance(out _);

            var type = TestResult.Assembly.GetType(typeof(ClassWithDictionaryObject).FullName);
            dynamic instance = Activator.CreateInstance(type);

            dynamic dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(someKeyType, someClassType));
            instance.Dictionary = dictionary;
            instance.Dictionary[someKey1] = someClass1;
            instance.Dictionary[someKey2] = CreateSomeClassInstance(out _);
            instance.Dictionary[someKey3] = null;

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Dictionary.Count, copy.Dictionary.Count);
            AssertCopyOfSomeClass(instance.Dictionary[someKey1], copy.Dictionary[someKey1]);
            AssertCopyOfSomeClass(instance.Dictionary[someKey2], copy.Dictionary[someKey2]);
            Assert.Null(copy.Dictionary[someKey3]);

            Assert.False(ReferenceEquals(instance.Dictionary, copy.Dictionary));
            Assert.False(ReferenceEquals(instance.Dictionary[someKey1], copy.Dictionary[someKey1]));
            Assert.False(ReferenceEquals(instance.Dictionary[someKey2], copy.Dictionary[someKey2]));
        }
    }
}