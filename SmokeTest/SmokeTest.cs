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
            if (source.Demos != null)
            {
                Demos = new Demo[source.Demos.Length];
                for (var i = 0; i < source.Demos.Length; i++)
                {
                    if (source.Demos[i] != null)
                        Demos[i] = new Demo(source.Demos[i]);
                }
            }
        }

        public Demo[] Demos { get; set; }
    }
}