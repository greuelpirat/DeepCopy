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
            var type = TestType<ClassWithSet>();
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Set = new HashSet<int> { 42, 84 };

            var copy = Activator.CreateInstance(type, instance);
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
            var type = TestType<ClassWithSet>();
            dynamic instance = Activator.CreateInstance(type);

            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Set);
        }

        [Fact]
        public void TestClassWithSetString()
        {
            var type = TestType<ClassWithSetString>();
            dynamic instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);
            instance.Set = new HashSet<string> { "Hello", "World", null };

            var copy = Activator.CreateInstance(type, instance);
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
            var type = TestType<ClassWithSetObject>();

            dynamic instance = Activator.CreateInstance(type);

            dynamic set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(TestType<SomeObject>()));
            Assert.NotNull(instance);
            instance.Set = set;
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(null);

            var copy = Activator.CreateInstance(type, instance);
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
            var type = TestType<ClassWithSetInstance>();

            dynamic instance = Activator.CreateInstance(type);

            dynamic list = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(TestType<SomeObject>()));
            Assert.NotNull(instance);
            instance.Set = list;
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(CreateSomeObject());
            instance.Set.Add(null);

            var copy = Activator.CreateInstance(type, instance);
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