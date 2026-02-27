namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Basic Martingale EA 3 strategy: WMA crossover with RSI filter.
/// Buys when WMA fast crosses above slow and RSI below midline, sells on opposite conditions.
/// </summary>
public class BasicMartingaleEa3Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public BasicMartingaleEa3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast WMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow WMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var fast = new WeightedMovingAverage { Length = FastPeriod };
		var slow = new WeightedMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevFast <= _prevSlow && fastValue > slowValue && rsiValue < 50 && Position <= 0)
				BuyMarket();
			else if (_prevFast >= _prevSlow && fastValue < slowValue && rsiValue > 50 && Position >= 0)
				SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_hasPrev = true;
	}
}
