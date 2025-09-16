using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on calculated H4 and L4 levels.
/// Places daily pending orders at H4 and L4 with stop loss and take profit.
/// </summary>
public class H4L4BreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="H4L4BreakoutStrategy"/>.
	/// </summary>
	public H4L4BreakoutStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 57m)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_stopLoss = Param(nameof(StopLoss), 7m)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security.PriceStep ?? 1m;

		StartProtection(
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Reset orders and positions at the end of each day
		CancelActiveOrders();
		if (Position != 0)
			ClosePosition();

		// Ensure trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = (candle.HighPrice - candle.LowPrice) * 1.1m / 2m;
		var h4 = candle.ClosePrice + range;
		var l4 = candle.ClosePrice - range;

		var volume = Volume + Math.Abs(Position);

		// Place daily pending orders
		SellLimit(h4, volume);
		BuyLimit(l4, volume);
	}
}
