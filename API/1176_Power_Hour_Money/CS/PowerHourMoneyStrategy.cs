using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Power Hour Money strategy.
/// Goes long when month, week, day and hour candles are all bullish.
/// Goes short when all are bearish.
/// Supports session filter, trailing stops and end of day close.
/// </summary>
public class PowerHourMoneyStrategy : Strategy
{
	private readonly StrategyParam<TradingSession> _session;
	private readonly StrategyParam<bool> _useTrail;
	private readonly StrategyParam<decimal> _longTrail;
	private readonly StrategyParam<decimal> _shortTrail;
	private readonly StrategyParam<bool> _closeEod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _monthGreen;
	private bool _weekGreen;
	private bool _dayGreen;
	private bool _hourGreen;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;

	/// <summary>
	/// Trading session filter.
	/// </summary>
	public TradingSession Session
	{
		get => _session.Value;
		set => _session.Value = value;
	}

	/// <summary>
	/// Use trailing stop loss.
	/// </summary>
	public bool UseTrail
	{
		get => _useTrail.Value;
		set => _useTrail.Value = value;
	}

	/// <summary>
	/// Long trailing stop percentage.
	/// </summary>
	public decimal LongTrailPercent
	{
		get => _longTrail.Value;
		set => _longTrail.Value = value;
	}

	/// <summary>
	/// Short trailing stop percentage.
	/// </summary>
	public decimal ShortTrailPercent
	{
		get => _shortTrail.Value;
		set => _shortTrail.Value = value;
	}

	/// <summary>
	/// Close positions at end of day.
	/// </summary>
	public bool ClosePositionsEod
	{
		get => _closeEod.Value;
		set => _closeEod.Value = value;
	}

	/// <summary>
	/// The type of candles used for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public PowerHourMoneyStrategy()
	{
		_session = Param(nameof(Session), TradingSession.NySession)
			.SetDisplay("Session", "Trading session", "Core");

		_useTrail = Param(nameof(UseTrail), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Trail Stop Settings");

		_longTrail = Param(nameof(LongTrailPercent), 0.2m)
			.SetDisplay("Long Trail %", "Long trailing stop percent", "Trail Stop Settings");

		_shortTrail = Param(nameof(ShortTrailPercent), 0.2m)
			.SetDisplay("Short Trail %", "Short trailing stop percent", "Trail Stop Settings");

		_closeEod = Param(nameof(ClosePositionsEod), true)
			.SetDisplay("Close EOD", "Close positions at 16:45", "Core");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Base candle series", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security, DataType)[]
		{
			(Security, CandleType),
			(Security, TimeSpan.FromHours(1).TimeFrame()),
			(Security, TimeSpan.FromDays(1).TimeFrame()),
			(Security, TimeSpan.FromDays(7).TimeFrame()),
			(Security, TimeSpan.FromDays(30).TimeFrame())
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_monthGreen = _weekGreen = _dayGreen = _hourGreen = false;
		_longStopPrice = 0m;
		_shortStopPrice = decimal.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		SubscribeCandles(TimeSpan.FromHours(1).TimeFrame())
			.Bind(c =>
			{
				if (c.State != CandleStates.Finished)
					return;

				_hourGreen = c.ClosePrice > c.OpenPrice;
			})
			.Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.Bind(c =>
			{
				if (c.State != CandleStates.Finished)
					return;

				_dayGreen = c.ClosePrice > c.OpenPrice;
			})
			.Start();

		SubscribeCandles(TimeSpan.FromDays(7).TimeFrame())
			.Bind(c =>
			{
				if (c.State != CandleStates.Finished)
					return;

				_weekGreen = c.ClosePrice > c.OpenPrice;
			})
			.Start();

		SubscribeCandles(TimeSpan.FromDays(30).TimeFrame())
			.Bind(c =>
			{
				if (c.State != CandleStates.Finished)
					return;

				_monthGreen = c.ClosePrice > c.OpenPrice;
			})
			.Start();

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var local = candle.OpenTime.ToLocalTime().TimeOfDay;

		var tradeable = Session switch
		{
			TradingSession.NySession => local >= new TimeSpan(9, 30, 0) && local <= new TimeSpan(11, 30, 0),
			TradingSession.ExtendedNy => local >= new TimeSpan(8, 0, 0) && local <= new TimeSpan(16, 0, 0),
			_ => true,
		};

		var monthGreen = _monthGreen;
		var weekGreen = _weekGreen;
		var dayGreen = _dayGreen;
		var hourGreen = _hourGreen;

		var monthRed = !monthGreen;
		var weekRed = !weekGreen;
		var dayRed = !dayGreen;
		var hourRed = !hourGreen;

		var callLong = tradeable && monthGreen && weekGreen && dayGreen && hourGreen;
		var callShort = tradeable && monthRed && weekRed && dayRed && hourRed;

		if (callLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (callShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (UseTrail)
		{
			if (Position > 0)
			{
				var stopValue = candle.ClosePrice * (1 - LongTrailPercent / 100m);
				_longStopPrice = Math.Max(stopValue, _longStopPrice);

				if (candle.ClosePrice <= _longStopPrice)
					SellMarket(Math.Abs(Position));
			}
			else if (Position < 0)
			{
				var stopValue = candle.ClosePrice * (1 + ShortTrailPercent / 100m);
				_shortStopPrice = Math.Min(stopValue, _shortStopPrice);

				if (candle.ClosePrice >= _shortStopPrice)
					BuyMarket(Math.Abs(Position));
			}
			else
			{
				_longStopPrice = 0m;
				_shortStopPrice = decimal.MaxValue;
			}
		}

		if (ClosePositionsEod && local.Hours == 16 && local.Minutes == 45 && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
		}
	}
}

/// <summary>
/// Trading sessions.
/// </summary>
public enum TradingSession
{
	/// <summary>
	/// NY Session 9:30-11:30.
	/// </summary>
	NySession,

	/// <summary>
	/// Extended NY Session 8-16.
	/// </summary>
	ExtendedNy,

	/// <summary>
	/// All sessions.
	/// </summary>
	All
}
