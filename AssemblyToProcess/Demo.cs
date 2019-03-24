using System.Collections.Generic;

namespace AssemblyToProcess
{
    public class Demo
    {
        public Demo(Demo source)
        {
            if (source.Dictionary != null)
            {
                Dictionary = new Dictionary<string, string>();
                foreach (var pair in source.Dictionary)
                    Dictionary[string.Copy(pair.Key)] = pair.Value != null ? string.Copy(pair.Value) : null;
            }
        }

        //public IDictionary<string, string> Dictionary { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
    }
}