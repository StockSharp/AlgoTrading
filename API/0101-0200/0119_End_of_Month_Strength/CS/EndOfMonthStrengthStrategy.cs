using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of End of Month Strength trading strategy.
/// Buys on the last week of the month, exits on the first week of the next month.
/// Also sells short in mid-month if price below MA.
/// </summary>
public class EndOfMonthStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private int _cooldown;
	private int _prevDayOfMonth;
	private int _prevMonth;

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
	/// Initializes a new instance of the <see cref="EndOfMonthStrengthStrategy"/>.
	/// </summary>
	public EndOfMonthStrengthStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 50)
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
		_prevDayOfMonth = 0;
		_prevMonth = 0;
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
		var dayOfMonth = candle.OpenTime.Day;
		var month = candle.OpenTime.Month;

		// Detect new day transition
		var isNewDay = dayOfMonth != _prevDayOfMonth;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDayOfMonth = dayOfMonth;
			_prevMonth = month;
			return;
		}

		// End-of-month zone: day >= 24
		var isEndOfMonth = dayOfMonth >= 24;
		// Beginning-of-month zone: day <= 5
		var isBeginOfMonth = dayOfMonth <= 5;
		// Mid-month zone: day between 10 and 20
		var isMidMonth = dayOfMonth >= 10 && dayOfMonth <= 20;

		// Entry: buy at end of month if flat
		if (isEndOfMonth && isNewDay && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Exit: sell at beginning of next month
		else if (isBeginOfMonth && isNewDay && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Short in mid-month if below MA
		else if (isMidMonth && isNewDay && Position == 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Cover short at end of month
		else if (isEndOfMonth && isNewDay && Position < 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevDayOfMonth = dayOfMonth;
		_prevMonth = month;
	}
}
