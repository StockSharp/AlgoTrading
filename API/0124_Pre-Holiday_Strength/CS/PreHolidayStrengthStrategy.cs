using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Pre-Holiday Strength trading strategy.
/// Buys on Thursday (pre-weekend strength effect) if above MA, exits Monday.
/// Also buys before month-end holidays (last 2 days of month).
/// </summary>
public class PreHolidayStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private int _cooldown;
	private DayOfWeek _prevDayOfWeek;
	private bool _enteredThisDay;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PreHolidayStrengthStrategy"/>.
	/// </summary>
	public PreHolidayStrengthStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_ma = default;
		_cooldown = 0;
		_prevDayOfWeek = DayOfWeek.Sunday;
		_enteredThisDay = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var dayOfWeek = candle.OpenTime.DayOfWeek;

		// Reset entry flag on new day
		if (dayOfWeek != _prevDayOfWeek)
			_enteredThisDay = false;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDayOfWeek = dayOfWeek;
			return;
		}

		// Pre-weekend buy: Thursday if above MA
		if (dayOfWeek == DayOfWeek.Thursday && !_enteredThisDay && Position == 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			_enteredThisDay = true;
		}
		// Exit on Monday
		else if (dayOfWeek == DayOfWeek.Monday && Position > 0 && !_enteredThisDay)
		{
			SellMarket();
			_cooldown = CooldownBars;
			_enteredThisDay = true;
		}
		// Short on Tuesday if below MA
		else if (dayOfWeek == DayOfWeek.Tuesday && !_enteredThisDay && Position == 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
			_enteredThisDay = true;
		}
		// Cover short on Wednesday
		else if (dayOfWeek == DayOfWeek.Wednesday && Position < 0 && !_enteredThisDay)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			_enteredThisDay = true;
		}

		_prevDayOfWeek = dayOfWeek;
	}
}
