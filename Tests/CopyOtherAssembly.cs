using System;
using System.Reflection;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassUsingOtherAssembly()
        {
            var type = GetTestType(typeof(ClassUsingOtherAssembly));
            var instance = Activator.CreateInstance(type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
        }

        [Fact]
        public void TestClassUsingOtherDeepCopyAssembly()
        {
            var type = GetTestType(typeof(ClassUsingOtherDeepCopyAssembly));
            var instance = Activator.CreateInstance(type);
            try
            {
                Activator.CreateInstance(type, instance);
                Assert.True(false);
            }
            catch (TargetInvocationException exception)
            {
                Assert.True(exception.InnerException is MissingMethodException);
                Assert.Contains("Void AnotherAssembly.DeepCopyClassFromAnotherAssembly..ctor(AnotherAssembly.DeepCopyClassFromAnotherAssembly)", exception.InnerException.Message);
                
            }
            catch
            {
                Assert.True(false);
            }
        }
    }
}