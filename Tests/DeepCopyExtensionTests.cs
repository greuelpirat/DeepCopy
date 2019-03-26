using System.Reflection;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithDeepCopyExtension()
        {
            var instance = CreateSomeObject();

            var method = GetTestType(typeof(ClassWithDeepCopyExtension))
                .GetMethod(nameof(ClassWithDeepCopyExtension.CopySomeObject), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            dynamic copy = method.Invoke(null, new object[] { instance });
            AssertCopyOfSomeClass(instance, copy);
        }
    }
}