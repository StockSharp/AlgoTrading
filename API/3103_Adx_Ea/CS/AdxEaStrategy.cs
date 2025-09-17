using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "ADX EA" MetaTrader expert advisor built around ADX/DI breakouts and crossovers.
/// </summary>
public class AdxEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<bool> _enableBreakoutStrategy;
	private readonly StrategyParam<bool> _enableCrossStrategy;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _requireAdxSlope;
	private readonly StrategyParam<bool> _confirmCrossOnBreakout;
	private readonly StrategyParam<bool> _enableMacdExit;
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<decimal> _minDirectionalDifference;
	private readonly StrategyParam<decimal> _minAdxMainLine;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _totalEquityRisk;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private AverageDirectionalIndex _adx = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _currentFastMa;
	private decimal? _currentSlowMa;
	private decimal? _previousFastMa;
	private decimal? _previousSlowMa;

	private decimal? _currentPlusDi;
	private decimal? _currentMinusDi;
	private decimal? _currentAdx;
	private decimal? _previousPlusDi;
	private decimal? _previousMinusDi;
	private decimal? _previousAdx;

	private decimal? _momentumCurrent;
	private decimal? _momentumPrev1;
	private decimal? _momentumPrev2;
	private decimal? _momentumPrev3;

	private decimal? _macdCurrent;
	private decimal? _macdSignalCurrent;
	private decimal? _macdPrev;
	private decimal? _macdSignalPrev;

	private decimal? _volumePrev1;
	private decimal? _volumePrev2;
	private decimal? _volumePrev3;

	private ICandleMessage? _lastCandle;
	private ICandleMessage? _previousCandle;
	private ICandleMessage? _previousCandle2;

	private DateTimeOffset? _maUpdateTime;
	private DateTimeOffset? _adxUpdateTime;
	private DateTimeOffset? _lastProcessedTime;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _equityPeak;

	public AdxEaStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetDisplay("Trade Volume", "Base volume for each new entry.", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe used for ADX and moving averages.", "Data")
			.SetCanOptimize(false);

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Momentum Candle Type", "Higher timeframe used to evaluate momentum strength.", "Data");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle Type", "Timeframe used for the exit MACD filter.", "Data");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Indicators")
			.SetCanOptimize(true);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period of the Average Directional Index.", "Indicators")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Period for the momentum calculation on the higher timeframe.", "Indicators")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA period for the exit MACD filter.", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA period for the exit MACD filter.", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal line period for the exit MACD filter.", "Indicators");

		_enableBreakoutStrategy = Param(nameof(EnableBreakoutStrategy), true)
			.SetDisplay("Enable Breakout", "Enable the ADX breakout logic.", "Trading Rules");

		_enableCrossStrategy = Param(nameof(EnableCrossStrategy), true)
			.SetDisplay("Enable Cross", "Enable the +DI/-DI crossover logic.", "Trading Rules");

		_useTrendFilter = Param(nameof(UseTrendFilter), true)
			.SetDisplay("Use Trend Filter", "Require +DI to stay above -DI for long trades and vice versa for shorts.", "Trading Rules");

		_requireAdxSlope = Param(nameof(RequireAdxSlope), true)
			.SetDisplay("Require Rising ADX", "Demand the ADX main line to slope upwards for crossover entries.", "Trading Rules");

		_confirmCrossOnBreakout = Param(nameof(ConfirmCrossOnBreakout), true)
			.SetDisplay("Confirm Cross", "Require breakout levels when confirming DI crosses.", "Trading Rules");

		_enableMacdExit = Param(nameof(EnableMacdExit), true)
			.SetDisplay("Enable MACD Exit", "Allow the monthly MACD filter to close positions.", "Risk");

		_entryLevel = Param(nameof(EntryLevel), 10m)
			.SetDisplay("Entry Level", "Minimum ADX/+DI/-DI level to qualify as breakout.", "Trading Rules");

		_exitLevel = Param(nameof(ExitLevel), 10m)
			.SetDisplay("Exit Level", "ADX threshold that disables new entries when trend weakens.", "Trading Rules");

		_minDirectionalDifference = Param(nameof(MinDirectionalDifference), 10m)
			.SetDisplay("Directional Gap", "Minimum difference between +DI and -DI.", "Trading Rules");

		_minAdxMainLine = Param(nameof(MinAdxMainLine), 10m)
			.SetDisplay("Minimum ADX", "Minimum ADX value for crossover entries.", "Trading Rules");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetDisplay("Momentum Buy Threshold", "Required momentum deviation from 100 for long trades.", "Trading Rules");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetDisplay("Momentum Sell Threshold", "Required momentum deviation from 100 for short trades.", "Trading Rules");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetDisplay("Max Trades", "Maximum number of pyramid steps allowed.", "Risk");

		_lotExponent = Param(nameof(LotExponent), 1.44m)
			.SetDisplay("Lot Exponent", "Multiplier applied when adding new positions.", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps.", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetDisplay("Stop Loss (steps)", "Stop loss distance expressed in price steps.", "Risk");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetDisplay("Trailing Stop (steps)", "Trailing stop distance expressed in price steps.", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break-even", "Move the protective stop once price moves into profit.", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 30m)
			.SetDisplay("Break-even Trigger", "Distance in steps required before arming the break-even logic.", "Risk");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 30m)
			.SetDisplay("Break-even Offset", "Additional distance applied when moving the stop beyond the entry price.", "Risk");

		_useEquityStop = Param(nameof(UseEquityStop), true)
			.SetDisplay("Use Equity Stop", "Activate drawdown based emergency exit.", "Risk");

		_totalEquityRisk = Param(nameof(TotalEquityRisk), 1m)
			.SetDisplay("Equity Risk (%)", "Maximum floating loss, in percent, before flattening all positions.", "Risk");
	}

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Primary timeframe used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe for the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used by the MACD exit filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public bool EnableBreakoutStrategy
	{
		get => _enableBreakoutStrategy.Value;
		set => _enableBreakoutStrategy.Value = value;
	}

	public bool EnableCrossStrategy
	{
		get => _enableCrossStrategy.Value;
		set => _enableCrossStrategy.Value = value;
	}

	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	public bool RequireAdxSlope
	{
		get => _requireAdxSlope.Value;
		set => _requireAdxSlope.Value = value;
	}

	public bool ConfirmCrossOnBreakout
	{
		get => _confirmCrossOnBreakout.Value;
		set => _confirmCrossOnBreakout.Value = value;
	}

	public bool EnableMacdExit
	{
		get => _enableMacdExit.Value;
		set => _enableMacdExit.Value = value;
	}

	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
	}

	public decimal MinDirectionalDifference
	{
		get => _minDirectionalDifference.Value;
		set => _minDirectionalDifference.Value = value;
	}

	public decimal MinAdxMainLine
	{
		get => _minAdxMainLine.Value;
		set => _minAdxMainLine.Value = value;
	}

	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	public decimal TotalEquityRisk
	{
		get => _totalEquityRisk.Value;
		set => _totalEquityRisk.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (MomentumCandleType != CandleType)
		{
			yield return (Security, MomentumCandleType);
		}

		yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentFastMa = null;
		_currentSlowMa = null;
		_previousFastMa = null;
		_previousSlowMa = null;
		_currentPlusDi = null;
		_currentMinusDi = null;
		_currentAdx = null;
		_previousPlusDi = null;
		_previousMinusDi = null;
		_previousAdx = null;
		_momentumCurrent = null;
		_momentumPrev1 = null;
		_momentumPrev2 = null;
		_momentumPrev3 = null;
		_macdCurrent = null;
		_macdSignalCurrent = null;
		_macdPrev = null;
		_macdSignalPrev = null;
		_volumePrev1 = null;
		_volumePrev2 = null;
		_volumePrev3 = null;
		_lastCandle = null;
		_previousCandle = null;
		_previousCandle2 = null;
		_maUpdateTime = null;
		_adxUpdateTime = null;
		_lastProcessedTime = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_equityPeak = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, ProcessMovingAverages)
			.BindEx(_adx, ProcessAdx)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(_momentum, ProcessMomentum)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacd)
			.Start();

		var takeProfitUnit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.Step) : null;
		var stopLossUnit = StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.Step) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, mainSubscription);
			DrawIndicator(priceArea, _fastMa);
			DrawIndicator(priceArea, _slowMa);
			DrawOwnTrades(priceArea);
		}

		var adxArea = CreateChartArea();
		if (adxArea != null)
		{
			DrawIndicator(adxArea, _adx);
		}
	}

