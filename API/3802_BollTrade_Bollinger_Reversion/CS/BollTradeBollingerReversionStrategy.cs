using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands reversion strategy.
/// Buys when price closes below lower band, sells when above upper band.
/// Uses middle band as exit target.
/// </summary>
public class BollTradeBollingerReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<DataType> _candleType;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollTradeBollingerReversionStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 0.5m)
			.SetDisplay("BB Width", "Bollinger Bands width", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFinal)
			return;

		var bb = (BollingerBandsValue)bbValue;
		var upper = bb.UpBand;
		var lower = bb.LowBand;
		var middle = bb.MovingAverage;

		if (upper == null || lower == null || middle == null)
			return;

		var close = candle.ClosePrice;
		var upperVal = upper.Value;
		var lowerVal = lower.Value;
		var midVal = middle.Value;

		// Buy when close below lower band (oversold)
		if (close < lowerVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell when close above upper band (overbought)
		else if (close > upperVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
