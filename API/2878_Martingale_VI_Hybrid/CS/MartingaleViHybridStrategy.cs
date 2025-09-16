using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "Martingale VI Hybrid" MetaTrader expert advisor to StockSharp high level API.
/// Combines a moving average and MACD filter with martingale position sizing, cash based targets,
/// and profit trailing in money.
/// </summary>
public class MartingaleViHybridStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingActivationMoney;
	private readonly StrategyParam<decimal> _trailingDrawdownMoney;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _pipStep;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<bool> _closeMaxOrders;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fastSma = null!;
	private SMA _slowSma = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevMacd;
	private decimal _prevMacdSignal;

	private int _martingaleDirection;
	private int _openTrades;
	private decimal _lastOrderVolume;
	private decimal _takeProfitPrice;
	private decimal _nextAddPrice;
	private decimal _lastEntryPrice;

	private bool _trailingActive;
	private decimal _maxProfit;

	private decimal _initialPortfolioValue;
	private bool _initialValueSet;

	private decimal _pipSize;

	/// <summary>
	/// Enables the money based global take profit target.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

/// <summary>
/// Profit in account currency required to close all positions.
/// </summary>
public decimal MoneyTakeProfit
{
	get => _moneyTakeProfit.Value;
	set => _moneyTakeProfit.Value = value;
}

/// <summary>
/// Enables the percentage based global take profit target.
/// </summary>
public bool UsePercentTakeProfit
{
	get => _usePercentTakeProfit.Value;
	set => _usePercentTakeProfit.Value = value;
}

/// <summary>
/// Percentage of the initial portfolio value used as a profit target.
/// </summary>
public decimal PercentTakeProfit
{
	get => _percentTakeProfit.Value;
	set => _percentTakeProfit.Value = value;
}

/// <summary>
/// Enables trailing in money once the floating profit reaches the activation level.
/// </summary>
public bool EnableTrailing
{
	get => _enableTrailing.Value;
	set => _enableTrailing.Value = value;
}

/// <summary>
/// Profit level that enables trailing stop in money.
/// </summary>
public decimal TrailingActivationMoney
{
	get => _trailingActivationMoney.Value;
	set => _trailingActivationMoney.Value = value;
}

/// <summary>
/// Maximum allowed drawdown from the profit peak during trailing.
/// </summary>
public decimal TrailingDrawdownMoney
{
	get => _trailingDrawdownMoney.Value;
	set => _trailingDrawdownMoney.Value = value;
}

/// <summary>
/// Take profit distance for every entry expressed in pips.
/// </summary>
public int TakeProfitPips
{
	get => _takeProfitPips.Value;
	set => _takeProfitPips.Value = value;
}

/// <summary>
/// Adverse price movement in pips required to add the next martingale order.
/// </summary>
public int PipStep
{
	get => _pipStep.Value;
	set => _pipStep.Value = value;
}

/// <summary>
/// Initial volume of the first order.
/// </summary>
public decimal InitialVolume
{
	get => _initialVolume.Value;
	set => _initialVolume.Value = value;
}

/// <summary>
/// Multiplier applied to each subsequent martingale order.
/// </summary>
public decimal VolumeMultiplier
{
	get => _volumeMultiplier.Value;
	set => _volumeMultiplier.Value = value;
}

/// <summary>
/// Maximum number of simultaneously opened martingale orders.
/// </summary>
public int MaxTrades
{
	get => _maxTrades.Value;
	set => _maxTrades.Value = value;
}

