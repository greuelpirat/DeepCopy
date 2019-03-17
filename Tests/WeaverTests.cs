using System;
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
        public void CopySimpleClass()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.SimpleClass");
            dynamic instance = Activator.CreateInstance(type);
            instance.Integer = 42;

            var copy = Activator.CreateInstance(type, instance);

            Assert.Equal(42, copy.Integer);
        }
    }
}

#endregion