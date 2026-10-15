using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Internal Market Shift strategy.
/// Detects internal market structure shifts at liquidity zones.
/// </summary>
public class LiquidityInternalMarketShiftStrategy : Strategy
{
	private readonly StrategyParam<int> _upperLB;
	private readonly StrategyParam<int> _lowerLB;
	private readonly StrategyParam<DataType> _candleType;

	public int UpperLiquidityLookback { get => _upperLB.Value; set => _upperLB.Value = value; }
	public int LowerLiquidityLookback { get => _lowerLB.Value; set => _lowerLB.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityInternalMarketShiftStrategy()
	{
		_upperLB = Param(nameof(UpperLiquidityLookback), 14).SetGreaterThanZero().SetDisplay("Upper LB", "Upper", "Signals");
		_lowerLB = Param(nameof(LowerLiquidityLookback), 14).SetGreaterThanZero().SetDisplay("Lower LB", "Lower", "Signals");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candles", "General");
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

		var highest = new Highest { Length = UpperLiquidityLookback };
		var lowest = new Lowest { Length = LowerLiquidityLookback };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(highest, lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, sub); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestVal, decimal lowestVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (highestVal == 0m || lowestVal == 0m)
			return;

		var bull = candle.ClosePrice > candle.OpenPrice;
		var bear = candle.ClosePrice < candle.OpenPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;

		// Market shift detection: strong candle at liquidity zone
		var strongCandle = range > 0 && body / range > 0.5m;

		// Bullish shift: price sweeps low and closes strong bullish
		var bullSignal = bull && strongCandle && candle.LowPrice <= lowestVal && candle.ClosePrice > lowestVal;
		// Bearish shift: price sweeps high and closes strong bearish
		var bearSignal = bear && strongCandle && candle.HighPrice >= highestVal && candle.ClosePrice < highestVal;

		if (bearSignal && Position >= 0)
			SellMarket();
		else if (bullSignal && Position <= 0)
			BuyMarket();
	}
}
