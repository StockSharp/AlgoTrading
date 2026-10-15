namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Buys a previous UTC day's high/low upward close cross with rising ADX during a UTC session.
/// Actual-fill profit targets, observed-price trailing stops and session-end closes protect longs.
/// </summary>
public class PreviousDayHighLowLongStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxProfit;
	private readonly StrategyParam<decimal> _maxStopLoss;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;
	private DateTime? _day;
	private decimal _dayHigh;
	private decimal _dayLow;
	private bool _isDayStartObserved;
	private bool _isPreviousDayAvailable;
	private decimal _previousDayHigh;
	private decimal _previousDayLow;
	private decimal? _previousClose;
	private decimal? _previousAdx;
	private DateTime? _entryDate;
	private decimal _filledPosition;
	private decimal _entryPrice;
	private decimal _extreme;
	private Order _pendingOrder;

	/// <summary>Initializes profit150, trailing distance15 in price units, ADX11 and UTC09:30–16:00.</summary>
	public PreviousDayHighLowLongStrategy()
	{
		_maxProfit = Param(nameof(MaxProfit), 150m).SetGreaterThanZero()
			.SetDisplay("Maximum Profit", "Target distance above actual entry price, in absolute price units", "Protection");
		_maxStopLoss = Param(nameof(MaxStopLoss), 15m).SetGreaterThanZero()
			.SetDisplay("Maximum Stop Loss", "Trailing distance below highest observed price, in absolute price units", "Protection");
		_adxLength = Param(nameof(AdxLength), 11).SetGreaterThanZero()
			.SetDisplay("ADX Length", "Native directional-strength period; current ADX must rise", "Indicators");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromMinutes(570))
			.SetDisplay("Session Start", "Inclusive UTC entry time", "Session");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(16))
			.SetDisplay("Session End", "Exclusive UTC entry time and mandatory close", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying UTC day levels and ADX", "General");
		OrderRegistering += order => _pendingOrder = order;
		OwnTradeReceived += ObserveFill;
	}

	/// <summary>Positive profit target distance from actual entry price, in absolute price units.</summary>
	public decimal MaxProfit { get => _maxProfit.Value; set => _maxProfit.Value = value; }

	/// <summary>Positive trailing distance from highest observed price, in absolute price units.</summary>
	public decimal MaxStopLoss { get => _maxStopLoss.Value; set => _maxStopLoss.Value = value; }

	/// <summary>Positive native ADX period.</summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>Inclusive UTC entry time, before SessionEnd.</summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }

	/// <summary>Exclusive UTC entry time and mandatory close, at most twenty-four hours.</summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }

	/// <summary>Finished candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_day = null;
		_previousClose = _previousAdx = null;
		_entryDate = null;
		_dayHigh = _dayLow = _previousDayHigh = _previousDayLow = 0m;
		_isDayStartObserved = _isPreviousDayAvailable = false;
		_filledPosition = _entryPrice = _extreme = 0m;
		_pendingOrder = null;
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
		if (MaxProfit <= 0m || MaxStopLoss <= 0m || AdxLength <= 0 || SessionStart < TimeSpan.Zero
			|| SessionStart >= SessionEnd || SessionEnd > TimeSpan.FromDays(1))
			throw new ArgumentException("Risk distances and ADX period must be positive; session times must be ordered within a UTC day.");
		ResetState();
		var quotes = new Subscription(DataType.Level1, Security);
		quotes.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
		StartTimer(TimeSpan.FromMinutes(1), () => TrySessionExit());
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx([adx], ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal volume, string comment)
		=> RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = comment,
		});

	private bool TrySessionExit()
	{
		var isOver = _entryDate is DateTime date && (CurrentTime.Date > date || CurrentTime.TimeOfDay >= SessionEnd);
		if (!isOver || Position <= 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return false;
		Submit(Sides.Sell, Position, "Previous-day session close");
		return true;
	}

	private void ObserveFill(Subscription subscription, MyTrade trade)
	{
		var previous = _filledPosition;
		var volume = trade.Trade.Volume;
		_filledPosition += trade.Order.Side == Sides.Buy ? volume : -volume;
		if (trade.Order.Side == Sides.Buy && _filledPosition > 0m)
		{
			_entryPrice = (_entryPrice * previous + trade.Trade.Price * volume) / _filledPosition;
			_extreme = previous == 0m ? trade.Trade.Price : Math.Max(_extreme, trade.Trade.Price);
		}
		if (_filledPosition == 0m)
			_entryPrice = _extreme = 0m;
	}

	private void ObservePrice(decimal price)
	{
		if (TrySessionExit() || Position <= 0m || _entryPrice <= 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		_extreme = Math.Max(_extreme, price);
		var comment = price <= _extreme - MaxStopLoss ? "Previous-day trailing stop"
			: price >= _entryPrice + MaxProfit ? "Previous-day profit target" : null;
		if (comment != null)
			Submit(Sides.Sell, Position, comment);
	}

	private void ObserveQuote(Level1ChangeMessage quote)
	{
		if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal price && price > 0m)
			ObservePrice(price);
	}

	private void ObserveDay(ICandleMessage candle)
	{
		var date = candle.OpenTime.Date;
		if (_day != date)
		{
			_isPreviousDayAvailable = _isDayStartObserved && _day == date.AddDays(-1);
			_previousDayHigh = _dayHigh;
			_previousDayLow = _dayLow;
			_day = date;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_isDayStartObserved = candle.OpenTime.TimeOfDay == TimeSpan.Zero;
		}
		else
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var previousClose = _previousClose;
		_previousClose = candle.ClosePrice;
		ObserveDay(candle);
		var previousAdx = _previousAdx;
		var value = (IAverageDirectionalIndexValue)values[0];
		var currentAdx = value.IsFormed ? value.MovingAverage : null;
		if (currentAdx is not null)
			_previousAdx = currentAdx;
		if (Position > 0m)
		{
			ObservePrice(candle.ClosePrice);
			return;
		}
		if (Position != 0m || IsPending || !IsFormedAndOnlineAndAllowTrading() || !_isPreviousDayAvailable
			|| previousClose is not decimal close || previousAdx is not decimal prior || currentAdx is not decimal strength || strength <= prior
			|| CurrentTime.TimeOfDay < SessionStart || CurrentTime.TimeOfDay >= SessionEnd)
			return;
		var isHighCross = close <= _previousDayHigh && candle.ClosePrice > _previousDayHigh;
		var isLowCross = close <= _previousDayLow && candle.ClosePrice > _previousDayLow;
		if (!isHighCross && !isLowCross)
			return;
		_entryDate = CurrentTime.Date;
		Submit(Sides.Buy, Volume, "Previous-day ADX breakout");
	}
}
