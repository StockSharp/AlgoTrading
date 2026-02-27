namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Universal Signal Demo strategy: Multi-indicator scoring.
/// Combines RSI and EMA signals to generate aggregate score for entries.
/// </summary>
public class UniversalSignalDemoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public UniversalSignalDemoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var score = 0;

		// RSI signal
		if (rsiValue < 30) score += 2;
		else if (rsiValue < 45) score += 1;
		else if (rsiValue > 70) score -= 2;
		else if (rsiValue > 55) score -= 1;

		// EMA signal
		if (close > emaValue) score += 1;
		else if (close < emaValue) score -= 1;

		// Candle direction
		if (candle.ClosePrice > candle.OpenPrice) score += 1;
		else if (candle.ClosePrice < candle.OpenPrice) score -= 1;

		if (score >= 2 && Position <= 0)
			BuyMarket();
		else if (score <= -2 && Position >= 0)
			SellMarket();
	}
}
