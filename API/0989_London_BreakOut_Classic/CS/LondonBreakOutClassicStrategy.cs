using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// London session breakout strategy based on Asian range.
/// </summary>
public class LondonBreakOutClassicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _crv;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _boxStart;
	private readonly StrategyParam<TimeSpan> _boxEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;

	private decimal _sessionHigh;
	private decimal _sessionLow;
	private DateTime _currentDay;
	private bool _tradeOpened;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _isLong;

	/// <summary>
	/// Risk reward factor.
	/// </summary>
	public decimal Crv
	{
		get => _crv.Value;
		set => _crv.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start time of the Asian session.
	/// </summary>
	public TimeSpan BoxStart
	{
		get => _boxStart.Value;
		set => _boxStart.Value = value;
	}

	/// <summary>
	/// End time of the Asian session.
	/// </summary>
	public TimeSpan BoxEnd
	{
		get => _boxEnd.Value;
		set => _boxEnd.Value = value;
	}

	/// <summary>
	/// Start time for breakout trading.
	/// </summary>
	public TimeSpan TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// End time for breakout trading.
	/// </summary>
	public TimeSpan TradeEnd
	{
		get => _tradeEnd.Value;
		set => _tradeEnd.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LondonBreakOutClassicStrategy"/> class.
	/// </summary>
	public LondonBreakOutClassicStrategy()
	{
		_crv = Param(nameof(Crv), 1m)
			.SetGreaterThanZero()
			.SetDisplay("CRV", "Risk reward factor", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_boxStart = Param(nameof(BoxStart), TimeSpan.Zero)
			.SetDisplay("Box Start", "Start time of the Asian session", "Session");

		_boxEnd = Param(nameof(BoxEnd), TimeSpan.FromHours(6) + TimeSpan.FromMinutes(55))
			.SetDisplay("Box End", "End time of the Asian session", "Session");

		_tradeStart = Param(nameof(TradeStart), TimeSpan.FromHours(7))
			.SetDisplay("Trade Start", "Start time for breakout trading", "Session");

		_tradeEnd = Param(nameof(TradeEnd), TimeSpan.FromHours(16))
			.SetDisplay("Trade End", "End time for breakout trading", "Session");
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
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_currentDay = default;
		_tradeOpened = false;
		_stopPrice = 0m;
		_takePrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcess).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_sessionHigh = 0m;
			_sessionLow = decimal.MaxValue;
			_tradeOpened = false;

			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		var time = candle.OpenTime.TimeOfDay;

		if (time >= BoxStart && time <= BoxEnd)
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			return;
		}

		if (time >= TradeStart && time <= TradeEnd)
		{
			if (!_tradeOpened)
			{
				var mid = (_sessionHigh + _sessionLow) / 2m;

				if (candle.HighPrice >= _sessionHigh && Position <= 0)
				{
					_stopPrice = mid;
					var entry = candle.ClosePrice;
					_takePrice = entry + (entry - _stopPrice) * Crv;
					BuyMarket(Volume + Math.Abs(Position));
					_isLong = true;
					_tradeOpened = true;
					return;
				}

				if (candle.LowPrice <= _sessionLow && Position >= 0)
				{
					_stopPrice = mid;
					var entry = candle.ClosePrice;
					_takePrice = entry - (_stopPrice - entry) * Crv;
					SellMarket(Volume + Math.Abs(Position));
					_isLong = false;
					_tradeOpened = true;
					return;
				}
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice || time >= TradeEnd)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice || time >= TradeEnd)
				BuyMarket(Math.Abs(Position));
		}
	}
}
