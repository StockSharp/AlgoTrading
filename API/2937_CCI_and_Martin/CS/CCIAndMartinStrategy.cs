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
/// CCI pattern strategy with optional martingale and step-based volume management.
/// </summary>
public class CCIAndMartinStrategy : Strategy
{
	/// <summary>
	/// Step increment behaviour for volume adjustments.
	/// </summary>
	public enum StepMode
	{
		/// <summary>
		/// Increase volume after a losing trade.
		/// </summary>
		Loss = -1,

		/// <summary>
		/// Increase volume after a profitable trade.
		/// </summary>
		Profit = 1,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<decimal> _martingaleCoefficient;
	private readonly StrategyParam<int> _martingaleTriggerLosses;
	private readonly StrategyParam<int> _martingaleMaxSteps;
	private readonly StrategyParam<bool> _enableStepAdjustments;
	private readonly StrategyParam<decimal> _stepVolumeIncrement;
	private readonly StrategyParam<decimal> _stepVolumeMax;
	private readonly StrategyParam<StepMode> _stepAdjustmentMode;

	private CommodityChannelIndex _cci = null!;
	private readonly List<decimal> _cciBuffer = new();
	private readonly List<ICandleMessage> _candleBuffer = new();

	private decimal _pipSize;
	private decimal _currentVolume;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _isLongPosition;

	private int _lossCount;
	private int _profitCount;
	private int _martingaleStep;

	/// <summary>
	/// Initializes a new instance of <see cref="CCIAndMartinStrategy"/>.
	/// </summary>
	public CCIAndMartinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_cciPeriod = Param(nameof(CciPeriod), 27)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index length", "CCI");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting trade volume", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimal price improvement before trailing updates", "Risk");

		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable Martingale", "Use martingale volume scaling", "Volume Management");

