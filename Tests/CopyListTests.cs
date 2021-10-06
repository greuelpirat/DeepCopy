using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithList()
        {
            var type = GetTestType(typeof(ClassWithList));
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.List = new List<int> { 42, 84 };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            Assert.Equal(instance.List[0], copy.List[0]);
            Assert.Equal(instance.List[1], copy.List[1]);

            Assert.NotSame(instance.List, copy.List);
            Assert.NotSame(instance.List[0], copy.List[0]);
            Assert.NotSame(instance.List[1], copy.List[1]);
        }

        [Fact]
        public void TestClassWithListNull()
        {
            var type = GetTestType(typeof(ClassWithList));
            dynamic instance = Activator.CreateInstance(type);

            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.List);
        }

        [Fact]
        public void TestClassWithListString()
        {
            var type = GetTestType(typeof(ClassWithListString));
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.List = new List<string> { "Hello", "World", null };

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            Assert.Equal(instance.List[0], copy.List[0]);
            Assert.Equal(instance.List[1], copy.List[1]);

            Assert.NotSame(instance.List, copy.List);
            Assert.NotSame(instance.List[0], copy.List[0]);
            Assert.NotSame(instance.List[1], copy.List[1]);

            Assert.Null(copy.List[2]);
        }

        [Fact]
        public void TestClassWithListObject()
        {
            var type = GetTestType(typeof(ClassWithListObject));

            dynamic instance = Activator.CreateInstance(type);

            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(GetTestType(typeof(SomeObject))));
            Assert.NotNull(instance);
            instance.List = list;
            instance.List.Add(CreateSomeObject());
            instance.List.Add(CreateSomeObject());
            instance.List.Add(null);

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            AssertCopyOfSomeClass(instance.List[0], copy.List[0]);
            AssertCopyOfSomeClass(instance.List[1], copy.List[1]);
            Assert.Null(copy.List[2]);
        }

        [Fact]
        public void TestAnotherClassWithListObject()
        {
            var type = GetTestType(typeof(ClassWithListInstance));

            dynamic instance = Activator.CreateInstance(type);

            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(GetTestType(typeof(SomeObject))));
            Assert.NotNull(instance);
            instance.List = list;
            instance.List.Add(CreateSomeObject());
            instance.List.Add(CreateSomeObject());
            instance.List.Add(null);

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            AssertCopyOfSomeClass(instance.List[0], copy.List[0]);
            AssertCopyOfSomeClass(instance.List[1], copy.List[1]);
            Assert.Null(copy.List[2]);
        }

        [Fact]
        public void TestClassWithListOfDictionary()
        {
            var type = GetTestType(typeof(ClassWithListOfDictionary));

            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);

            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), GetTestType(typeof(SomeObject)));
            dynamic instance1 = Activator.CreateInstance(dictionaryType);
            Assert.NotNull(instance1);
            instance1["one"] = CreateSomeObject();
            instance1["two"] = CreateSomeObject();
            dynamic instance2 = Activator.CreateInstance(dictionaryType);
            Assert.NotNull(instance2);
            instance2["three"] = CreateSomeObject();
            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(dictionaryType));
            Assert.NotNull(list);
            list.Add(instance1);
            list.Add(instance2);
            instance.List = list;

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            Assert.Equal(instance.List[0].Count, copy.List[0].Count);
            Assert.Equal(instance.List[1].Count, copy.List[1].Count);
            AssertCopyOfSomeClass(instance.List[0]["one"], copy.List[0]["one"]);
            AssertCopyOfSomeClass(instance.List[0]["two"], copy.List[0]["two"]);
            AssertCopyOfSomeClass(instance.List[1]["three"], copy.List[1]["three"]);
        }
    }
}