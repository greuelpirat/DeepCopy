using System.Reflection;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithDeepCopyExtension_CopySomeObject()
        {
            var method = GetTestType(typeof(ClassWithDeepCopyExtension))
                .GetMethod(nameof(ClassWithDeepCopyExtension.CopySomeObject), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            var instance = CreateSomeObject();
            dynamic copy = method.Invoke(null, new object[] { instance });
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithDeepCopyExtension_CopyBaseClass_DerivedClass()
        {
            var method = GetTestType(typeof(ClassWithDeepCopyExtension))
                .GetMethod(nameof(ClassWithDeepCopyExtension.CopyBaseClass), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            var derivedInstance = CreateTestInstance(typeof(DerivedClass));
            derivedInstance.Object = CreateSomeObject();
            derivedInstance.BaseObject = CreateSomeObject();

            dynamic derivedCopy = method.Invoke(null, new object[] { derivedInstance });
            Assert.NotNull(derivedCopy);
            Assert.Equal(derivedInstance.GetType(), derivedCopy.GetType());
            Assert.NotSame(derivedInstance, derivedCopy);
            AssertCopyOfSomeClass(derivedInstance.Object, derivedCopy.Object);
            AssertCopyOfSomeClass(derivedInstance.BaseObject, derivedCopy.BaseObject);
        }

        [Fact]
        public void TestClassWithDeepCopyExtension_CopyBaseClass_OtherDerivedClass()
        {
            var method = GetTestType(typeof(ClassWithDeepCopyExtension))
                .GetMethod(nameof(ClassWithDeepCopyExtension.CopyBaseClass), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            var derivedInstance = CreateTestInstance(typeof(OtherDerivedClass));
            derivedInstance.OtherObject = CreateSomeObject();
            derivedInstance.BaseObject = CreateSomeObject();

            dynamic derivedCopy = method.Invoke(null, new object[] { derivedInstance });
            Assert.NotNull(derivedCopy);
            Assert.Equal(derivedInstance.GetType(), derivedCopy.GetType());
            Assert.NotSame(derivedInstance, derivedCopy);
            AssertCopyOfSomeClass(derivedInstance.OtherObject, derivedCopy.OtherObject);
            AssertCopyOfSomeClass(derivedInstance.BaseObject, derivedCopy.BaseObject);
        }
    }
}