using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestInnerClass()
        {
            var type = GetTestType(typeof(OuterClassObject.InnerClassObject));
            dynamic instance = Activator.CreateInstance(type);
            
            var one = CreateTestInstance(typeof(OuterClassObject.InnerClassObject.InnerClassOne));
            one.ObjectOne = CreateSomeObject();
            instance.One = one;

            var two = CreateTestInstance(typeof(OuterClassObject.InnerClassObject.InnerClassTwo));
            two.ObjectTwo = CreateSomeObject();
            instance.Two = two;

            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.One.ObjectOne, copy.One.ObjectOne);
            AssertCopyOfSomeClass(instance.Two.ObjectTwo, copy.Two.ObjectTwo);
        }  
    }
}