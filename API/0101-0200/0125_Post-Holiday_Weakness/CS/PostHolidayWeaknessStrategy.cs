using System;
using System.Collections.Generic;
using System.Globalization;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Post-Holiday Weakness strategy.
/// Days are UTC days of a market that trades around the clock.
/// Holidays is a comma-separated list of yyyy-MM-dd dates. The market trades every day, so it sells short at the close of the first candle
/// of the calendar day after a holiday and covers at that day's last candle; a percent stop limits the loss.
/// </summary>
public class PostHolidayWeaknessStrategy : Strategy
{
	private readonly StrategyParam<string> _holidays;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private readonly HashSet<DateTime> _holidayDates = [];

	/// <summary>
	/// Comma-separated yyyy-MM-dd holiday dates.
	/// </summary>
	public string Holidays
	{
		get => _holidays.Value;
		set => _holidays.Value = value;
	}

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
	public PostHolidayWeaknessStrategy()
	{
		_holidays = Param(nameof(Holidays), "2024-01-01,2024-01-15,2024-02-19,2024-03-29,2024-05-27,2024-06-19,2024-07-04,2024-09-02,2024-11-28,2024-12-25")
			.SetDisplay("Holidays", "Comma-separated yyyy-MM-dd holiday dates", "Calendar");

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
		_holidayDates.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_holidayDates.Clear();

		foreach (var item in Holidays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			_holidayDates.Add(DateTime.ParseExact(item, "yyyy-MM-dd", CultureInfo.InvariantCulture));

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
		var firstOfDay = _day != day;

		if (firstOfDay)
		{
			_day = day;
		}

		// The candle is the day's last when the next one would open on another day.
		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var lastOfDay = (candle.OpenTime + frame).Date != day;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (lastOfDay)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (!(firstOfDay && _holidayDates.Contains(day.AddDays(-1))))
			return;

		if ((-1) > 0)
			BuyMarket(Volume);
		else
			SellMarket(Volume);
	}
}
