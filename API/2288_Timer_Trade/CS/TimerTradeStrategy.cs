using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that alternates buy and sell orders at fixed time intervals.
/// Each position is protected with stop-loss and take-profit.
/// </summary>
public class TimerTradeStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _timerInterval;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossLevel;
	private readonly StrategyParam<decimal> _takeProfitLevel;

	private bool _isBuyNext = true;

	/// <summary>
	/// Interval between timer events.
	/// </summary>
	public TimeSpan TimerInterval
	{
		get => _timerInterval.Value;
		set => _timerInterval.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossLevel
	{
		get => _stopLossLevel.Value;
		set => _stopLossLevel.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitLevel
	{
		get => _takeProfitLevel.Value;
		set => _takeProfitLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TimerTradeStrategy"/>.
	/// </summary>
	public TimerTradeStrategy()
	{
		_timerInterval = Param(nameof(TimerInterval), TimeSpan.FromSeconds(30))
			.SetDisplay("Timer Interval", "Interval between trades", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetGreaterThanZero();

		_stopLossLevel = Param(nameof(StopLossLevel), 10m)
			.SetDisplay("Stop Loss Level", "Stop loss in points", "Risk")
			.SetGreaterThanZero();

		_takeProfitLevel = Param(nameof(TakeProfitLevel), 50m)
			.SetDisplay("Take Profit Level", "Take profit in points", "Risk")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, null)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(TakeProfitLevel, UnitTypes.Step),
			stopLoss: new Unit(StopLossLevel, UnitTypes.Step));

		Timer.Start(TimerInterval, ProcessTimer);
	}

	private void ProcessTimer()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isBuyNext)
			BuyMarket(Volume);
		else
			SellMarket(Volume);

		_isBuyNext = !_isBuyNext;
	}
}
