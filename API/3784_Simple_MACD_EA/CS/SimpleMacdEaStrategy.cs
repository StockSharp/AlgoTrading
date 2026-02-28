using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple MACD EA strategy using fast/slow EMA difference for trend detection.
/// Buy when EMA difference crosses above zero, sell when it crosses below zero.
/// </summary>
public class SimpleMacdEaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;
	private bool _hasPrev;

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SimpleMacdEaStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = fast - slow;

		if (!_hasPrev)
		{
			_prevDiff = diff;
			_hasPrev = true;
			return;
		}

		// Buy: MACD crosses above zero
		var longSignal = _prevDiff <= 0 && diff > 0;
		// Sell: MACD crosses below zero
		var shortSignal = _prevDiff >= 0 && diff < 0;

		if (Position <= 0 && longSignal)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (Position >= 0 && shortSignal)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevDiff = diff;
	}
}
