using System;
using System.Diagnostics;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple benchmarking strategy.
/// Measures execution time of a trivial operation to demonstrate benchmarking logic.
/// </summary>
public class BenchStrategy : Strategy
{
	private readonly StrategyParam<int> _samples;
	private readonly StrategyParam<int> _loops;

	/// <summary>
	/// Number of samples to run.
	/// </summary>
	public int Samples
	{
		get => _samples.Value;
		set => _samples.Value = value;
	}

	/// <summary>
	/// Number of iterations in each sample.
	/// </summary>
	public int Loops
	{
		get => _loops.Value;
		set => _loops.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BenchStrategy()
	{
		_samples = Param(nameof(Samples), 10)
			.SetGreaterThanZero()
			.SetDisplay("Samples", "Total samples to execute", "Benchmark");

		_loops = Param(nameof(Loops), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Loops", "Iterations inside each sample", "Benchmark");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Array.Empty<(Security, DataType)>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		RunBenchmark();

		Stop();
	}

	private void RunBenchmark()
	{
		var sw = new Stopwatch();
		var totalTicks = 0L;
		var minTicks = long.MaxValue;
		var maxTicks = 0L;

		for (var s = 0; s < Samples; s++)
		{
			sw.Restart();

			for (var i = 0; i < Loops; i++)
			{
				// Trivial operation under test
				_ = Math.Abs(i);
			}

			sw.Stop();

			var ticks = sw.ElapsedTicks;
			totalTicks += ticks;

			if (ticks < minTicks)
				minTicks = ticks;

			if (ticks > maxTicks)
				maxTicks = ticks;
		}

		var average = totalTicks / (decimal)Samples;

		LogInfo($"Benchmark loops: {Loops}, samples: {Samples}");
		LogInfo($"Average ticks: {average}, min ticks: {minTicks}, max ticks: {maxTicks}");
	}
}
