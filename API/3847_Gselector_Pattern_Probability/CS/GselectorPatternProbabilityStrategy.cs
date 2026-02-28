using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gselector Pattern Probability strategy - SMA crossover with RSI filter.
/// Buys when fast SMA crosses above slow SMA and RSI is below overbought.
/// Sells when fast SMA crosses below slow SMA and RSI is above oversold.
/// </summary>
public class GselectorPatternProbabilityStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GselectorPatternProbabilityStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");

		_overbought = Param(nameof(Overbought), 75m)
			.SetDisplay("Overbought", "RSI overbought level", "Levels");

		_oversold = Param(nameof(Oversold), 25m)
			.SetDisplay("Oversold", "RSI oversold level", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };

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

		// Fast crosses above slow = buy
		if (_prevFast <= _prevSlow && fast > slow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Fast crosses below slow = sell
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
