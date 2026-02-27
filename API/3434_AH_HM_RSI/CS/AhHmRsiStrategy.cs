namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Hammer/Hanging Man + RSI strategy.
/// Buys on hammer with low RSI, sells on hanging man with high RSI.
/// </summary>
public class AhHmRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<decimal> _rsiHigh;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public decimal RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }

	public AhHmRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_rsiLow = Param(nameof(RsiLow), 40m)
			.SetDisplay("RSI Low", "RSI oversold threshold for buy", "Signals");
		_rsiHigh = Param(nameof(RsiHigh), 60m)
			.SetDisplay("RSI High", "RSI overbought threshold for sell", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0 || body <= 0) return;

		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isHammer = lowerShadow > body * 2 && upperShadow < body;
		var isHangingMan = upperShadow > body * 2 && lowerShadow < body;

		if (isHammer && rsiValue < RsiLow && Position <= 0)
			BuyMarket();
		else if (isHangingMan && rsiValue > RsiHigh && Position >= 0)
			SellMarket();
	}
}
