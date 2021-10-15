using DeepCopy;
using System;
using System.Collections.Generic;

namespace SmokeTest
{
    public enum ReadMeEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class ReadMeSample
    {
        public ReadMeSample() { }
        // ReSharper disable once UnusedParameter.Local
        [InjectDeepCopy] public ReadMeSample(ReadMeSample source) { }

        public int Integer { get; set; }
        public ReadMeEnum Enum { get; set; }
        public DateTime DateTime { get; set; }
        public string String { get; set; }
        public IList<ReadMeSample> List { get; set; }
        public IDictionary<ReadMeEnum, ReadMeSample> Dictionary { get; set; }
    }
}