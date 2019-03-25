using System.Reflection;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestSampleDeepCopyProvider()
        {
            var instance = CreateSomeObject();

            var method = GetTestType(typeof(SampleDeepCopyExtension))
                .GetMethod(nameof(SampleDeepCopyExtension.CopyOtherObject), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            dynamic copy = method.Invoke(null, new object[] { instance });
            AssertCopyOfSomeClass(instance, copy);
        }
    }
}