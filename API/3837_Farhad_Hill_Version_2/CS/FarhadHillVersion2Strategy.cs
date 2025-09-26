using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Farhad Hill Version 2 expert advisor.
/// Combines MACD, Stochastic, Parabolic SAR, Momentum and optional moving averages
/// to filter entries. Includes money management, configurable stop-loss, take-profit,
/// and multiple trailing stop behaviours.
/// </summary>
public class FarhadHillVersion2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _accountIsMini;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _tradeSizePercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _maxLots;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStopType;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _firstMovePips;
	private readonly StrategyParam<decimal> _trailingStop1;
	private readonly StrategyParam<decimal> _secondMovePips;
	private readonly StrategyParam<decimal> _trailingStop2;
	private readonly StrategyParam<decimal> _thirdMovePips;
	private readonly StrategyParam<decimal> _trailingStop3;
	private readonly StrategyParam<bool> _useMacd;
	private readonly StrategyParam<bool> _useStochasticLevel;
	private readonly StrategyParam<bool> _useStochasticCross;
	private readonly StrategyParam<bool> _useParabolicSar;
	private readonly StrategyParam<bool> _useMomentum;
	private readonly StrategyParam<bool> _useMovingAverageCross;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticHigh;
	private readonly StrategyParam<decimal> _stochasticLow;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumHigh;
	private readonly StrategyParam<decimal> _momentumLow;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<MovingAverageMode> _maMode;
	private readonly StrategyParam<AppliedPriceMode> _maPrice;

	private LengthIndicator<decimal> _fastMa;
	private LengthIndicator<decimal> _slowMa;
	private LinearRegression? _fastLsma;
	private LinearRegression? _slowLsma;
	private StochasticOscillator _stochastic = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ParabolicSar _parabolicSar = null!;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private decimal? _prevFastMaValue;
	private decimal? _prevSlowMaValue;
	private decimal? _prevStochK;
	private decimal? _prevStochD;
	private decimal? _prevSarValue;
	private decimal? _entryPrice;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private decimal _pipSize;
	private bool _firstLevelTriggered;
	private bool _secondLevelTriggered;

	private Order _stopOrder;
	private Order _takeProfitOrder;

	/// <summary>
	/// Initializes strategy parameters using defaults from the MQL implementation.
	/// </summary>
	public FarhadHillVersion2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for all indicators", "General");

		_accountIsMini = Param(nameof(AccountIsMini), false)
			.SetDisplay("Mini Account", "Treat volumes as mini lots (0.1 minimum)", "Money Management");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Enable dynamic volume sizing", "Money Management");

		_tradeSizePercent = Param(nameof(TradeSizePercent), 5m)
			.SetGreaterThanZero();
		_tradeSizePercent.SetDisplay("Risk Percent", "Percent of equity used for volume sizing", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetGreaterThanZero();
		_fixedVolume.SetDisplay("Fixed Volume", "Volume in lots when money management is disabled", "Money Management");

		_maxLots = Param(nameof(MaxLots), 100m)
			.SetGreaterThanZero();
		_maxLots.SetDisplay("Maximum Lots", "Upper limit for calculated lot size", "Money Management");

		_stopLossPips = Param(nameof(StopLossPips), 0m);
		_stopLossPips.SetDisplay("Stop Loss (pips)", "Distance of stop-loss in pips (0 disables)", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m);
		_takeProfitPips.SetDisplay("Take Profit (pips)", "Distance of take-profit in pips (0 disables)", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true);
		_useTrailingStop.SetDisplay("Use Trailing Stop", "Enable trailing stop management", "Risk");

		_trailingStopType = Param(nameof(TrailingStopType), 2)
			.SetRange(1, 3);
		_trailingStopType.SetDisplay("Trailing Type", "1=Immediate, 2=Delay, 3=Three levels", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m);
		_trailingStopPips.SetDisplay("Trailing Stop (pips)", "Distance used by trailing type 2", "Risk");

		_firstMovePips = Param(nameof(FirstMovePips), 20m);
		_firstMovePips.SetDisplay("First Move (pips)", "Trigger for first trailing level", "Risk");

		_trailingStop1 = Param(nameof(TrailingStop1), 20m);
		_trailingStop1.SetDisplay("Trailing Stop 1 (pips)", "Offset after first trigger", "Risk");

		_secondMovePips = Param(nameof(SecondMovePips), 30m);
		_secondMovePips.SetDisplay("Second Move (pips)", "Trigger for second trailing level", "Risk");

		_trailingStop2 = Param(nameof(TrailingStop2), 20m);
		_trailingStop2.SetDisplay("Trailing Stop 2 (pips)", "Offset after second trigger", "Risk");

		_thirdMovePips = Param(nameof(ThirdMovePips), 40m);
		_thirdMovePips.SetDisplay("Third Move (pips)", "Trigger for third trailing stage", "Risk");

		_trailingStop3 = Param(nameof(TrailingStop3), 20m);
		_trailingStop3.SetDisplay("Trailing Stop 3 (pips)", "Trailing distance after stage three", "Risk");

		_useMacd = Param(nameof(UseMacd), true);
		_useMacd.SetDisplay("Use MACD", "Enable MACD filter", "Indicators");

		_useStochasticLevel = Param(nameof(UseStochasticLevel), true);
		_useStochasticLevel.SetDisplay("Use Stochastic Level", "Require stochastic to be inside thresholds", "Indicators");

		_useStochasticCross = Param(nameof(UseStochasticCross), false);
		_useStochasticCross.SetDisplay("Use Stochastic Cross", "Require %K/%D bullish or bearish cross", "Indicators");

		_useParabolicSar = Param(nameof(UseParabolicSar), true);
		_useParabolicSar.SetDisplay("Use Parabolic SAR", "Enable Parabolic SAR filter", "Indicators");

		_useMomentum = Param(nameof(UseMomentum), true);
		_useMomentum.SetDisplay("Use Momentum", "Enable momentum filter", "Indicators");

		_useMovingAverageCross = Param(nameof(UseMovingAverageCross), false);
		_useMovingAverageCross.SetDisplay("Use MA Cross", "Enable optional moving average confirmation", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12);
		_macdFast.SetGreaterThanZero();
		_macdFast.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26);
		_macdSlow.SetGreaterThanZero();
		_macdSlow.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9);
		_macdSignal.SetGreaterThanZero();
		_macdSignal.SetDisplay("MACD Signal", "Signal EMA length", "Indicators");

		_stochasticK = Param(nameof(StochasticK), 5);
		_stochasticK.SetGreaterThanZero();
		_stochasticK.SetDisplay("Stochastic %K", "Fast %K period", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 3);
		_stochasticD.SetGreaterThanZero();
		_stochasticD.SetDisplay("Stochastic %D", "Signal smoothing period", "Indicators");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3);
		_stochasticSlowing.SetGreaterThanZero();
		_stochasticSlowing.SetDisplay("Stochastic Slowing", "Additional smoothing", "Indicators");

		_stochasticHigh = Param(nameof(StochasticHigh), 60m);
		_stochasticHigh.SetDisplay("Stochastic High", "Upper threshold", "Indicators");

		_stochasticLow = Param(nameof(StochasticLow), 35m);
		_stochasticLow.SetDisplay("Stochastic Low", "Lower threshold", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14);
		_momentumPeriod.SetGreaterThanZero();
		_momentumPeriod.SetDisplay("Momentum Period", "Lookback for momentum", "Indicators");

		_momentumHigh = Param(nameof(MomentumHigh), 100m);
		_momentumHigh.SetDisplay("Momentum High", "Upper momentum threshold", "Indicators");

		_momentumLow = Param(nameof(MomentumLow), 100m);
		_momentumLow.SetDisplay("Momentum Low", "Lower momentum threshold", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 21);
		_slowMaPeriod.SetGreaterThanZero();
		_slowMaPeriod.SetDisplay("Slow MA", "Slow moving average period", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 2);
		_fastMaPeriod.SetGreaterThanZero();
		_fastMaPeriod.SetDisplay("Fast MA", "Fast moving average period", "Indicators");

		_maMode = Param(nameof(MaMode), MovingAverageMode.Smoothed);
		_maMode.SetDisplay("MA Mode", "Moving average calculation", "Indicators");

		_maPrice = Param(nameof(MaPrice), AppliedPriceMode.Typical);
		_maPrice.SetDisplay("MA Price", "Applied price for moving averages", "Indicators");
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

		_fastMa = null;
		_slowMa = null;
		_fastLsma = null;
		_slowLsma = null;
		_fastMaValue = null;
		_slowMaValue = null;
		_prevFastMaValue = null;
		_prevSlowMaValue = null;
		_prevStochK = null;
		_prevStochD = null;
		_prevSarValue = null;
		_entryPrice = null;
		_currentStopPrice = null;
		_currentTakeProfitPrice = null;
		_pipSize = 0m;
		_firstLevelTriggered = false;
		_secondLevelTriggered = false;
		_stopOrder = null;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		{
			_pipSize = 0.0001m;
			LogWarning("Price step is not specified. Using 0.0001 as a fallback pip size.");
		}

		_fastMa = CreateMovingAverage(MaMode, FastMaPeriod, out _fastLsma);
		_slowMa = CreateMovingAverage(MaMode, SlowMaPeriod, out _slowLsma);

		_stochastic = new StochasticOscillator
		{
			Length = StochasticK,
			K = { Length = StochasticK },
			D = { Length = StochasticD },
			Slowing = StochasticSlowing
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod,
			CandlePrice = CandlePrice.Open
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		_parabolicSar = new ParabolicSar
		{
			Acceleration = 0.02m,
			AccelerationMax = 0.2m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, _momentum, _macd, _parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

private void ProcessCandle(
ICandleMessage candle,
IIndicatorValue stochasticValue,
IIndicatorValue momentumValue,
IIndicatorValue macdValue,
decimal sarValue)
{
// Work only with finished candles to keep logic aligned with MT4 EA behaviour.
var isFinal = candle.State == CandleStates.Finished;
var price = GetAppliedPrice(candle, MaPrice);

// Update optional moving averages and exit early if they are not ready yet.
if (!ProcessMovingAverages(candle, price, isFinal))
return;

if (!isFinal)
return;

// Avoid processing when strategy is not ready or trading is blocked.
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!stochasticValue.IsFinal || !momentumValue.IsFinal || !macdValue.IsFinal)
return;

		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
			return;

		var momentum = momentumValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdMain || macdTyped.Signal is not decimal macdSignal)
			return;

		var previousK = _prevStochK;
		var previousD = _prevStochD;
		_prevStochK = stochK;
		_prevStochD = stochD;

		var previousSar = _prevSarValue;
		_prevSarValue = sarValue;

// Manage trailing logic before evaluating new entries.
ApplyTrailing(candle);

if (Position != 0m)
return;

// Check each filter for a new long or short signal.
var buySignal = IsBuySignal(candle, macdMain, macdSignal, stochK, stochD, previousK, previousD, momentum, sarValue, previousSar);
var sellSignal = IsSellSignal(candle, macdMain, macdSignal, stochK, stochD, previousK, previousD, momentum, sarValue, previousSar);

if (!buySignal && !sellSignal)
return;

// Compute tradable volume using configured money-management mode.
var volume = NormalizeVolume(GetTradeVolume());
if (volume <= 0m)
return;

		if (buySignal)
		{
			LogInfo("Opening long position after bullish filter alignment.");
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			LogInfo("Opening short position after bearish filter alignment.");
			SellMarket(volume);
		}
	}

