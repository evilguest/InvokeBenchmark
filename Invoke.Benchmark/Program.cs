using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using InvokeBenchmark;

#nullable enable

namespace Invoke.Benchmark
{

    [DisassemblyDiagnoser]
    [InProcess]
    public class MethodCallTest
    {

        public static IEnumerable<CallMethod> Arguments()
        {
            var bar = Helper.GetIBar();

            return new CallMethod[] {
            new("Interface", new WrapperOverInterface(bar)),
            new("Delegate", new WrapperOverDelegate(bar)),
            new("Delegate from MI", new WrapperOverDelegateFromMethodInfo(bar)),
            new("Delegate from ptr", new WrapperOverDelegateFromPtr(bar)),
            new("Ptr from delegate", new WrapperOverPtrFromDelegate(bar)),
            new("Managed func ptr", new WrapperOverManagedFunPtr(bar)),
            new("Unmanaged func ptr", new WrapperOverUnmanagedFunPtr(bar)),
            new("DllImport", new WrapperOverDllImport()),
            new("DllGetProcAddr", new WrapperOverDllGetProcAddr()),
            new("DllImportFCnGC", new WrapperOverNoGcTransitionDllImport())
        };
        }

        [Benchmark(Baseline = true)]
        public ulong Baseline()
        {
            var fn = new Baseline();
            ulong sum = 0;

            for (var i = 0; i < 1_000_000; i++)
                fn.Foo(ref sum);

            return sum;
        }

        [ArgumentsSource(nameof(Arguments))]
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public ulong CallTest(CallMethod m)
        {
            var fn = m.Func;
            ulong sum = 0;

            for (var i = 0; i < 1_000_000; i++)
                fn.Foo(ref sum);

            return sum;
        }

        public record CallMethod(string Name, MethodWrapper Func)
        {
            public override string ToString() => Name;
        }
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<MethodCallTest>(DefaultConfig.Instance.AddColumn(new BaselineOffsetColumn("[m=Managed func ptr]")).WithOrderer(new FastestToSlowestOrderer()), args);
        }
    }
}