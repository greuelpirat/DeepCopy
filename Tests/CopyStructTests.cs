using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestSomeStruct()
        {
            var instance = CreateSomeStruct();
            var copy = CreateTestInstance<SomeStruct>((object)instance);

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

        [Fact]
        public void TestStructWithReference()
        {
            var instance = CreateTestInstance<StructWithReference>();
            instance.Object = CreateSomeObject();
            var copy = CreateTestInstance<StructWithReference>((object)instance);

            Assert.NotNull(copy);
            Assert.NotSame(instance, copy);
            Assert.Same(instance.Object, copy.Object);
        }
    }
}