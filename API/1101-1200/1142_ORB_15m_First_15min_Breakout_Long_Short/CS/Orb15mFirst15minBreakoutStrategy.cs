using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ORB 15m first 15 minute breakout strategy.
/// At the close of the first 15 minute bar after the session open (Stockholm time) a bullish bar opens a long and a bearish bar a short.
/// The stop sits at the opposite extreme of that bar, the size risks RiskPct of equity on the stop distance, the optional target is
/// RMultiple times the risk, and any open position is closed at the session end.
/// </summary>
public class Orb15mFirst15minBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPct;
	private readonly StrategyParam<bool> _tpTenR;
	private readonly StrategyParam<decimal> _rMultiple;
	private readonly StrategyParam<int> _sessionOpenHour;
	private readonly StrategyParam<int> _sessionOpenMinute;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<int> _sessionEndMinute;
	private readonly StrategyParam<DataType> _candleType;

	private static readonly TimeZoneInfo _stockholm = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

	private DateTime _currentDay;
	private bool _tradedToday;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Percent of equity risked per trade.
	/// </summary>
	public decimal RiskPct
	{
		get => _riskPct.Value;
		set => _riskPct.Value = value;
	}

	/// <summary>
	/// Use the take profit at RMultiple times risk.
	/// </summary>
	public bool TpTenR
	{
		get => _tpTenR.Value;
		set => _tpTenR.Value = value;
	}

	/// <summary>
	/// Take profit as a multiple of risk.
	/// </summary>
	public decimal RMultiple
	{
		get => _rMultiple.Value;
		set => _rMultiple.Value = value;
	}

	/// <summary>
	/// Session open hour (Stockholm time).
	/// </summary>
	public int SessionOpenHour
	{
		get => _sessionOpenHour.Value;
		set => _sessionOpenHour.Value = value;
	}

	/// <summary>
	/// Session open minute (Stockholm time).
	/// </summary>
	public int SessionOpenMinute
	{
		get => _sessionOpenMinute.Value;
		set => _sessionOpenMinute.Value = value;
	}

	/// <summary>
	/// Session end hour (Stockholm time).
	/// </summary>
	public int SessionEndHour
	{
		get => _sessionEndHour.Value;
		set => _sessionEndHour.Value = value;
	}

	/// <summary>
	/// Session end minute (Stockholm time).
	/// </summary>
	public int SessionEndMinute
	{
		get => _sessionEndMinute.Value;
		set => _sessionEndMinute.Value = value;
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
	public Orb15mFirst15minBreakoutStrategy()
	{
		_riskPct = Param(nameof(RiskPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk");

		_tpTenR = Param(nameof(TpTenR), true)
			.SetDisplay("Use Take Profit", "Take profit at RMultiple times risk", "Risk");

		_rMultiple = Param(nameof(RMultiple), 10m)
			.SetGreaterThanZero()
			.SetDisplay("R Multiple", "Take profit as a multiple of risk", "Risk");

		_sessionOpenHour = Param(nameof(SessionOpenHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Session Open Hour", "Session open hour (Stockholm time)", "Session");

		_sessionOpenMinute = Param(nameof(SessionOpenMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Session Open Minute", "Session open minute (Stockholm time)", "Session");

		_sessionEndHour = Param(nameof(SessionEndHour), 22)
			.SetRange(0, 23)
			.SetDisplay("Session End Hour", "Session end hour (Stockholm time)", "Session");

		_sessionEndMinute = Param(nameof(SessionEndMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Session End Minute", "Session end minute (Stockholm time)", "Session");

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
		ResetState();
	}

	private void ResetState()
	{
		_currentDay = default;
		_tradedToday = false;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime.ToUniversalTime(), _stockholm);
		var tod = local.TimeOfDay;
		var sessionOpen = new TimeSpan(SessionOpenHour, SessionOpenMinute, 0);
		var sessionEnd = new TimeSpan(SessionEndHour, SessionEndMinute, 0);

		if (local.Date != _currentDay)
		{
			_currentDay = local.Date;
			_tradedToday = false;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			var closeTime = local + (candle.CloseTime - candle.OpenTime);

			if (Position > 0 && (_stopPrice is decimal ls && candle.LowPrice <= ls || _takePrice is decimal lt && candle.HighPrice >= lt || closeTime.TimeOfDay >= sessionEnd))
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
			}
			else if (Position < 0 && (_stopPrice is decimal ss && candle.HighPrice >= ss || _takePrice is decimal st && candle.LowPrice <= st || closeTime.TimeOfDay >= sessionEnd))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
			}

			return;
		}

		// Only the first bar of the session is the reference bar.
		if (_tradedToday || tod != sessionOpen)
			return;

		_tradedToday = true;

		var isLong = candle.ClosePrice > candle.OpenPrice;
		var isShort = candle.ClosePrice < candle.OpenPrice;

		if (!isLong && !isShort)
			return;

		var entry = candle.ClosePrice;
		var stop = isLong ? candle.LowPrice : candle.HighPrice;
		var risk = Math.Abs(entry - stop);

		if (risk <= 0)
			return;

		var volume = Volume;
		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > 0)
		{
			var riskVolume = equity * RiskPct / 100m / risk;
			if (riskVolume > 0)
				volume = riskVolume;
		}

		_stopPrice = stop;
		_takePrice = TpTenR ? (isLong ? entry + risk * RMultiple : entry - risk * RMultiple) : null;

		if (isLong)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}
}
