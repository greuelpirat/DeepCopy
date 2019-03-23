using System.Collections.Generic;
using DeepCopyConstructor;

namespace SmokeTest
{
    [AddDeepCopyConstructor]
    public class SmokeTest
    {
        public IList<string> List { get; set; }
        public string[] Array { get; set; }
    }

    public class Demo
    {
        public Demo(Demo source)
        {
            if (source.Array != null)
            {
                Array = new string[source.Array.Length];
                for (var i = 0; i < source.Array.Length; i++)
                    Array[i] = source.Array[i] != null ? string.Copy(source.Array[i]) : null;
            }

            if (source.List != null)
            {
                List = new List<int>();
                for (var i = 0; i < source.List.Count; i++)
                    List.Add(source.List[i]);
            }

            if (source.Ints != null)
            {
                Ints = new Dictionary<int, int>();
                foreach (var pair in Ints)
                    Ints[pair.Key] = pair.Value;
            }

            if (source.Strings != null)
            {
                Strings = new Dictionary<string, string>();
                foreach (var pair in Strings)
                    Strings[pair.Key] = pair.Value == null ? null : string.Copy(pair.Value);
            }
        }

        public string[] Array { get; set; }
        public IList<int> List { get; set; }
        public IDictionary<int, int> Ints { get; set; }
        public IDictionary<string, string> Strings { get; set; }
    }
}