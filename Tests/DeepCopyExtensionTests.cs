using System;
using System.Collections.Generic;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithDeepCopyExtension_CopySomeObject()
        {
            var method = StaticTestMethod(typeof(ClassWithDeepCopyExtension), nameof(ClassWithDeepCopyExtension.CopySomeObject));
            var instance = CreateSomeObject();
            dynamic copy = method.Invoke(null, new object[] { instance });
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithDeepCopyExtension_CopyBaseClass_DerivedClass()
        {
            var method = StaticTestMethod(typeof(ClassWithDeepCopyExtension), nameof(ClassWithDeepCopyExtension.CopyBaseClass));
            var derivedInstance = TestInstance<DerivedClass>();
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
            var method = StaticTestMethod(typeof(ClassWithDeepCopyExtension), nameof(ClassWithDeepCopyExtension.CopyBaseClass));

            var derivedInstance = TestInstance<OtherDerivedClass>();
            derivedInstance.OtherObject = CreateSomeObject();
            derivedInstance.BaseObject = CreateSomeObject();

            dynamic derivedCopy = method.Invoke(null, new object[] { derivedInstance });
            Assert.NotNull(derivedCopy);
            Assert.Equal(derivedInstance.GetType(), derivedCopy.GetType());
            Assert.NotSame(derivedInstance, derivedCopy);
            AssertCopyOfSomeClass(derivedInstance.OtherObject, derivedCopy.OtherObject);
            AssertCopyOfSomeClass(derivedInstance.BaseObject, derivedCopy.BaseObject);
        }

        [Fact]
        public void TestClassWithDeepCopyExtension_CopyBaseClassCollection()
        {
            var method = StaticTestMethod(typeof(ClassWithDeepCopyExtension), nameof(ClassWithDeepCopyExtension.CopyBaseClassCollection));

            var anotherDerivedClass = TestInstance<AnotherDerivedClass>();
            anotherDerivedClass.BaseObject = CreateSomeObject();
            anotherDerivedClass.AnotherObject = CreateSomeObject();

            var yetAnotherDerivedClass = TestInstance<YetAnotherDerivedClass>();
            yetAnotherDerivedClass.BaseObject = CreateSomeObject();
            yetAnotherDerivedClass.YetAnotherObject = CreateSomeObject();

            var instance = TestInstance<BaseClassCollection>();
            dynamic list = Activator.CreateInstance(typeof(List<>).MakeGenericType(TestType<AbstractBaseClass>()));
            instance.BaseClasses = list;
            instance.BaseClasses.Add(anotherDerivedClass);
            instance.BaseClasses.Add(yetAnotherDerivedClass);

            dynamic copy = method.Invoke(null, new object[] { instance });
            Assert.NotNull(copy);
            Assert.Equal(instance.GetType(), copy.GetType());
            Assert.NotSame(instance, copy);
            Assert.NotNull(copy.BaseClasses);
            Assert.Equal(2, copy.BaseClasses.Count);

            AssertCopyOfSomeClass(instance.BaseClasses[0].BaseObject, copy.BaseClasses[0].BaseObject);
            AssertCopyOfSomeClass(instance.BaseClasses[0].AnotherObject, copy.BaseClasses[0].AnotherObject);
            AssertCopyOfSomeClass(instance.BaseClasses[1].BaseObject, copy.BaseClasses[1].BaseObject);
            AssertCopyOfSomeClass(instance.BaseClasses[1].YetAnotherObject, copy.BaseClasses[1].YetAnotherObject);
        }

        [Fact]
        public void TestDeepCopyExtensionsForNestedTypes()
        {
            var method = StaticTestMethod(typeof(ClassWithDeepCopyExtension), nameof(ClassWithDeepCopyExtension.DeepCopyInnerClassObject));

            var instance = TestInstance<OuterClassObject.InnerClassObject>();
            Assert.NotNull(instance);
            instance.One = TestInstance<OuterClassObject.InnerClassObject.InnerClassOne>();
            instance.One.ObjectOne = CreateSomeObject();
            instance.Two = TestInstance<OuterClassObject.InnerClassObject.InnerClassTwo>();
            instance.Two.ObjectTwo = CreateSomeObject();

            dynamic copy = method.Invoke(null, new object[] { instance });
            Assert.NotNull(copy);
            Assert.Same(instance.GetType(), copy.GetType());
            Assert.NotSame(instance, copy);
            AssertCopyOfSomeClass(instance.One.ObjectOne, copy.One.ObjectOne);
            AssertCopyOfSomeClass(instance.Two.ObjectTwo, copy.Two.ObjectTwo);
        }
    }
}