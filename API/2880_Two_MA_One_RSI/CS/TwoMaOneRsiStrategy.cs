namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

/// <summary>
/// Port of the “Two MA one RSI” MetaTrader 5 strategy.
/// Combines a fast and slow moving average crossover with RSI filters and fixed/trailed exits.
/// </summary>
public class TwoMaOneRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowPeriodMultiplier;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<bool> _buyPreviousFastBelowSlow;
	private readonly StrategyParam<bool> _buyCurrentFastAboveSlow;
	private readonly StrategyParam<bool> _buyRequiresRsiAboveUpper;
	private readonly StrategyParam<bool> _sellPreviousFastAboveSlow;
	private readonly StrategyParam<bool> _sellCurrentFastBelowSlow;
	private readonly StrategyParam<bool> _sellRequiresRsiBelowLower;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<decimal> _tradeVolume;

	private LengthIndicator<decimal> _fastMa = null!;
	private LengthIndicator<decimal> _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	private decimal? _previousRsi;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	private decimal _pipSize;
	private decimal _stepPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="TwoMaOneRsiStrategy"/>.
	/// </summary>
	public TwoMaOneRsiStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Candle Type", "Working timeframe", "General");

	_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
	.SetDisplay("MA Type", "Type of moving averages", "Indicators");

	_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
	.SetGreaterThanZero()
	.SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators");

	_slowPeriodMultiplier = Param(nameof(SlowPeriodMultiplier), 2)
	.SetGreaterThanZero()
	.SetDisplay("Slow MA Multiplier", "Slow MA period = Fast period * multiplier", "Indicators");

	_fastMaShift = Param(nameof(FastMaShift), 3)
	.SetGreaterOrEqualZero()
	.SetDisplay("Fast MA Shift", "Horizontal shift applied to the fast MA", "Indicators");

	_slowMaShift = Param(nameof(SlowMaShift), 0)
	.SetGreaterOrEqualZero()
	.SetDisplay("Slow MA Shift", "Horizontal shift applied to the slow MA", "Indicators");

	_rsiPeriod = Param(nameof(RsiPeriod), 10)
	.SetGreaterThanZero()
	.SetDisplay("RSI Period", "Length of the RSI filter", "Indicators");

	_rsiUpperLevel = Param(nameof(RsiUpperLevel), 70m)
	.SetRange(0m, 100m)
	.SetDisplay("RSI Upper", "RSI threshold for long confirmation", "Indicators");

	_rsiLowerLevel = Param(nameof(RsiLowerLevel), 30m)
	.SetRange(0m, 100m)
	.SetDisplay("RSI Lower", "RSI threshold for short confirmation", "Indicators");

	_buyPreviousFastBelowSlow = Param(nameof(BuyPreviousFastBelowSlow), true)
	.SetDisplay("Buy Previous <", "Require fast MA below slow MA two bars ago", "Signals");

	_buyCurrentFastAboveSlow = Param(nameof(BuyCurrentFastAboveSlow), true)
	.SetDisplay("Buy Current >", "Require fast MA above slow MA on the last bar", "Signals");

	_buyRequiresRsiAboveUpper = Param(nameof(BuyRequiresRsiAboveUpper), true)
	.SetDisplay("Buy RSI >", "Require RSI above the upper level", "Signals");

	_sellPreviousFastAboveSlow = Param(nameof(SellPreviousFastAboveSlow), true)
	.SetDisplay("Sell Previous >", "Require fast MA above slow MA two bars ago", "Signals");

	_sellCurrentFastBelowSlow = Param(nameof(SellCurrentFastBelowSlow), true)
	.SetDisplay("Sell Current <", "Require fast MA below slow MA on the last bar", "Signals");

	_sellRequiresRsiBelowLower = Param(nameof(SellRequiresRsiBelowLower), true)
	.SetDisplay("Sell RSI <", "Require RSI below the lower level", "Signals");

	_stopLossPips = Param(nameof(StopLossPips), 50)
	.SetGreaterOrEqualZero()
	.SetDisplay("Stop Loss (pips)", "Distance of the stop-loss in pips", "Risk");

	_takeProfitPips = Param(nameof(TakeProfitPips), 150)
	.SetGreaterOrEqualZero()
	.SetDisplay("Take Profit (pips)", "Distance of the take-profit in pips", "Risk");

	_trailingStopPips = Param(nameof(TrailingStopPips), 15)
	.SetGreaterOrEqualZero()
	.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

	_trailingStepPips = Param(nameof(TrailingStepPips), 5)
	.SetGreaterOrEqualZero()
	.SetDisplay("Trailing Step (pips)", "Minimum improvement before trailing moves", "Risk");

	_maxPositions = Param(nameof(MaxPositions), 10)
	.SetGreaterOrEqualZero()
	.SetDisplay("Max Positions", "Maximum simultaneous entries per side (0 = unlimited)", "Risk");

	_profitClose = Param(nameof(ProfitClose), 100m)
	.SetGreaterOrEqualZero()
	.SetDisplay("Profit Close", "Close all positions when floating profit (in currency) reaches this level", "Risk");

	_closeOppositePositions = Param(nameof(CloseOppositePositions), false)
	.SetDisplay("Close Opposite", "Close opposite positions before opening a new trade", "Risk");

	_tradeVolume = Param(nameof(TradeVolume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Trade Volume", "Order volume for each new entry", "Trading");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Type of moving averages.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
	get => _maType.Value;
	set => _maType.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
	get => _fastMaPeriod.Value;
	set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to compute the slow MA period.
	/// </summary>
	public int SlowPeriodMultiplier
	{
	get => _slowPeriodMultiplier.Value;
	set => _slowPeriodMultiplier.Value = value;
	}

	/// <summary>
	/// Calculated slow moving average period.
	/// </summary>
	public int SlowMaPeriod => FastMaPeriod * SlowPeriodMultiplier;

	/// <summary>
	/// Horizontal shift for the fast moving average.
	/// </summary>
	public int FastMaShift
	{
	get => _fastMaShift.Value;
	set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Horizontal shift for the slow moving average.
	/// </summary>
	public int SlowMaShift
	{
	get => _slowMaShift.Value;
	set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI level used for long signals.
	/// </summary>
	public decimal RsiUpperLevel
	{
	get => _rsiUpperLevel.Value;
	set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI level used for short signals.
	/// </summary>
	public decimal RsiLowerLevel
	{
	get => _rsiLowerLevel.Value;
	set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Require fast MA to be below slow MA two bars ago for long entries.
	/// </summary>
	public bool BuyPreviousFastBelowSlow
	{
	get => _buyPreviousFastBelowSlow.Value;
	set => _buyPreviousFastBelowSlow.Value = value;
	}

	/// <summary>
	/// Require fast MA to be above slow MA on the last bar for long entries.
	/// </summary>
	public bool BuyCurrentFastAboveSlow
	{
	get => _buyCurrentFastAboveSlow.Value;
	set => _buyCurrentFastAboveSlow.Value = value;
	}

	/// <summary>
	/// Require RSI to be above the upper level for long entries.
	/// </summary>
	public bool BuyRequiresRsiAboveUpper
	{
	get => _buyRequiresRsiAboveUpper.Value;
	set => _buyRequiresRsiAboveUpper.Value = value;
	}

	/// <summary>
	/// Require fast MA to be above slow MA two bars ago for short entries.
	/// </summary>
	public bool SellPreviousFastAboveSlow
	{
	get => _sellPreviousFastAboveSlow.Value;
	set => _sellPreviousFastAboveSlow.Value = value;
	}

	/// <summary>
	/// Require fast MA to be below slow MA on the last bar for short entries.
	/// </summary>
	public bool SellCurrentFastBelowSlow
	{
	get => _sellCurrentFastBelowSlow.Value;
	set => _sellCurrentFastBelowSlow.Value = value;
	}

	/// <summary>
	/// Require RSI to be below the lower level for short entries.
	/// </summary>
	public bool SellRequiresRsiBelowLower
	{
	get => _sellRequiresRsiBelowLower.Value;
	set => _sellRequiresRsiBelowLower.Value = value;
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
	/// Trailing step in pips.
	/// </summary>
	public int TrailingStepPips
	{
	get => _trailingStepPips.Value;
	set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous entries per direction.
	/// </summary>
	public int MaxPositions
	{
	get => _maxPositions.Value;
	set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Floating profit target that triggers closing all positions.
	/// </summary>
	public decimal ProfitClose
	{
	get => _profitClose.Value;
	set => _profitClose.Value = value;
	}

	/// <summary>
	/// Close opposite positions before entering a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
	get => _closeOppositePositions.Value;
	set => _closeOppositePositions.Value = value;
	}

	/// <summary>
	/// Volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
	get => _tradeVolume.Value;
	set
	{
	_tradeVolume.Value = value;
	Volume = value;
	}
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

	_fastHistory.Clear();
	_slowHistory.Clear();
	_previousRsi = null;
	ResetLongState();
	ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (TrailingStopPips > 0 && TrailingStepPips == 0)
	throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

	Volume = TradeVolume;

	_fastMa = CreateMovingAverage(MaType, FastMaPeriod);
	_slowMa = CreateMovingAverage(MaType, SlowMaPeriod);
	_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

	_fastHistory.Clear();
	_slowHistory.Clear();
	_previousRsi = null;

	_pipSize = Security?.PriceStep ?? 1m;
	if (Security?.PriceStep is decimal step && step < 1m)
	_pipSize = step * 10m;

	_stepPrice = Security?.StepPrice ?? 1m;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_fastMa, _slowMa, _rsi, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawOwnTrades(area);

	var rsiArea = CreateChartArea();
	if (rsiArea != null)
	DrawIndicator(rsiArea, _rsi);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_rsi.IsFormed)
	{
	_fastHistory.Add(fastValue);
	_slowHistory.Add(slowValue);
	_previousRsi = rsiValue;
	return;
	}

	_fastHistory.Add(fastValue);
	_slowHistory.Add(slowValue);

	UpdateRiskManagement(candle);
	if (ProfitClose > 0m && TryCloseOnProfit(candle))
	{
	_previousRsi = rsiValue;
	return;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousRsi = rsiValue;
	return;
	}

	var fastPrevIndex = _fastHistory.Count - FastMaShift - 2;
	var fastPrevPrevIndex = _fastHistory.Count - FastMaShift - 3;
	var slowPrevIndex = _slowHistory.Count - SlowMaShift - 2;
	var slowPrevPrevIndex = _slowHistory.Count - SlowMaShift - 3;

	if (fastPrevIndex < 0 || fastPrevPrevIndex < 0 || slowPrevIndex < 0 || slowPrevPrevIndex < 0 || _previousRsi is null)
	{
	_previousRsi = rsiValue;
	return;
	}

	var fastPrev = _fastHistory[fastPrevIndex];
	var fastPrevPrev = _fastHistory[fastPrevPrevIndex];
	var slowPrev = _slowHistory[slowPrevIndex];
	var slowPrevPrev = _slowHistory[slowPrevPrevIndex];
	var prevRsi = _previousRsi.Value;

	var buyPrevCondition = BuyPreviousFastBelowSlow ? fastPrevPrev < slowPrevPrev : fastPrevPrev > slowPrevPrev;
	var buyCurrentCondition = BuyCurrentFastAboveSlow ? fastPrev > slowPrev : fastPrev < slowPrev;
	var buyRsiCondition = BuyRequiresRsiAboveUpper ? prevRsi > RsiUpperLevel : prevRsi < RsiUpperLevel;
	var buySignal = buyPrevCondition && buyCurrentCondition && buyRsiCondition;

	var sellPrevCondition = SellPreviousFastAboveSlow ? fastPrevPrev > slowPrevPrev : fastPrevPrev < slowPrevPrev;
	var sellCurrentCondition = SellCurrentFastBelowSlow ? fastPrev < slowPrev : fastPrev > slowPrev;
	var sellRsiCondition = SellRequiresRsiBelowLower ? prevRsi < RsiLowerLevel : prevRsi > RsiLowerLevel;
	var sellSignal = sellPrevCondition && sellCurrentCondition && sellRsiCondition;

	if (buySignal)
	TryOpenLong(candle);

	if (sellSignal)
	TryOpenShort(candle);

	_previousRsi = rsiValue;
	}

	private void TryOpenLong(ICandleMessage candle)
	{
	var currentLongCount = GetLongCount();
	if (MaxPositions > 0 && currentLongCount >= MaxPositions)
		return;

	if (!CloseOppositePositions && Position < 0)
		return;

	var existingLongVolume = Math.Max(Position, 0m);
	var volume = TradeVolume;
	if (CloseOppositePositions && Position < 0)
	{
		volume += Math.Abs(Position);
		ResetShortState();
	}

	if (volume <= 0m)
		return;

	BuyMarket(volume);

	var entryPrice = candle.ClosePrice;
	UpdateLongEntry(existingLongVolume, volume, entryPrice);
	LogInfo($"Opened long at {entryPrice} with volume {volume}.");
	}

	private void TryOpenShort(ICandleMessage candle)
	{
	var currentShortCount = GetShortCount();
	if (MaxPositions > 0 && currentShortCount >= MaxPositions)
		return;

	if (!CloseOppositePositions && Position > 0)
		return;

	var existingShortVolume = Math.Max(-Position, 0m);
	var volume = TradeVolume;
	if (CloseOppositePositions && Position > 0)
	{
		volume += Position;
		ResetLongState();
	}

	if (volume <= 0m)
		return;

	SellMarket(volume);

	var entryPrice = candle.ClosePrice;
	UpdateShortEntry(existingShortVolume, volume, entryPrice);
	LogInfo($"Opened short at {entryPrice} with volume {volume}.");
	}

	private void UpdateLongEntry(decimal existingVolume, decimal addedVolume, decimal entryPrice)
	{
	if (_longEntryPrice is null || existingVolume <= 0m)
	{
		_longEntryPrice = entryPrice;
	}
	else
	{
		var total = existingVolume + addedVolume;
		_longEntryPrice = total > 0m
			? ((_longEntryPrice.Value * existingVolume) + entryPrice * addedVolume) / total
			: entryPrice;
	}

	if (_longEntryPrice.HasValue)
	{
		var averageEntry = _longEntryPrice.Value;
		if (StopLossPips > 0)
			_longStopPrice = averageEntry - StopLossPips * _pipSize;
		else if (TrailingStopPips <= 0)
			_longStopPrice = null;

		_longTakePrice = TakeProfitPips > 0 ? averageEntry + TakeProfitPips * _pipSize : null;
	}
	else
	{
		_longStopPrice = null;
		_longTakePrice = null;
	}

	_shortEntryPrice = null;
	_shortStopPrice = null;
	_shortTakePrice = null;
	}

	private void UpdateShortEntry(decimal existingVolume, decimal addedVolume, decimal entryPrice)
	{
	if (_shortEntryPrice is null || existingVolume <= 0m)
	{
		_shortEntryPrice = entryPrice;
	}
	else
	{
		var total = existingVolume + addedVolume;
		_shortEntryPrice = total > 0m
			? ((_shortEntryPrice.Value * existingVolume) + entryPrice * addedVolume) / total
			: entryPrice;
	}

	if (_shortEntryPrice.HasValue)
	{
		var averageEntry = _shortEntryPrice.Value;
		if (StopLossPips > 0)
			_shortStopPrice = averageEntry + StopLossPips * _pipSize;
		else if (TrailingStopPips <= 0)
			_shortStopPrice = null;

		_shortTakePrice = TakeProfitPips > 0 ? averageEntry - TakeProfitPips * _pipSize : null;
	}
	else
	{
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	_longEntryPrice = null;
	_longStopPrice = null;
	_longTakePrice = null;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
	if (Position > 0)
	{
	TryUpdateLongTrailing(candle);

	if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
	{
	LogInfo($"Long stop hit at {_longStopPrice.Value}.");
	CloseLong();
	return;
	}

	if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
	{
	LogInfo($"Long take-profit hit at {_longTakePrice.Value}.");
	CloseLong();
	}
	}
	else if (Position < 0)
	{
	TryUpdateShortTrailing(candle);

	if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
	{
	LogInfo($"Short stop hit at {_shortStopPrice.Value}.");
	CloseShort();
	return;
	}

	if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
	{
	LogInfo($"Short take-profit hit at {_shortTakePrice.Value}.");
	CloseShort();
	}
	}
	}

	private void TryUpdateLongTrailing(ICandleMessage candle)
	{
	if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || _longEntryPrice is null)
	return;

	var trailingDistance = TrailingStopPips * _pipSize;
	var trailingStep = TrailingStepPips * _pipSize;
	var profit = candle.ClosePrice - _longEntryPrice.Value;

	if (profit <= trailingDistance + trailingStep)
	return;

	var newStop = candle.ClosePrice - trailingDistance;

	if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value + trailingStep)
	{
	_longStopPrice = newStop;
	LogInfo($"Adjusted long trailing stop to {newStop}.");
	}
	}

	private void TryUpdateShortTrailing(ICandleMessage candle)
	{
	if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || _shortEntryPrice is null)
	return;

	var trailingDistance = TrailingStopPips * _pipSize;
	var trailingStep = TrailingStepPips * _pipSize;
	var profit = _shortEntryPrice.Value - candle.ClosePrice;

	if (profit <= trailingDistance + trailingStep)
	return;

	var newStop = candle.ClosePrice + trailingDistance;

	if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value - trailingStep)
	{
	_shortStopPrice = newStop;
	LogInfo($"Adjusted short trailing stop to {newStop}.");
	}
	}

	private bool TryCloseOnProfit(ICandleMessage candle)
	{
	if (ProfitClose <= 0m)
	return false;

	var step = Security?.PriceStep ?? 1m;
	if (step == 0m)
	step = 1m;

	var stepPrice = _stepPrice;
	if (stepPrice == 0m)
	stepPrice = 1m;

	decimal profit = 0m;

	if (Position > 0 && _longEntryPrice.HasValue)
	{
	var diff = candle.ClosePrice - _longEntryPrice.Value;
	profit = diff / step * stepPrice * Position;
	}
	else if (Position < 0 && _shortEntryPrice.HasValue)
	{
	var diff = _shortEntryPrice.Value - candle.ClosePrice;
	profit = diff / step * stepPrice * Math.Abs(Position);
	}

	if (profit >= ProfitClose && profit > 0m)
	{
	LogInfo($"Floating profit {profit} reached target {ProfitClose}. Closing positions.");
	CloseAllPositions();
	return true;
	}

	return false;
	}

	private void CloseLong()
	{
	if (Position <= 0)
	return;

	SellMarket(Position);
	ResetLongState();
	}

	private void CloseShort()
	{
	if (Position >= 0)
	return;

	BuyMarket(Math.Abs(Position));
	ResetShortState();
	}

	private void CloseAllPositions()
	{
	if (Position > 0)
	CloseLong();
	else if (Position < 0)
	CloseShort();
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

	private int GetLongCount()
	{
	if (TradeVolume <= 0m)
	return 0;

	return (int)Math.Round(Math.Max(Position, 0m) / TradeVolume, MidpointRounding.AwayFromZero);
	}

	private int GetShortCount()
	{
	if (TradeVolume <= 0m)
	return 0;

	return (int)Math.Round(Math.Max(-Position, 0m) / TradeVolume, MidpointRounding.AwayFromZero);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
	return type switch
	{
	MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
	MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
	MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
	MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
	MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
	_ => new SimpleMovingAverage { Length = length },
	};
	}
}
