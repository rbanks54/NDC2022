using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnostics.Windows;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args,
        DefaultConfig.Instance
            //    .AddDiagnoser(new EtwProfiler(
            //    new EtwProfilerConfig(performExtraBenchmarksRun: false))
            //)
            .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()))
        );
