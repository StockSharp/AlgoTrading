namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Day Opening MACD Histogram strategy: MACD histogram direction.
/// Buys when MACD histogram turns positive, sells when turns negative.
/// </summary>
public class DayOpeningMacdHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevHistogram;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public DayOpeningMacdHistogramStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "Indicators");
		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "MACD signal period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHistogram = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevHistogram = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = FastPeriod }, LongMa = { Length = SlowPeriod } },
			SignalMa = { Length = SignalPeriod }
		};
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!macdValue.IsFinal) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed) return;
		if (typed.Macd is not decimal macdMain || typed.Signal is not decimal signal) return;

		var histogram = macdMain - signal;

		if (_hasPrev)
		{
			if (_prevHistogram <= 0 && histogram > 0 && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevHistogram >= 0 && histogram < 0 && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevHistogram = histogram;
		_hasPrev = true;
	}
}
