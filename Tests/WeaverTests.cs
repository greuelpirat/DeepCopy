using System;
using AssemblyToProcess;
using DeepCopyConstructor.Fody;
using Fody;
using Xunit;

#pragma warning disable 618

#region WeaverTests

namespace Tests
{
    public class WeaverTests
    {
        private static readonly TestResult TestResult;

        static WeaverTests()
        {
            var weavingTask = new ModuleWeaver();
            TestResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        }

        private static dynamic CreateSomeClassInstance(out Type type)
        {
            type = TestResult.Assembly.GetType("AssemblyToProcess.SomeClass");
            dynamic instance = Activator.CreateInstance(type);
            instance.Integer = 42;
            instance.Enum = (int) SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello";
            return instance;
        }

        private static void AssertCopyOfSomeClass(dynamic instance, dynamic copy)
        {
            Assert.NotNull(copy);
            Assert.False(ReferenceEquals(instance, copy));
            Assert.False(ReferenceEquals(instance.DateTime, copy.DateTime));
            Assert.False(ReferenceEquals(instance.String, copy.String));

            Assert.Equal(instance.Integer, copy.Integer);
            Assert.Equal(instance.Enum, copy.Enum);
            Assert.Equal(instance.DateTime, copy.DateTime);
            Assert.Equal(instance.String, copy.String);
        }

        [Fact]
        public void CopySomeClass()
        {
            var instance = CreateSomeClassInstance(out var type);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void CopyClassWithObject()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithObject");
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeClassInstance(out _);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }

        [Fact]
        public void CopyClassWithArray()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithArray");
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] {42, 84};

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.False(ReferenceEquals(instance.Array, copy.Array));
            Assert.False(ReferenceEquals(instance.Array[0], copy.Array[0]));
            Assert.False(ReferenceEquals(instance.Array[1], copy.Array[1]));
        }

        [Fact]
        public void CopyClassWithStringArray()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithStringArray");
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] {"Hello", "World", null};

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.False(ReferenceEquals(instance.Array, copy.Array));
            Assert.False(ReferenceEquals(instance.Array[0], copy.Array[0]));
            Assert.False(ReferenceEquals(instance.Array[1], copy.Array[1]));

            Assert.Null(copy.Array[2]);
        }

        [Fact]
        public void CopyClassWithObjectArray()
        {
            var someClass1 = CreateSomeClassInstance(out var someClassType);

            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithObjectArray");

            dynamic instance = Activator.CreateInstance(type);

            dynamic array = Array.CreateInstance(someClassType, 3);
            instance.Array = array;
            instance.Array[0] = someClass1;
            instance.Array[1] = CreateSomeClassInstance(out _);
            instance.Array[2] = null;

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            AssertCopyOfSomeClass(instance.Array[0], copy.Array[0]);
            AssertCopyOfSomeClass(instance.Array[1], copy.Array[1]);
            Assert.Null(copy.Array[2]);
        }
    }
}

#endregion