/// <summary>
/// Fast moving average period.
/// </summary>
public int FastPeriod
{
	get => _fastPeriod.Value;
	set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow moving average period.
/// </summary>
public int SlowPeriod
{
	get => _slowPeriod.Value;
	set => _slowPeriod.Value = value;
}

/// <summary>
/// MACD fast EMA period.
/// </summary>
public int MacdFastPeriod
{
	get => _macdFastPeriod.Value;
	set => _macdFastPeriod.Value = value;
}

/// <summary>
/// MACD slow EMA period.
/// </summary>
public int MacdSlowPeriod
{
	get => _macdSlowPeriod.Value;
	set => _macdSlowPeriod.Value = value;
}

/// <summary>
/// MACD signal period.
/// </summary>
public int MacdSignalPeriod
{
	get => _macdSignalPeriod.Value;
	set => _macdSignalPeriod.Value = value;
}

/// <summary>
/// Closes all trades when the martingale limit is reached instead of keeping them open.
/// </summary>
public bool CloseMaxOrders
{
	get => _closeMaxOrders.Value;
	set => _closeMaxOrders.Value = value;
}

/// <summary>
/// Candle type used for indicator calculations.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="MartingaleViHybridStrategy"/>.
/// </summary>
public MartingaleViHybridStrategy()
{
	_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
	.SetDisplay("Use Money TP", "Enable cash based take profit", "Risk");

	_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 10m)
	.SetGreaterThanZero()
	.SetDisplay("Money TP", "Profit target in currency", "Risk")
	.SetCanOptimize(true);

	_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
	.SetDisplay("Use Percent TP", "Enable percent take profit", "Risk");

	_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
	.SetGreaterThanZero()
	.SetDisplay("Percent TP", "Profit target as percent of equity", "Risk")
	.SetCanOptimize(true);

	_enableTrailing = Param(nameof(EnableTrailing), true)
	.SetDisplay("Enable Trailing", "Use trailing stop in money", "Risk");

	_trailingActivationMoney = Param(nameof(TrailingActivationMoney), 40m)
	.SetGreaterThanZero()
	.SetDisplay("Trailing Activation", "Profit required to start trailing", "Risk")
	.SetCanOptimize(true);

	_trailingDrawdownMoney = Param(nameof(TrailingDrawdownMoney), 10m)
	.SetGreaterThanZero()
	.SetDisplay("Trailing Drawdown", "Allowed profit give back", "Risk")
	.SetCanOptimize(true);

	_takeProfitPips = Param(nameof(TakeProfitPips), 10)
	.SetGreaterThanZero()
	.SetDisplay("Take Profit (pips)", "Distance for per-trade take profit", "Trading")
	.SetCanOptimize(true);

	_pipStep = Param(nameof(PipStep), 10)
	.SetGreaterThanZero()
	.SetDisplay("Pip Step", "Distance before adding a new order", "Martingale")
	.SetCanOptimize(true);

	_initialVolume = Param(nameof(InitialVolume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Initial Volume", "Volume of the first order", "Martingale")
	.SetCanOptimize(true);

	_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
	.SetGreaterThanZero()
	.SetDisplay("Volume Multiplier", "Multiplier for each addition", "Martingale")
	.SetCanOptimize(true);

	_maxTrades = Param(nameof(MaxTrades), 4)
	.SetGreaterThanZero()
	.SetDisplay("Max Trades", "Maximum number of martingale orders", "Martingale");

	_fastPeriod = Param(nameof(FastPeriod), 1)
	.SetGreaterThanZero()
	.SetDisplay("Fast MA", "Fast moving average period", "Indicators");

	_slowPeriod = Param(nameof(SlowPeriod), 50)
	.SetGreaterThanZero()
	.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
	.SetCanOptimize(true);

	_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
	.SetGreaterThanZero()
	.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
	.SetCanOptimize(true);

	_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
	.SetGreaterThanZero()
	.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
	.SetCanOptimize(true);

	_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
	.SetCanOptimize(true);

	_closeMaxOrders = Param(nameof(CloseMaxOrders), true)
	.SetDisplay("Close Max Orders", "Close all trades when max trades reached", "Martingale");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Candle Type", "Primary candle type", "General");
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

	_prevFast = 0m;
	_prevSlow = 0m;
	_prevMacd = 0m;
	_prevMacdSignal = 0m;

	ResetMartingaleState();

	_initialPortfolioValue = 0m;
	_initialValueSet = false;

	_pipSize = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_fastSma = new SMA { Length = FastPeriod };
	_slowSma = new SMA { Length = SlowPeriod };
	_macd = new MovingAverageConvergenceDivergenceSignal
	{
		Macd =
		{
			ShortMa = { Length = MacdFastPeriod },
			LongMa = { Length = MacdSlowPeriod },
		},
	SignalMa = { Length = MacdSignalPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription.BindEx(_macd, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, subscription);
	DrawIndicator(area, _fastSma);
	DrawIndicator(area, _slowSma);
	DrawIndicator(area, _macd);
	DrawOwnTrades(area);
}

_pipSize = CalculatePipSize();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	var fastValue = _fastSma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
	var slowValue = _slowSma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

	if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
	return;

	if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal macdSignal)
	{
		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevMacd = macd;
		_prevMacdSignal = macdSignal;
		return;
	}

if (!_fastSma.IsFormed || !_slowSma.IsFormed || !_macd.IsFormed)
{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	_prevMacd = macd;
	_prevMacdSignal = macdSignal;
	return;
}

if (!_initialValueSet)
{
	var currentValue = Portfolio?.CurrentValue ?? 0m;
	if (currentValue > 0m)
	{
		_initialPortfolioValue = currentValue;
		_initialValueSet = true;
	}
}

ManageExistingPosition(candle);
if (Position != 0)
{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	_prevMacd = macd;
	_prevMacdSignal = macdSignal;
	return;
}

if (!IsFormedAndOnlineAndAllowTrading())
{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	_prevMacd = macd;
	_prevMacdSignal = macdSignal;
	return;
}

var buySignal = _prevFast > _prevSlow && _prevMacd < _prevMacdSignal;
var sellSignal = _prevFast < _prevSlow && _prevMacd > _prevMacdSignal;

if (buySignal)
{
	EnterLong(candle.ClosePrice);
}
else if (sellSignal)
{
	EnterShort(candle.ClosePrice);
}

_prevFast = fastValue;
_prevSlow = slowValue;
_prevMacd = macd;
_prevMacdSignal = macdSignal;
}

private void ManageExistingPosition(ICandleMessage candle)
{
	if (Position == 0)
	{
		ResetMartingaleState();
		return;
	}

var price = candle.ClosePrice;
var unrealized = CalculateUnrealizedPnL(price);

if (UseMoneyTakeProfit && unrealized >= MoneyTakeProfit && MoneyTakeProfit > 0m)
{
	CloseAll();
	return;
}

if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialValueSet)
{
	var target = _initialPortfolioValue * PercentTakeProfit / 100m;
	if (target > 0m && unrealized >= target)
	{
		CloseAll();
		return;
	}
}

HandleTrailing(unrealized);
if (Position == 0)
return;

if (_takeProfitPrice > 0m)
{
	var reached = _martingaleDirection > 0
	? candle.HighPrice >= _takeProfitPrice
	: candle.LowPrice <= _takeProfitPrice;

	if (reached)
	{
		CloseAll();
		return;
	}
}

HandleMartingaleAdditions(candle);
}

private void EnterLong(decimal price)
{
	if (InitialVolume <= 0m)
	return;

	BuyMarket(InitialVolume);

	_martingaleDirection = 1;
	_openTrades = 1;
	_lastOrderVolume = InitialVolume;
	UpdateTargetsAfterEntry(price);
	ResetTrailingState();
}

private void EnterShort(decimal price)
{
	if (InitialVolume <= 0m)
	return;

	SellMarket(InitialVolume);

	_martingaleDirection = -1;
	_openTrades = 1;
	_lastOrderVolume = InitialVolume;
	UpdateTargetsAfterEntry(price);
	ResetTrailingState();
}

private void HandleMartingaleAdditions(ICandleMessage candle)
{
	if (_martingaleDirection == 0 || PipStep <= 0 || _pipSize <= 0m || VolumeMultiplier <= 0m)
	return;

	if (_openTrades >= MaxTrades && MaxTrades > 0)
	{
		if (CloseMaxOrders)
		CloseAll();
		return;
	}

if (_nextAddPrice <= 0m)
return;

if (_martingaleDirection > 0)
{
	var testPrice = Math.Min(candle.LowPrice, candle.ClosePrice);
	if (testPrice <= _nextAddPrice)
	AddMartingaleOrder(_nextAddPrice);
}
else
{
	var testPrice = Math.Max(candle.HighPrice, candle.ClosePrice);
	if (testPrice >= _nextAddPrice)
	AddMartingaleOrder(_nextAddPrice);
}
}

private void AddMartingaleOrder(decimal price)
{
	var newVolume = _lastOrderVolume * VolumeMultiplier;
	if (newVolume <= 0m)
	return;

	if (_martingaleDirection > 0)
	BuyMarket(newVolume);
	else
	SellMarket(newVolume);

	_openTrades++;
	_lastOrderVolume = newVolume;
	UpdateTargetsAfterEntry(price);
	ResetTrailingState();
}

private void HandleTrailing(decimal unrealized)
{
	if (!EnableTrailing || TrailingActivationMoney <= 0m || TrailingDrawdownMoney <= 0m)
	return;

	if (!_trailingActive)
	{
		if (unrealized >= TrailingActivationMoney)
		{
			_trailingActive = true;
			_maxProfit = unrealized;
		}
	return;
}

if (unrealized > _maxProfit)
{
	_maxProfit = unrealized;
	return;
}

if (unrealized <= _maxProfit - TrailingDrawdownMoney)
CloseAll();
}

private void CloseAll()
{
	if (Position > 0)
	SellMarket(Math.Abs(Position));
else if (Position < 0)
BuyMarket(Math.Abs(Position));

ResetMartingaleState();
ResetTrailingState();
}

private void ResetMartingaleState()
{
	_martingaleDirection = 0;
	_openTrades = 0;
	_lastOrderVolume = 0m;
	_takeProfitPrice = 0m;
	_nextAddPrice = 0m;
	_lastEntryPrice = 0m;
}

private void ResetTrailingState()
{
	_trailingActive = false;
	_maxProfit = 0m;
}

private void UpdateTargetsAfterEntry(decimal price)
{
	_lastEntryPrice = price;

	if (_pipSize <= 0m)
	{
		_takeProfitPrice = 0m;
		_nextAddPrice = 0m;
		return;
	}

var tpOffset = TakeProfitPips * _pipSize;
var stepOffset = PipStep * _pipSize;

if (_martingaleDirection > 0)
{
	_takeProfitPrice = price + tpOffset;
	_nextAddPrice = PipStep > 0 ? price - stepOffset : 0m;
}
else if (_martingaleDirection < 0)
{
	_takeProfitPrice = price - tpOffset;
	_nextAddPrice = PipStep > 0 ? price + stepOffset : 0m;
}
else
{
	_takeProfitPrice = 0m;
	_nextAddPrice = 0m;
}
}

private decimal CalculateUnrealizedPnL(decimal price)
{
	var priceStep = Security.PriceStep ?? 0m;
	var stepPrice = Security.StepPrice ?? 0m;
	if (priceStep <= 0m || stepPrice <= 0m || Position == 0)
	return 0m;

	var diff = price - PositionAvgPrice;
	var steps = diff / priceStep;
	return steps * stepPrice * Position;
}

private decimal CalculatePipSize()
{
	var step = Security.PriceStep ?? 0m;
	if (step <= 0m)
	return 0m;

	if (step == 0.00001m || step == 0.001m)
	return step * 10m;

	return step;
}
}
