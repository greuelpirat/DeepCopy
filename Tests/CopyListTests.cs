using System;
using System.Collections.Generic;
using AssemblyToProcess;
using System.ComponentModel;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithList()
        {
            var instance = TestInstance<ClassWithList>();
            instance.List = new List<int> { 42, 84 };

            var copy = CopyByConstructor(instance);
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
            var instance = TestInstance<ClassWithList>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.List);
        }

        [Fact]
        public void TestClassWithListString()
        {
            var instance = TestInstance<ClassWithListString>();
            instance.List = new List<string> { "Hello", "World", null };

            var copy = CopyByConstructor(instance);
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
            var instance = TestInstance<ClassWithListObject>();

            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(TestType<SomeObject>()));
            instance.List = list;
            instance.List.Add(CreateSomeObject());
            instance.List.Add(CreateSomeObject());
            instance.List.Add(null);

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            AssertCopyOfSomeClass(instance.List[0], copy.List[0]);
            AssertCopyOfSomeClass(instance.List[1], copy.List[1]);
            Assert.Null(copy.List[2]);
        }

        [Fact]
        public void TestAnotherClassWithListObject()
        {
            var instance = TestInstance<ClassWithListInstance>();

            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(TestType<SomeObject>()));
            instance.List = list;
            instance.List.Add(CreateSomeObject());
            instance.List.Add(CreateSomeObject());
            instance.List.Add(null);

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            AssertCopyOfSomeClass(instance.List[0], copy.List[0]);
            AssertCopyOfSomeClass(instance.List[1], copy.List[1]);
            Assert.Null(copy.List[2]);
        }

        [Fact]
        public void TestClassWithListOfDictionary()
        {
            var instance = TestInstance<ClassWithListOfDictionary>();

            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), TestType<SomeObject>());
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

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.List.Count, copy.List.Count);
            Assert.Equal(instance.List[0].Count, copy.List[0].Count);
            Assert.Equal(instance.List[1].Count, copy.List[1].Count);
            AssertCopyOfSomeClass(instance.List[0]["one"], copy.List[0]["one"]);
            AssertCopyOfSomeClass(instance.List[0]["two"], copy.List[0]["two"]);
            AssertCopyOfSomeClass(instance.List[1]["three"], copy.List[1]["three"]);
        }

        [Fact]
        public void TestClassWithBindingList()
        {
            var instance = TestInstance<ClassWithBindingList>();
            instance.Strings = new BindingList<string>
            {
                "one",
                "two"
            };

            var copy = CopyByConstructor(instance);
            Assert.NotNull(copy);
            Assert.NotNull(copy.Strings);
            Assert.Equal("one", copy.Strings[0]);
            Assert.Equal("two", copy.Strings[1]);
            Assert.NotSame(instance.Strings[0], copy.Strings[0]);
            Assert.NotSame(instance.Strings[1], copy.Strings[1]);
        }
    }
}