// Copyright © Erickson Lopez. MIT License.
// Run all benchmarks in this assembly
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();

