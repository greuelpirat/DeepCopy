namespace AssemblyToProcess;

public record DerivedRecord : AnyRecord
{
    public int AnotherInteger { get; set; }
    public SomeObject AnotherObject { get; set; }
}