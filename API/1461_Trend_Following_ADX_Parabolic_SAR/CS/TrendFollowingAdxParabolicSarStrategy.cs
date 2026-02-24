using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using ADX (via StdDev proxy) and EMA.
/// Opens long when trend is strong upward, short when strong downward.
/// </summary>
public class TrendFollowingAdxParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	public int TrendPeriod { get => _trendPeriod.Value; set => _trendPeriod.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendFollowingAdxParabolicSarStrategy()
	{
		_trendPeriod = Param(nameof(TrendPeriod), 14)
			.SetDisplay("Trend Period", "Period for trend strength", "Parameters");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast EMA", "Fast EMA period", "Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow EMA", "Slow EMA period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var stdDev = new StandardDeviation { Length = TrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || stdVal <= 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;
		// Trend strength: how far apart the EMAs are relative to volatility
		var trendStrength = Math.Abs(fastVal - slowVal) / stdVal;

		var longCond = trendStrength > 0.2m && fastVal > slowVal && close > fastVal;
		var shortCond = trendStrength > 0.2m && fastVal < slowVal && close < fastVal;

		// EMA crossover for exits
		var crossDown = _prevFast >= _prevSlow && fastVal < slowVal;
		var crossUp = _prevFast <= _prevSlow && fastVal > slowVal;

		if (Position > 0 && crossDown)
			SellMarket();
		else if (Position < 0 && crossUp)
			BuyMarket();

		if (longCond && Position <= 0)
			BuyMarket();
		else if (shortCond && Position >= 0)
			SellMarket();

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
