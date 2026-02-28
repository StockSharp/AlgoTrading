using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Glam Trader strategy - EMA crossover with momentum confirmation.
/// Buys when fast EMA crosses above slow EMA and momentum is positive.
/// Sells when fast EMA crosses below slow EMA and momentum is negative.
/// </summary>
public class GlamTraderSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GlamTraderSimpleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var mom = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, mom, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal mom)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Fast crosses above slow + positive momentum = buy
		if (_prevFast <= _prevSlow && fast > slow && mom > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Fast crosses below slow + negative momentum = sell
		else if (_prevFast >= _prevSlow && fast < slow && mom < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