private bool ProcessMovingAverages(ICandleMessage candle, decimal price, bool isFinal)
{
// The EA allows switching off the moving-average confirmation entirely.
if (!UseMovingAverageCross)
return true;

if (!TryProcessIndicator(_fastMa, _fastLsma, price, candle, isFinal, out var fastValue))
return false;

		if (!TryProcessIndicator(_slowMa, _slowLsma, price, candle, isFinal, out var slowValue))
			return false;

		if (isFinal)
		{
			_prevFastMaValue = _fastMaValue;
			_prevSlowMaValue = _slowMaValue;
			_fastMaValue = fastValue;
			_slowMaValue = slowValue;
		}

		return true;
	}

private bool TryProcessIndicator(LengthIndicator<decimal> ma, LinearRegression? lsma, decimal price, ICandleMessage candle, bool isFinal, out decimal value)
{
value = 0m;
IIndicatorValue? result = null;

// Linear regression acts as the "Least Squares Moving Average" option in MT4.
if (lsma != null)
{
result = lsma.Process(price, candle.OpenTime, isFinal);
if (!lsma.IsFormed)
return false;
}
else if (ma != null)
{
result = ma.Process(price, candle.OpenTime, isFinal);
if (!ma.IsFormed)
return false;
}
else
{
// When the user disables moving averages the method simply succeeds.
return true;
}

		var converted = result?.ToNullableDecimal();
		if (converted is not decimal decimalValue)
			return false;

		value = decimalValue;
		return true;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceMode mode)
	{
		return mode switch
		{
			AppliedPriceMode.Close => candle.ClosePrice,
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

private bool IsBuySignal(
ICandleMessage candle,
decimal macdMain,
decimal macdSignal,
decimal stochK,
		decimal stochD,
		decimal? prevK,
		decimal? prevD,
		decimal momentum,
decimal sarValue,
decimal? prevSar)
{
// MACD main line must be below the signal line for a long setup.
if (UseMacd && macdMain >= macdSignal)
return false;

if (UseStochasticLevel && stochK >= StochasticLow)
return false;

if (UseStochasticCross)
{
// Require a fresh bullish cross where %K rises above %D.
if (prevK is null || prevD is null)
return false;

if (stochK <= stochD)
return false;

			if (prevK >= prevD)
				return false;
		}

if (UseParabolicSar)
{
// Parabolic SAR must flip below price with a downward step.
if (sarValue > candle.ClosePrice)
return false;

if (prevSar is null || prevSar <= sarValue)
return false;
}

if (UseMomentum && momentum >= MomentumLow)
return false;

if (UseMovingAverageCross)
{
// Confirm that the fast moving average is above the slow average.
if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow)
return false;

if (fast <= slow)
return false;
}

return true;
}

private bool IsSellSignal(
ICandleMessage candle,
decimal macdMain,
		decimal macdSignal,
		decimal stochK,
		decimal stochD,
		decimal? prevK,
		decimal? prevD,
		decimal momentum,
decimal sarValue,
decimal? prevSar)
{
// For shorts the MACD main line must stay above the signal line.
if (UseMacd && macdMain <= macdSignal)
return false;

if (UseStochasticLevel && stochK <= StochasticHigh)
return false;

if (UseStochasticCross)
{
// Require a new bearish cross where %K falls below %D.
if (prevK is null || prevD is null)
return false;

if (stochK >= stochD)
return false;

			if (prevK <= prevD)
				return false;
		}

if (UseParabolicSar)
{
// SAR should sit above price while stepping upward.
if (sarValue < candle.ClosePrice)
return false;

if (prevSar is null || prevSar >= sarValue)
return false;
}

if (UseMomentum && momentum <= MomentumHigh)
return false;

if (UseMovingAverageCross)
{
// Confirm that the fast moving average stays below the slow line.
if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow)
return false;

if (fast >= slow)
return false;
}

return true;
}

