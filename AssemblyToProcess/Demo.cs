using System.Collections.Generic;

namespace AssemblyToProcess
{
    public class Demo
    {
        public Demo(Demo source)
        {
            if (source.Ints != null)
            {
                Ints = new Dictionary<int, int>();
                foreach (var pair in source.Ints)
                    Ints[pair.Key] = pair.Value;
            }

            /*
            if (source.Strings != null)
            {
                Strings = new Dictionary<string, string>();
                foreach (var pair in source.Strings)
                    Strings[pair.Key] = pair.Value == null ? null : string.Copy(pair.Value);
            }
            */
        }

        public IDictionary<int, int> Ints { get; set; }
        public IDictionary<string, string> Strings { get; set; }
    }
}