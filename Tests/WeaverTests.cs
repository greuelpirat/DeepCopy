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

        [Fact]
        public void CopySomeClass()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.SomeClass");
            dynamic instance = Activator.CreateInstance(type);
            instance.Integer = 42;
            instance.Enum = (int) SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello";

            var copy = Activator.CreateInstance(type, instance);

            Assert.Equal(instance.Integer, copy.Integer);
            Assert.Equal(instance.Enum, copy.Enum);
            Assert.Equal(instance.DateTime, copy.DateTime);
            Assert.Equal(instance.String, copy.String);

            Assert.False(ReferenceEquals(instance.DateTime, copy.DateTime));
            Assert.False(ReferenceEquals(instance.String, copy.String));
        }

        [Fact]
        public void CopyClassWithObject()
        {
            var otherType = TestResult.Assembly.GetType("AssemblyToProcess.OtherClass");
            dynamic otherInstance = Activator.CreateInstance(otherType);

            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithObject");
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = otherInstance;
            instance.Object.String = "Hello";
            instance.Object.Float = 1.5f;

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Object.String, copy.Object.String);
            Assert.Equal(instance.Object.Float, copy.Object.Float);

            Assert.False(ReferenceEquals(instance.Object, copy.Object));
            Assert.False(ReferenceEquals(instance.Object.String, copy.Object.String));
            Assert.False(ReferenceEquals(instance.Object.Float, copy.Object.Float));
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
    }
}

#endregion