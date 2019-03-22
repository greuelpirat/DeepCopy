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
            if (source.List != null)
            {
                List = new List<string>();
                for (var i = 0; i < source.List.Count; i++)
                    List.Add(source.List[i] != null ? string.Copy(source.List[i]) : null);

                for (var i = 0; i < source.Ints.Count; i++)
                    Ints.Add(source.Ints[i]);
            }
        }

        public IList<string> List { get; set; }
        public IList<int> Ints { get; set; }
    }
}