using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Open Drive strategy.
/// The market trades around the clock, so the session is the UTC day.
/// The first candle of each UTC day is the opening drive when its volume exceeds the average of the previous VolumePeriod candles;
/// the strategy joins its direction at its close. The position closes on the first candle that closes against it, when the drive stalls,
/// and a trailing percent stop follows it as price extends.
/// </summary>
public class OpenDriveStrategy : Strategy
{
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private readonly List<decimal> _volumes = [];

	/// <summary>
	/// Previous candles the opening volume is compared with.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop loss percentage.
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
	public OpenDriveStrategy()
	{
		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Previous candles the opening volume is compared with", "Session");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Trailing stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_volumes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_volumes.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), isStopTrailing: true, useMarketOrders: true, isLocalStop: true);

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

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		var firstOfDay = _day != day;

		if (firstOfDay)
			_day = day;

		var average = _volumes.Count == VolumePeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumePeriod)
			_volumes.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close < candle.OpenPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (close > candle.OpenPrice)
				BuyMarket(-Position);
		}
		else if (firstOfDay && average is decimal avgVolume && candle.TotalVolume > avgVolume)
		{
			if (close > candle.OpenPrice)
				BuyMarket(Volume);
			else if (close < candle.OpenPrice)
				SellMarket(Volume);
		}
	}
}
