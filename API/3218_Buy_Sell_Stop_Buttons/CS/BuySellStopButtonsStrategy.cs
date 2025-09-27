namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Manual trading helper that mirrors the "Buy Sell Stop Buttons" MetaTrader expert advisor.
/// </summary>
public class BuySellStopButtonsStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfitInMoney;
	private readonly StrategyParam<decimal> _takeProfitInMoney;
	private readonly StrategyParam<bool> _useTakeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingProfitMoney;
	private readonly StrategyParam<decimal> _trailingLossMoney;
	private readonly StrategyParam<bool> _useBollingerExit;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<decimal> _orderLots;
	private readonly StrategyParam<int> _numberOfTrades;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyRequest;
	private readonly StrategyParam<bool> _sellRequest;
	private readonly StrategyParam<bool> _closeRequest;

	private readonly BollingerBands _bollinger = new()
	{
		Length = 20,
		Width = 2m
};

private decimal _lastPosition;
private decimal _averagePrice;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;
private decimal? _longTargetPrice;
private decimal? _shortTargetPrice;
private decimal? _longTrailingStop;
private decimal? _shortTrailingStop;
private bool _moneyTrailingActive;
private decimal _moneyTrailingPeak;
private bool _pipInitialized;
private decimal _pipSize;

/// <summary>
/// Initializes a new instance of <see cref="BuySellStopButtonsStrategy"/>.
/// </summary>
public BuySellStopButtonsStrategy()
{
	_useTakeProfitInMoney = Param(nameof(UseTakeProfitInMoney), false)
	.SetDisplay("TP in money", "Close all positions once aggregated profit reaches the threshold.", "Money targets")
	.SetCanOptimize(false);

	_takeProfitInMoney = Param(nameof(TakeProfitInMoney), 10m)
	.SetDisplay("TP amount", "Monetary profit that triggers the immediate close when TP in money is enabled.", "Money targets")
	.SetGreaterThan(0m);

	_useTakeProfitPercent = Param(nameof(UseTakeProfitPercent), false)
	.SetDisplay("TP percent", "Close all positions once profit exceeds the configured percentage of equity.", "Money targets")
	.SetCanOptimize(false);

	_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
	.SetDisplay("TP %", "Percentage of portfolio value used by the percent based take-profit.", "Money targets")
	.SetGreaterThan(0m);

	_enableTrailing = Param(nameof(EnableTrailing), true)
	.SetDisplay("Enable trailing", "Activates the equity based trailing block from the original expert.", "Money targets")
	.SetCanOptimize(false);

	_trailingProfitMoney = Param(nameof(TrailingProfitMoney), 40m)
	.SetDisplay("Trailing activation", "Profit in money that arms the trailing equity lock.", "Money targets")
	.SetGreaterThan(0m);

	_trailingLossMoney = Param(nameof(TrailingLossMoney), 10m)
	.SetDisplay("Trailing give back", "Maximum profit give back allowed once the trailing lock is armed.", "Money targets")
	.SetGreaterThan(0m);

	_useBollingerExit = Param(nameof(UseBollingerExit), true)
	.SetDisplay("Use Bollinger exit", "Mimics the chart stop button that closes positions at the opposite band.", "Exits")
	.SetCanOptimize(false);

	_useBreakEven = Param(nameof(UseBreakEven), true)
	.SetDisplay("Move to break even", "Enables the break-even logic that shifts the stop above the entry.", "Exits")
	.SetCanOptimize(false);

	_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
	.SetDisplay("Break-even trigger", "Distance in pips required before the stop is moved to break even.", "Exits")
	.SetGreaterThan(0m);

	_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
	.SetDisplay("Break-even offset", "Additional pips added on top of the entry price once break-even is active.", "Exits");

	_orderLots = Param(nameof(OrderLots), 0.01m)
	.SetDisplay("Lot size", "Base lot size used for each manual order.", "Trading")
	.SetGreaterThanZero();

	_numberOfTrades = Param(nameof(NumberOfTrades), 3)
	.SetDisplay("Order count", "Number of tickets that the button opened in MetaTrader.", "Trading")
	.SetGreaterOrEqual(1);

	_stopLossPips = Param(nameof(StopLossPips), 20m)
	.SetDisplay("Stop-loss (pips)", "Initial protective stop distance measured in pips.", "Exits")
	.SetNotNegative();

	_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
	.SetDisplay("Take-profit (pips)", "Initial target distance measured in pips.", "Exits")
	.SetNotNegative();

	_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
	.SetDisplay("Trailing stop (pips)", "Distance used by the ticket based trailing stop block.", "Exits")
	.SetNotNegative();

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle type", "Heartbeat candles used to evaluate the management rules.", "Data")
	.SetCanOptimize(false);

	_buyRequest = Param(nameof(BuyRequest), false)
	.SetDisplay("Buy request", "Set to true to replicate the BUY button action.", "Manual controls")
	.SetCanOptimize(false);

	_sellRequest = Param(nameof(SellRequest), false)
	.SetDisplay("Sell request", "Set to true to replicate the SELL button action.", "Manual controls")
	.SetCanOptimize(false);

	_closeRequest = Param(nameof(CloseRequest), false)
	.SetDisplay("Close request", "Set to true to emulate the CLOSE button that flattened all tickets.", "Manual controls")
	.SetCanOptimize(false);
}

