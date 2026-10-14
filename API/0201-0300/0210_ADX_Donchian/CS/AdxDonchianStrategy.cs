using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX Donchian strategy.
/// The borders sit Multiplier percent inside the DonchianPeriod channel, which includes the current candle. With ADX above AdxThreshold
/// a close at or above the upper border goes long and one at or below the lower border goes short, reversing an opposite position.
/// The position closes once ADX falls below AdxThreshold minus 5, and a percent stop limits the loss.
/// </summary>
public class AdxDonchianStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// ADX value for strong trend detection.
	/// </summary>
	public int AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Percent the borders sit inside the channel.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	public AdxDonchianStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_donchianPeriod = Param(nameof(DonchianPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Candles the channel spans", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "ADX value for strong trend detection", "Indicators");

		_multiplier = Param(nameof(Multiplier), 0.1m)
			.SetNotNegative()
			.SetDisplay("Multiplier", "Percent the borders sit inside the channel", "Indicators");

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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var donchian = new DonchianChannels { Length = DonchianPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, donchian, ProcessCandle)
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
			DrawIndicator(area, donchian);
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFormed || !donchianValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		if (donchianValue is not IDonchianChannelsValue { UpperBand: decimal upper, LowerBand: decimal lower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upperBorder = upper * (1 - Multiplier / 100);
		var lowerBorder = lower * (1 + Multiplier / 100);
		var strong = strength > AdxThreshold;
		var close = candle.ClosePrice;

		if (strong && close >= upperBorder && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (strong && close <= lowerBorder && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && strength < AdxThreshold - 5)
			SellMarket(Position);
		else if (Position < 0 && strength < AdxThreshold - 5)
			BuyMarket(-Position);
	}
}