private void ProcessMovingAverages(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
if (candle.State != CandleStates.Finished)
{
return;
}

// Shift stored candles so that _previousCandle mimics MQL's Candle[1] and _previousCandle2 mimics Candle[2].
_previousCandle2 = _previousCandle;
_previousCandle = _lastCandle;
_lastCandle = candle;

// Keep track of the latest and previous LWMA readings for later comparison.
_previousFastMa = _currentFastMa;
_previousSlowMa = _currentSlowMa;
_currentFastMa = fastValue;
_currentSlowMa = slowValue;

_maUpdateTime = candle.CloseTime;

// Update volume history so Volume[1], Volume[2], Volume[3] are always available for the filters.
UpdateVolumeHistory(candle.TotalVolume ?? 0m);

TryProcessSignal(candle);
}

private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
{
if (candle.State != CandleStates.Finished)
{
return;
}

if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue typed)
{
return;
}

if (typed.Adx is not decimal adx || typed.PlusDi is not decimal plusDi || typed.MinusDi is not decimal minusDi)
{
return;
}

// Preserve the previous ADX/+DI/−DI values to reproduce the EA's cross detection.
_previousAdx = _currentAdx;
_previousPlusDi = _currentPlusDi;
_previousMinusDi = _currentMinusDi;

_currentAdx = adx;
_currentPlusDi = plusDi;
_currentMinusDi = minusDi;

