using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Heikin Ashi candles with Supertrend filter and ATR based stops.
/// </summary>
public class HeikenAshiSupertrendAtrSlStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSupertrend;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenAtrMultiplier;
	private readonly StrategyParam<bool> _useHardStop;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private AverageTrueRange _atr;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevHaHigh;
	private decimal _prevHaLow;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _breakEvenActive;

	/// <summary>
	/// Enable Supertrend direction filter.
	/// </summary>
	public bool UseSupertrend
	{
		get => _useSupertrend.Value;
		set => _useSupertrend.Value = value;
	}

	/// <summary>
	/// ATR period for Supertrend and stops.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend multiplier.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Enable break even stop.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// ATR multiplier to activate break even.
	/// </summary>
	public decimal BreakEvenAtrMultiplier
	{
		get => _breakEvenAtrMultiplier.Value;
		set => _breakEvenAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Enable initial ATR stop loss.
	/// </summary>
	public bool UseHardStop
	{
		get => _useHardStop.Value;
		set => _useHardStop.Value = value;
	}

	/// <summary>
	/// ATR multiplier for initial stop loss.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HeikenAshiSupertrendAtrSlStrategy()
	{
		_useSupertrend = Param(nameof(UseSupertrend), true)
			.SetDisplay("Use Supertrend", "Enable Supertrend direction filter", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for Supertrend and stops", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 14, 1);

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Supertrend Factor", "Multiplier for Supertrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Use Break Even", "Enable break even stop", "Stops");

		_breakEvenAtrMultiplier = Param(nameof(BreakEvenAtrMultiplier), 1m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Break Even ATR Mult", "Move stop to entry after ATR move", "Stops")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_useHardStop = Param(nameof(UseHardStop), false)
			.SetDisplay("Use Hard Stop", "Enable initial ATR stop", "Stops");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Stop Loss ATR Mult", "ATR multiplier for initial stop", "Stops")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevHaOpen = 0;
		_prevHaClose = 0;
		_prevHaHigh = 0;
		_prevHaLow = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_breakEvenActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_supertrend = new() { Length = AtrPeriod, Multiplier = AtrFactor };
		_atr = new() { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_supertrend, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		decimal haOpen, haClose, haHigh, haLow;

		if (_prevHaOpen == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
			haHigh = candle.HighPrice;
			haLow = candle.LowPrice;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
			haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
			haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);
		}

		var isGreen = haClose > haOpen;
		var isRed = haClose < haOpen;

		var threshold = Security.MinPriceStep * 0.1m;
		var noBottomWick = Math.Abs(Math.Min(haOpen, haClose) - haLow) <= threshold;
		var noTopWick = Math.Abs(haHigh - Math.Max(haOpen, haClose)) <= threshold;

		var isUptrend = candle.ClosePrice > supertrendValue;
		var isDowntrend = candle.ClosePrice < supertrendValue;

		var longCondition = isGreen && noBottomWick && (!UseSupertrend || isUptrend);
		var shortCondition = isRed && noTopWick && (!UseSupertrend || isDowntrend);
		var exitLongCondition = isRed && noTopWick;
		var exitShortCondition = isGreen && noBottomWick;

		if (Position > 0)
		{
			if (UseBreakEven && !_breakEvenActive && candle.HighPrice >= _entryPrice + atrValue * BreakEvenAtrMultiplier)
			{
				_breakEvenActive = true;
				_stopPrice = _entryPrice;
			}

			if (UseHardStop && _stopPrice > 0 && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (exitLongCondition)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}
		}
		else if (Position < 0)
		{
			if (UseBreakEven && !_breakEvenActive && candle.LowPrice <= _entryPrice - atrValue * BreakEvenAtrMultiplier)
			{
				_breakEvenActive = true;
				_stopPrice = _entryPrice;
			}

			if (UseHardStop && _stopPrice > 0 && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (exitShortCondition)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
		}

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_breakEvenActive = false;
			if (UseHardStop)
				_stopPrice = candle.ClosePrice - atrValue * StopLossAtrMultiplier;
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_breakEvenActive = false;
			if (UseHardStop)
				_stopPrice = candle.ClosePrice + atrValue * StopLossAtrMultiplier;
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevHaHigh = haHigh;
		_prevHaLow = haLow;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0;
		_stopPrice = 0;
		_breakEvenActive = false;
	}
}
