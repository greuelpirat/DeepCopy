[![build](https://github.com/greuelpirat/DeepCopy/actions/workflows/build.yml/badge.svg)](https://github.com/greuelpirat/DeepCopy/actions/workflows/build.yml)
[![nuget](https://img.shields.io/nuget/v/DeepCopy.Fody.svg)](https://www.nuget.org/packages/DeepCopy.Fody/)


## This is an add-in for [Fody](https://github.com/Fody/Home/)

![Icon](https://raw.githubusercontent.com/greuelpirat/DeepCopy/main/package_icon.png)

Generate copy constructors and extension methods to create a new instance with deep copy of properties.

## Usage

See [Wiki](https://github.com/greuelpirat/DeepCopy/wiki)

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).

### NuGet installation

Install the [DeepCopy.Fody NuGet package](https://nuget.org/packages/DeepCopy.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package DeepCopy.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.

### Add to FodyWeavers.xml

Add `<DeepCopy/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <DeepCopy/>
</Weavers>
```

## Sample
Source at `SmokeTest\ReadMeSample.cs`

#### Your Code
```csharp
public class ReadMeSample
{
  public ReadMeSample() { }
  [DeepCopyConstructor] public ReadMeSample(ReadMeSample source) { }
  
  public int Integer { get; set; }
  public ReadMeEnum Enum { get; set; }
  public DateTime DateTime { get; set; }
  public string String { get; set; }
  public IList<ReadMeSample> List { get; set; }
  public IDictionary<ReadMeEnum, ReadMeSample> Dictionary { get; set; }
}
```

#### What gets compiled
```csharp
public class ReadMeSample
{
  public ReadMeSample() { }

  public ReadMeSample(ReadMeSample source)
  {
    this.Integer = source.Integer;
    this.Enum = source.Enum;
    this.DateTime = source.DateTime;
    this.String = source.String != null ? new string(source.String.ToCharArray()) : (string)null;
    if (source.List != null)
    {
      IList<ReadMeSample> readMeSampleList = (IList<ReadMeSample>)new System.Collections.Generic.List<ReadMeSample>();
      foreach (ReadMeSample source1 in (IEnumerable<ReadMeSample>)source.List)
        readMeSampleList.Add(source1 != null ? new ReadMeSample(source1) : (ReadMeSample)null);
      this.List = readMeSampleList;
    }
    if (source.Dictionary == null)
      return;
    IDictionary<ReadMeEnum, ReadMeSample> dictionary = (IDictionary<ReadMeEnum, ReadMeSample>)new System.Collections.Generic.Dictionary<ReadMeEnum, ReadMeSample>();
    foreach (KeyValuePair<ReadMeEnum, ReadMeSample> keyValuePair in (IEnumerable<KeyValuePair<ReadMeEnum, ReadMeSample>>)source.Dictionary)
    {
      ReadMeEnum key = keyValuePair.Key;
      ReadMeSample readMeSample = keyValuePair.Value != null ? new ReadMeSample(keyValuePair.Value) : (ReadMeSample)null;
      dictionary[key] = readMeSample;
    }
    this.Dictionary = dictionary;
  }
  
  public int Integer { get; set; }
  public ReadMeEnum Enum { get; set; }
  public DateTime DateTime { get; set; }
  public string String { get; set; }
  public IList<ReadMeSample> List { get; set; }
  public IDictionary<ReadMeEnum, ReadMeSample> Dictionary { get; set; }
}
```
Decompiled with `JetBrains dotPeek 2021.2.1 Build 212.0.20210826.112035`

## Icon

Icon copy by projecthayat  of [The Noun Project](http://thenounproject.com)