using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP ADX strategy.
/// The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
/// While ADX is above AdxThreshold, a close above VWAP goes long and a close below it goes short, reversing an opposite position.
/// The position closes once ADX drops below AdxExitThreshold, and a percent stop limits the loss.
/// </summary>
public class VwapAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _adxExitThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _cumulativePriceVolume;
	private decimal _cumulativeVolume;

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level required to enter.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// ADX level below which the position closes.
	/// </summary>
	public decimal AdxExitThreshold
	{
		get => _adxExitThreshold.Value;
		set => _adxExitThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public VwapAdxStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level required to enter", "ADX");

		_adxExitThreshold = Param(nameof(AdxExitThreshold), 20m)
			.SetDisplay("ADX Exit Threshold", "ADX level below which the position closes", "ADX");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_day != day)
		{
			_day = day;
			_cumulativePriceVolume = 0;
			_cumulativeVolume = 0;
		}

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		_cumulativePriceVolume += typicalPrice * candle.TotalVolume;
		_cumulativeVolume += candle.TotalVolume;

		if (!adxValue.IsFormed || _cumulativeVolume <= 0)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var vwap = _cumulativePriceVolume / _cumulativeVolume;
		var close = candle.ClosePrice;
		var strong = strength > AdxThreshold;

		if (strong && close > vwap && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (strong && close < vwap && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && strength < AdxExitThreshold)
			SellMarket(Position);
		else if (Position < 0 && strength < AdxExitThreshold)
			BuyMarket(-Position);
	}
}
