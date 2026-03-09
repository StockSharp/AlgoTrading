namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Meeting Lines + CCI strategy.
/// Buys on bullish meeting lines with low CCI, sells on bearish meeting lines with high CCI.
/// </summary>
public class AmlCciMeetingLinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLow;
	private readonly StrategyParam<decimal> _cciHigh;

	private ICandleMessage _prevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciLow { get => _cciLow.Value; set => _cciLow.Value = value; }
	public decimal CciHigh { get => _cciHigh.Value; set => _cciHigh.Value = value; }

	public AmlCciMeetingLinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_cciLow = Param(nameof(CciLow), -50m)
			.SetDisplay("CCI Low", "CCI level for bullish entry", "Signals");
		_cciHigh = Param(nameof(CciHigh), 50m)
			.SetDisplay("CCI High", "CCI level for bearish entry", "Signals");
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
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
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

				// Bullish meeting lines
				if (prevBearish && currBullish && closesNear && cciValue < CciLow && Position <= 0)
					BuyMarket();

				var prevBullish = _prevCandle.ClosePrice > _prevCandle.OpenPrice;
				var currBearish = candle.OpenPrice > candle.ClosePrice;
				var closesNear2 = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				// Bearish meeting lines
				if (prevBullish && currBearish && closesNear2 && cciValue > CciHigh && Position >= 0)
					SellMarket();
			}
		}

		_prevCandle = candle;
	}
}
