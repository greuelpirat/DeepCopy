namespace AssemblyToProcess
{
    public static class Demo
    {
        public static SomeObject Copy(SomeObject source)
        {
            return source != null ? new SomeObject() : null;
        }
    }
}