using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy inspired by the Tipu Expert Advisor.
/// Aligns multi-timeframe momentum signals and adds a risk-free pyramiding module.
/// </summary>
public class TipuEaStrategy : Strategy
{
	private readonly StrategyParam<bool> _allowHedging;
	private readonly StrategyParam<bool> _closeOnReverseSignal;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _maxRiskPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _enableRiskFreePyramiding;
	private readonly StrategyParam<decimal> _riskFreeStepPips;
	private readonly StrategyParam<decimal> _pyramidIncrementVolume;
	private readonly StrategyParam<decimal> _pyramidMaxVolume;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingCushionPips;
	private readonly StrategyParam<int> _higherFastLength;
	private readonly StrategyParam<int> _higherSlowLength;
	private readonly StrategyParam<int> _lowerFastLength;
	private readonly StrategyParam<int> _lowerSlowLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _higherSignalWindowMinutes;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;

	private EMA _higherFast = null!;
	private EMA _higherSlow = null!;
	private EMA _lowerFast = null!;
	private EMA _lowerSlow = null!;
	private AverageDirectionalIndex _higherAdx = null!;
	private AverageTrueRange _lowerAtr = null!;

	private bool _higherInitialized;
	private bool _lowerInitialized;
	private decimal _higherPrevFast;
	private decimal _higherPrevSlow;
	private decimal _lowerPrevFast;
	private decimal _lowerPrevSlow;
	private int _higherTrendDirection;
	private int _lastHigherSignalDirection;
	private DateTimeOffset _lastHigherSignalTime;
	private bool _isHigherRange;
	private decimal _lastAtrValue;
	private decimal _averageEntryPrice;
	private decimal _currentStopPrice;
	private decimal _currentTargetPrice;
	private bool _riskFreeActivated;
	private decimal _positionVolume;
	private decimal _nextLongPyramidPrice;
	private decimal _nextShortPyramidPrice;

	public bool AllowHedging
	{
		get => _allowHedging.Value;
		set => _allowHedging.Value = value;
	}