_adxUpdateTime = candle.CloseTime;

TryProcessSignal(candle);
}

private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
{
if (candle.State != CandleStates.Finished)
{
return;
}

// Maintain a rolling window so we can reference Momentum[1], Momentum[2] and Momentum[3].
_momentumPrev3 = _momentumPrev2;
_momentumPrev2 = _momentumPrev1;
_momentumPrev1 = _momentumCurrent;
_momentumCurrent = momentumValue;
}

private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
{
return;
}

if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
{
return;
}

if (typed.Macd is not decimal macdMain || typed.Signal is not decimal macdSignal)
{
return;
}

// Store MACD values from the previous closed candle to match the EA's index = 1 checks.
_macdPrev = _macdCurrent;
_macdSignalPrev = _macdSignalCurrent;
_macdCurrent = macdMain;
_macdSignalCurrent = macdSignal;
}

private void TryProcessSignal(ICandleMessage candle)
{
if (_maUpdateTime != candle.CloseTime || _adxUpdateTime != candle.CloseTime)
{
return;
}

if (_lastProcessedTime == candle.CloseTime)
{
return;
}

// All indicator snapshots must be available before evaluating the MetaTrader rules.
if (_currentFastMa is null || _currentSlowMa is null || _currentPlusDi is null || _currentMinusDi is null || _currentAdx is null)
{
return;
}

if (_previousPlusDi is null || _previousMinusDi is null || _previousAdx is null)
{
return;
}

if (_previousCandle is null || _previousCandle2 is null)
{
return;
}

if (_momentumPrev1 is null || _momentumPrev2 is null || _momentumPrev3 is null)
{
return;
}

if (_volumePrev1 is null || _volumePrev2 is null || _volumePrev3 is null)
{
return;
}

if (TradeVolume <= 0m)
{
return;
}

if (!IsFormedAndOnlineAndAllowTrading())
{
return;
}

_lastProcessedTime = candle.CloseTime;

// Replicate all MetaTrader filters: volume expansion, momentum deviation, DI cross and MACD polarity.
var volumeCondition = _volumePrev1.Value > _volumePrev2.Value || _volumePrev1.Value > _volumePrev3.Value;
var momBuyDeviation = Math.Abs(100m - _momentumPrev1.Value);
		var momSellDeviation = Math.Abs(_momentumPrev1.Value - 100m);
		var momBuyDeviation2 = Math.Abs(100m - _momentumPrev2.Value);
		var momBuyDeviation3 = Math.Abs(100m - _momentumPrev3.Value);
		var momSellDeviation2 = Math.Abs(_momentumPrev2.Value - 100m);
		var momSellDeviation3 = Math.Abs(_momentumPrev3.Value - 100m);
		var momIncreasing = _momentumPrev1.Value > _momentumPrev2.Value;
		var momDecreasing = _momentumPrev1.Value < _momentumPrev2.Value;

		var plusCrossUp = _currentPlusDi.Value > _currentMinusDi.Value && _previousPlusDi.Value < _previousMinusDi.Value;
		var minusCrossUp = _currentMinusDi.Value > _currentPlusDi.Value && _previousMinusDi.Value < _previousPlusDi.Value;
		var adxRising = _currentAdx.Value > _previousAdx.Value;
		var adxAboveEntry = _currentAdx.Value > EntryLevel || _currentPlusDi.Value > EntryLevel || _currentMinusDi.Value > EntryLevel;
		var adxAboveExit = _currentAdx.Value > ExitLevel;
		var adxAboveMin = _currentAdx.Value > MinAdxMainLine;
		var diGap = Math.Abs(_currentPlusDi.Value - _currentMinusDi.Value);

		var maBullish = _currentFastMa.Value > _currentSlowMa.Value;
		var maBearish = _currentFastMa.Value < _currentSlowMa.Value;

		var bullishStructure = _previousCandle2.LowPrice < _previousCandle.HighPrice;
		var bearishStructure = _previousCandle.LowPrice < _previousCandle2.HighPrice;

		var macdBullish = _macdPrev is not null && _macdSignalPrev is not null && _macdPrev.Value > _macdSignalPrev.Value;
		var macdBearish = _macdPrev is not null && _macdSignalPrev is not null && _macdPrev.Value < _macdSignalPrev.Value;

		var breakoutBuy = EnableBreakoutStrategy
			&& adxAboveEntry
			&& diGap > MinDirectionalDifference
			&& (!UseTrendFilter || _currentPlusDi.Value > _currentMinusDi.Value)
			&& maBullish
			&& momIncreasing
			&& volumeCondition
			&& (momBuyDeviation > MomentumBuyThreshold || momBuyDeviation2 > MomentumBuyThreshold || momBuyDeviation3 > MomentumBuyThreshold)
			&& bullishStructure
			&& macdBullish
			&& adxAboveExit;

		var breakoutSell = EnableBreakoutStrategy
			&& adxAboveEntry
			&& diGap > MinDirectionalDifference
			&& (!UseTrendFilter || _currentMinusDi.Value > _currentPlusDi.Value)
			&& maBearish
			&& momDecreasing
			&& volumeCondition
			&& (momSellDeviation > MomentumSellThreshold || momSellDeviation2 > MomentumSellThreshold || momSellDeviation3 > MomentumSellThreshold)
			&& bearishStructure
			&& macdBearish
			&& adxAboveExit;

		var crossBuy = EnableCrossStrategy
			&& plusCrossUp
			&& (!RequireAdxSlope || adxRising)
			&& (!ConfirmCrossOnBreakout || adxAboveEntry)
			&& adxAboveMin
			&& maBullish
			&& momIncreasing
			&& volumeCondition
			&& macdBullish;

		var crossSell = EnableCrossStrategy
			&& minusCrossUp
			&& (!RequireAdxSlope || adxRising)
			&& (!ConfirmCrossOnBreakout || adxAboveEntry)
			&& adxAboveMin
			&& maBearish
			&& momDecreasing
			&& volumeCondition
			&& macdBearish;

		var shouldBuy = breakoutBuy || crossBuy;
		var shouldSell = breakoutSell || crossSell;

		CheckEquityStop();

		if (EnableMacdExit)
		{
			EvaluateMacdExit();
		}

		UpdateTrailingAndBreakEven(candle);

		if (!adxAboveExit)
		{
			return;
		}

		if (shouldBuy)
		{
			OpenLongPosition();
		}
		else if (shouldSell)
		{
			OpenShortPosition();
		}
	}

	private void OpenLongPosition()
	{
		if (!CanOpenNewTrade())
		{
			return;
		}

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		var volume = GetNextVolume();
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);
	}

	private void OpenShortPosition()
	{
		if (!CanOpenNewTrade())
		{
			return;
		}

		if (Position > 0)
		{
			SellMarket(Position);
		}

		var volume = GetNextVolume();
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);
	}

