using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Santa Claus Rally trading strategy.
/// Buys at end of each month (seasonal rally pattern) and exits early next month.
/// Also applies trend following via MA filter for short positions in mid-month.
/// </summary>
public class SantaClausRallyStrategy : Strategy
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
	/// Initializes a new instance of the <see cref="SantaClausRallyStrategy"/>.
	/// </summary>
	public SantaClausRallyStrategy()
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

		// Rally zone: last week of month (day >= 25)
		var isRallyZone = dayOfMonth >= 25;
		// Exit zone: first week of month (day 3-7)
		var isExitZone = dayOfMonth >= 3 && dayOfMonth <= 7;
		// Mid-month short zone: day 12-18
		var isShortZone = dayOfMonth >= 12 && dayOfMonth <= 18;

		// Buy at end of month for rally
		if (isRallyZone && isNewDay && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Exit long in early next month
		else if (isExitZone && isNewDay && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Short mid-month if below MA
		else if (isShortZone && isNewDay && Position == 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Cover short before rally zone
		else if (isRallyZone && isNewDay && Position < 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevDayOfMonth = dayOfMonth;
	}
}
