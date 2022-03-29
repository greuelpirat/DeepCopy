using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestAnyRecord()
        {
            var instance = Create<AnyRecord>();
            var copy = CopyByConstructor(instance);

            Assert.NotNull(copy);
            Assert.NotSame(instance, copy);
            Assert.NotSame(instance.DateTime, copy.DateTime);
            Assert.NotSame(instance.String, copy.String);

            Assert.Equal(instance.Integer, copy.Integer);
            Assert.Equal(instance.Enum, copy.Enum);
            Assert.Equal(instance.DateTime, copy.DateTime);
            Assert.Equal(instance.String, copy.String);

            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }
    }
}