using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic and RSI based strategy converted from "The Puncher".
/// Buys when both oscillators confirm oversold conditions and sells when they confirm overbought conditions.
/// Includes configurable stop-loss, take-profit, break-even and trailing stop logic.
/// </summary>
public class PuncherStrategy : Strategy
{
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticSignalPeriod;
	private readonly StrategyParam<int> _stochasticSmoothingPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _breakEvenActivated;
	private decimal? _lastTrailingPrice;

	/// <summary>
	/// Period of the Stochastic oscillator base calculation.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Period used to smooth the %K line (signal).
	/// </summary>
	public int StochasticSignalPeriod
	{
		get => _stochasticSignalPeriod.Value;
		set => _stochasticSignalPeriod.Value = value;
	}

	/// <summary>
	/// Period used to smooth the %D line.
	/// </summary>
	public int StochasticSmoothingPeriod
	{
		get => _stochasticSmoothingPeriod.Value;
		set => _stochasticSmoothingPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold shared by Stochastic and RSI.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold shared by Stochastic and RSI.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips (0 disables the stop-loss).
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips (0 disables the take-profit).
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips (0 disables the trailing stop).
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price improvement in pips before moving the trailing stop again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Profit in pips required to move the stop to break-even (0 disables the feature).
	/// </summary>
	public int BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public PuncherStrategy()
	{
		_stochasticPeriod = Param(nameof(StochasticPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Base period for the Stochastic oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_stochasticSignalPeriod = Param(nameof(StochasticSignalPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Signal", "Smoothing period for the %K line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochasticSmoothingPeriod = Param(nameof(StochasticSmoothingPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smoothing", "Smoothing period for the %D line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Threshold for oversold detection", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Threshold for overbought detection", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 60, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Distance of the profit target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 120, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 40, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Minimum improvement before trailing stop updates", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 20, 2);

		_breakEvenPips = Param(nameof(BreakEvenPips), 21)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-Even (pips)", "Profit needed to move the stop to entry", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 40, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
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

		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
		_lastTrailingPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = StochasticPeriod,
		};
		stochastic.K.Length = StochasticSignalPeriod;
		stochastic.D.Length = StochasticSmoothingPeriod;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal || !rsiValue.IsFinal)
			return;

		var stochData = (StochasticOscillatorValue)stochasticValue;
		if (stochData.D is not decimal signalValue)
			return;

		var rsi = rsiValue.GetValue<decimal>();

		if (ManagePosition(candle))
			return;

		var isBuySignal = signalValue < OversoldLevel && rsi < OversoldLevel;
		var isSellSignal = signalValue > OverboughtLevel && rsi > OverboughtLevel;

		if (Position > 0 && isSellSignal)
		{
			CloseLong();
			return;
		}

		if (Position < 0 && isBuySignal)
		{
			CloseShort();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (isBuySignal && Position <= 0)
		{
			EnterLong(candle);
			return;
		}

		if (isSellSignal && Position >= 0)
		{
			EnterShort(candle);
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			return HandleLongPosition(candle);
		}

		if (Position < 0)
		{
			return HandleShortPosition(candle);
		}

		if (_stopPrice.HasValue || _takeProfitPrice.HasValue || _entryPrice != 0m)
		{
			ResetProtectionState();
		}

		return false;
	}

	private bool HandleLongPosition(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			_entryPrice = candle.ClosePrice;

		var priceStep = GetPriceStep();

		if (BreakEvenPips > 0 && !_breakEvenActivated)
		{
			var breakEvenPrice = _entryPrice + GetPipValue(BreakEvenPips, priceStep);
			if (candle.HighPrice >= breakEvenPrice)
			{
				if (_stopPrice is null || _stopPrice < _entryPrice)
				{
					_stopPrice = _entryPrice;
					_breakEvenActivated = true;
				}
			}
		}

		if (TrailingStopPips > 0)
		{
			var trailingDistance = GetPipValue(TrailingStopPips, priceStep);
			var trailingStep = TrailingStepPips > 0 ? GetPipValue(TrailingStepPips, priceStep) : 0m;
			_lastTrailingPrice ??= _entryPrice;

			if (candle.HighPrice >= _entryPrice + trailingDistance)
			{
				var referencePrice = _lastTrailingPrice.Value;
				var shouldUpdate = referencePrice == _entryPrice || trailingStep == 0m || candle.HighPrice - referencePrice >= trailingStep;
				if (shouldUpdate)
				{
					var newStop = candle.HighPrice - trailingDistance;
					if (_stopPrice is null || newStop > _stopPrice)
						_stopPrice = newStop;
					_lastTrailingPrice = candle.HighPrice;
				}
			}
		}

		if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
		{
			CloseLong();
			return true;
		}

		if (_stopPrice is decimal sl && candle.LowPrice <= sl)
		{
			CloseLong();
			return true;
		}

		return false;
	}

	private bool HandleShortPosition(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			_entryPrice = candle.ClosePrice;

		var priceStep = GetPriceStep();

		if (BreakEvenPips > 0 && !_breakEvenActivated)
		{
			var breakEvenPrice = _entryPrice - GetPipValue(BreakEvenPips, priceStep);
			if (candle.LowPrice <= breakEvenPrice)
			{
				if (_stopPrice is null || _stopPrice > _entryPrice)
				{
					_stopPrice = _entryPrice;
					_breakEvenActivated = true;
				}
			}
		}

		if (TrailingStopPips > 0)
		{
			var trailingDistance = GetPipValue(TrailingStopPips, priceStep);
			var trailingStep = TrailingStepPips > 0 ? GetPipValue(TrailingStepPips, priceStep) : 0m;
			_lastTrailingPrice ??= _entryPrice;

			if (candle.LowPrice <= _entryPrice - trailingDistance)
			{
				var referencePrice = _lastTrailingPrice.Value;
				var shouldUpdate = referencePrice == _entryPrice || trailingStep == 0m || referencePrice - candle.LowPrice >= trailingStep;
				if (shouldUpdate)
				{
					var newStop = candle.LowPrice + trailingDistance;
					if (_stopPrice is null || newStop < _stopPrice)
						_stopPrice = newStop;
					_lastTrailingPrice = candle.LowPrice;
				}
			}
		}

		if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
		{
			CloseShort();
			return true;
		}

		if (_stopPrice is decimal sl && candle.HighPrice >= sl)
		{
			CloseShort();
			return true;
		}

		return false;
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume + (Position < 0 ? -Position : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		InitializeProtection(isLong: true);
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume + (Position > 0 ? Position : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		InitializeProtection(isLong: false);
	}

	private void CloseLong()
	{
		if (Position > 0)
			SellMarket(Position);
		ResetProtectionState();
	}

	private void CloseShort()
	{
		if (Position < 0)
			BuyMarket(-Position);
		ResetProtectionState();
	}

	private void InitializeProtection(bool isLong)
	{
		var priceStep = GetPriceStep();
		var stopOffset = StopLossPips > 0 ? GetPipValue(StopLossPips, priceStep) : (decimal?)null;
		var takeOffset = TakeProfitPips > 0 ? GetPipValue(TakeProfitPips, priceStep) : (decimal?)null;

		_stopPrice = isLong
			? (stopOffset.HasValue ? _entryPrice - stopOffset.Value : null)
			: (stopOffset.HasValue ? _entryPrice + stopOffset.Value : null);

		_takeProfitPrice = isLong
			? (takeOffset.HasValue ? _entryPrice + takeOffset.Value : null)
			: (takeOffset.HasValue ? _entryPrice - takeOffset.Value : null);

		_breakEvenActivated = false;
		_lastTrailingPrice = _entryPrice;
	}

	private void ResetProtectionState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
		_lastTrailingPrice = null;
	}

	private static decimal GetPipValue(int pips, decimal priceStep)
	{
		return priceStep * pips;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 1m;
	}
}
