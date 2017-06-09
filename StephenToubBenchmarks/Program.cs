using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnusedVariable
namespace StephenToubBenchmarks
{
  public class MainConfig : ManualConfig
  {
    public MainConfig()
    {
      Add(Job.Default.With(Runtime.Clr).With(Jit.RyuJit).With(Platform.X64).WithId("NET4.7_RyuJIT-x64"));
      Add(Job.Default.With(Runtime.Mono).WithId("Mono5.0.1-x64"));
      Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20).WithId("Core2.0-x64"));
      Add(RPlotExporter.Default);
      KeepBenchmarkFiles = true;
    }
  }

  public class MonitoringConfig : ManualConfig
  {
    public MonitoringConfig()
    {
      Add(Job.Default.With(RunStrategy.Monitoring).With(Runtime.Clr).With(Jit.RyuJit).With(Platform.X64).WithId("NET4.7_RyuJIT-x64"));
      Add(Job.Default.With(RunStrategy.Monitoring).With(Runtime.Mono).WithId("Mono5.0.1-x64"));
      Add(Job.Default.With(RunStrategy.Monitoring).With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20).WithId("Core2.0-x64"));
      Add(RPlotExporter.Default);
      KeepBenchmarkFiles = true;
    }
  }


  // *** Collections ***

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class QueueBenchmark1
  {
    private Queue<int> q = new Queue<int>();

    [Benchmark]
    public void Run()
    {
      q.Enqueue(0);
      q.Dequeue();
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class QueueBenchmark2
  {
    private Queue<int> q = new Queue<int>();

    [Params(10, 100, 1_000, 10_000)]
    public int N;

    [Benchmark]
    public void Run()
    {
      for (int i = 0; i < N; i++)
        q.Enqueue(i);
      for (int i = 0; i < N; i++)
        q.Dequeue();
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class SortedSetBenchmark1
  {
    private SortedSet<int> s = new SortedSet<int>();

    [Params(1, 100, 100_000, 10_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      for (int i = 0; i < N; i++)
        s.Add(i);
    }

    [Benchmark]
    public int Run() => s.Min;
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class ListBenchmark1
  {
    private List<int> l = new List<int>();

    [Benchmark]
    public void Run()
    {
      l.Add(0);
      l.RemoveAt(0);
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class ListBenchmark2
  {
    private List<int> l = new List<int>();

    [Params(10, 100, 1000)]
    public int N;

    [Benchmark]
    public void Run()
    {
      for (int i = 0; i < N; i++)
        l.Add(i);
      for (int i = 0; i < N; i++)
        l.RemoveAt(N - 1 - i);
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  [MemoryDiagnoser]
  public class ConcurrentQueueBenchmark1
  {
    private ConcurrentQueue<int> q = new ConcurrentQueue<int>();

    [Benchmark]
    public void Run()
    {
      q.Enqueue(0);
      q.TryDequeue(out int _);
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  public class ConcurrentQueueBenchmark2
  {
    private ConcurrentQueue<int> q = new ConcurrentQueue<int>();

    [Params(100, 100_000, 1_000_000)]
    public int N;

    [Benchmark]
    public void Run()
    {
      for (int i = 0; i < N; i++)
        q.Enqueue(0);
      for (int i = 0; i < N; i++)
        q.TryDequeue(out int _);
    }
  }

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("Collections")]
  public class ConcurrentQueueBenchmark3
  {
    private ConcurrentQueue<int> q = new ConcurrentQueue<int>();

    [Params(100_000_000)]
    public int N;

    [Benchmark]
    public void Run()
    {
      Task consumer = Task.Run(() =>
      {
        int total = 0;
        while (total < N) if (q.TryDequeue(out int _)) total++;
      });
      for (int i = 0; i < N; i++)
        q.Enqueue(i);
      consumer.Wait();
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Collections")]
  [MemoryDiagnoser]
  public class ConcurrentBag
  {
    private ConcurrentBag<int> q = new ConcurrentBag<int> { 1, 2 };

    [Benchmark]
    public void Run()
    {
      q.Add(0);
      q.TryTake(out int _);
    }
  }

  // *** LINQ ***

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("LINQ")]
  public class ConcatBenchmark
  {
    private IEnumerable<int> zeroToTen, result;

    [Params(1_000, 10_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      zeroToTen = Enumerable.Range(0, 10);
      result = zeroToTen;
      for (int i = 0; i < N; i++)
        result = result.Concat(zeroToTen);
    }

    [Benchmark]
    public void Run()
    {
      foreach (int i in result) { }
    }
  }

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("LINQ")]
  public class OrderByBenchmark
  {
    private IEnumerable<int> nToZero;

    [Params(100, 1_000, 10_000, 10_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      nToZero = Enumerable.Range(0, N).Reverse();
    }

    [Benchmark]
    public void Run() => nToZero.OrderBy(i => i).Skip(4).First();
  }

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("LINQ")]
  public class SelectBenchmark
  {
    private IEnumerable<int> nToZero;

    [Params(100, 1_000, 10_000, 10_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      nToZero = Enumerable.Range(0, N).Reverse();
    }

    [Benchmark]
    public void Run() => nToZero.Select(i => i).ToList();
  }

  [MemoryDiagnoser]
  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("LINQ")]
  public class ToArrayBenchmark
  {
    private IEnumerable<int> source;

    [Params(1_000, 10_000, 100_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      source = Enumerable.Range(0, N);
    }

    [Benchmark]
    public void Run() => source.ToArray();
  }

  // *** Compression ***

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Compression")]
  public class DeflateStreamBenchmark
  {
    private byte[] raw;

    [Params(1 * 1024 * 1024, 100 * 1024 * 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      raw = new byte[N];
      for (int i = 0; i < raw.Length; i++) raw[i] = (byte)i;
    }

    [Benchmark]
    public void Run()
    {
      // Compress it
      var compressed = new MemoryStream();
      using (DeflateStream ds = new DeflateStream(compressed, CompressionMode.Compress, true))
      {
        ds.Write(raw, 0, raw.Length);
      }
      compressed.Position = 0;

      // Decompress it
      var decompressed = new MemoryStream();
      using (DeflateStream ds = new DeflateStream(compressed, CompressionMode.Decompress))
      {
        ds.CopyTo(decompressed);
      }
      decompressed.Position = 0;
    }
  }

  // *** Cryptography ***

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Cryptography")]
  public class SHA256Benchmark
  {
    private byte[] raw;
    private SHA256 sha;

    [Params(1 * 1024 * 1024, 100 * 1024 * 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      sha = SHA256.Create();
      raw = new byte[N];
      for (int i = 0; i < raw.Length; i++) raw[i] = (byte)i;
    }

    [Benchmark]
    public byte[] Run() => sha.ComputeHash(raw);
  }

  // *** Math ***

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("Math")]
  public class BigIntegerModPowBenchmark
  {
    private BigInteger a, b, c;

    [GlobalSetup]
    public void Setup()
    {
      var rand = new Random(42);
      a = Create(rand, 8192);
      b = Create(rand, 8192);
      c = Create(rand, 8192);
    }

    [Benchmark]
    public void Run() => BigInteger.ModPow(a, b, c);

    private static BigInteger Create(Random rand, int bits)
    {
      var value = new byte[(bits + 7) / 8 + 1];
      rand.NextBytes(value);
      value[value.Length - 1] = 0;
      return new BigInteger(value);
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("Math")]
  public class DivRemBenchmark
  {
    private static long a = 99, b = 10, rem;

    [Benchmark]
    public long Run() => Math.DivRem(a, b, out rem);
  }

  // *** Serialization ***

  [Config(typeof(MonitoringConfig))]
  [BenchmarkCategory("Serialization")]
  public class SerializationBenchmark
  {
    private BinaryFormatter formatter;
    private MemoryStream mem;

    [Params(100_000, 1_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      var books = new List<Book>();
      for (int i = 0; i < 1_000_000; i++)
      {
        string id = i.ToString();
        books.Add(new Book { Name = id, Id = id });
      }

      formatter = new BinaryFormatter();
      mem = new MemoryStream();
      formatter.Serialize(mem, books);
    }

    [Benchmark]
    public void Run()
    {
      mem.Position = 0;
      formatter.Deserialize(mem);
    }

    [Serializable]
    private class Book
    {
#pragma warning disable 414
      public string Name;
      public string Id;
#pragma warning restore 414
    }
  }

  // *** TextProcessing ***

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  [MemoryDiagnoser]
  public class RegexIsMatchBenchmark
  {
    [Benchmark]
    public bool Run() => Regex.IsMatch("555-867-5309", @"^\d{3}-\d{3}-\d{4}$");
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  [MemoryDiagnoser]
  public class UrlDecodeBenchmark
  {
    [Benchmark]
    public string Run() => WebUtility.UrlDecode("abcdefghijklmnopqrstuvwxyz");
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  [MemoryDiagnoser]
  public class Utf8GetBytesBenchmark
  {
    private string s;

    [Params(256, 512, 1024, 2048)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      s = new string(Enumerable.Range(0, 1024).Select(i => (char)('a' + i)).ToArray());
    }

    [Benchmark]
    public byte[] Run() => Encoding.UTF8.GetBytes(s);
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  [MemoryDiagnoser]
  public class EnumParseBenchmark
  {
    [Benchmark]
    public object Run() => Enum.Parse(typeof(Colors), "Red, Orange, Yellow, Green, Blue");

    [Flags]
    private enum Colors
    {
      Red = 0x1,
      Orange = 0x2,
      Yellow = 0x4,
      Green = 0x8,
      Blue = 0x10
    }
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  [MemoryDiagnoser]
  public class DateTimeToStringBenchmark
  {
    private DateTime dt = new DateTime(2017, 06, 07, 1, 2, 3);

    [Benchmark]
    public string RunO() => dt.ToString("o");

    [Benchmark]
    public string RunR() => dt.ToString("r");
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  public class IndexOfBenchmark
  {
    private string s;

    [Params(10, 100, 500)]
    public int N;

    [GlobalSetup]
    public void Setup() => s = string.Concat(Enumerable.Repeat("a", N)) + "b";

    [Benchmark]
    public void Run() => s.IndexOf('b');
  }

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("TextProcessing")]
  public class StartWithBenchmark
  {
    private string s = "abcdefghijklmnopqrstuvwxyz";

    [Benchmark]
    public void Run() => s.StartsWith("abcdefghijklmnopqrstuvwxy-", StringComparison.Ordinal);
  }

  // *** FileSystem ***

  [Config(typeof(MainConfig))]
  [BenchmarkCategory("FileSystem")]
  [MemoryDiagnoser]
  public class FileSystemBenchmark
  {
    private string inputPath = Path.GetTempFileName(), outputPath = Path.GetTempFileName();

    [Params(1_000_000, 50_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      byte[] data = new byte[50_000_000];
      new Random(42).NextBytes(data);
      File.WriteAllBytes(inputPath, data);
    }

    [Benchmark]
    public async Task Run()
    {
      using (var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, useAsync: true))
      using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x1000, useAsync: true))
      {
        await input.CopyToAsync(output);
      }
    }
  }

  class Program
  {
    static void Main(string[] args)
    {
      BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);

      // BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(new[] { "*", "--category=Collections" });
      // BenchmarkRunner.Run<ConcurrentBag>();
    }
  }
}