		_martingaleCoefficient = Param(nameof(MartingaleCoefficient), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Coefficient", "Multiplier applied after qualifying losses", "Volume Management");

		_martingaleTriggerLosses = Param(nameof(MartingaleTriggerLosses), 1)
			.SetNotNegative()
			.SetDisplay("Martingale Trigger", "Losses required before scaling volume", "Volume Management");

		_martingaleMaxSteps = Param(nameof(MartingaleMaxSteps), 3)
			.SetNotNegative()
			.SetDisplay("Martingale Steps", "Maximum martingale multiplications", "Volume Management");

		_enableStepAdjustments = Param(nameof(EnableStepAdjustments), false)
			.SetDisplay("Enable Step Adjustments", "Use step-based volume increments", "Volume Management");

		_stepVolumeIncrement = Param(nameof(StepVolumeIncrement), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Step Volume", "Volume increment when step rule triggers", "Volume Management");

		_stepVolumeMax = Param(nameof(StepVolumeMax), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Step Volume Cap", "Maximum allowed volume when step rule is active", "Volume Management");

		_stepAdjustmentMode = Param(nameof(StepAdjustmentMode), StepMode.Loss)
			.SetDisplay("Step Mode", "Condition that increases position size", "Volume Management");
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Initial trading volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables martingale scaling after losses.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Martingale multiplier applied to the current volume.
	/// </summary>
	public decimal MartingaleCoefficient
	{
		get => _martingaleCoefficient.Value;
		set => _martingaleCoefficient.Value = value;
	}

	/// <summary>
	/// Number of consecutive losses required before martingale scaling.
	/// </summary>
	public int MartingaleTriggerLosses
	{
		get => _martingaleTriggerLosses.Value;
		set => _martingaleTriggerLosses.Value = value;
	}

	/// <summary>
	/// Maximum number of martingale multiplications.
	/// </summary>
	public int MartingaleMaxSteps
	{
		get => _martingaleMaxSteps.Value;
		set => _martingaleMaxSteps.Value = value;
	}

	/// <summary>
	/// Enables step-based volume adjustments.
	/// </summary>
	public bool EnableStepAdjustments
	{
		get => _enableStepAdjustments.Value;
		set => _enableStepAdjustments.Value = value;
	}

	/// <summary>
	/// Volume increment for the step adjustment logic.
	/// </summary>
	public decimal StepVolumeIncrement
	{
		get => _stepVolumeIncrement.Value;
		set => _stepVolumeIncrement.Value = value;
	}

	/// <summary>
	/// Maximum volume allowed for the step adjustment logic.
	/// </summary>
	public decimal StepVolumeMax
	{
		get => _stepVolumeMax.Value;
		set => _stepVolumeMax.Value = value;
	}

	/// <summary>
	/// Determines whether step adjustments react to losses or profits.
	/// </summary>
	public StepMode StepAdjustmentMode
	{
		get => _stepAdjustmentMode.Value;
		set => _stepAdjustmentMode.Value = value;
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

	_cciBuffer.Clear();
	_candleBuffer.Clear();

	_pipSize = 0m;
	_currentVolume = InitialVolume;
	_stopLossOffset = 0m;
	_takeProfitOffset = 0m;
	_trailingStopOffset = 0m;
	_trailingStepOffset = 0m;

	_entryPrice = null;
	_stopPrice = null;
	_takePrice = null;
	_isLongPosition = false;

	_lossCount = 0;
	_profitCount = 0;
	_martingaleStep = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (EnableMartingale && EnableStepAdjustments)
	throw new InvalidOperationException("Martingale and step adjustments cannot be enabled simultaneously.");

	if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
	throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

	_cci = new CommodityChannelIndex { Length = CciPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_cci, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _cci);
	DrawOwnTrades(area);
	}

	_pipSize = CalculatePipSize();
	_stopLossOffset = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
	_takeProfitOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
	_trailingStopOffset = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
	_trailingStepOffset = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

	ApplyInitialVolume();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Store indicator and candle data for pattern recognition.
	_cciBuffer.Add(cciValue);
	if (_cciBuffer.Count > 4)
	_cciBuffer.RemoveAt(0);

	_candleBuffer.Add(candle);
	if (_candleBuffer.Count > 3)
	_candleBuffer.RemoveAt(0);

	ManageActivePosition(candle);

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (Position != 0)
	return;

	if (_cciBuffer.Count < 4 || _candleBuffer.Count < 3)
	return;

	var cci0 = _cciBuffer[^1];
	var cci1 = _cciBuffer[^2];
	var cci2 = _cciBuffer[^3];
	var cci3 = _cciBuffer[^4];

	var candle0 = _candleBuffer[^1];
	var candle1 = _candleBuffer[^2];
	var candle2 = _candleBuffer[^3];

	var bullishPattern = candle2.OpenPrice > candle2.ClosePrice &&
	candle1.OpenPrice > candle1.ClosePrice &&
	candle0.OpenPrice < candle0.ClosePrice &&
	candle1.OpenPrice < candle0.ClosePrice;

	var bearishPattern = candle2.OpenPrice < candle2.ClosePrice &&
	candle1.OpenPrice < candle1.ClosePrice &&
	candle0.OpenPrice > candle0.ClosePrice &&
	candle1.OpenPrice > candle0.ClosePrice;

	var bullishCci = cci1 < 5m && cci2 < cci3 && cci1 < cci2 && cci0 > cci1;
	var bearishCci = cci1 > -5m && cci2 > cci3 && cci1 > cci2 && cci0 < cci1;

	if (bullishPattern && bullishCci)
	{
	EnterPosition(true, candle.ClosePrice);
	}
	else if (bearishPattern && bearishCci)
	{
	EnterPosition(false, candle.ClosePrice);
	}
	}

	private void EnterPosition(bool isLong, decimal referencePrice)
	{
	var volume = _currentVolume;
	if (volume <= 0m)
	return;

	if (isLong)
	{
	BuyMarket(volume);
	}
	else
	{
	SellMarket(volume);
	}

	_isLongPosition = isLong;
	_entryPrice = referencePrice;
	_stopPrice = CalculateStopPrice(isLong, referencePrice);
	_takePrice = CalculateTakePrice(isLong, referencePrice);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
	if (Position == 0 || _entryPrice == null)
	return;

	UpdateTrailingStop(candle);
	CheckExitSignals(candle);
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
	if (_trailingStopOffset <= 0m || _entryPrice == null)
	return;

	var closePrice = candle.ClosePrice;

	if (_isLongPosition)
	{
	var progress = closePrice - _entryPrice.Value;
	if (progress > _trailingStopOffset + _trailingStepOffset)
	{
	var desiredStop = closePrice - _trailingStopOffset;
	if (_stopPrice is decimal currentStop)
	{
	if (desiredStop - currentStop >= _trailingStepOffset)
	_stopPrice = desiredStop;
	}
	else
	{
	_stopPrice = desiredStop;
	}
	}
	}
	else
	{
	var progress = _entryPrice.Value - closePrice;
	if (progress > _trailingStopOffset + _trailingStepOffset)
	{
	var desiredStop = closePrice + _trailingStopOffset;
	if (_stopPrice is decimal currentStop)
	{
	if (currentStop - desiredStop >= _trailingStepOffset)
	_stopPrice = desiredStop;
	}
	else
	{
	_stopPrice = desiredStop;
	}
	}
	}
	}

	private void CheckExitSignals(ICandleMessage candle)
	{
	if (_entryPrice == null)
	return;

	var low = candle.LowPrice;
	var high = candle.HighPrice;

	if (_isLongPosition)
	{
	if (_stopPrice is decimal stop && low <= stop)
	{
	CloseCurrentPosition(stop);
	return;
	}

	if (_takePrice is decimal take && high >= take)
	{
	CloseCurrentPosition(take);
	}
	}
	else
	{
	if (_stopPrice is decimal stop && high >= stop)
	{
	CloseCurrentPosition(stop);
	return;
	}

	if (_takePrice is decimal take && low <= take)
	{
	CloseCurrentPosition(take);
	}
	}
	}

	private void CloseCurrentPosition(decimal exitPrice)
	{
	if (Position > 0)
	{
	SellMarket(Position);
	}
	else if (Position < 0)
	{
	BuyMarket(-Position);
	}

	if (_entryPrice == null)
	return;

	var isProfit = _isLongPosition ? exitPrice > _entryPrice.Value : exitPrice < _entryPrice.Value;

	if (isProfit)
	{
	_profitCount++;
	}
	else
	{
	_lossCount++;
	}

	UpdateVolumeAfterClose(isProfit);

	_entryPrice = null;
	_stopPrice = null;
	_takePrice = null;
	}

	private void UpdateVolumeAfterClose(bool isProfit)
	{
	if (EnableMartingale)
	{
	if (!isProfit)
	{
	if (_martingaleStep < MartingaleMaxSteps && _lossCount >= MartingaleTriggerLosses)
	{
	var nextVolume = NormalizeVolume(_currentVolume * MartingaleCoefficient);
	_currentVolume = nextVolume == 0m ? InitialVolume : nextVolume;
	_martingaleStep++;
	Volume = _currentVolume;
	return;
	}
	}
	else
	{
	ApplyInitialVolume();
	return;
	}
	}

	if (EnableStepAdjustments)
	{
	var candidate = NormalizeVolume(_currentVolume + StepVolumeIncrement);

	if (StepAdjustmentMode == StepMode.Loss)
	{
	if (!isProfit)
	{
	if (candidate == 0m)
	{
	ApplyInitialVolume();
	return;
	}

	if (candidate <= StepVolumeMax)
	{
	_currentVolume = candidate;
	Volume = _currentVolume;
	}
	else
	{
	ApplyInitialVolume();
	}
	}
	else
	{
	ApplyInitialVolume();
	}
	}
	else
	{
	if (isProfit)
	{
	if (candidate == 0m)
	{
	ApplyInitialVolume();
	return;
	}

	if (candidate <= StepVolumeMax)
	{
	_currentVolume = candidate;
	Volume = _currentVolume;
	}
	else
	{
	ApplyInitialVolume();
	}
	}
	else
	{
	ApplyInitialVolume();
	}
	}

	_lossCount = 0;
	_profitCount = 0;
	_martingaleStep = 0;
	return;
	}

	if (!EnableMartingale)
	{
	ApplyInitialVolume();
	}
	}

	private decimal? CalculateStopPrice(bool isLong, decimal entryPrice)
	{
	if (_stopLossOffset <= 0m)
	{
	return null;
	}

	return isLong ? entryPrice - _stopLossOffset : entryPrice + _stopLossOffset;
	}

	private decimal? CalculateTakePrice(bool isLong, decimal entryPrice)
	{
	if (_takeProfitOffset <= 0m)
	{
	return null;
	}

	return isLong ? entryPrice + _takeProfitOffset : entryPrice - _takeProfitOffset;
	}

	private void ApplyInitialVolume()
	{
	var normalized = NormalizeVolume(InitialVolume);
	_currentVolume = normalized == 0m ? InitialVolume : normalized;
	Volume = _currentVolume;

	_lossCount = 0;
	_profitCount = 0;
	_martingaleStep = 0;
	}

	private decimal NormalizeVolume(decimal volume)
	{
	if (volume <= 0m)
	{
	return 0m;
	}

	var step = Security?.VolumeStep ?? 1m;
	if (step <= 0m)
	{
	step = 1m;
	}

	var steps = Math.Floor(volume / step);
	if (steps <= 0m)
	{
	return 0m;
	}

	return steps * step;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0.0001m;
	if (step <= 0m)
	{
	step = 0.0001m;
	}

	var digits = GetDecimalDigits(step);
	return (digits == 3 || digits == 5) ? step * 10m : step;
	}

	private static int GetDecimalDigits(decimal value)
	{
	value = Math.Abs(value);
	var digits = 0;

	while (value != Math.Truncate(value) && digits < 8)
	{
	value *= 10m;
	digits++;
	}

	return digits;
	}
}