namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

/// <summary>
/// Generates a path using a simple Markov Chain Monte Carlo simulation.
/// </summary>
public class FunctionSimpleMarkovChainMonteCarloSimulationStrategy : Strategy
{
	private readonly StrategyParam<int> _iterations;
	private readonly StrategyParam<decimal> _startValue;
	private readonly StrategyParam<decimal> _deviationAmount;

	private readonly Random _random = new();

	/// <summary>
	/// Number of simulation steps.
	/// </summary>
	public int Iterations
	{
		get => _iterations.Value;
		set => _iterations.Value = value;
	}

	/// <summary>
	/// Initial value for simulation.
	/// </summary>
	public decimal StartValue
	{
		get => _startValue.Value;
		set => _startValue.Value = value;
	}

	/// <summary>
	/// Maximum deviation per step.
	/// </summary>
	public decimal DeviationAmount
	{
		get => _deviationAmount.Value;
		set => _deviationAmount.Value = value;
	}

	public FunctionSimpleMarkovChainMonteCarloSimulationStrategy()
	{
		_iterations = Param(nameof(Iterations), 100)
			.SetDisplay("Iterations", "Number of simulation steps", "General");
		_startValue = Param(nameof(StartValue), 500m)
			.SetDisplay("Start Value", "Initial simulation value", "General");
		_deviationAmount = Param(nameof(DeviationAmount), 25m)
			.SetDisplay("Deviation Amount", "Maximum deviation per step", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		RunSimulation();
	}

	private void RunSimulation()
	{
		var path = new decimal[Iterations];
		path[0] = StartValue;

		for (var i = 1; i < Iterations; i++)
		{
			path[i] = MarkovChainStep(path[i - 1], DeviationAmount);
		}
	}

	private decimal MarkovChainStep(decimal baseValue, decimal deviation)
	{
		var distance = (decimal)_random.NextDouble() * deviation;
		var p = _random.NextDouble();

		if (p < 0.3)
			return baseValue + distance;
		if (p < 0.6)
			return baseValue - distance;
		return baseValue;
	}
}
