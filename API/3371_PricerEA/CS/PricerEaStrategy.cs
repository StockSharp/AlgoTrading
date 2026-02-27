namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// PricerEA strategy: Bollinger Bands mean reversion with RSI filter.
/// Buys at lower band when RSI is oversold, sells at upper band when overbought.
/// </summary>
public class PricerEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public PricerEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var bb = new BollingerBands { Length = BbPeriod, Width = 2 };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(bb, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbVal, IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!bbVal.IsFinal || !rsiVal.IsFinal) return;

		var bbv = (BollingerBandsValue)bbVal;
		if (bbv.UpBand is not decimal upper || bbv.LowBand is not decimal lower) return;

		var rsi = rsiVal.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close <= lower && rsi < 35 && Position <= 0) BuyMarket();
		else if (close >= upper && rsi > 65 && Position >= 0) SellMarket();
	}
}
