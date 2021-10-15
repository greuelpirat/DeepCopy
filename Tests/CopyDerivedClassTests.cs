using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestDerivedClass()
        {
            var instance = TestInstance<DerivedClass>();
            instance.Object = CreateSomeObject();
            instance.BaseObject = CreateSomeObject();

            var copy = CopyByConstructor(instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
            AssertCopyOfSomeClass(instance.BaseObject, copy.BaseObject);
        }

        [Fact]
        public void TestClassWithDerivedProperties()
        {
            var instance = TestInstance<ClassWithDerivedProperties>();
            instance.Dictionary = Dictionary();
            instance.List = List();
            instance.Set = Set();

            var copy = CopyByConstructor(instance);
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

        private static dynamic Dictionary()
        {
            var instance = TestInstance<DictionaryClass>();
            instance.SomeProperty = CreateSomeObject();
            instance["foo"] = CreateSomeObject();
            return instance;
        }

        private static dynamic List()
        {
            var instance = TestInstance<ListClass>();
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());
            return instance;
        }

        private static dynamic Set()
        {
            var instance = TestInstance<SetClass>();
            instance.SomeProperty = CreateSomeObject();
            instance.Add(CreateSomeObject());
            return instance;
        }

        [Fact]
        public void TestDerivedDictionaryClass()
        {
            var instance = Dictionary();
            var copy = CopyByConstructor(instance);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance["foo"], copy["foo"]);
        }

        [Fact]
        public void TestDerivedListClass()
        {
            var instance = List();
            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            AssertCopyOfSomeClass(instance[0], copy[0]);
        }

        [Fact]
        public void TestDerivedSetClass()
        {
            var instance = Set();
            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Count, copy.Count);
            AssertCopyOfSomeClass(instance.SomeProperty, copy.SomeProperty);
            var instanceArray = ToArray(instance);
            var copyArray = ToArray(copy);
            AssertCopyOfSomeClass(instanceArray[0], copyArray[0]);
        }
    }
}