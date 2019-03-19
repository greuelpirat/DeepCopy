using DeepCopyConstructor;

namespace SmokeTest
{
    [AddDeepCopyConstructor]
    public class SmokeTest
    {
        public string[] Strings { get; set; }
    }

    public class Demo
    {
        public Demo(Demo source)
        {
            if (source.Strings != null)
            {
                Strings = new string[source.Strings.Length];
                for (var i = 0; i < source.Strings.Length; i++)
                {
                    Strings[i] = string.Copy(source.Strings[i]);
                }
            }
        }

        public string[] Strings { get; set; }
    }
}