namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Automatically applies take-profit and stop-loss to existing positions.
/// </summary>
public class AutostopStrategy : Strategy
{
	private readonly StrategyParam<bool> _monitorTakeProfit;
	private readonly StrategyParam<bool> _monitorStopLoss;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;

	public bool MonitorTakeProfit { get => _monitorTakeProfit.Value; set => _monitorTakeProfit.Value = value; }
	public bool MonitorStopLoss { get => _monitorStopLoss.Value; set => _monitorStopLoss.Value = value; }
	public int TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="AutostopStrategy"/>.
	/// </summary>
	public AutostopStrategy()
	{
		_monitorTakeProfit = Param(nameof(MonitorTakeProfit), true)
			.SetDisplay("Monitor Take Profit", "Enable automatic take profit", "General");
		_monitorStopLoss = Param(nameof(MonitorStopLoss), true)
			.SetDisplay("Monitor Stop Loss", "Enable automatic stop loss", "General");
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 30)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (ticks)", "Take profit distance in ticks", "Risk");
		_stopLossTicks = Param(nameof(StopLossTicks), 30)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (ticks)", "Stop loss distance in ticks", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 1m;
		var tp = MonitorTakeProfit ? new Unit(TakeProfitTicks * step, UnitTypes.Point) : new Unit();
		var sl = MonitorStopLoss ? new Unit(StopLossTicks * step, UnitTypes.Point) : new Unit();

		StartProtection(tp, sl);
	}
}
