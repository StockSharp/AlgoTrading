using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bread and Butter 2 ADX AMA strategy. Uses KAMA direction with ADX filter.
/// </summary>
public class Breadandbutter2AdxAmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kamaPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private decimal? _prevKama;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KamaPeriod { get => _kamaPeriod.Value; set => _kamaPeriod.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public Breadandbutter2AdxAmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_kamaPeriod = Param(nameof(KamaPeriod), 10).SetGreaterThanZero().SetDisplay("KAMA Period", "KAMA lookback", "Indicators");
		_fastPeriod = Param(nameof(FastPeriod), 8).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevKama = null;
		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevKama == null) { _prevKama = fast; return; }
		var prevAbove = _prevKama.Value > slow;
		var currAbove = fast > slow;
		_prevKama = fast;
		if (!prevAbove && currAbove && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (prevAbove && !currAbove && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
