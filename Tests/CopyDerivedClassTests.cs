using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestDerivedClass()
        {
            var type = GetTestType(typeof(DerivedClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeObject();
            instance.BaseObject = CreateSomeObject();

            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
            AssertCopyOfSomeClass(instance.BaseObject, copy.BaseObject);
        }

        [Fact]
        public void TestClassWithDerivedProperties()
        {
            var type = GetTestType(typeof(ClassWithDerivedProperties));
            dynamic instance = Activator.CreateInstance(type);
            instance.Dictionary = Dictionary(out _);
            instance.List = List(out _);
            instance.Set = Set(out _);

            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            AssertCopyOfSomeClass(instance.Dictionary.SomeProperty, copy.Dictionary.SomeProperty);
            AssertCopyOfSomeClass(instance.Dictionary["foo"], copy.Dictionary["foo"]);
            Assert.Equal(instance.List.Count, copy.List.Count);
            AssertCopyOfSomeClass(instance.List.SomeProperty, copy.List.SomeProperty);
            AssertCopyOfSomeClass(instance.List[0], copy.List[0]);
            AssertCopyOfSomeClass(instance.Set.SomeProperty, copy.Set.SomeProperty);
            var instanceArray = ToArray(instance.Set);
            var copyArray = ToArray(copy.Set);
            AssertCopyOfSomeClass(instanceArray[0], copyArray[0]);
        }

        private static dynamic Dictionary(out Type type)
        {
            type = GetTestType(typeof(DictionaryClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance["foo"] = CreateSomeObject();
            return instance;
        }

        private static dynamic List(out Type type)
        {
            type = GetTestType(typeof(ListClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());
            return instance;
        }

        private static dynamic Set(out Type type)
        {
            type = GetTestType(typeof(SetClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());
            return instance;
        }

        [Fact]
        public void TestDerivedDictionaryClass()
        {
            var instance = Dictionary(out var type);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance["foo"], copy["foo"]);
        }

        [Fact]
        public void TestDerivedListClass()
        {
            var instance = List(out var type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance[0], copy[0]);
        }

        [Fact]
        public void TestDerivedSetClass()
        {
            var instance = Set(out var type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            var instanceArray = ToArray(instance);
            var copyArray = ToArray(copy);
            AssertCopyOfSomeClass(instanceArray[0], copyArray[0]);
        }
    }
}