private decimal GetTradeVolume()
{
// Use fixed lot size when money management is disabled.
if (!UseMoneyManagement)
{
var lot = FixedVolume;
return AccountIsMini ? AdjustMiniVolume(lot) : lot;
}

// Portfolio equity mirrors FreeMargin behaviour from the original EA.
var equity = Portfolio?.CurrentValue ?? 0m;
if (equity <= 0m)
return AccountIsMini ? AdjustMiniVolume(FixedVolume) : FixedVolume;

// Recreate the lot calculation used in the MQL version.
var raw = Math.Floor(equity * TradeSizePercent / 10000m * 10m) / 10m;
if (AccountIsMini)
{
raw = Math.Floor(raw * 10m) / 10m;
			raw = Math.Clamp(raw, 0.1m, MaxLots);
		}
		else
		{
			raw = Math.Clamp(raw, 1m, MaxLots);
		}

		return raw;
	}

private decimal AdjustMiniVolume(decimal lot)
{
// Convert standard lots to mini lots and clamp to allowed range.
if (lot > 1m)
lot /= 10m;

if (lot < 0.1m)
lot = 0.1m;

		return Math.Min(lot, MaxLots);
	}

private void ApplyTrailing(ICandleMessage candle)
{
if (!UseTrailingStop)
return;

if (Position == 0m)
{
// Reset staged flags after position is closed.
_firstLevelTriggered = false;
_secondLevelTriggered = false;
return;
}

if (_entryPrice is null)
return;

var volume = Math.Abs(Position);
if (volume <= 0m)
return;

var isLong = Position > 0m;
switch (TrailingStopType)
{
case 1:
// Type 1: classic stop that hugs price with a fixed distance.
UpdateTrailingType1(isLong, candle.ClosePrice, volume);
break;
case 2:
// Type 2: wait for price to move by configured distance, then trail.
UpdateTrailingType2(isLong, candle.ClosePrice, volume);
break;
case 3:
// Type 3: emulate multi-level break-even and trailing sequence.
UpdateTrailingType3(isLong, candle.ClosePrice, volume);
break;
}
}

	private void UpdateTrailingType1(bool isLong, decimal closePrice, decimal volume)
	{
		var distance = StopLossPips * _pipSize;
		if (distance <= 0m || _currentStopPrice is null)
			return;

		if (isLong)
		{
			var candidate = closePrice - distance;
			if (candidate <= _currentStopPrice)
				return;

			if (closePrice - _currentStopPrice <= distance)
				return;

			UpdateStopOrder(true, candidate, volume);
		}
		else
		{
			var candidate = closePrice + distance;
			if (candidate >= _currentStopPrice)
				return;

			if (_currentStopPrice - closePrice <= distance)
				return;

			UpdateStopOrder(false, candidate, volume);
		}
	}

	private void UpdateTrailingType2(bool isLong, decimal closePrice, decimal volume)
	{
		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
			return;

		var entry = _entryPrice ?? 0m;
		if (isLong)
		{
			if (closePrice - entry <= distance)
				return;

			var candidate = closePrice - distance;
			if (_currentStopPrice is not null && candidate <= _currentStopPrice)
				return;

			UpdateStopOrder(true, candidate, volume);
		}
		else
		{
			if (entry - closePrice <= distance)
				return;

			var candidate = closePrice + distance;
			if (_currentStopPrice is not null && candidate >= _currentStopPrice)
				return;

			UpdateStopOrder(false, candidate, volume);
		}
	}

	private void UpdateTrailingType3(bool isLong, decimal closePrice, decimal volume)
	{
		var entry = _entryPrice ?? 0m;
		var firstTrigger = FirstMovePips * _pipSize;
		var secondTrigger = SecondMovePips * _pipSize;
		var thirdTrigger = ThirdMovePips * _pipSize;

		if (isLong)
		{
			if (closePrice - entry > firstTrigger)
			{
				var target = entry + firstTrigger - TrailingStop1 * _pipSize;
				if (_currentStopPrice is null || target > _currentStopPrice)
				{
					UpdateStopOrder(true, target, volume);
					_firstLevelTriggered = true;
				}
			}

			if (closePrice - entry > secondTrigger)
			{
				var target = entry + secondTrigger - TrailingStop2 * _pipSize;
				if (_currentStopPrice is null || target > _currentStopPrice)
				{
					UpdateStopOrder(true, target, volume);
					_secondLevelTriggered = true;
				}
			}

			if (closePrice - entry > thirdTrigger)
			{
				var target = closePrice - TrailingStop3 * _pipSize;
				if (_currentStopPrice is null || target > _currentStopPrice)
					UpdateStopOrder(true, target, volume);
			}
		}
		else
		{
			if (entry - closePrice > firstTrigger)
			{
				var target = entry - firstTrigger + TrailingStop1 * _pipSize;
				if (_currentStopPrice is null || target < _currentStopPrice)
				{
					UpdateStopOrder(false, target, volume);
					_firstLevelTriggered = true;
				}
			}

			if (entry - closePrice > secondTrigger)
			{
				var target = entry - secondTrigger + TrailingStop2 * _pipSize;
				if (_currentStopPrice is null || target < _currentStopPrice)
				{
					UpdateStopOrder(false, target, volume);
					_secondLevelTriggered = true;
				}
			}

			if (entry - closePrice > thirdTrigger)
			{
				var target = closePrice + TrailingStop3 * _pipSize;
				if (_currentStopPrice is null || target < _currentStopPrice)
					UpdateStopOrder(false, target, volume);
			}
		}
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		var normalizedVolume = NormalizeVolume(volume);
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active && _stopOrder.Price == stopPrice && _stopOrder.Volume == normalizedVolume)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = isLong
			? SellStop(price: stopPrice, volume: normalizedVolume)
			: BuyStop(price: stopPrice, volume: normalizedVolume);

		_currentStopPrice = stopPrice;
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takePrice, decimal volume)
	{
		var normalizedVolume = NormalizeVolume(volume);
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active && _takeProfitOrder.Price == takePrice && _takeProfitOrder.Volume == normalizedVolume)
			return;

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = isLong
			? SellLimit(price: takePrice, volume: normalizedVolume)
			: BuyLimit(price: takePrice, volume: normalizedVolume);

		_currentTakeProfitPrice = takePrice;
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private void ResetTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}

	private void ResetProtectionOrders()
	{
		ResetStopOrder();
		ResetTakeProfitOrder();
	}

	private void InitializeProtection(bool isLong, decimal price, decimal volume)
	{
		_firstLevelTriggered = false;
		_secondLevelTriggered = false;

		if (StopLossPips > 0m)
		{
			var stop = isLong ? price - StopLossPips * _pipSize : price + StopLossPips * _pipSize;
			UpdateStopOrder(isLong, stop, volume);
		}
		else
		{
			ResetStopOrder();
		}

		if (TakeProfitPips > 0m)
		{
			var target = isLong ? price + TakeProfitPips * _pipSize : price - TakeProfitPips * _pipSize;
			UpdateTakeProfitOrder(isLong, target, volume);
		}
		else
		{
			ResetTakeProfitOrder();
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_entryPrice = PositionPrice;

		var volume = Math.Abs(Position);
		if (volume > 0m)
			InitializeProtection(Position > 0m, PositionPrice, volume);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			ResetProtectionOrders();
		}
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMode mode, int length, out LinearRegression? lsma)
	{
		lsma = null;
		return mode switch
		{
			MovingAverageMode.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMode.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMode.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMode.LinearWeighted => new WeightedMovingAverage { Length = length },
			MovingAverageMode.LeastSquares => lsma = new LinearRegression { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private DataType CandleType => _candleType.Value;
	private bool AccountIsMini => _accountIsMini.Value;
	private bool UseMoneyManagement => _useMoneyManagement.Value;
	private decimal TradeSizePercent => _tradeSizePercent.Value;
	private decimal FixedVolume => _fixedVolume.Value;
	private decimal MaxLots => _maxLots.Value;
	private decimal StopLossPips => _stopLossPips.Value;
	private decimal TakeProfitPips => _takeProfitPips.Value;
	private bool UseTrailingStop => _useTrailingStop.Value;
	private int TrailingStopType => _trailingStopType.Value;
	private decimal TrailingStopPips => _trailingStopPips.Value;
	private decimal FirstMovePips => _firstMovePips.Value;
	private decimal TrailingStop1 => _trailingStop1.Value;
	private decimal SecondMovePips => _secondMovePips.Value;
	private decimal TrailingStop2 => _trailingStop2.Value;
	private decimal ThirdMovePips => _thirdMovePips.Value;
	private decimal TrailingStop3 => _trailingStop3.Value;
	private bool UseMacd => _useMacd.Value;
	private bool UseStochasticLevel => _useStochasticLevel.Value;
	private bool UseStochasticCross => _useStochasticCross.Value;
	private bool UseParabolicSar => _useParabolicSar.Value;
	private bool UseMomentum => _useMomentum.Value;
	private bool UseMovingAverageCross => _useMovingAverageCross.Value;
	private int MacdFast => _macdFast.Value;
	private int MacdSlow => _macdSlow.Value;
	private int MacdSignal => _macdSignal.Value;
	private int StochasticK => _stochasticK.Value;
	private int StochasticD => _stochasticD.Value;
	private int StochasticSlowing => _stochasticSlowing.Value;
	private decimal StochasticHigh => _stochasticHigh.Value;
	private decimal StochasticLow => _stochasticLow.Value;
	private int MomentumPeriod => _momentumPeriod.Value;
	private decimal MomentumHigh => _momentumHigh.Value;
	private decimal MomentumLow => _momentumLow.Value;
	private int SlowMaPeriod => _slowMaPeriod.Value;
	private int FastMaPeriod => _fastMaPeriod.Value;
	private MovingAverageMode MaMode => _maMode.Value;
	private AppliedPriceMode MaPrice => _maPrice.Value;

	/// <summary>
	/// Moving average modes compatible with the original EA options.
	/// </summary>
	public enum MovingAverageMode
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3,
		LeastSquares = 4
	}

	/// <summary>
	/// Applied price selection for moving averages.
	/// </summary>
	public enum AppliedPriceMode
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}
}
