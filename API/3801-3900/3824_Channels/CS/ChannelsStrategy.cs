using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Channels strategy - EMA envelope breakout.
/// Buys when price breaks above fast EMA + offset, sells when it breaks below.
/// Uses slow EMA as trend filter.
/// </summary>
public class ChannelsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ChannelsStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevFast = 0m; _prevSlow = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

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

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: price crosses above fast EMA, fast EMA above slow EMA (uptrend)
		if (close > fast && fast > slow && _prevFast <= _prevSlow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell: price crosses below fast EMA, fast EMA below slow EMA (downtrend)
		else if (close < fast && fast < slow && _prevFast >= _prevSlow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long when fast crosses below slow
		else if (Position > 0 && fast < slow && _prevFast >= _prevSlow)
		{
			SellMarket();
		}
		// Exit short when fast crosses above slow
		else if (Position < 0 && fast > slow && _prevFast <= _prevSlow)
		{
			BuyMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
