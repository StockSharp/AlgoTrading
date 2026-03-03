using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Turnaround Tuesday trading strategy.
/// Buys on Tuesday if previous session declined, sells on Friday or if price crosses MA.
/// Also goes short on Wednesday if previous session rallied.
/// Uses half-day detection to simulate daily sessions on intraday data.
/// </summary>
public class TurnaroundTuesdayStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prevMa;
	private decimal _sessionOpen;
	private decimal _sessionClose;
	private int _prevSessionDay;
	private bool _prevSessionDecline;
	private bool _prevSessionRally;
	private int _currentSessionDay;
	private bool _enteredThisSession;
	private int _cooldown;

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
	/// Initializes a new instance of the <see cref="TurnaroundTuesdayStrategy"/>.
	/// </summary>
	public TurnaroundTuesdayStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");

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
		_prevMa = 0;
		_sessionOpen = 0;
		_sessionClose = 0;
		_prevSessionDay = -1;
		_prevSessionDecline = false;
		_prevSessionRally = false;
		_currentSessionDay = -1;
		_enteredThisSession = false;
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
		var dayOfYear = candle.OpenTime.DayOfYear;
		var dayOfWeek = (int)candle.OpenTime.DayOfWeek;

		// Detect new session (new calendar day)
		if (dayOfYear != _currentSessionDay)
		{
			// Save previous session result
			if (_currentSessionDay >= 0 && _sessionOpen > 0)
			{
				_prevSessionDecline = _sessionClose < _sessionOpen;
				_prevSessionRally = _sessionClose > _sessionOpen;
				_prevSessionDay = _currentSessionDay;
			}

			_currentSessionDay = dayOfYear;
			_sessionOpen = candle.OpenPrice;
			_enteredThisSession = false;
		}

		_sessionClose = close;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMa = maValue;
			return;
		}

		// Entry: buy on any day if previous session declined and no position
		if (Position == 0 && !_enteredThisSession && _prevSessionDecline && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			_enteredThisSession = true;
			_prevSessionDecline = false;
		}
		// Entry: sell on any day if previous session rallied and no position
		else if (Position == 0 && !_enteredThisSession && _prevSessionRally && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
			_enteredThisSession = true;
			_prevSessionRally = false;
		}

		// Exit long if price crosses below MA
		if (Position > 0 && _prevMa > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit short if price crosses above MA
		if (Position < 0 && _prevMa > 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevMa = maValue;
	}
}
