using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades pullbacks using the Center of Gravity oscillator on two timeframes.
/// </summary>
public class CgOscillatorX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<bool> _buyCloseSignal;
	private readonly StrategyParam<bool> _sellCloseSignal;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _signalCooldownBars;

	private CenterOfGravityOscillator _trendIndicator;
	private CenterOfGravityOscillator _signalIndicator;

	private int _trendDirection;
	private decimal? _trendPrevCg;
	private decimal? _signalPrevCg;
	private decimal? _signalPrevPrevCg;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private int _cooldownRemaining;

	public DataType TrendCandleType { get => _trendCandleType.Value; set => _trendCandleType.Value = value; }
	public DataType SignalCandleType { get => _signalCandleType.Value; set => _signalCandleType.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public bool BuyCloseSignal { get => _buyCloseSignal.Value; set => _buyCloseSignal.Value = value; }
	public bool SellCloseSignal { get => _sellCloseSignal.Value; set => _sellCloseSignal.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public CgOscillatorX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Trend Candle Type", "Higher timeframe for trend detection", "General");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Signal Candle Type", "Lower timeframe for trade execution", "General");

		_trendLength = Param(nameof(TrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "CG length on the trend timeframe", "Indicator");

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "CG length on the signal timeframe", "Indicator");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entries", "Enable long entries during uptrend", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entries", "Enable short entries during downtrend", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long On Trend Flip", "Exit long positions when higher trend turns bearish", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short On Trend Flip", "Exit short positions when higher trend turns bullish", "Trading");

		_buyCloseSignal = Param(nameof(BuyCloseSignal), false)
			.SetDisplay("Close Long On Pullback", "Exit long positions when the oscillator confirms a bearish hook", "Trading");

		_sellCloseSignal = Param(nameof(SellCloseSignal), false)
			.SetDisplay("Close Short On Pullback", "Exit short positions when the oscillator confirms a bullish hook", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Distance", "Absolute stop-loss distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit Distance", "Absolute take-profit distance in price units", "Risk");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 6)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed signal candles to wait before a new entry", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TrendCandleType);

		if (!TrendCandleType.Equals(SignalCandleType))
			yield return (Security, SignalCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trendIndicator = new CenterOfGravityOscillator
		{
			Length = TrendLength
		};

		_signalIndicator = new CenterOfGravityOscillator
		{
			Length = SignalLength
		};

		SubscribeCandles(TrendCandleType)
			.BindEx(_trendIndicator, ProcessTrend)
			.Start();

		SubscribeCandles(SignalCandleType)
			.BindEx(_signalIndicator, ProcessSignal)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendDirection = 0;
		_trendPrevCg = null;
		_signalPrevCg = null;
		_signalPrevPrevCg = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_cooldownRemaining = 0;
	}

	private void ProcessTrend(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_trendIndicator.IsFormed)
			return;

		var cgValue = value.GetValue<decimal>();
		var prevCg = _trendPrevCg;
		_trendPrevCg = cgValue;

		if (cgValue > 0)
			_trendDirection = 1;
		else if (cgValue < 0)
			_trendDirection = -1;
		else
			_trendDirection = 0;
	}

	private void ProcessSignal(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_signalIndicator.IsFormed)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var cgValue = value.GetValue<decimal>();

		var prevCg = _signalPrevCg;
		var prevPrevCg = _signalPrevPrevCg;

		_signalPrevPrevCg = _signalPrevCg;
		_signalPrevCg = cgValue;

		if (prevCg is null)
			return;

		if (TryCloseByRisk(candle))
			return;

		var closeBuy = BuyCloseSignal && prevCg < 0;
		var closeSell = SellCloseSignal && prevCg > 0;
		var openBuy = false;
		var openSell = false;
		var bullishHook = prevPrevCg.HasValue && prevPrevCg.Value >= prevCg && cgValue > prevCg;
		var bearishHook = prevPrevCg.HasValue && prevPrevCg.Value <= prevCg && cgValue < prevCg;

		if (_trendDirection < 0)
		{
			if (BuyClose)
				closeBuy = true;

			if (_cooldownRemaining == 0 && SellOpen && bearishHook)
				openSell = true;
		}
		else if (_trendDirection > 0)
		{
			if (SellClose)
				closeSell = true;

			if (_cooldownRemaining == 0 && BuyOpen && bullishHook)
				openBuy = true;
		}

		if (closeBuy && Position > 0)
		{
			SellMarket(Position);
			ResetRiskTargets();
		}

		if (closeSell && Position < 0)
		{
			BuyMarket(-Position);
			ResetRiskTargets();
		}

		if (openBuy && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
			SetRiskTargets(candle.ClosePrice, true);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (openSell && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
			SellMarket(volume);
			SetRiskTargets(candle.ClosePrice, false);
			_cooldownRemaining = SignalCooldownBars;
		}
	}

	private bool TryCloseByRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetRiskTargets();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetRiskTargets();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetRiskTargets();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetRiskTargets();
				return true;
			}
		}

		return false;
	}

	private void SetRiskTargets(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (StopLoss > 0m)
			_stopPrice = isLong ? entryPrice - StopLoss : entryPrice + StopLoss;
		else
			_stopPrice = null;

		if (TakeProfit > 0m)
			_takePrice = isLong ? entryPrice + TakeProfit : entryPrice - TakeProfit;
		else
			_takePrice = null;
	}

	private void ResetRiskTargets()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}
}
