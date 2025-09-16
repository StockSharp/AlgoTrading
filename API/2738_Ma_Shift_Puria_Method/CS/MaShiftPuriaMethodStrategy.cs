using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Puria method strategy that monitors EMA slopes and MACD momentum with optional trailing management.
/// </summary>
public class MaShiftPuriaMethodStrategy : Strategy
{
	private readonly StrategyParam<bool> _useManualVolume;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _shiftMinPips;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<bool> _useFractalTrailing;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _fastPrev1;
	private decimal? _fastPrev2;
	private decimal? _fastPrev3;
	private decimal? _slowPrev1;
	private decimal? _slowPrev2;
	private decimal? _slowPrev3;
	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	private readonly decimal[] _highWindow = new decimal[5];
	private readonly decimal[] _lowWindow = new decimal[5];
	private int _fractalCount;
	private decimal? _lastUpperFractal;
	private decimal? _lastLowerFractal;

	/// <summary>
	/// Use manual volume instead of risk-based sizing.
	/// </summary>
	public bool UseManualVolume
	{
		get => _useManualVolume.Value;
		set => _useManualVolume.Value = value;
	}

	/// <summary>
	/// Manual trade volume.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used when calculating position size.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum move required before updating the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of position units allowed in one direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum EMA separation (in pips) required for a valid signal.
	/// </summary>
	public decimal ShiftMinPips
	{
		get => _shiftMinPips.Value;
		set => _shiftMinPips.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Enable fractal-based trailing stop adjustments.
	/// </summary>
	public bool UseFractalTrailing
	{
		get => _useFractalTrailing.Value;
		set => _useFractalTrailing.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MaShiftPuriaMethodStrategy()
	{
		_useManualVolume = Param(nameof(UseManualVolume), true)
		.SetDisplay("Manual Volume", "Use fixed trade volume", "Risk");

		_manualVolume = Param(nameof(ManualVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Fixed trade volume", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 9m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Portfolio risk percent per trade", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 45)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 75)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Minimum advance before trailing", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum units per direction", "Risk");

		_shiftMinPips = Param(nameof(ShiftMinPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Shift Minimum", "Minimal EMA separation in pips", "Signals");

		_fastLength = Param(nameof(FastLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 80)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_macdFast = Param(nameof(MacdFast), 11)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "MACD fast period", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 102)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "MACD slow period", "Indicators");

		_useFractalTrailing = Param(nameof(UseFractalTrailing), false)
		.SetDisplay("Fractal Trailing", "Enable fractal trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for calculations", "General");
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

	_fastPrev1 = null;
	_fastPrev2 = null;
	_fastPrev3 = null;
	_slowPrev1 = null;
	_slowPrev2 = null;
	_slowPrev3 = null;
	_macdPrev1 = null;
	_macdPrev2 = null;
	_macdPrev3 = null;

	ResetLongState();
	ResetShortState();

	Array.Clear(_highWindow, 0, _highWindow.Length);
	Array.Clear(_lowWindow, 0, _lowWindow.Length);
	_fractalCount = 0;
	_lastUpperFractal = null;
	_lastLowerFractal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_fastEma = new ExponentialMovingAverage { Length = FastLength };
	_slowEma = new ExponentialMovingAverage { Length = SlowLength };
	_macd = new MovingAverageConvergenceDivergence
	{
	Fast = MacdFast,
	Slow = MacdSlow,
	Signal = 9
	};

	Volume = ManualVolume;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_fastEma, _slowEma, _macd, ProcessCandle)
	.Start();

	StartProtection();

	var priceArea = CreateChartArea();
	if (priceArea != null)
	{
	DrawCandles(priceArea, subscription);
	DrawIndicator(priceArea, _fastEma);
	DrawIndicator(priceArea, _slowEma);

	var macdArea = CreateChartArea("MACD");
	if (macdArea != null)
	{
	DrawIndicator(macdArea, _macd);
	}

	DrawOwnTrades(priceArea);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal macdMain, decimal macdSignal, decimal macdHistogram)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Silence unused parameters from MACD binding.
	_ = macdSignal;
	_ = macdHistogram;

	var pip = GetPipSize();

	UpdateFractals(candle);

	var prevFast1 = _fastPrev1;
	var prevFast2 = _fastPrev2;
	var prevFast3 = _fastPrev3;
	var prevSlow1 = _slowPrev1;
	var prevSlow3 = _slowPrev3;
	var prevMacd1 = _macdPrev1;
	var prevMacd3 = _macdPrev3;

	UpdateHistory(fast, slow, macdMain);

	ManageLongPosition(candle, pip);
	ManageShortPosition(candle, pip);

	if (!prevFast1.HasValue || !prevFast2.HasValue || !prevFast3.HasValue ||
	!prevSlow1.HasValue || !prevSlow3.HasValue || !prevMacd1.HasValue || !prevMacd3.HasValue)
	{
	return;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var fast1 = prevFast1.Value;
	var fast2 = prevFast2.Value;
	var fast3 = prevFast3.Value;
	var slow1 = prevSlow1.Value;
	var slow3 = prevSlow3.Value;
	var macd1 = prevMacd1.Value;
	var macd3 = prevMacd3.Value;

	if (pip <= 0m)
	pip = 0.0001m;

	var x1Long = (fast1 - fast2) / pip;
	var x2Long = (fast2 - fast3) / pip;

	var x1Short = (fast2 - fast1) / pip;
	var x2Short = (fast3 - fast2) / pip;

	var shiftRequirement = ShiftMinPips;

	var buySignal = fast1 > slow1 &&
	slow1 > slow3 &&
	fast1 > fast2 &&
	macd1 > 0m &&
	macd3 < 0m &&
	x1Long > shiftRequirement &&
	(x1Long >= x2Long || x2Long <= 0m);

	var sellSignal = fast1 < slow1 &&
	slow1 < slow3 &&
	fast1 < fast2 &&
	macd1 < 0m &&
	macd3 > 0m &&
	x1Short > shiftRequirement &&
	(x1Short >= x2Short || x2Short <= 0m);

	if (buySignal)
	{
	TryEnterLong(candle, pip);
	}
	else if (sellSignal)
	{
	TryEnterShort(candle, pip);
	}
	}

	private void TryEnterLong(ICandleMessage candle, decimal pip)
	{
	var stopDistance = StopLossPips > 0 ? StopLossPips * pip : 0m;
	var volumePerTrade = GetTradeVolume(stopDistance);
	if (volumePerTrade <= 0m)
	return;

	var maxVolume = volumePerTrade * MaxPositions;
	if (maxVolume <= 0m)
	return;

	var limit = maxVolume - Position;
	if (limit <= 0m)
	return;

	var volumeToBuy = volumePerTrade;
	if (Position < 0m)
	volumeToBuy += -Position;

	if (volumeToBuy > limit)
	volumeToBuy = limit;

	if (volumeToBuy <= 0m)
	return;

	BuyMarket(volumeToBuy);

	_longEntryPrice = candle.ClosePrice;
	_longStopPrice = StopLossPips > 0 ? candle.ClosePrice - stopDistance : null;
	_longTakePrice = TakeProfitPips > 0 ? candle.ClosePrice + TakeProfitPips * pip : null;

	ResetShortState();
	}

	private void TryEnterShort(ICandleMessage candle, decimal pip)
	{
	var stopDistance = StopLossPips > 0 ? StopLossPips * pip : 0m;
	var volumePerTrade = GetTradeVolume(stopDistance);
	if (volumePerTrade <= 0m)
	return;

	var maxVolume = volumePerTrade * MaxPositions;
	if (maxVolume <= 0m)
	return;

	var limit = maxVolume + Position;
	if (limit <= 0m)
	return;

	var volumeToSell = volumePerTrade;
	if (Position > 0m)
	volumeToSell += Position;

	if (volumeToSell > limit)
	volumeToSell = limit;

	if (volumeToSell <= 0m)
	return;

	SellMarket(volumeToSell);

	_shortEntryPrice = candle.ClosePrice;
	_shortStopPrice = StopLossPips > 0 ? candle.ClosePrice + stopDistance : null;
	_shortTakePrice = TakeProfitPips > 0 ? candle.ClosePrice - TakeProfitPips * pip : null;

	ResetLongState();
	}

	private void ManageLongPosition(ICandleMessage candle, decimal pip)
	{
	if (Position <= 0m)
	{
	ResetLongState();
	return;
	}

	if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
	{
	SellMarket(Position);
	ResetLongState();
	return;
	}

	if (_longTakePrice is decimal take && candle.HighPrice >= take)
	{
	SellMarket(Position);
	ResetLongState();
	return;
	}

	if (TrailingStopPips > 0 && _longEntryPrice is decimal entry)
	{
	var distance = TrailingStopPips * pip;
	var step = TrailingStepPips * pip;
	if (distance > 0m)
	{
	var profit = candle.ClosePrice - entry;
	if (profit > (TrailingStopPips + TrailingStepPips) * pip)
	{
	var threshold = candle.ClosePrice - (distance + step);
	if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
	{
	_longStopPrice = candle.ClosePrice - distance;
	}
	}
	}
	}

	if (UseFractalTrailing && _longEntryPrice is decimal longEntry && _longStopPrice.HasValue && TakeProfitPips > 0)
	{
	var target = TakeProfitPips * pip;
	if (target > 0m)
	{
	var profit = candle.ClosePrice - longEntry;
	if (profit >= 0.95m * target && _lastLowerFractal is decimal lower && lower > _longStopPrice.Value)
	{
	_longStopPrice = lower;
	}
	}
	}

	if (_longStopPrice is decimal trailing && candle.LowPrice <= trailing)
	{
	SellMarket(Position);
	ResetLongState();
	}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal pip)
	{
	if (Position >= 0m)
	{
	ResetShortState();
	return;
	}

	var shortVolume = Math.Abs(Position);

	if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
	{
	BuyMarket(shortVolume);
	ResetShortState();
	return;
	}

	if (_shortTakePrice is decimal take && candle.LowPrice <= take)
	{
	BuyMarket(shortVolume);
	ResetShortState();
	return;
	}

	if (TrailingStopPips > 0 && _shortEntryPrice is decimal entry)
	{
	var distance = TrailingStopPips * pip;
	var step = TrailingStepPips * pip;
	if (distance > 0m)
	{
	var profit = entry - candle.ClosePrice;
	if (profit > (TrailingStopPips + TrailingStepPips) * pip)
	{
	var threshold = candle.ClosePrice + (distance + step);
	if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold)
	{
	_shortStopPrice = candle.ClosePrice + distance;
	}
	}
	}
	}

	if (UseFractalTrailing && _shortEntryPrice is decimal shortEntry && _shortStopPrice.HasValue && TakeProfitPips > 0)
	{
	var target = TakeProfitPips * pip;
	if (target > 0m)
	{
	var profit = shortEntry - candle.ClosePrice;
	if (profit >= 0.95m * target && _lastUpperFractal is decimal upper && upper < _shortStopPrice.Value)
	{
	_shortStopPrice = upper;
	}
	}
	}

	if (_shortStopPrice is decimal trailing && candle.HighPrice >= trailing)
	{
	BuyMarket(shortVolume);
	ResetShortState();
	}
	}

	private void UpdateHistory(decimal fast, decimal slow, decimal macdMain)
	{
	_fastPrev3 = _fastPrev2;
	_fastPrev2 = _fastPrev1;
	_fastPrev1 = fast;

	_slowPrev3 = _slowPrev2;
	_slowPrev2 = _slowPrev1;
	_slowPrev1 = slow;

	_macdPrev3 = _macdPrev2;
	_macdPrev2 = _macdPrev1;
	_macdPrev1 = macdMain;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
	for (var i = 0; i < _highWindow.Length - 1; i++)
	{
	_highWindow[i] = _highWindow[i + 1];
	_lowWindow[i] = _lowWindow[i + 1];
	}

	_highWindow[^1] = candle.HighPrice;
	_lowWindow[^1] = candle.LowPrice;

	if (_fractalCount < _highWindow.Length)
	_fractalCount++;

	if (_fractalCount < _highWindow.Length)
	return;

	var center = _highWindow.Length / 2;
	var potentialUpper = _highWindow[center];
	var potentialLower = _lowWindow[center];

	var isUpper = true;
	for (var i = 0; i < _highWindow.Length; i++)
	{
	if (i == center)
	continue;

	if (_highWindow[i] >= potentialUpper)
	{
	isUpper = false;
	break;
	}
	}

	if (isUpper)
	_lastUpperFractal = potentialUpper;

	var isLower = true;
	for (var i = 0; i < _lowWindow.Length; i++)
	{
	if (i == center)
	continue;

	if (_lowWindow[i] <= potentialLower)
	{
	isLower = false;
	break;
	}
	}

	if (isLower)
	_lastLowerFractal = potentialLower;
	}

	private decimal GetTradeVolume(decimal stopDistance)
	{
	if (UseManualVolume || stopDistance <= 0m)
	return ManualVolume;

	var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	if (portfolioValue <= 0m)
	return ManualVolume;

	var riskAmount = portfolioValue * RiskPercent / 100m;
	if (riskAmount <= 0m)
	return ManualVolume;

	var volume = riskAmount / stopDistance;
	if (volume <= 0m)
	return ManualVolume;

	var step = Security?.VolumeStep ?? 0m;
	if (step > 0m)
	{
	var stepsCount = Math.Floor((double)(volume / step));
	volume = stepsCount <= 0 ? step : (decimal)stepsCount * step;
	}

	var minVolume = Security?.MinVolume ?? 0m;
	if (minVolume > 0m && volume < minVolume)
	volume = minVolume;

	var maxVolume = Security?.MaxVolume ?? 0m;
	if (maxVolume > 0m && volume > maxVolume)
	volume = maxVolume;

	return volume;
	}

	private decimal GetPipSize()
	{
	var step = Security?.PriceStep;
	if (step is null || step <= 0m)
	return 0.0001m;

	if (step < 0.01m)
	return step.Value * 10m;

	return step.Value;
	}

	private void ResetLongState()
	{
	_longEntryPrice = null;
	_longStopPrice = null;
	_longTakePrice = null;
	}

	private void ResetShortState()
	{
	_shortEntryPrice = null;
	_shortStopPrice = null;
	_shortTakePrice = null;
	}
}