/// <summary>
/// Closes trades when open profit reaches the configured money target.
/// </summary>
public bool UseTakeProfitInMoney
{
	get => _useTakeProfitInMoney.Value;
	set => _useTakeProfitInMoney.Value = value;
}

/// <summary>
/// Profit required to trigger the money based close.
/// </summary>
public decimal TakeProfitInMoney
{
	get => _takeProfitInMoney.Value;
	set => _takeProfitInMoney.Value = value;
}

/// <summary>
/// Enables the percent based equity target.
/// </summary>
public bool UseTakeProfitPercent
{
	get => _useTakeProfitPercent.Value;
	set => _useTakeProfitPercent.Value = value;
}

/// <summary>
/// Percentage of equity used by the percent take-profit block.
/// </summary>
public decimal TakeProfitPercent
{
	get => _takeProfitPercent.Value;
	set => _takeProfitPercent.Value = value;
}

/// <summary>
/// Enables the equity trailing lock from the original panel.
/// </summary>
public bool EnableTrailing
{
	get => _enableTrailing.Value;
	set => _enableTrailing.Value = value;
}

/// <summary>
/// Profit in money that arms the trailing lock.
/// </summary>
public decimal TrailingProfitMoney
{
	get => _trailingProfitMoney.Value;
	set => _trailingProfitMoney.Value = value;
}

/// <summary>
/// Maximum profit give back allowed once the trailing lock is active.
/// </summary>
public decimal TrailingLossMoney
{
	get => _trailingLossMoney.Value;
	set => _trailingLossMoney.Value = value;
}

/// <summary>
/// Enables the Bollinger band exit button.
/// </summary>
public bool UseBollingerExit
{
	get => _useBollingerExit.Value;
	set => _useBollingerExit.Value = value;
}

/// <summary>
/// Enables the break-even stop adjustment.
/// </summary>
public bool UseBreakEven
{
	get => _useBreakEven.Value;
	set => _useBreakEven.Value = value;
}

/// <summary>
/// Profit distance in pips required before the break-even rule activates.
/// </summary>
public decimal BreakEvenTriggerPips
{
	get => _breakEvenTriggerPips.Value;
	set => _breakEvenTriggerPips.Value = value;
}

/// <summary>
/// Offset in pips added when moving the stop to break-even.
/// </summary>
public decimal BreakEvenOffsetPips
{
	get => _breakEvenOffsetPips.Value;
	set => _breakEvenOffsetPips.Value = value;
}

/// <summary>
/// Base lot size used for each manual action.
/// </summary>
public decimal OrderLots
{
	get => _orderLots.Value;
	set => _orderLots.Value = value;
}

/// <summary>
/// Number of tickets dispatched per button press in the MQL version.
/// </summary>
public int NumberOfTrades
{
	get => _numberOfTrades.Value;
	set => _numberOfTrades.Value = value;
}

/// <summary>
/// Initial stop-loss in pips.
/// </summary>
public decimal StopLossPips
{
	get => _stopLossPips.Value;
	set => _stopLossPips.Value = value;
}

/// <summary>
/// Initial take-profit in pips.
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
/// Candle data type that drives the management loop.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Manual BUY button replica.
/// </summary>
public bool BuyRequest
{
	get => _buyRequest.Value;
	set => _buyRequest.Value = value;
}

/// <summary>
/// Manual SELL button replica.
/// </summary>
public bool SellRequest
{
	get => _sellRequest.Value;
	set => _sellRequest.Value = value;
}

