namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MACD Diver And RSI strategy: RSI extremes + MACD histogram crossover.
/// Buys when RSI below 30 and MACD histogram crosses above zero.
/// Sells when RSI above 70 and MACD histogram crosses below zero.
/// </summary>
public class MacdDiverAndRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevHistogram;
	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public MacdDiverAndRsiStrategy()
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
		_prevHistogram = 0;
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

		var histogram = macdMain - signal;
		var rsi = rsiValue.ToDecimal();

		if (_hasPrev)
		{
			if (_prevHistogram <= 0 && histogram > 0 && _prevRsi < 35 && Position <= 0)
				BuyMarket();
			else if (_prevHistogram >= 0 && histogram < 0 && _prevRsi > 65 && Position >= 0)
				SellMarket();
		}

		_prevHistogram = histogram;
		_prevRsi = rsi;
		_hasPrev = true;
	}
}
