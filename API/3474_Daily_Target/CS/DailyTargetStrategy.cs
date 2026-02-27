namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Daily Target strategy: TEMA crossover.
/// Buys when fast TEMA crosses above slow TEMA, sells on cross below.
/// </summary>
public class DailyTargetStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public DailyTargetStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast TEMA", "Fast TEMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow TEMA", "Slow TEMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var fast = new TripleExponentialMovingAverage { Length = FastPeriod };
		var slow = new TripleExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
				BuyMarket();
			else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
				SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_hasPrev = true;
	}
}
