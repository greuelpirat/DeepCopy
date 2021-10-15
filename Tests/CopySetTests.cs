using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithSet()
        {
            var instance = TestInstance<ClassWithSet>();
            instance.Set = new HashSet<int> { 42, 84 };

            var copy = CopyByConstructor(instance);
            var array = ToArray(instance.Set);
            var arrayCopy = ToArray(copy.Set);
            Assert.Equal(instance.Set.Count, copy.Set.Count);
            Assert.Equal(array[0], arrayCopy[0]);
            Assert.Equal(array[1], arrayCopy[1]);

            Assert.NotSame(instance.Set, copy.Set);
            Assert.NotSame(array[0], arrayCopy[0]);
            Assert.NotSame(array[1], arrayCopy[1]);
        }

        [Fact]
        public void TestClassWithSetNull()
        {
            var instance = TestInstance<ClassWithSet>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Set);
        }

        [Fact]
        public void TestClassWithSetString()
        {
            var instance = TestInstance<ClassWithSetString>();
            instance.Set = new HashSet<string> { "Hello", "World", null };

            var copy = CopyByConstructor(instance);
            var array = ToArray(instance.Set);
            var arrayCopy = ToArray(copy.Set);
            Assert.Equal(instance.Set.Count, copy.Set.Count);
            Assert.Equal(array[0], arrayCopy[0]);
            Assert.Equal(array[1], arrayCopy[1]);

            Assert.NotSame(instance.Set, copy.Set);
            Assert.NotSame(array[0], arrayCopy[0]);
            Assert.NotSame(array[1], arrayCopy[1]);

            Assert.Null(arrayCopy[2]);
        }

        [Fact]
        public void TestClassWithSetObject()
        {
            var instance = TestInstance<ClassWithSetObject>();
            instance.Set = (dynamic)Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(TestType<SomeObject>()));
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(null);

            var copy = CopyByConstructor(instance);
            var array = ToArray(instance.Set);
            var arrayCopy = ToArray(copy.Set);
            Assert.Equal(instance.Set.Count, copy.Set.Count);
            AssertCopyOfSomeClass(array[0], arrayCopy[0]);
            AssertCopyOfSomeClass(array[1], arrayCopy[1]);
            Assert.Null(arrayCopy[2]);
        }

        [Fact]
        public void TestAnotherClassWithSetObject()
        {
            var instance = TestInstance<ClassWithSetInstance>();
            instance.Set = (dynamic)Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(TestType<SomeObject>()));
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(null);

            var copy = CopyByConstructor(instance);
            var array = ToArray(instance.Set);
            var arrayCopy = ToArray(copy.Set);
            Assert.Equal(instance.Set.Count, copy.Set.Count);
            AssertCopyOfSomeClass(array[0], arrayCopy[0]);
            AssertCopyOfSomeClass(array[1], arrayCopy[1]);
            Assert.Null(arrayCopy[2]);
        }

        private static T[] ToArray<T>(ICollection<T> set)
        {
            var array = new T[set.Count];
            set.CopyTo(array, 0);
            return array;
        }
    }
}