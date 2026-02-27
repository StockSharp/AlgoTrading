using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RAVI AO strategy. Uses fast/slow EMA difference (RAVI concept) for crossover signals.
/// </summary>
public class RaviAoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public RaviAoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = null;
		_prevSlow = null;
		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevFast == null || _prevSlow == null) { _prevFast = fastVal; _prevSlow = slowVal; return; }
		var prevAbove = _prevFast.Value > _prevSlow.Value;
		var currAbove = fastVal > slowVal;
		_prevFast = fastVal; _prevSlow = slowVal;
		if (!prevAbove && currAbove && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (prevAbove && !currAbove && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
