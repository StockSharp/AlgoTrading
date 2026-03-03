using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of First Day of Month trading strategy.
/// Enters long on the first few days of month, exits around the 5th-10th day.
/// Enters short in mid-month if price below MA.
/// </summary>
public class FirstDayOfMonthStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private int _cooldown;
	private int _prevDayOfMonth;

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
	/// Initializes a new instance of the <see cref="FirstDayOfMonthStrategy"/>.
	/// </summary>
	public FirstDayOfMonthStrategy()
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
		var isNewDay = dayOfMonth != _prevDayOfMonth;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevDayOfMonth = dayOfMonth;
			return;
		}

		// First days of month zone: day 1-3
		var isFirstDays = dayOfMonth >= 1 && dayOfMonth <= 3;
		// Exit zone: day 8-12
		var isExitZone = dayOfMonth >= 8 && dayOfMonth <= 12;
		// Mid-month zone for shorts: day 15-20
		var isMidMonth = dayOfMonth >= 15 && dayOfMonth <= 20;
		// End-of-month exit zone for shorts: day 25+
		var isEndOfMonth = dayOfMonth >= 25;

		// Entry: buy on first days of month
		if (isFirstDays && isNewDay && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Exit long around mid-first-week
		else if (isExitZone && isNewDay && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Short entry mid-month if below MA
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
	}
}
