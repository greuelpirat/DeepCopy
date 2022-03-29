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

        private static dynamic TestInstance<T>()
        {
            var testType = TestType<T>();
            var testInstance = Activator.CreateInstance(testType);
            Assert.IsType(testType, testInstance);
            return testInstance;
        }

        private static dynamic CopyByConstructor(object instance)
        {
            var testType = instance.GetType();
            var copy = Activator.CreateInstance(testType, instance);
            Assert.IsType(testType, copy);
            Assert.NotSame(instance, copy);
            return copy;
        }

        private static dynamic CreateSomeObject() => Create<SomeObject>(false);

        private static dynamic Create<T>(bool setSomeObject = true)
        {
            var instance = TestInstance<T>();
            instance.Integer = Random.Next();
            instance.Enum = (int)SomeEnum.Value1;
            instance.DateTime = DateTime.Now;
            instance.String = "Hello " + Random.Next();
            if (setSomeObject)
                instance.Object = CreateSomeObject();
            return instance;
        }

        private static dynamic CreateRandomSomeKey() => Activator.CreateInstance(TestType<SomeKey>(), Random.Next(), Random.Next());

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