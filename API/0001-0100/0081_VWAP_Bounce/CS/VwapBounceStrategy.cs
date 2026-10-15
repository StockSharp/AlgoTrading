using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Bounce strategy.
/// The VWAP restarts every UTC day from the typical price of each candle weighted by its volume. A bullish candle closing below
/// the VWAP turns the position long and a bearish candle closing above it turns it short; a percent stop limits the loss.
/// </summary>
public class VwapBounceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _priceVolume;
	private decimal _volume;

	/// <summary>
	/// Stop-loss percentage.
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
	public VwapBounceStrategy()
	{
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
		_priceVolume = default;
		_volume = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_priceVolume = default;
		_volume = default;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_day != day)
		{
			_day = day;
			_priceVolume = 0;
			_volume = 0;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_priceVolume += typical * candle.TotalVolume;
		_volume += candle.TotalVolume;

		if (_volume <= 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var vwap = _priceVolume / _volume;
		var close = candle.ClosePrice;

		if (close > candle.OpenPrice && close < vwap && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < candle.OpenPrice && close > vwap && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
