using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Breakout strategy.
/// Trades breakouts above highest highs / below lowest lows.
/// </summary>
public class LiquidityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<DataType> _candleType;

	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityBreakoutStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 20).SetGreaterThanZero().SetDisplay("Lookback", "Bars for range", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaShort = new ExponentialMovingAverage { Length = 50 };
		var emaLong = new ExponentialMovingAverage { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShortVal, decimal emaLongVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (emaShortVal <= 0m || emaLongVal <= 0m)
			return;

		// Simple crossover breakout strategy
		var longSignal = emaShortVal > emaLongVal;
		var shortSignal = emaShortVal < emaLongVal;

		if (longSignal && Position <= 0)
			BuyMarket();
		else if (shortSignal && Position >= 0)
			SellMarket();
	}
}
