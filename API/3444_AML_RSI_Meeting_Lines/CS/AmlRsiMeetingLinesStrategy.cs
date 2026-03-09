namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Meeting Lines + RSI strategy.
/// Buys on bullish meeting lines with low RSI, sells on bearish meeting lines with high RSI.
/// </summary>
public class AmlRsiMeetingLinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<decimal> _rsiHigh;

	private ICandleMessage _prevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public decimal RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }

	public AmlRsiMeetingLinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_rsiLow = Param(nameof(RsiLow), 40m)
			.SetDisplay("RSI Low", "RSI level for bullish entry", "Signals");
		_rsiHigh = Param(nameof(RsiHigh), 60m)
			.SetDisplay("RSI High", "RSI level for bearish entry", "Signals");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null)
		{
			var avgBody = (Math.Abs(candle.ClosePrice - candle.OpenPrice) +
						   Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice)) / 2m;

			if (avgBody > 0)
			{
				var prevBearish = _prevCandle.OpenPrice > _prevCandle.ClosePrice;
				var currBullish = candle.ClosePrice > candle.OpenPrice;
				var closesNear = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBearish && currBullish && closesNear && rsiValue < RsiLow && Position <= 0)
					BuyMarket();

				var prevBullish = _prevCandle.ClosePrice > _prevCandle.OpenPrice;
				var currBearish = candle.OpenPrice > candle.ClosePrice;
				var closesNear2 = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBullish && currBearish && closesNear2 && rsiValue > RsiHigh && Position >= 0)
					SellMarket();
			}
		}

		_prevCandle = candle;
	}
}
