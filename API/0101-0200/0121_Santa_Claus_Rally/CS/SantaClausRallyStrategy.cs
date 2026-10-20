namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using Ecng.ComponentModel;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Long-only rally over the last five December sessions and first two January sessions.</summary>
public class SantaClausRallyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private int _entryYear = -1;
	private Order _pendingOrder;

	/// <summary>Signal candle interval.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Loss limit as a percentage of the actual entry price.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>Initializes the seasonal strategy.</summary>
	public SantaClausRallyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles used for calendar decisions", "Data");
		_stopLoss = Param(nameof(StopLoss), 2m).SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percentage loss limit from the executed entry", "Risk");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryYear = -1;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (Security?.Board is null)
			throw new InvalidOperationException("The instrument's trading board and calendar are required.");
		base.OnStarted2(time);
		_entryYear = -1;
		_pendingOrder = null;
		StartProtection(new Unit(), new Unit(StopLoss, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private (DateTime entry, DateTime exit) GetSeason(int year)
	{
		var calendar = Security.Board.WorkingTime;
		var entry = new DateTime(year, 12, 31);
		for (var sessions = 0; ; entry = entry.AddDays(-1))
		{
			if (calendar.IsWorkingDate(entry) && ++sessions == 5)
				break;
		}
		var exit = new DateTime(year + 1, 1, 1);
		for (var sessions = 0; ; exit = exit.AddDays(1))
		{
			if (calendar.IsWorkingDate(exit) && ++sessions == 2)
				break;
		}
		return (entry, exit);
	}

	private TimeSpan SessionEnd(DateTime date)
	{
		var calendar = Security.Board.WorkingTime;
		if (!calendar.IsEnabled)
			return TimeSpan.FromDays(1);
		var period = calendar.GetPeriod(date);
		IEnumerable<Range<TimeSpan>> sessions = period?.Times;
		if (calendar.SpecialDays.TryGetValue(date, out var special))
			sessions = special;
		else if (period is not null && period.SpecialDays.TryGetValue(date.DayOfWeek, out var weekday))
			sessions = weekday;
		return sessions is not null && sessions.Any() ? sessions.Max(r => r.Max) : TimeSpan.FromDays(1);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime, Security.Board.TimeZone);
		var date = local.Date;
		var year = Position > 0m && _entryYear > 0
			? _entryYear
			: local.Month == 12 ? local.Year : local.Year - 1;
		var (entry, exit) = GetSeason(year);

		if (Position > 0m)
		{
			var barEnd = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime + (TimeSpan)CandleType.Arg, Security.Board.TimeZone);
			if (date > exit || date == exit && barEnd >= exit + SessionEnd(exit))
				SellMarket(Position);
		}
		else if (Position == 0m && local.Month == 12 && date >= entry && _entryYear != year
			&& Security.Board.WorkingTime.IsWorkingDate(date))
		{
			_entryYear = year;
			BuyMarket(Volume);
		}
	}
}
