using System;
using AssemblyToProcess;
using DeepCopy.Fody;
using Fody;
using Xunit;

#pragma warning disable 618

#region WeaverTests

namespace Tests
{
    public partial class WeaverTests
    {
        private static readonly TestResult TestResult;
        private static Random Random { get; } = new Random();

        static WeaverTests()
        {
            var weavingTask = new ModuleWeaver();
            TestResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        }

        private static Type GetTestType(Type type)
        {
            return TestResult.Assembly.GetType(type.FullName ?? throw new WeavingException($"{type} has no name"));
        }

        private static dynamic CreateTestInstance(Type type, params object[] args) => Activator.CreateInstance(GetTestType(type), args);

        private static dynamic CreateSomeObject()
        {
            var instance = CreateTestInstance(typeof(SomeObject));
            instance.Integer = Random.Next();
            instance.Enum = (int) SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello " + Random.Next();
            return instance;
        }
        
        private static dynamic CreateSomeStruct()
        {
            var instance = CreateTestInstance(typeof(SomeStruct));
            instance.Integer = Random.Next();
            instance.Enum = (int) SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello " + Random.Next();
            instance.Object = CreateSomeObject();
            return instance;
        }

        private static dynamic CreateSomeKey()
        {
            return CreateTestInstance(typeof(SomeKey), Random.Next(), Random.Next());
        }

        private static void AssertCopyOfSomeClass(dynamic instance, dynamic copy)
        {
            Assert.NotNull(copy);
            Assert.NotSame(instance, copy);
            Assert.NotSame(instance.DateTime, copy.DateTime);
            Assert.NotSame(instance.String, copy.String);

            Assert.Equal(instance.Integer, copy.Integer);
            Assert.Equal(instance.Enum, copy.Enum);
            Assert.Equal(instance.DateTime, copy.DateTime);
            Assert.Equal(instance.String, copy.String);
        }
    }
}

#endregion