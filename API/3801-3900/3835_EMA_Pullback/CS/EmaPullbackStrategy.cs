using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA Pullback strategy - fast/slow EMA crossover with pullback entry.
/// After a bullish crossover, waits for a pullback to fast EMA to enter long.
/// After a bearish crossover, waits for a pullback to fast EMA to enter short.
/// </summary>
public class EmaPullbackStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevClose;
	private bool _hasPrev;
	private bool _bullishCross;
	private bool _bearishCross;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaPullbackStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevFast = 0m; _prevSlow = 0m; _prevClose = 0m; _hasPrev = false; _bullishCross = false; _bearishCross = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_bullishCross = false;
		_bearishCross = false;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		// Detect crossovers
		if (_prevFast <= _prevSlow && fast > slow)
		{
			_bullishCross = true;
			_bearishCross = false;
		}
		else if (_prevFast >= _prevSlow && fast < slow)
		{
			_bearishCross = true;
			_bullishCross = false;
		}

		// Pullback entry: after bullish cross, wait for close to touch fast EMA
		if (_bullishCross && fast > slow && _prevClose > _prevFast && close <= fast && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_bullishCross = false;
		}
		// Pullback entry: after bearish cross, wait for close to touch fast EMA
		else if (_bearishCross && fast < slow && _prevClose < _prevFast && close >= fast && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_bearishCross = false;
		}
		// Exit on opposite crossover
		else if (Position > 0 && fast < slow)
		{
			SellMarket();
		}
		else if (Position < 0 && fast > slow)
		{
			BuyMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevClose = close;
	}
}
