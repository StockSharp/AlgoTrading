namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Channel trading strategy that places limit orders at the end of the monitored session.
/// </summary>
public class ChannelEaLimitsStrategy : Strategy
{
	private readonly StrategyParam<int> _beginHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _sessionStart;
	private DateTimeOffset _sessionEnd;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private int _barsInSession;
	private DateTimeOffset? _prevCandleClose;
	private bool _ordersPlaced;
	private bool _needsSessionReset;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelEaLimitsStrategy"/> class.
	/// </summary>
	public ChannelEaLimitsStrategy()
	{
		_beginHour = Param(nameof(BeginHour), 1)
			.SetDisplay("Begin Hour", "Hour when session tracking starts (0-23)", "Session")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 10)
			.SetDisplay("End Hour", "Hour when limit orders are placed (0-23)", "Session")
			.SetRange(0, 23);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for each limit order", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to build the session channel", "General");
	}

	/// <summary>
	/// Hour when session tracking starts.
	/// </summary>
	public int BeginHour
	{
		get => _beginHour.Value;
		set => _beginHour.Value = value;
	}

	/// <summary>
	/// Hour when the strategy places new pending orders.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Volume per limit order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_sessionStart = DateTimeOffset.MinValue;
		_sessionEnd = DateTimeOffset.MinValue;
		_sessionHigh = decimal.MinValue;
		_sessionLow = decimal.MaxValue;
		_barsInSession = 0;
		_prevCandleClose = null;
		_ordersPlaced = false;
		_needsSessionReset = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closeTime = candle.CloseTime;
		var sessionStart = CalculateSessionStart(closeTime);

		if (_sessionStart != sessionStart)
		{
			_sessionStart = sessionStart;
			_sessionEnd = CalculateSessionEnd(_sessionStart);
			ResetSessionState();
		}

		var tradingReady = IsFormedAndOnlineAndAllowTrading();

		if (_needsSessionReset && tradingReady)
		{
			ClearForNewSession();
			_needsSessionReset = false;
		}

		if (candle.OpenTime >= _sessionStart && candle.OpenTime < _sessionEnd)
		{
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			if (_sessionHigh == decimal.MinValue || high > _sessionHigh)
				_sessionHigh = high;

			if (_sessionLow == decimal.MaxValue || low < _sessionLow)
				_sessionLow = low;

			_barsInSession++;
		}

		if (!_ordersPlaced && tradingReady && _prevCandleClose.HasValue)
		{
			var previousClose = _prevCandleClose.Value;

			if (previousClose < _sessionEnd && closeTime >= _sessionEnd)
			{
				if (_barsInSession >= 2 && _sessionLow < _sessionHigh)
				{
					BuyLimit(OrderVolume, _sessionLow);
					SellLimit(OrderVolume, _sessionHigh);
					_ordersPlaced = true;
				}
			}
		}

		_prevCandleClose = closeTime;
	}

	private void ResetSessionState()
	{
		_sessionHigh = decimal.MinValue;
		_sessionLow = decimal.MaxValue;
		_barsInSession = 0;
		_ordersPlaced = false;
		_needsSessionReset = true;
	}

	private void ClearForNewSession()
	{
		CancelActiveOrders();

		if (Position != 0)
			ClosePosition();
	}

	private DateTimeOffset CalculateSessionStart(DateTimeOffset time)
	{
		var offset = time.Offset;
		var day = new DateTimeOffset(time.Date, offset);
		var start = day.AddHours(BeginHour);
		var startHour = TimeSpan.FromHours(BeginHour);

		if (BeginHour <= EndHour)
		{
			if (time < start)
				start = start.AddDays(-1);
		}
		else
		{
			if (time.TimeOfDay < startHour)
				start = start.AddDays(-1);
		}

		return start;
	}

	private DateTimeOffset CalculateSessionEnd(DateTimeOffset sessionStart)
	{
		var offset = sessionStart.Offset;
		var day = new DateTimeOffset(sessionStart.Date, offset);
		var end = day.AddHours(EndHour);

		if (EndHour <= BeginHour || end <= sessionStart)
			end = end.AddDays(1);

		return end;
	}
}
