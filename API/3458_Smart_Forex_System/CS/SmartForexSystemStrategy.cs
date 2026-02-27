namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Smart Forex System strategy: Triple EMA alignment.
/// Enters when fast > mid > slow (buy) or fast < mid < slow (sell).
/// </summary>
public class SmartForexSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MidPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public SmartForexSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_midPeriod = Param(nameof(MidPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Mid EMA", "Mid EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var mid = new ExponentialMovingAverage { Length = MidPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, mid, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal midValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (fastValue > midValue && midValue > slowValue && Position <= 0)
			BuyMarket();
		else if (fastValue < midValue && midValue < slowValue && Position >= 0)
			SellMarket();
	}
}
