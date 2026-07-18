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
	private bool _wasBullishAlignment;
	private bool _hasPrevAlignment;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MidPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public SmartForexSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_midPeriod = Param(nameof(MidPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Mid EMA", "Mid EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasBullishAlignment = false;
		_hasPrevAlignment = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_wasBullishAlignment = false;
		_hasPrevAlignment = false;
		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var mid = new ExponentialMovingAverage { Length = MidPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, mid, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal midValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var bullishAlignment = fastValue > midValue && midValue > slowValue;
		var bearishAlignment = fastValue < midValue && midValue < slowValue;
		var crossedUp = bullishAlignment && (!_hasPrevAlignment || !_wasBullishAlignment);
		var crossedDown = bearishAlignment && (!_hasPrevAlignment || _wasBullishAlignment);

		if (crossedUp && Position <= 0)
			BuyMarket();
		else if (crossedDown && Position >= 0)
			SellMarket();

		if (bullishAlignment || bearishAlignment)
		{
			_wasBullishAlignment = bullishAlignment;
			_hasPrevAlignment = true;
		}
	}
}
