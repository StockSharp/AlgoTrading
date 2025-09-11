using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates retrieval of basic fundamental metrics like cash, debt and revenue.
/// Logs values around earnings reports without generating trading signals.
/// </summary>
public class FunamentalAndFinancialsStrategy : Strategy
{
	private readonly StrategyParam<string> _output;
	private readonly StrategyParam<string> _period;

	/// <summary>
	/// Output display mode.
	/// </summary>
	public string Output
	{
		get => _output.Value;
		set => _output.Value = value;
	}

	/// <summary>
	/// Reporting period.
	/// </summary>
	public string Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public FunamentalAndFinancialsStrategy()
	{
		_output = Param(nameof(Output), "Per Share")
			.SetDisplay("Output", "Output type", "General")
			.SetCanOptimize(true);

		_period = Param(nameof(Period), "FQ")
			.SetDisplay("Period", "Reporting period", "General")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// TODO integrate fundamental data requests and logging
	}
}
