using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day of Week trading strategy.
/// Enters long on Monday and short on Friday, with MA trend filter.
/// Uses daily transitions to limit trade frequency.
/// </summary>
public class DayOfWeekStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prevMa;
	private decimal _prevClose;
	private DayOfWeek _lastTradeDay;
	private int _cooldown;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DayOfWeekStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "SMA period", "Indicators")
			.SetRange(10, 50);

		_cooldownBars = Param(nameof(CooldownBars), 300)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 2000);
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
		_prevMa = 0;
		_prevClose = 0;
		_lastTradeDay = default;
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

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var day = candle.OpenTime.DayOfWeek;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMa = ma;
			_prevClose = close;
			return;
		}

		// Exit logic: MA cross
		if (Position > 0 && close < ma && _prevMa > 0 && _prevClose >= _prevMa)
		{
			SellMarket();
			_cooldown = CooldownBars;
			_lastTradeDay = day;
		}
		else if (Position < 0 && close > ma && _prevMa > 0 && _prevClose <= _prevMa)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			_lastTradeDay = day;
		}

		// Entry logic: day-of-week based (one trade per day transition)
		if (Position == 0 && day != _lastTradeDay)
		{
			// Monday/Tuesday: buy if above MA
			if ((day == DayOfWeek.Monday || day == DayOfWeek.Tuesday) && close > ma)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_lastTradeDay = day;
			}
			// Thursday/Friday: sell if below MA
			else if ((day == DayOfWeek.Thursday || day == DayOfWeek.Friday) && close < ma)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_lastTradeDay = day;
			}
		}

		_prevMa = ma;
		_prevClose = close;
	}
}
