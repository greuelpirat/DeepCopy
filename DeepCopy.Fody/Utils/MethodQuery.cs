namespace DeepCopy.Fody.Utils
{
    public readonly struct MethodQuery
    {
        public readonly string ReturnType;
        public readonly string DeclaringType;
        public readonly string Name;
        public readonly string Arguments;

        public MethodQuery(string returnType, string declaringType, string name, string arguments)
        {
            ReturnType = returnType;
            DeclaringType = declaringType;
            Name = name;
            Arguments = arguments;
        }

        public static implicit operator MethodQuery(string name) => new(null, null, name, null);
    }
}