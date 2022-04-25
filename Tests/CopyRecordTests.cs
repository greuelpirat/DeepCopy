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
        
        [Fact]
        public void TestRecordDeepCopyExtension()
        {
            var instance = Create<DerivedRecord>();
            instance.AnotherInteger = 42;
            instance.AnotherObject = CreateSomeObject();
            
            var copyMethod = StaticTestMethod(typeof(AnyRecordExtensions), nameof(AnyRecordExtensions.DeepCopy));
            dynamic copy = copyMethod.Invoke(null, new object[] { instance });

            Assert.NotNull(copy);
            Assert.NotSame(instance, copy);
            Assert.NotSame(instance.DateTime, copy.DateTime);
            Assert.NotSame(instance.String, copy.String);

            Assert.Equal(instance.Integer, copy.Integer);
            Assert.Equal(instance.Enum, copy.Enum);
            Assert.Equal(instance.DateTime, copy.DateTime);
            Assert.Equal(instance.String, copy.String);
            Assert.Equal(instance.AnotherInteger, copy.AnotherInteger);
            
            AssertCopyOfSomeClass(instance.Object, copy.Object);
            AssertCopyOfSomeClass(instance.AnotherObject, copy.AnotherObject);
        }
    }
}