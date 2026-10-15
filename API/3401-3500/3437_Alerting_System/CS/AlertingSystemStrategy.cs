namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Alerting System strategy: Bollinger Band breakout.
/// Buys when price crosses above upper band, sells when below lower band.
/// </summary>
public class AlertingSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }

	public AlertingSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetDisplay("BB Width", "Bollinger Bands width multiplier", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var bb = new BollingerBands
		{
			Length = BbPeriod,
			Width = BbWidth
		};
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(bb, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!bbValue.IsFinal) return;

		var typed = (BollingerBandsValue)bbValue;
		if (typed.UpBand is not decimal upper || typed.LowBand is not decimal lower) return;

		var close = candle.ClosePrice;

		// Mean reversion: buy at lower band, sell at upper band
		if (close < lower && Position <= 0)
			BuyMarket();
		else if (close > upper && Position >= 0)
			SellMarket();
	}
}
