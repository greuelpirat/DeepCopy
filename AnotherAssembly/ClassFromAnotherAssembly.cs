namespace AnotherAssembly
{
    public class ClassFromAnotherAssembly
    {
        public string Property { get; set; }

        public ClassFromAnotherAssembly() { }

        public ClassFromAnotherAssembly(ClassFromAnotherAssembly source)
        {
            Property = source.Property;
        }
    }
}