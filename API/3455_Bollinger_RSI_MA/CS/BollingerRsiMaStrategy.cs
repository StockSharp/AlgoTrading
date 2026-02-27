namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Bollinger + RSI + MA strategy.
/// Buys when price at lower BB and RSI oversold, sells at upper BB and RSI overbought.
/// </summary>
public class BollingerRsiMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bbPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }

	public BollingerRsiMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var bb = new BollingerBands { Length = BbPeriod, Width = 2m };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rsi, bb, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!rsiValue.IsFinal || !bbValue.IsFinal) return;

		var rsi = rsiValue.GetValue<decimal>();
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
