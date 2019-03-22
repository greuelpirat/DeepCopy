using System;
using AssemblyToProcess;
using DeepCopyConstructor.Fody;
using Fody;
using Xunit;

#pragma warning disable 618

#region WeaverTests

namespace Tests
{
    public partial class WeaverTests
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
    }
}

#endregion