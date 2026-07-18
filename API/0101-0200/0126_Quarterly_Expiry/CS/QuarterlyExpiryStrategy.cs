using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Quarterly Expiry trading strategy.
/// Trades around monthly expiry dates (3rd Friday area of each month).
/// Buys if above MA in expiry week, sells if below. Exits next week.
/// </summary>
public class QuarterlyExpiryStrategy : Strategy
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
	/// Initializes a new instance of the <see cref="QuarterlyExpiryStrategy"/>.
	/// </summary>
	public QuarterlyExpiryStrategy()
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

		// Expiry week zone: day 15-19 (around 3rd Friday of each month)
		var isExpiryWeek = dayOfMonth >= 15 && dayOfMonth <= 19;
		// Post-expiry exit zone: day 22-25
		var isPostExpiry = dayOfMonth >= 22 && dayOfMonth <= 25;
		// Start of month entry zone: day 1-5
		var isStartOfMonth = dayOfMonth >= 1 && dayOfMonth <= 5;
		// Pre-expiry exit: day 12-14
		var isPreExpiry = dayOfMonth >= 12 && dayOfMonth <= 14;

		// Entry in expiry week: buy if above MA
		if (isExpiryWeek && isNewDay && Position == 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Exit after expiry week
		else if (isPostExpiry && isNewDay && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Short entry at start of month if below MA
		else if (isStartOfMonth && isNewDay && Position == 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Cover short before expiry
		else if (isPreExpiry && isNewDay && Position < 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevDayOfMonth = dayOfMonth;
	}
}
