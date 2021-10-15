using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestInnerClass()
        {
            var instance = TestInstance<OuterClassObject.InnerClassObject>();

            var one = TestInstance<OuterClassObject.InnerClassObject.InnerClassOne>();
            one.ObjectOne = CreateSomeObject();
            Assert.NotNull(instance);
            instance.One = one;

            var two = TestInstance<OuterClassObject.InnerClassObject.InnerClassTwo>();
            two.ObjectTwo = CreateSomeObject();
            instance.Two = two;

            var copy = CopyByConstructor(instance);
            AssertCopyOfSomeClass(instance.One.ObjectOne, copy.One.ObjectOne);
            AssertCopyOfSomeClass(instance.Two.ObjectTwo, copy.Two.ObjectTwo);
        }  
    }
}