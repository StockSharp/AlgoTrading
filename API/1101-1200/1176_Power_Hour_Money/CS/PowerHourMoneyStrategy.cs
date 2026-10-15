namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades causal developing month, week, day and hour colors in New York sessions,
/// with optional executable-quote trailing and market-time end-of-day closing.
/// </summary>
public class PowerHourMoneyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _isAllSessions;
	private readonly StrategyParam<bool> _isEndOfDayCloseEnabled;
	private readonly StrategyParam<TimeSpan> _endOfDayTime;
	private readonly StrategyParam<bool> _isTrailingEnabled;
	private readonly StrategyParam<decimal> _longTrailingPercent;
	private readonly StrategyParam<decimal> _shortTrailingPercent;
	private DateOnly? _month;
	private DateOnly? _week;
	private DateOnly? _day;
	private DateTime? _hour;
	private decimal? _monthOpen;
	private decimal? _weekOpen;
	private decimal? _dayOpen;
	private decimal? _hourOpen;
	private Order _pendingOrder;
	private Sides? _heldSide;
	private decimal _extreme;

	/// <summary>Initializes a 09:30–11:30 New York window, disabled optional exits and five-minute signals.</summary>
	public PowerHourMoneyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying causal calendar opens and closes", "General");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromMinutes(570))
			.SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1))
			.SetDisplay("Session Start", "Inclusive New York wall-clock time", "Sessions");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromMinutes(690))
			.SetRange(TimeSpan.FromTicks(1), TimeSpan.FromDays(1))
			.SetDisplay("Session End", "Exclusive New York wall-clock time", "Sessions");
		_isAllSessions = Param(nameof(IsAllSessions), false)
			.SetDisplay("All Sessions", "Allow entries throughout the day", "Sessions");
		_isEndOfDayCloseEnabled = Param(nameof(IsEndOfDayCloseEnabled), false)
			.SetDisplay("End of Day Close", "Close at the cutoff and block further entries until New York midnight", "Protection");
		_endOfDayTime = Param(nameof(EndOfDayTime), TimeSpan.FromMinutes(1005))
			.SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1))
			.SetDisplay("End of Day Time", "New York wall-clock cutoff", "Protection");
		_isTrailingEnabled = Param(nameof(IsTrailingEnabled), false)
			.SetDisplay("Trailing Enabled", "Protect both sides on real executable Level1 quotes", "Protection");
		_longTrailingPercent = Param(nameof(LongTrailingPercent), 2m).SetGreaterThanZero()
			.SetDisplay("Long Trailing Percent", "Percent below the highest observed bid since entry", "Protection");
		_shortTrailingPercent = Param(nameof(ShortTrailingPercent), 2m).SetGreaterThanZero()
			.SetDisplay("Short Trailing Percent", "Percent above the lowest observed ask since entry", "Protection");
		OrderRegistering += order => _pendingOrder = order;
		OwnTradeReceived += ObserveFill;
	}

	/// <summary>Base candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	/// <summary>Inclusive start of the New York entry window.</summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	/// <summary>Exclusive end of the New York entry window.</summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
	/// <summary>Allow all session times; an enabled daily cutoff still blocks late entries.</summary>
	public bool IsAllSessions { get => _isAllSessions.Value; set => _isAllSessions.Value = value; }
	/// <summary>Close positions and block new entries at the New York cutoff.</summary>
	public bool IsEndOfDayCloseEnabled { get => _isEndOfDayCloseEnabled.Value; set => _isEndOfDayCloseEnabled.Value = value; }
	/// <summary>Daily cutoff in New York wall-clock time.</summary>
	public TimeSpan EndOfDayTime { get => _endOfDayTime.Value; set => _endOfDayTime.Value = value; }
	/// <summary>Enable executable-quote percent trailing for both sides.</summary>
	public bool IsTrailingEnabled { get => _isTrailingEnabled.Value; set => _isTrailingEnabled.Value = value; }
	/// <summary>Long trailing distance in percent, strictly between zero and one hundred.</summary>
	public decimal LongTrailingPercent { get => _longTrailingPercent.Value; set => _longTrailingPercent.Value = value; }
	/// <summary>Short trailing distance in percent, strictly between zero and one hundred.</summary>
	public decimal ShortTrailingPercent { get => _shortTrailingPercent.Value; set => _shortTrailingPercent.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_month = _week = _day = null;
		_hour = null;
		_monthOpen = _weekOpen = _dayOpen = _hourOpen = null;
		_pendingOrder = null;
		_heldSide = null;
		_extreme = 0m;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (SessionStart < TimeSpan.Zero || SessionStart >= SessionEnd || SessionEnd > TimeSpan.FromDays(1)
			|| EndOfDayTime < TimeSpan.Zero || EndOfDayTime >= TimeSpan.FromDays(1)
			|| LongTrailingPercent <= 0m || LongTrailingPercent >= 100m
			|| ShortTrailingPercent <= 0m || ShortTrailingPercent >= 100m)
			throw new ArgumentException("Sessions must be ordered within a day and trailing percentages within (0, 100).");
		ResetState();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
		}
		if (IsEndOfDayCloseEnabled)
			StartTimer(TimeSpan.FromMinutes(1), () => TryEndOfDay());
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal volume, string comment)
		=> RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market, Side = side, Volume = volume, Comment = comment });

	private bool TryEndOfDay()
	{
		if (!IsEndOfDayCloseEnabled || TimeZoneInfo.ConvertTimeFromUtc(CurrentTime, TimeHelper.Est).TimeOfDay < EndOfDayTime)
			return false;
		if (Position != 0m && !IsPending && IsFormedAndOnlineAndAllowTrading())
			Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), "Power Hour end-of-day close");
		return true;
	}

	private void ObserveFill(Subscription subscription, MyTrade trade)
	{
		Sides? side = Position > 0m ? Sides.Buy : Position < 0m ? Sides.Sell : null;
		if (side != _heldSide)
			_extreme = side is null ? 0m : trade.Trade.Price;
		else if (side is Sides current)
			_extreme = current == Sides.Buy ? Math.Max(_extreme, trade.Trade.Price) : Math.Min(_extreme, trade.Trade.Price);
		_heldSide = side;
	}

	private void ObserveQuote(Level1ChangeMessage quote)
	{
		if (TryEndOfDay() || _heldSide is not Sides side || Position == 0m)
			return;
		var field = side == Sides.Buy ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (quote.TryGetDecimal(field) is not decimal price || price <= 0m)
			return;
		_extreme = side == Sides.Buy ? Math.Max(_extreme, price) : Math.Min(_extreme, price);
		var stop = side == Sides.Buy ? _extreme * (1m - LongTrailingPercent / 100m) : _extreme * (1m + ShortTrailingPercent / 100m);
		if (IsTrailingEnabled && !IsPending && IsFormedAndOnlineAndAllowTrading() && (side == Sides.Buy ? price <= stop : price >= stop))
			Submit(side == Sides.Buy ? Sides.Sell : Sides.Buy, Math.Abs(Position), "Power Hour trailing close");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime, TimeHelper.Est);
		var day = DateOnly.FromDateTime(local);
		var month = new DateOnly(local.Year, local.Month, 1);
		var week = day.AddDays(-(((int)local.DayOfWeek + 6) % 7));
		var hour = new DateTime(candle.OpenTime.Year, candle.OpenTime.Month, candle.OpenTime.Day, candle.OpenTime.Hour, 0, 0, DateTimeKind.Utc);
		var midnight = local.TimeOfDay == TimeSpan.Zero;
		if (_month != month)
		{
			_monthOpen = _month is not null || local.Day == 1 && midnight ? candle.OpenPrice : null;
			_month = month;
		}
		if (_week != week)
		{
			_weekOpen = _week is not null || local.DayOfWeek == DayOfWeek.Monday && midnight ? candle.OpenPrice : null;
			_week = week;
		}
		if (_day != day)
		{
			_dayOpen = _day is not null || midnight ? candle.OpenPrice : null;
			_day = day;
		}
		if (_hour != hour)
		{
			_hourOpen = _hour is not null || candle.OpenTime == hour ? candle.OpenPrice : null;
			_hour = hour;
		}
		if (TryEndOfDay() || IsPending || !IsFormedAndOnlineAndAllowTrading()
			|| _monthOpen is not decimal monthly || _weekOpen is not decimal weekly
			|| _dayOpen is not decimal daily || _hourOpen is not decimal hourly)
			return;
		var now = TimeZoneInfo.ConvertTimeFromUtc(CurrentTime, TimeHelper.Est).TimeOfDay;
		if (!IsAllSessions && (now < SessionStart || now >= SessionEnd))
			return;
		var close = candle.ClosePrice;
		if (close > monthly && close > weekly && close > daily && close > hourly && Position <= 0m)
			Submit(Sides.Buy, Volume + Math.Abs(Position), "Power Hour consensus entry");
		else if (close < monthly && close < weekly && close < daily && close < hourly && Position >= 0m)
			Submit(Sides.Sell, Volume + Math.Abs(Position), "Power Hour consensus entry");
	}
}
