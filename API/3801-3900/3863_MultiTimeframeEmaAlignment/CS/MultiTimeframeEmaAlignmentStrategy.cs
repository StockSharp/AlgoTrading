using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-Timeframe EMA Alignment strategy - fast/slow EMA crossover with trend EMA filter.
/// Buys when fast EMA crosses above slow EMA while close is above trend EMA.
/// Sells when fast EMA crosses below slow EMA while close is below trend EMA.
/// </summary>
public class MultiTimeframeEmaAlignmentStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int TrendPeriod { get => _trendPeriod.Value; set => _trendPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiTimeframeEmaAlignmentStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 100)
			.SetDisplay("Trend EMA", "Trend EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		var trend = new ExponentialMovingAverage { Length = TrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, trend, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		if (_prevFast <= _prevSlow && fast > slow && close > trend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevFast >= _prevSlow && fast < slow && close < trend && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