/// <summary>
/// Manual CLOSE button replica.
/// </summary>
public bool CloseRequest
{
	get => _closeRequest.Value;
	set => _closeRequest.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	Volume = GetManualVolume();

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(_bollinger, ProcessCandle).Start();

	StartProtection();
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
	base.OnNewMyTrade(trade);

	var previousPosition = _lastPosition;
	var newPosition = Position;

	_lastPosition = newPosition;

	if (newPosition == 0m)
	{
		ResetProtectionState();
		return;
	}

	var tradeDirection = trade.Order.Side == Sides.Buy ? 1m : -1m;
	var tradeVolume = trade.Trade.Volume;
	var tradePrice = trade.Trade.Price;

	if (previousPosition == 0m || Math.Sign(previousPosition) != Math.Sign(newPosition))
	{
		ResetProtectionState();
		_averagePrice = tradePrice;
		InitializeProtectionForDirection(newPosition);
		return;
	}

	if (Math.Sign(tradeDirection) == Math.Sign(newPosition))
	{
		var previousAbs = Math.Abs(previousPosition);
		var newAbs = Math.Abs(newPosition);
		if (previousAbs == 0m)
		{
			_averagePrice = tradePrice;
		}
		else
		{
			_averagePrice = ((previousAbs * _averagePrice) + (tradeVolume * tradePrice)) / newAbs;
		}

		InitializeProtectionForDirection(newPosition);
	}
}

private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	HandleManualCommands();

	if (Position == 0m)
	{
		ResetMoneyTrailing();
		return;
	}

	var closePrice = candle.ClosePrice;

	if (HandleMoneyTargets(closePrice))
	return;

	ApplyBreakEven(closePrice);
	ApplyTrailingStop(closePrice);
	CheckInitialStops(closePrice);

	if (!UseBollingerExit)
	return;

	if (Position > 0m && closePrice >= upperBand)
	{
		ClosePosition();
		return;
	}

	if (Position < 0m && closePrice <= lowerBand)
	ClosePosition();
}

private void HandleManualCommands()
{
	var volume = GetManualVolume();
	if (volume <= 0m)
	return;

	if (CloseRequest && Position != 0m)
	{
		ClosePosition();
		CloseRequest = false;
		return;
	}

	if (BuyRequest)
	{
		var totalVolume = volume;
		if (Position < 0m)
		totalVolume += Math.Abs(Position);

		if (totalVolume > 0m)
		BuyMarket(totalVolume);

		BuyRequest = false;
	}

	if (SellRequest)
	{
		var totalVolume = volume;
		if (Position > 0m)
		totalVolume += Math.Abs(Position);

		if (totalVolume > 0m)
		SellMarket(totalVolume);

		SellRequest = false;
	}
}

private bool HandleMoneyTargets(decimal closePrice)
{
	var openProfit = GetOpenProfit(closePrice);

	if (UseTakeProfitInMoney && openProfit >= TakeProfitInMoney)
	{
		ClosePosition();
		return true;
	}

	if (UseTakeProfitPercent)
	{
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue > 0m)
		{
			var target = portfolioValue * TakeProfitPercent / 100m;
			if (openProfit >= target)
			{
				ClosePosition();
				return true;
			}
		}
	}

	if (EnableTrailing)
	{
		if (openProfit >= TrailingProfitMoney)
		{
			_moneyTrailingActive = true;
			if (openProfit > _moneyTrailingPeak)
			_moneyTrailingPeak = openProfit;
		}

		if (_moneyTrailingActive)
		{
			if (openProfit > _moneyTrailingPeak)
			_moneyTrailingPeak = openProfit;

			if (openProfit <= _moneyTrailingPeak - TrailingLossMoney)
			{
				ClosePosition();
				return true;
			}
		}
	}

	return false;
}

private void ApplyBreakEven(decimal closePrice)
{
	if (!UseBreakEven || _averagePrice == 0m)
	return;

	var pipSize = GetPipSize();
	if (pipSize <= 0m)
	return;

	var triggerDistance = BreakEvenTriggerPips * pipSize;
	if (triggerDistance <= 0m)
	return;

	if (Position > 0m)
	{
		var profitDistance = closePrice - _averagePrice;
		if (profitDistance >= triggerDistance)
		{
			var offset = BreakEvenOffsetPips * pipSize;
			var candidate = _averagePrice + offset;
			if (!_longStopPrice.HasValue || candidate > _longStopPrice.Value)
			_longStopPrice = candidate;
		}
	}
	else if (Position < 0m)
	{
		var profitDistance = _averagePrice - closePrice;
		if (profitDistance >= triggerDistance)
		{
			var offset = BreakEvenOffsetPips * pipSize;
			var candidate = _averagePrice - offset;
			if (!_shortStopPrice.HasValue || candidate < _shortStopPrice.Value)
			_shortStopPrice = candidate;
		}
	}
}

