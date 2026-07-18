namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Dual Stoploss strategy: dual SMA crossover with confirmation.
/// Buys when fast SMA crosses above mid SMA and mid is above slow, sells on opposite.
/// </summary>
public class DualStoplossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevMid;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MidPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public DualStoplossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");
		_midPeriod = Param(nameof(MidPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Mid SMA", "Mid SMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevMid = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var mid = new SimpleMovingAverage { Length = MidPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, mid, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal midValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevFast <= _prevMid && fastValue > midValue && midValue > slowValue && Position <= 0)
				BuyMarket();
			else if (_prevFast >= _prevMid && fastValue < midValue && midValue < slowValue && Position >= 0)
				SellMarket();
		}
		else
		{
			if (fastValue > midValue && midValue > slowValue && Position <= 0)
				BuyMarket();
			else if (fastValue < midValue && midValue < slowValue && Position >= 0)
				SellMarket();
		}

		_prevFast = fastValue;
		_prevMid = midValue;
		_hasPrev = true;
	}
}
