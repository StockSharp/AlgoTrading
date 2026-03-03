using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on the price movement fade during the lunch break.
/// Fades the prior trend around midday, with MA confirmation and cooldown.
/// </summary>
public class LunchBreakFadeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prevClose;
	private decimal _prevPrevClose;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="LunchBreakFadeStrategy"/>.
	/// </summary>
	public LunchBreakFadeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Strategy");

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
		_prevClose = 0;
		_prevPrevClose = 0;
		_cooldown = 0;
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
		var hour = candle.OpenTime.Hour;

		if (_prevClose == 0)
		{
			_prevClose = close;
			return;
		}

		if (_prevPrevClose == 0)
		{
			_prevPrevClose = _prevClose;
			_prevClose = close;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevClose = _prevClose;
			_prevClose = close;
			return;
		}

		// Lunch zone: hours 11-14
		var isLunchTime = hour >= 11 && hour <= 14;

		if (isLunchTime)
		{
			var priorUptrend = _prevClose > _prevPrevClose;
			var priorDowntrend = _prevClose < _prevPrevClose;
			var currentBearish = close < candle.OpenPrice;
			var currentBullish = close > candle.OpenPrice;

			// Fade uptrend at lunch: short
			if (priorUptrend && currentBearish && Position == 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			// Fade downtrend at lunch: long
			else if (priorDowntrend && currentBullish && Position == 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		// Exit on MA cross
		if (Position > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrevClose = _prevClose;
		_prevClose = close;
	}
}
