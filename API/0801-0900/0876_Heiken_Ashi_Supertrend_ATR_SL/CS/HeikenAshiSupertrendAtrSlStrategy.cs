using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heiken Ashi Supertrend ATR-SL strategy.
/// A green Heiken Ashi candle without a lower wick buys and a red one without an upper wick sells; with UseSupertrend the
/// Supertrend (AtrPeriod, AtrFactor) must point the same way. A long closes on a red candle without an upper wick and a short on a
/// green candle without a lower wick, reversing when the entry filter agrees. With UseHardStop the stop sits
/// StopLossAtrMultiplier ATRs from the entry; with UseBreakEven it moves to the entry price once price has run
/// BreakEvenAtrMultiplier ATRs in favour. ATR values are taken at entry.
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

	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal? _upperBand;
	private decimal? _lowerBand;
	private decimal? _prevClose;
	private int _trend;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private decimal? _stopPrice;

	/// <summary>
	/// Require the Supertrend direction for entries.
	/// </summary>
	public bool UseSupertrend
	{
		get => _useSupertrend.Value;
		set => _useSupertrend.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend ATR factor.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Move the stop to the entry price after a favourable move.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Favourable move in ATRs that activates the break even.
	/// </summary>
	public decimal BreakEvenAtrMultiplier
	{
		get => _breakEvenAtrMultiplier.Value;
		set => _breakEvenAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Use an ATR stop loss from the entry.
	/// </summary>
	public bool UseHardStop
	{
		get => _useHardStop.Value;
		set => _useHardStop.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ATRs.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public HeikenAshiSupertrendAtrSlStrategy()
	{
		_useSupertrend = Param(nameof(UseSupertrend), true)
			.SetDisplay("Use Supertrend", "Require the Supertrend direction for entries", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "Supertrend ATR factor", "Indicators");

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Use Break Even", "Move the stop to the entry price after a favourable move", "Risk");

		_breakEvenAtrMultiplier = Param(nameof(BreakEvenAtrMultiplier), 1m)
			.SetNotNegative()
			.SetDisplay("Break Even ATR", "Favourable move in ATRs that activates the break even", "Risk");

		_useHardStop = Param(nameof(UseHardStop), false)
			.SetDisplay("Use Hard Stop", "Use an ATR stop loss from the entry", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop loss distance in ATRs", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_haOpen = null;
		_haClose = null;
		_upperBand = null;
		_lowerBand = null;
		_prevClose = null;
		_trend = 1;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void Enter(bool isLong, decimal close, decimal atr)
	{
		if (isLong)
			BuyMarket(Volume + Math.Abs(Position));
		else
			SellMarket(Volume + Math.Abs(Position));

		_entryPrice = close;
		_entryAtr = atr;
		_stopPrice = UseHardStop && StopLossAtrMultiplier > 0
			? (isLong ? close - StopLossAtrMultiplier * atr : close + StopLossAtrMultiplier * atr)
			: null;
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Heiken Ashi candle.
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal prevHaOpen && _haClose is decimal prevHaClose
			? (prevHaOpen + prevHaClose) / 2m
			: (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));

		_haOpen = haOpen;
		_haClose = haClose;

		var close = candle.ClosePrice;

		// Supertrend on the regular candles.
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var lower = hl2 - AtrFactor * atr;
		var upper = hl2 + AtrFactor * atr;

		if (_lowerBand is decimal prevLower && _prevClose is decimal pc1 && pc1 > prevLower)
			lower = Math.Max(lower, prevLower);

		if (_upperBand is decimal prevUpper && _prevClose is decimal pc2 && pc2 < prevUpper)
			upper = Math.Min(upper, prevUpper);

		if (_trend < 0 && _upperBand is decimal lastUpper && close > lastUpper)
			_trend = 1;
		else if (_trend > 0 && _lowerBand is decimal lastLower && close < lastLower)
			_trend = -1;

		_lowerBand = lower;
		_upperBand = upper;
		_prevClose = close;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var greenCandle = haClose > haOpen && haLow >= haOpen;
		var redCandle = haClose < haOpen && haHigh <= haOpen;

		var longSignal = greenCandle && (!UseSupertrend || _trend > 0);
		var shortSignal = redCandle && (!UseSupertrend || _trend < 0);

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return;
			}

			if (redCandle)
			{
				if (shortSignal)
					Enter(false, close, atr);
				else
					SellMarket(Position);

				return;
			}

			if (UseBreakEven && candle.HighPrice - _entryPrice >= BreakEvenAtrMultiplier * _entryAtr && (_stopPrice is not decimal s || s < _entryPrice))
				_stopPrice = _entryPrice;
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				return;
			}

			if (greenCandle)
			{
				if (longSignal)
					Enter(true, close, atr);
				else
					BuyMarket(-Position);

				return;
			}

			if (UseBreakEven && _entryPrice - candle.LowPrice >= BreakEvenAtrMultiplier * _entryAtr && (_stopPrice is not decimal s || s > _entryPrice))
				_stopPrice = _entryPrice;
		}
		else if (longSignal)
		{
			Enter(true, close, atr);
		}
		else if (shortSignal)
		{
			Enter(false, close, atr);
		}
	}
}
