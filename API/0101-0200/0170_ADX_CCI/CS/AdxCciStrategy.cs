using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX CCI strategy.
/// While ADX is above AdxThreshold, CCI below CciOversold goes long and CCI above CciOverbought goes short, reversing an opposite position.
/// The position closes when the trend weakens, ADX falling below AdxThreshold, or when CCI crosses the zero line, and a percent stop
/// limits the loss.
/// </summary>
public class AdxCciStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
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
	/// ADX level of a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Period of CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level for longs.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
	}

	/// <summary>
	/// CCI level for shorts.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
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
	public AdxCciStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI", "Indicators");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level for longs", "Indicators");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level for shorts", "Indicators");

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
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, cci, ProcessCandle)
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
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFormed || !cciValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cci = cciValue.GetValue<decimal>();
		var strong = strength > AdxThreshold;
		var weak = strength < AdxThreshold;

		if (strong && cci < CciOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (strong && cci > CciOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (weak || cci >= 0))
			SellMarket(Position);
		else if (Position < 0 && (weak || cci <= 0))
			BuyMarket(-Position);
	}
}
