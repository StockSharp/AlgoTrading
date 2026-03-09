namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MACD Divergence RSI strategy: RSI filter + MACD signal line crossover.
/// Buys when RSI below threshold and MACD crosses above signal.
/// Sells when RSI above threshold and MACD crosses below signal.
/// </summary>
public class MacdDivergenceRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public MacdDivergenceRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = 0;
		_prevSignal = 0;
		_prevRsi = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
			SignalMa = { Length = 9 }
		};
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!macdValue.IsFinal || !rsiValue.IsFinal) return;
		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed) return;
		if (typed.Macd is not decimal macdMain || typed.Signal is not decimal signal) return;

		var rsi = rsiValue.ToDecimal();

		if (_hasPrev)
		{
			if (_prevMacd <= _prevSignal && macdMain > signal && rsi < 40 && Position <= 0)
				BuyMarket();
			else if (_prevMacd >= _prevSignal && macdMain < signal && rsi > 60 && Position >= 0)
				SellMarket();
		}

		_prevMacd = macdMain;
		_prevSignal = signal;
		_prevRsi = rsi;
		_hasPrev = true;
	}
}
