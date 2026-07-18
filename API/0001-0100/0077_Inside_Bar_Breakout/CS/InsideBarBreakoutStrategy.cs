using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Inside Bar Breakout strategy.
/// Detects inside bar patterns (high lower than previous high, low higher than previous low).
/// Enters on breakout of the inside bar's high (buy) or low (sell).
/// Uses SMA for exit signals.
/// </summary>
public class InsideBarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _prevCandle;
	private ICandleMessage _insideBar;
	private bool _waitingForBreakout;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public InsideBarBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevCandle = null;
		_insideBar = null;
		_waitingForBreakout = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_insideBar = null;
		_waitingForBreakout = false;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCandle = candle;
			_waitingForBreakout = false;
			return;
		}

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		// Check for breakout of a previously detected inside bar
		if (_waitingForBreakout && _insideBar != null && Position == 0)
		{
			if (candle.HighPrice > _insideBar.HighPrice)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_waitingForBreakout = false;
			}
			else if (candle.LowPrice < _insideBar.LowPrice)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_waitingForBreakout = false;
			}
		}

		// Check if current candle is an inside bar
		if (candle.HighPrice < _prevCandle.HighPrice && candle.LowPrice > _prevCandle.LowPrice)
		{
			_insideBar = candle;
			_waitingForBreakout = true;
		}

		// Exit logic using SMA
		if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevCandle = candle;
	}
}
