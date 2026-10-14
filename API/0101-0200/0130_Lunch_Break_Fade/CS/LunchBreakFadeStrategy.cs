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
/// Lunch Break Fade strategy.
/// The market trades around the clock, so the session is the UTC day.
/// The morning move runs from the UTC day's open to the close at LunchHour. At that close the strategy enters against the move
/// and covers at the candle ending at LunchEndHour, before volume returns; a percent stop limits the loss.
/// </summary>
public class LunchBreakFadeStrategy : Strategy
{
	private readonly StrategyParam<int> _lunchHour;
	private readonly StrategyParam<int> _lunchEndHour;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _dayOpen;

	/// <summary>
	/// UTC hour that ends the morning.
	/// </summary>
	public int LunchHour
	{
		get => _lunchHour.Value;
		set => _lunchHour.Value = value;
	}

	/// <summary>
	/// UTC hour at which the position is covered.
	/// </summary>
	public int LunchEndHour
	{
		get => _lunchEndHour.Value;
		set => _lunchEndHour.Value = value;
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
	public LunchBreakFadeStrategy()
	{
		_lunchHour = Param(nameof(LunchHour), 12)
			.SetRange(0, 23)
			.SetDisplay("Lunch Hour", "UTC hour that ends the morning", "Session");

		_lunchEndHour = Param(nameof(LunchEndHour), 14)
			.SetRange(0, 23)
			.SetDisplay("Lunch End Hour", "UTC hour at which the position is covered", "Session");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

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
		_dayOpen = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_dayOpen = default;

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
		{
			_day = day;
			_dayOpen = candle.OpenPrice;
		}

		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var closeTime = candle.OpenTime + frame;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (closeTime >= day.AddHours(LunchEndHour))
				ClosePosition();

			return;
		}

		if (closeTime != day.AddHours(LunchHour))
			return;

		if (candle.ClosePrice > _dayOpen)
			SellMarket(Volume);
		else if (candle.ClosePrice < _dayOpen)
			BuyMarket(Volume);
	}
}