private void ApplyTrailingStop(decimal closePrice)
{
	var pipSize = GetPipSize();
	if (pipSize <= 0m)
	return;

	var trailingDistance = TrailingStopPips * pipSize;
	if (trailingDistance <= 0m)
	return;

	if (Position > 0m)
	{
		var profitDistance = closePrice - _averagePrice;
		if (profitDistance >= trailingDistance)
		{
			var candidate = closePrice - trailingDistance;
			if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
			_longTrailingStop = candidate;
		}

		if (_longTrailingStop.HasValue && closePrice <= _longTrailingStop.Value)
		ClosePosition();
	}
	else if (Position < 0m)
	{
		var profitDistance = _averagePrice - closePrice;
		if (profitDistance >= trailingDistance)
		{
			var candidate = closePrice + trailingDistance;
			if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
			_shortTrailingStop = candidate;
		}

		if (_shortTrailingStop.HasValue && closePrice >= _shortTrailingStop.Value)
		ClosePosition();
	}
}

private void CheckInitialStops(decimal closePrice)
{
	if (Position > 0m)
	{
		if (_longStopPrice.HasValue && closePrice <= _longStopPrice.Value)
		{
			ClosePosition();
			return;
		}

		if (_longTargetPrice.HasValue && closePrice >= _longTargetPrice.Value)
		ClosePosition();
	}
	else if (Position < 0m)
	{
		if (_shortStopPrice.HasValue && closePrice >= _shortStopPrice.Value)
		{
			ClosePosition();
			return;
		}

		if (_shortTargetPrice.HasValue && closePrice <= _shortTargetPrice.Value)
		ClosePosition();
	}
}

private void InitializeProtectionForDirection(decimal position)
{
	if (position > 0m)
	{
		var pipSize = GetPipSize();
		if (pipSize <= 0m)
		return;

		_longStopPrice = StopLossPips > 0m ? _averagePrice - StopLossPips * pipSize : null;
		_longTargetPrice = TakeProfitPips > 0m ? _averagePrice + TakeProfitPips * pipSize : null;
		_longTrailingStop = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
		_shortTrailingStop = null;
	}
	else if (position < 0m)
	{
		var pipSize = GetPipSize();
		if (pipSize <= 0m)
		return;

		_shortStopPrice = StopLossPips > 0m ? _averagePrice + StopLossPips * pipSize : null;
		_shortTargetPrice = TakeProfitPips > 0m ? _averagePrice - TakeProfitPips * pipSize : null;
		_shortTrailingStop = null;
		_longStopPrice = null;
		_longTargetPrice = null;
		_longTrailingStop = null;
	}
}

private void ResetProtectionState()
{
	_averagePrice = 0m;
	_longStopPrice = null;
	_shortStopPrice = null;
	_longTargetPrice = null;
	_shortTargetPrice = null;
	_longTrailingStop = null;
	_shortTrailingStop = null;
	ResetMoneyTrailing();
}

private void ResetMoneyTrailing()
{
	_moneyTrailingActive = false;
	_moneyTrailingPeak = 0m;
}

private decimal GetManualVolume()
{
	var lotVolume = OrderLots * NumberOfTrades;
	return lotVolume > 0m ? lotVolume : 0m;
}

private decimal GetOpenProfit(decimal closePrice)
{
	var priceStep = Security?.PriceStep ?? 0m;
	var stepPrice = Security?.StepPrice ?? 0m;

	if (priceStep > 0m && stepPrice > 0m && _averagePrice != 0m)
	{
		var diff = closePrice - _averagePrice;
		var steps = diff / priceStep;
		return steps * stepPrice * Position;
	}

	return (closePrice - _averagePrice) * Position;
}

private decimal GetPipSize()
{
	if (_pipInitialized)
	return _pipSize;

	var security = Security;
	var step = security?.MinPriceStep ?? 0m;
	if (step <= 0m)
	step = security?.PriceStep ?? 0m;

	if (step <= 0m)
	step = 0.0001m;

	var decimals = security?.Decimals ?? 0;
	var adjust = decimals is 3 or 5 ? 10m : 1m;

	_pipSize = step * adjust;
	if (_pipSize <= 0m)
	_pipSize = step;

	if (_pipSize <= 0m)
	_pipSize = 0.0001m;

	_pipInitialized = true;
	return _pipSize;
}
}

