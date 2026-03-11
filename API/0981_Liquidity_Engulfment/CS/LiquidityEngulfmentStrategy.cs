using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity and Engulfment combination strategy.
/// </summary>
public class LiquidityEngulfmentStrategy : Strategy
{
	private readonly StrategyParam<int> _upperLookback;
	private readonly StrategyParam<int> _lowerLookback;
	private readonly StrategyParam<DataType> _candleType;

	public int UpperLookback { get => _upperLookback.Value; set => _upperLookback.Value = value; }
	public int LowerLookback { get => _lowerLookback.Value; set => _lowerLookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityEngulfmentStrategy()
	{
		_upperLookback = Param(nameof(UpperLookback), 14).SetGreaterThanZero().SetDisplay("Upper Lookback", "Upper liquidity", "Indicators");
		_lowerLookback = Param(nameof(LowerLookback), 14).SetGreaterThanZero().SetDisplay("Lower Lookback", "Lower liquidity", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
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

		var highest = new Highest { Length = UpperLookback };
		var lowest = new Lowest { Length = LowerLookback };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(highest, lowest, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal highestVal, decimal lowestVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (highestVal == 0m || lowestVal == 0m)
			return;

		var bull = candle.ClosePrice > candle.OpenPrice;
		var bear = candle.ClosePrice < candle.OpenPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;

		// Engulfing candle: body is large relative to range
		var engulfing = range > 0 && body / range > 0.6m;

		// Bullish: price touched lower liquidity zone and engulfing bullish candle
		var bullSignal = bull && engulfing && candle.LowPrice <= lowestVal;
		// Bearish: price touched upper liquidity zone and engulfing bearish candle
		var bearSignal = bear && engulfing && candle.HighPrice >= highestVal;

		if (bearSignal && Position >= 0)
			SellMarket();
		else if (bullSignal && Position <= 0)
			BuyMarket();
	}
}