	public bool CloseOnReverseSignal
	{
		get => _closeOnReverseSignal.Value;
		set => _closeOnReverseSignal.Value = value;
	}

	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal MaxRiskPips
	{
		get => _maxRiskPips.Value;
		set => _maxRiskPips.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public bool EnableRiskFreePyramiding
	{
		get => _enableRiskFreePyramiding.Value;
		set => _enableRiskFreePyramiding.Value = value;
	}

	public decimal RiskFreeStepPips
	{
		get => _riskFreeStepPips.Value;
		set => _riskFreeStepPips.Value = value;
	}

	public decimal PyramidIncrementVolume
	{
		get => _pyramidIncrementVolume.Value;
		set => _pyramidIncrementVolume.Value = value;
	}

	public decimal PyramidMaxVolume
	{
		get => _pyramidMaxVolume.Value;
		set => _pyramidMaxVolume.Value = value;
	}

	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	public decimal TrailingCushionPips
	{
		get => _trailingCushionPips.Value;
		set => _trailingCushionPips.Value = value;
	}

	public int HigherFastLength
	{
		get => _higherFastLength.Value;
		set => _higherFastLength.Value = value;
	}

	public int HigherSlowLength
	{
		get => _higherSlowLength.Value;
		set => _higherSlowLength.Value = value;
	}

	public int LowerFastLength
	{
		get => _lowerFastLength.Value;
		set => _lowerFastLength.Value = value;
	}

	public int LowerSlowLength
	{
		get => _lowerSlowLength.Value;
		set => _lowerSlowLength.Value = value;
	}

	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int HigherSignalWindowMinutes
	{
		get => _higherSignalWindowMinutes.Value;
		set => _higherSignalWindowMinutes.Value = value;
	}

	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	public DataType LowerCandleType
	{
		get => _lowerCandleType.Value;
		set => _lowerCandleType.Value = value;
	}

	public TipuEaStrategy()
	{
		_allowHedging = Param(nameof(AllowHedging), false)
			.SetDisplay("Allow Hedging", "Allow adding trades without closing opposite direction", "Risk");

		_closeOnReverseSignal = Param(nameof(CloseOnReverseSignal), true)
			.SetDisplay("Close On Reverse", "Close the active position when the opposite signal appears", "Risk");

		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
			.SetDisplay("Enable Take Profit", "Enable fixed take profit target", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_maxRiskPips = Param(nameof(MaxRiskPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Max Risk (pips)", "Maximum stop distance allowed in pips", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume", "General");

		_enableRiskFreePyramiding = Param(nameof(EnableRiskFreePyramiding), true)
			.SetDisplay("Enable Risk Free", "Allow risk-free pyramiding of winners", "Risk");

		_riskFreeStepPips = Param(nameof(RiskFreeStepPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Free Step (pips)", "Profit distance required before locking and adding", "Risk");

		_pyramidIncrementVolume = Param(nameof(PyramidIncrementVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Pyramid Increment", "Additional volume added on each pyramid step", "Risk");

		_pyramidMaxVolume = Param(nameof(PyramidMaxVolume), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Pyramid Max Volume", "Maximum accumulated position volume", "Risk");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Enable Trailing", "Enable trailing stop once trade is in profit", "Risk");

		_trailingStartPips = Param(nameof(TrailingStartPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start (pips)", "Profit in pips required before trailing", "Risk");

		_trailingCushionPips = Param(nameof(TrailingCushionPips), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Cushion (pips)", "Distance between price and trailing stop", "Risk");

		_higherFastLength = Param(nameof(HigherFastLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Higher Fast EMA", "Fast EMA length on higher timeframe", "Signals");

		_higherSlowLength = Param(nameof(HigherSlowLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("Higher Slow EMA", "Slow EMA length on higher timeframe", "Signals");

		_lowerFastLength = Param(nameof(LowerFastLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Lower Fast EMA", "Fast EMA length on signal timeframe", "Signals");

		_lowerSlowLength = Param(nameof(LowerSlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Lower Slow EMA", "Slow EMA length on signal timeframe", "Signals");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period for range detection", "Signals");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Below this ADX value the market is treated as ranging", "Signals");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for initial stop calculation", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for the initial stop", "Risk");

		_higherSignalWindowMinutes = Param(nameof(HigherSignalWindowMinutes), 120)
			.SetGreaterThanZero()
			.SetDisplay("Higher Signal Window", "Minutes within which the higher timeframe signal must be recent", "Signals");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher Timeframe", "Higher timeframe candles used for context", "General");

		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Timeframe", "Primary timeframe used for entries", "General");

		Volume = TradeVolume;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, LowerCandleType), (Security, HigherCandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_higherInitialized = false;
		_lowerInitialized = false;
		_higherPrevFast = 0m;
		_higherPrevSlow = 0m;
		_lowerPrevFast = 0m;
		_lowerPrevSlow = 0m;
		_higherTrendDirection = 0;
		_lastHigherSignalDirection = 0;
		_lastHigherSignalTime = default;
		_isHigherRange = false;
		_lastAtrValue = 0m;
		_averageEntryPrice = 0m;
		_currentStopPrice = 0m;
		_currentTargetPrice = 0m;
		_riskFreeActivated = false;
		_positionVolume = 0m;
		_nextLongPyramidPrice = 0m;
		_nextShortPyramidPrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_higherFast = new EMA { Length = HigherFastLength };
		_higherSlow = new EMA { Length = HigherSlowLength };
		_lowerFast = new EMA { Length = LowerFastLength };
		_lowerSlow = new EMA { Length = LowerSlowLength };
		_higherAdx = new AverageDirectionalIndex { Length = AdxLength };
		_lowerAtr = new AverageTrueRange { Length = AtrLength };

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
			.BindEx(_higherFast, _higherSlow, _higherAdx, ProcessHigherCandle)
			.Start();

		var lowerSubscription = SubscribeCandles(LowerCandleType);
		lowerSubscription
			.BindEx(_lowerFast, _lowerSlow, _lowerAtr, ProcessLowerCandle)
			.Start();
	}

	private void ProcessHigherCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (fastValue is not DecimalIndicatorValue { IsFinal: true, Value: var fast })
		return;

		if (slowValue is not DecimalIndicatorValue { IsFinal: true, Value: var slow })
		return;

		if (adxValue is not AverageDirectionalIndexValue adx || !adxValue.IsFinal)
		return;

		if (adx.MovingAverage is not decimal adxStrength)
		return;

		if (!_higherInitialized)
		{
			if (!_higherFast.IsFormed || !_higherSlow.IsFormed)
			return;

			_higherPrevFast = fast;
			_higherPrevSlow = slow;
			_higherInitialized = true;
			_higherTrendDirection = fast > slow ? 1 : fast < slow ? -1 : 0;
			_isHigherRange = adxStrength < AdxThreshold;
			return;
		}

		var crossUp = fast > slow && _higherPrevFast <= _higherPrevSlow;
		var crossDown = fast < slow && _higherPrevFast >= _higherPrevSlow;

		if (crossUp)
		{
			_higherTrendDirection = 1;
			_lastHigherSignalDirection = 1;
			_lastHigherSignalTime = GetCandleCloseTime(candle, HigherCandleType);
		}
		else if (crossDown)
		{
			_higherTrendDirection = -1;
			_lastHigherSignalDirection = -1;
			_lastHigherSignalTime = GetCandleCloseTime(candle, HigherCandleType);
		}
		else if (fast > slow)
		{
			_higherTrendDirection = 1;
		}
		else if (fast < slow)
		{
			_higherTrendDirection = -1;
		}

		_isHigherRange = adxStrength < AdxThreshold;

		_higherPrevFast = fast;
		_higherPrevSlow = slow;
	}

	private void ProcessLowerCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (fastValue is not DecimalIndicatorValue { IsFinal: true, Value: var fast })
		return;

		if (slowValue is not DecimalIndicatorValue { IsFinal: true, Value: var slow })
		return;

		if (atrValue is not DecimalIndicatorValue { IsFinal: true, Value: var atr })
		return;

		_lastAtrValue = atr;

		if (!_lowerInitialized)
		{
			if (!_lowerFast.IsFormed || !_lowerSlow.IsFormed || !_lowerAtr.IsFormed)
			return;

			_lowerPrevFast = fast;
			_lowerPrevSlow = slow;
			_lowerInitialized = true;
			return;
		}

		var crossUp = fast > slow && _lowerPrevFast <= _lowerPrevSlow;
		var crossDown = fast < slow && _lowerPrevFast >= _lowerPrevSlow;

		_lowerPrevFast = fast;
		_lowerPrevSlow = slow;

		var closeTime = GetCandleCloseTime(candle, LowerCandleType);

		if (crossUp)
		HandleLongSignal(candle, closeTime);

		if (crossDown)
		HandleShortSignal(candle, closeTime);

		ManageOpenPosition(candle, crossUp, crossDown);
	}

	private void HandleLongSignal(ICandleMessage candle, DateTimeOffset closeTime)
	{
		if (_isHigherRange)
		return;

		if (!IsHigherSignalValid(closeTime, 1))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position < 0)
		{
			if (!AllowHedging)
			{
				if (CloseOnReverseSignal)
				{
					BuyMarket(Math.Abs(Position));
					ResetPositionState();
				}
				else
				{
					return;
				}
			}
			else if (CloseOnReverseSignal)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}

		if (Position > 0)
		return;

		var entryPrice = candle.ClosePrice;
		var atrDistance = _lastAtrValue * AtrMultiplier;
		if (atrDistance <= 0m)
		return;

		var maxRisk = ToPrice(MaxRiskPips);
		if (maxRisk > 0m && atrDistance > maxRisk)
		atrDistance = maxRisk;

		var stopPrice = entryPrice - atrDistance;
		if (stopPrice <= 0m)
		return;

		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		var previousVolume = Math.Abs(_positionVolume);
		var newVolume = previousVolume + volume;
		_averageEntryPrice = previousVolume == 0m ? entryPrice : (previousVolume * _averageEntryPrice + entryPrice * volume) / newVolume;
		_positionVolume = newVolume;
		_currentStopPrice = stopPrice;
		_currentTargetPrice = EnableTakeProfit ? entryPrice + ToPrice(TakeProfitPips) : 0m;
		_riskFreeActivated = false;
		_nextLongPyramidPrice = _averageEntryPrice + ToPrice(RiskFreeStepPips);
	}

	private void HandleShortSignal(ICandleMessage candle, DateTimeOffset closeTime)
	{
		if (_isHigherRange)
		return;

		if (!IsHigherSignalValid(closeTime, -1))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0)
		{
			if (!AllowHedging)
			{
				if (CloseOnReverseSignal)
				{
					SellMarket(Math.Abs(Position));
					ResetPositionState();
				}
				else
				{
					return;
				}
			}
			else if (CloseOnReverseSignal)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}

		if (Position < 0)
		return;

		var entryPrice = candle.ClosePrice;
		var atrDistance = _lastAtrValue * AtrMultiplier;
		if (atrDistance <= 0m)
		return;

		var maxRisk = ToPrice(MaxRiskPips);
		if (maxRisk > 0m && atrDistance > maxRisk)
		atrDistance = maxRisk;

		var stopPrice = entryPrice + atrDistance;
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		SellMarket(volume);

		var previousVolume = Math.Abs(_positionVolume);
		var newVolume = previousVolume + volume;
		_averageEntryPrice = previousVolume == 0m ? entryPrice : (previousVolume * _averageEntryPrice + entryPrice * volume) / newVolume;
		_positionVolume = -newVolume;
		_currentStopPrice = stopPrice;
		_currentTargetPrice = EnableTakeProfit ? entryPrice - ToPrice(TakeProfitPips) : 0m;
		_riskFreeActivated = false;
		_nextShortPyramidPrice = _averageEntryPrice - ToPrice(RiskFreeStepPips);
	}

	private void ManageOpenPosition(ICandleMessage candle, bool crossUp, bool crossDown)
	{
		var price = candle.ClosePrice;

		if (Position > 0)
		{
			if (CloseOnReverseSignal && crossDown)
			{
				ExitLong();
				return;
			}

			if (_currentStopPrice > 0m && price <= _currentStopPrice)
			{
				ExitLong();
				return;
			}

			if (_currentTargetPrice > 0m && price >= _currentTargetPrice)
			{
				ExitLong();
				return;
			}

			UpdateTrailingStopLong(price);
			UpdateRiskFreeLong(price);
		}
		else if (Position < 0)
		{
			if (CloseOnReverseSignal && crossUp)
			{
				ExitShort();
				return;
			}

			if (_currentStopPrice > 0m && price >= _currentStopPrice)
			{
				ExitShort();
				return;
			}

			if (_currentTargetPrice > 0m && price <= _currentTargetPrice)
			{
				ExitShort();
				return;
			}

			UpdateTrailingStopShort(price);
			UpdateRiskFreeShort(price);
		}
	}

	private void UpdateTrailingStopLong(decimal price)
	{
		if (!EnableTrailingStop)
		return;

		var start = ToPrice(TrailingStartPips);
		if (start <= 0m)
		return;

		if (price - _averageEntryPrice < start)
		return;

		var cushion = ToPrice(TrailingCushionPips);
		if (cushion <= 0m)
		return;

		var newStop = price - cushion;
		if (newStop > _currentStopPrice)
		_currentStopPrice = newStop;
	}

	private void UpdateTrailingStopShort(decimal price)
	{
		if (!EnableTrailingStop)
		return;

		var start = ToPrice(TrailingStartPips);
		if (start <= 0m)
		return;

		if (_averageEntryPrice - price < start)
		return;

		var cushion = ToPrice(TrailingCushionPips);
		if (cushion <= 0m)
		return;

		var newStop = price + cushion;
		if (_currentStopPrice == 0m || newStop < _currentStopPrice)
		_currentStopPrice = newStop;
	}

	private void UpdateRiskFreeLong(decimal price)
	{
		if (!EnableRiskFreePyramiding)
		return;

		var step = ToPrice(RiskFreeStepPips);
		if (step <= 0m)
		return;

		if (!_riskFreeActivated)
		{
			if (price - _averageEntryPrice >= step)
			{
				_currentStopPrice = Math.Max(_currentStopPrice, _averageEntryPrice);
				_riskFreeActivated = true;
			}
			else
			{
				return;
			}
		}

		if (_nextLongPyramidPrice <= 0m)
		_nextLongPyramidPrice = _averageEntryPrice + step;

		if (price < _nextLongPyramidPrice)
		return;

		var currentVolume = Math.Abs(_positionVolume);
		var maxVolume = PyramidMaxVolume;
		if (maxVolume <= 0m)
		return;

		if (currentVolume >= maxVolume)
		{
			_currentStopPrice = Math.Max(_currentStopPrice, price - step);
			return;
		}

		var increment = Math.Min(PyramidIncrementVolume, maxVolume - currentVolume);
		if (increment <= 0m)
		return;

		BuyMarket(increment);

		var newVolume = currentVolume + increment;
		_averageEntryPrice = (currentVolume * _averageEntryPrice + price * increment) / newVolume;
		_positionVolume = newVolume;
		_currentStopPrice = Math.Max(_currentStopPrice, price - step);
		_nextLongPyramidPrice = price + step;
	}

	private void UpdateRiskFreeShort(decimal price)
	{
		if (!EnableRiskFreePyramiding)
		return;

		var step = ToPrice(RiskFreeStepPips);
		if (step <= 0m)
		return;

		if (!_riskFreeActivated)
		{
			if (_averageEntryPrice - price >= step)
			{
				_currentStopPrice = _currentStopPrice == 0m ? _averageEntryPrice : Math.Min(_currentStopPrice, _averageEntryPrice);
				_riskFreeActivated = true;
			}
			else
			{
				return;
			}
		}

		if (_nextShortPyramidPrice >= _averageEntryPrice || _nextShortPyramidPrice == 0m)
		_nextShortPyramidPrice = _averageEntryPrice - step;

		if (price > _nextShortPyramidPrice)
		return;

		var currentVolume = Math.Abs(_positionVolume);
		var maxVolume = PyramidMaxVolume;
		if (maxVolume <= 0m)
		return;

		if (currentVolume >= maxVolume)
		{
			_currentStopPrice = _currentStopPrice == 0m ? price + step : Math.Min(_currentStopPrice, price + step);
			return;
		}

		var increment = Math.Min(PyramidIncrementVolume, maxVolume - currentVolume);
		if (increment <= 0m)
		return;

		SellMarket(increment);

		var newVolume = currentVolume + increment;
		_averageEntryPrice = (currentVolume * _averageEntryPrice + price * increment) / newVolume;
		_positionVolume = -newVolume;
		_currentStopPrice = _currentStopPrice == 0m ? price + step : Math.Min(_currentStopPrice, price + step);
		_nextShortPyramidPrice = price - step;
	}

	private void ExitLong()
	{
		if (Position <= 0)
		return;

		SellMarket(Math.Abs(Position));
		ResetPositionState();
	}

	private void ExitShort()
	{
		if (Position >= 0)
		return;

		BuyMarket(Math.Abs(Position));
		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_averageEntryPrice = 0m;
		_currentStopPrice = 0m;
		_currentTargetPrice = 0m;
		_riskFreeActivated = false;
		_positionVolume = 0m;
		_nextLongPyramidPrice = 0m;
		_nextShortPyramidPrice = 0m;
	}

	private bool IsHigherSignalValid(DateTimeOffset time, int direction)
	{
		if (_higherTrendDirection != direction)
		return false;

		if (_lastHigherSignalDirection != direction)
		return false;

		if (_lastHigherSignalTime == default)
		return false;

		var window = TimeSpan.FromMinutes(HigherSignalWindowMinutes);
		if (window <= TimeSpan.Zero)
		return true;

		return time - _lastHigherSignalTime <= window;
	}

	private decimal ToPrice(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var step = Security?.PriceStep ?? 0.0001m;
		return pips * step;
	}

	private DateTimeOffset GetCandleCloseTime(ICandleMessage candle, DataType candleType)
	{
		if (candle.CloseTime != default)
		return candle.CloseTime;

		return candle.OpenTime + GetTimeFrame(candleType);
	}

	private static TimeSpan GetTimeFrame(DataType dataType)
	{
		return dataType.Arg switch
		{
			TimeSpan timeSpan => timeSpan,
			_ => TimeSpan.FromMinutes(1)
		};
	}
}
