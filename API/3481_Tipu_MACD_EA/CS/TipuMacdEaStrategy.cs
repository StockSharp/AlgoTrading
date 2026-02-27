namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Tipu MACD EA strategy: MACD signal line crossover.
/// Buys when MACD crosses above signal, sells when crosses below.
/// </summary>
public class TipuMacdEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

	public TipuMacdEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
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

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed) return;
		if (typed.Macd is not decimal macdMain || typed.Signal is not decimal signal) return;

		if (_hasPrev)
		{
			if (_prevMacd <= _prevSignal && macdMain > signal && Position <= 0)
				BuyMarket();
			else if (_prevMacd >= _prevSignal && macdMain < signal && Position >= 0)
				SellMarket();
		}

		_prevMacd = macdMain;
		_prevSignal = signal;
		_hasPrev = true;
	}
}