private bool CanOpenNewTrade()
{
if (TradeVolume <= 0m)
{
return false;
}

// Convert the current net position into the number of MetaTrader-style trades.
var currentTrades = TradeVolume == 0m ? 0 : (int)Math.Round(Math.Abs(Position) / TradeVolume);
return currentTrades < MaxTrades;
}

private decimal GetNextVolume()
{
var baseVolume = TradeVolume;
if (baseVolume <= 0m)
{
return 0m;
}

// Apply the geometric lot growth used by the EA (Lots * LotExponent^CountTrades).
var existingTrades = baseVolume == 0m ? 0 : (int)Math.Round(Math.Abs(Position) / baseVolume);
var multiplier = (decimal)Math.Pow((double)LotExponent, existingTrades);
return baseVolume * multiplier;
}

private void UpdateVolumeHistory(decimal volume)
{
// Maintain a three-bar buffer so the volume filter can access Volume[1..3].
_volumePrev3 = _volumePrev2;
_volumePrev2 = _volumePrev1;
_volumePrev1 = volume;
}

private void UpdateTrailingAndBreakEven(ICandleMessage candle)
{
if (Position == 0)
{
_longTrailingStop = null;
_shortTrailingStop = null;
return;
}

var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
{
return;
}

var trailingDistance = TrailingStopSteps > 0m ? TrailingStopSteps * step : 0m;
var breakEvenTrigger = BreakEvenTrigger > 0m ? BreakEvenTrigger * step : 0m;
var breakEvenOffset = BreakEvenOffset > 0m ? BreakEvenOffset * step : 0m;

if (Position > 0)
{
var entry = PositionAvgPrice;

if (trailingDistance > 0m)
{
var candidate = candle.ClosePrice - trailingDistance;
if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
{
_longTrailingStop = candidate;
}

if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
{
SellMarket(Position);
_longTrailingStop = null;
return;
}
}

if (UseBreakEven && breakEvenTrigger > 0m && candle.HighPrice - entry >= breakEvenTrigger)
{
var breakEvenPrice = entry + breakEvenOffset;
if (candle.LowPrice <= breakEvenPrice)
{
SellMarket(Position);
_longTrailingStop = null;
}
}
}
else if (Position < 0)
{
var entry = PositionAvgPrice;
var absPosition = Math.Abs(Position);

if (trailingDistance > 0m)
{
var candidate = candle.ClosePrice + trailingDistance;
if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
{
_shortTrailingStop = candidate;
}

if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
{
BuyMarket(absPosition);
_shortTrailingStop = null;
return;
}
}

if (UseBreakEven && breakEvenTrigger > 0m && entry - candle.LowPrice >= breakEvenTrigger)
{
var breakEvenPrice = entry - breakEvenOffset;
if (candle.HighPrice >= breakEvenPrice)
{
BuyMarket(absPosition);
_shortTrailingStop = null;
}
}
}
}

	private void CheckEquityStop()
	{
		if (!UseEquityStop)
		{
			return;
		}

		_equityPeak = Math.Max(_equityPeak, PnL);
		var drawdown = _equityPeak - PnL;

		if (drawdown >= TotalEquityRisk && Position != 0)
		{
			ClosePosition();
		}
	}

	private void EvaluateMacdExit()
	{
		if (_macdPrev is null || _macdSignalPrev is null)
		{
			return;
		}

		if (Position > 0 && _macdPrev.Value <= _macdSignalPrev.Value)
		{
			SellMarket(Position);
			_longTrailingStop = null;
		}
		else if (Position < 0 && _macdPrev.Value >= _macdSignalPrev.Value)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrailingStop = null;
		}
	}
}
