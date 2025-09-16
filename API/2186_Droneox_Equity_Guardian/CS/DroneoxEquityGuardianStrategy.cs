using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors account equity and closes all positions when the equity reaches the target or stop levels.
/// Optionally stops the strategy to disable further trading.
/// </summary>
public class DroneoxEquityGuardianStrategy : Strategy
{
	private readonly StrategyParam<decimal> _equityTarget;
	private readonly StrategyParam<decimal> _equityStop;
	private readonly StrategyParam<bool> _closePositions;
	private readonly StrategyParam<bool> _disableTrading;

	private bool _triggered;

	/// <summary>
	/// Initializes a new instance of the <see cref="DroneoxEquityGuardianStrategy"/> class.
	/// </summary>
	public DroneoxEquityGuardianStrategy()
	{
		_equityTarget = Param(nameof(EquityTarget), 999999m)
			.SetDisplay("Equity Target", "Equity take profit in USD", "Risk");

		_equityStop = Param(nameof(EquityStop), 0m)
			.SetDisplay("Equity Stop", "Equity stop loss in USD", "Risk");

		_closePositions = Param(nameof(ClosePositions), true)
			.SetDisplay("Close Positions", "Close all positions when threshold reached", "Risk");

		_disableTrading = Param(nameof(DisableTrading), true)
			.SetDisplay("Disable Trading", "Stop the strategy after closing positions", "Risk");
	}

	/// <summary>Equity take profit in USD.</summary>
	public decimal EquityTarget
	{
		get => _equityTarget.Value;
		set => _equityTarget.Value = value;
	}

	/// <summary>Equity stop loss in USD.</summary>
	public decimal EquityStop
	{
		get => _equityStop.Value;
		set => _equityStop.Value = value;
	}

	/// <summary>Close all positions when threshold is reached.</summary>
	public bool ClosePositions
	{
		get => _closePositions.Value;
		set => _closePositions.Value = value;
	}

	/// <summary>Stop the strategy after closing positions.</summary>
	public bool DisableTrading
	{
		get => _disableTrading.Value;
		set => _disableTrading.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		Timer.Start(TimeSpan.FromSeconds(1), CheckEquity);
	}

	private void CheckEquity()
	{
		if (_triggered)
			return;

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (equity <= EquityStop)
		{
			ProcessThreshold("Equity stop level reached");
		}
		else if (equity >= EquityTarget)
		{
			ProcessThreshold("Equity target level reached");
		}
	}

	private void ProcessThreshold(string message)
	{
		if (ClosePositions)
			CloseAll();

		if (DisableTrading)
			Stop();

		AddInfo(message);
		_triggered = true;
	}

	private void CloseAll()
	{
		CancelActiveOrders();

		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}
}
