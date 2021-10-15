using System;
using AssemblyToProcess;
using DeepCopy.Fody;
using Fody;
using System.Reflection;
using Xunit;

#pragma warning disable 618

#region WeaverTests

namespace Tests
{
    public partial class WeaverTests
    {
        private static readonly TestResult TestResult;
        private static Random Random { get; } = new();

        static WeaverTests()
        {
            var weavingTask = new ModuleWeaver();
            TestResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        }

        private static Type TestType<T>() => TestType(typeof(T));

        private static Type TestType(Type type)
        {
            return TestResult.Assembly.GetType(type.FullName ?? throw new WeavingException($"{type} has no name"))
                   ?? throw new WeavingException($"{type} not found in test assembly");
        }

        private static MethodInfo StaticTestMethod(Type type, string methodName)
        {
            var method = TestType(type).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            return method;
        }

        private static dynamic TestInstance<T>(params object[] args)
        {
            var testType = TestType<T>();
            var testInstance = Activator.CreateInstance(testType, args);
            Assert.IsType(testType, testInstance);
            return testInstance;
        }

        private static dynamic CreateSomeObject()
        {
            var instance = TestInstance<SomeObject>();
            instance.Integer = Random.Next();
            instance.Enum = (int)SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello " + Random.Next();
            return instance;
        }

        private static dynamic CreateSomeStruct()
        {
            var instance = TestInstance<SomeStruct>();
            instance.Integer = Random.Next();
            instance.Enum = (int)SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello " + Random.Next();
            instance.Object = CreateSomeObject();
            return instance;
        }

        private static dynamic CreateSomeKey() => TestInstance<SomeKey>(Random.Next(), Random.Next());

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