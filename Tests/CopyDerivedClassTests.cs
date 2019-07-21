using System;
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
        public void TestDerivedDictionaryClass()
        {
            var type = GetTestType(typeof(DictionaryClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance["foo"] = CreateSomeObject();

            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance["foo"], copy["foo"]);
        }

        [Fact]
        public void TestDerivedListClass()
        {
            var type = GetTestType(typeof(ListClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance[0], copy[0]);
        }

        [Fact]
        public void TestDerivedSetClass()
        {
            var type = GetTestType(typeof(SetClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            var instanceArray = ToArray(instance);
            var copyArray = ToArray(copy);
            AssertCopyOfSomeClass(instanceArray[0], copyArray[0]);
        }
    }
}