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
/// Manual risk-managed trading strategy that mirrors the AIS4 Trade Machine expert advisor.
/// </summary>
public class AisTradeMachineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ManualCommands> _command;
	private readonly StrategyParam<decimal> _stopPrice;
	private readonly StrategyParam<decimal> _takePrice;
	private readonly StrategyParam<decimal> _accountReserve;
	private readonly StrategyParam<decimal> _orderReserve;

	private ManualCommands _lastCommand;
	private bool _commandHandled;
	private decimal _peakEquity;
	private Order _stopOrder;
	private Order _takeOrder;
	private decimal _currentStopPrice;
	private decimal _currentTakePrice;

	/// <summary>
	/// Available manual commands.
	/// </summary>
	public enum ManualCommands
	{
		/// <summary>
		/// Idle state, no action is performed.
		/// </summary>
		Wait = -1,

		/// <summary>
		/// Send a market buy order.
		/// </summary>
		Buy = 0,

		/// <summary>
		/// Send a market sell order.
		/// </summary>
		Sell = 1,

		/// <summary>
		/// Update stop-loss and take-profit orders for the active position.
		/// </summary>
		Modify = 6,

		/// <summary>
		/// Close the active position immediately.
		/// </summary>
		Close = 7
	}

/// <summary>
/// Initializes a new instance of the <see cref="AisTradeMachineStrategy"/> class.
/// </summary>
public AisTradeMachineStrategy()
{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles used to read current prices.", "Market Data");

	_command = Param(nameof(Command), ManualCommands.Wait)
	.SetDisplay("Command", "Manual command to execute. The value resets to Wait after handling.", "Control");

	_stopPrice = Param(nameof(StopPrice), 0m)
	.SetRange(0m, 100000000m)
	.SetDisplay("Stop Price", "Absolute stop-loss level supplied by the operator.", "Control");

	_takePrice = Param(nameof(TakePrice), 0m)
	.SetRange(0m, 100000000m)
	.SetDisplay("Take Price", "Absolute take-profit level supplied by the operator.", "Control");

	_accountReserve = Param(nameof(AccountReserve), 0.2m)
	.SetRange(0m, 0.9m)
	.SetDisplay("Account Reserve", "Fraction of equity kept as reserve (0-0.9).", "Risk Management")
	.SetCanOptimize(true)
	.SetOptimize(0.1m, 0.5m, 0.05m);

	_orderReserve = Param(nameof(OrderReserve), 0.04m)
	.SetRange(0.001m, 0.5m)
	.SetDisplay("Order Reserve", "Fraction of equity allocated to a single trade.", "Risk Management")
	.SetCanOptimize(true)
	.SetOptimize(0.01m, 0.1m, 0.01m);
}

/// <summary>
/// Candle type used to observe price updates.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Manual command parameter.
/// </summary>
public ManualCommands Command
{
	get => _command.Value;
	set => _command.Value = value;
}

/// <summary>
/// Stop-loss price parameter.
/// </summary>
public decimal StopPrice
{
	get => _stopPrice.Value;
	set => _stopPrice.Value = value;
}

/// <summary>
/// Take-profit price parameter.
/// </summary>
public decimal TakePrice
{
	get => _takePrice.Value;
	set => _takePrice.Value = value;
}

/// <summary>
/// Fraction of equity kept as reserve.
/// </summary>
public decimal AccountReserve
{
	get => _accountReserve.Value;
	set => _accountReserve.Value = value;
}

/// <summary>
/// Fraction of equity allocated to a single trade.
/// </summary>
public decimal OrderReserve
{
	get => _orderReserve.Value;
	set => _orderReserve.Value = value;
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

	_lastCommand = Command;
	_commandHandled = true;
	_peakEquity = 0m;
	_currentStopPrice = 0m;
	_currentTakePrice = 0m;

	CancelProtectionOrders(resetLevels: true);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_peakEquity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawOwnTrades(area);
	}
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
	base.OnOwnTradeReceived(trade);

	if (Position == 0m)
	{
		CancelProtectionOrders(resetLevels: true);
	}
else
{
	ApplyProtection(Position > 0m, Math.Abs(Position));
}
}

/// <inheritdoc />
protected override void OnStopped()
{
	CancelProtectionOrders(resetLevels: true);

	base.OnStopped();
}

private void ProcessCandle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	UpdatePeakEquity();

	var command = Command;

	if (_lastCommand != command)
	_commandHandled = false;

	if (_commandHandled)
	{
		_lastCommand = command;
		return;
	}

switch (command)
{
	case ManualCommands.Wait:
	_commandHandled = true;
	break;

	case ManualCommands.Buy:
	case ManualCommands.Sell:
	TryHandleEntry(command, candle);
	ResetCommandToWait();
	break;

	case ManualCommands.Modify:
	TryHandleModify(candle);
	ResetCommandToWait();
	break;

	case ManualCommands.Close:
	HandleCloseCommand();
	ResetCommandToWait();
	break;

	default:
	LogWarning($"Unknown command value {(int)command}. Resetting to Wait.");
	ResetCommandToWait();
	break;
}

_lastCommand = Command;
}

private void TryHandleEntry(ManualCommands command, ICandleMessage candle)
{
	if (!TryGetEquity(out var equity))
	{
		LogWarning("Portfolio value is not available. Entry command ignored.");
		return;
	}

if (!TryComputeRiskBudget(equity, out var riskBudget))
return;

if (Position != 0m)
{
	LogWarning("Entry command ignored because an open position already exists.");
	return;
}

if (Security is null)
{
	LogWarning("Security information is not available. Entry command ignored.");
	return;
}

var isLong = command == ManualCommands.Buy;
var price = candle.ClosePrice;
var stopPrice = StopPrice;
var takePrice = TakePrice;

if (!ValidateProtectionLevels(isLong, price, stopPrice, takePrice, requireBoth: true))
return;

var volume = CalculateVolume(price, stopPrice, riskBudget);
if (volume <= 0m)
return;

if (isLong)
{
	BuyMarket(volume);
	LogInfo($"Buy command executed at price {price} with volume {volume}.");
}
else
{
	SellMarket(volume);
	LogInfo($"Sell command executed at price {price} with volume {volume}.");
}

_currentStopPrice = stopPrice;
_currentTakePrice = takePrice;

ApplyProtection(isLong, volume);
}

private void TryHandleModify(ICandleMessage candle)
{
	if (Position == 0m)
	{
		LogWarning("Modify command ignored because there is no active position.");
		return;
	}

var isLong = Position > 0m;
var price = candle.ClosePrice;
var stopPrice = StopPrice > 0m ? StopPrice : _currentStopPrice;
var takePrice = TakePrice > 0m ? TakePrice : _currentTakePrice;

if (!ValidateProtectionLevels(isLong, price, stopPrice, takePrice, requireBoth: false))
return;

_currentStopPrice = stopPrice;
_currentTakePrice = takePrice;

ApplyProtection(isLong, Math.Abs(Position));

LogInfo($"Protection updated for {(isLong ? "long" : "short")} position. Stop={stopPrice}, Take={takePrice}.");
}

private void HandleCloseCommand()
{
	var position = Position;

	if (position > 0m)
	{
		SellMarket(position);
		LogInfo("Close command sent for long position.");
	}
else if (position < 0m)
{
	BuyMarket(Math.Abs(position));
	LogInfo("Close command sent for short position.");
}
else
{
	LogInfo("Close command ignored because there is no open position.");
}

CancelProtectionOrders(resetLevels: true);
}

private bool ValidateProtectionLevels(bool isLong, decimal price, decimal stopPrice, decimal takePrice, bool requireBoth)
{
	if (requireBoth)
	{
		if (stopPrice <= 0m || takePrice <= 0m)
		{
			LogWarning("Both stop and take prices must be provided before sending an entry command.");
			return false;
		}
}
else
{
	if (stopPrice <= 0m && takePrice <= 0m)
	{
		LogWarning("Modify command ignored because no price levels were supplied.");
		return false;
	}
}

var minDistance = GetMinimalDistance();

if (stopPrice > 0m)
{
	if (isLong)
	{
		if (stopPrice >= price - minDistance)
		{
			LogWarning($"Stop price {stopPrice} is too close to the current price {price} for a long position.");
			return false;
		}
}
else
{
	if (stopPrice <= price + minDistance)
	{
		LogWarning($"Stop price {stopPrice} is too close to the current price {price} for a short position.");
		return false;
	}
}
}

if (takePrice > 0m)
{
	if (isLong)
	{
		if (takePrice <= price + minDistance)
		{
			LogWarning($"Take price {takePrice} is too close to the current price {price} for a long position.");
			return false;
		}
}
else
{
	if (takePrice >= price - minDistance)
	{
		LogWarning($"Take price {takePrice} is too close to the current price {price} for a short position.");
		return false;
	}
}
}

return true;
}

private decimal CalculateVolume(decimal price, decimal stopPrice, decimal riskBudget)
{
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	{
		LogWarning("PriceStep is not available for the security. Unable to size the order.");
		return 0m;
	}

var stepPrice = Security?.StepPrice ?? priceStep;
if (stepPrice <= 0m)
{
	LogWarning("StepPrice is not available for the security. Unable to size the order.");
	return 0m;
}

var stopDistance = Math.Abs(price - stopPrice);
if (stopDistance <= 0m)
{
	LogWarning("Stop distance must be positive to compute the trade volume.");
	return 0m;
}

var riskPerUnit = stopDistance / priceStep * stepPrice;
if (riskPerUnit <= 0m)
{
	LogWarning("Unable to compute risk per unit. Check instrument metadata.");
	return 0m;
}

var rawVolume = riskBudget / riskPerUnit;
var adjustedVolume = AdjustVolume(Math.Abs(rawVolume));

if (adjustedVolume <= 0m)
{
	LogWarning("Calculated volume is below the instrument minimum. Command skipped.");
	return 0m;
}

return adjustedVolume;
}

private decimal AdjustVolume(decimal volume)
{
	if (Security is null)
	return volume;

	var step = Security.VolumeStep ?? 0m;
	if (step > 0m)
	volume = step * Math.Floor(volume / step);

	var minVolume = Security.MinVolume ?? 0m;
	if (minVolume > 0m && volume < minVolume)
	return 0m;

	var maxVolume = Security.MaxVolume;
	if (maxVolume != null && volume > maxVolume.Value)
	volume = maxVolume.Value;

	return volume;
}

private void ApplyProtection(bool isLong, decimal volumeHint)
{
	CancelProtectionOrders();

	var volume = Math.Abs(Position);
	if (volume <= 0m)
	volume = AdjustVolume(Math.Abs(volumeHint));
	else
	volume = AdjustVolume(volume);

	if (volume <= 0m)
	return;

	if (_currentStopPrice > 0m)
	{
		_stopOrder = isLong
		? SellStop(volume, _currentStopPrice)
		: BuyStop(volume, _currentStopPrice);
	}

if (_currentTakePrice > 0m)
{
	_takeOrder = isLong
	? SellLimit(volume, _currentTakePrice)
	: BuyLimit(volume, _currentTakePrice);
}
}

private void CancelProtectionOrders(bool resetLevels = false)
{
	CancelOrderIfActive(ref _stopOrder);
	CancelOrderIfActive(ref _takeOrder);

	if (resetLevels)
	{
		_currentStopPrice = 0m;
		_currentTakePrice = 0m;
	}
}

private void CancelOrderIfActive(ref Order order)
{
	if (order == null)
	return;

	if (order.State.IsActive())
	CancelOrder(order);

	order = null;
}

private bool TryGetEquity(out decimal equity)
{
	equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	return equity > 0m;
}

private bool TryComputeRiskBudget(decimal equity, out decimal riskBudget)
{
	var orderReserve = Math.Max(0m, OrderReserve);
	var accountReserve = Math.Clamp(AccountReserve, 0m, 0.99m);

	if (orderReserve <= 0m)
	{
		LogWarning("OrderReserve must be positive to execute trades.");
		riskBudget = 0m;
		return false;
	}

if (_peakEquity <= 0m)
_peakEquity = equity;

var capital = _peakEquity * (1m - accountReserve);
var equityReserve = equity - capital;

riskBudget = equity * orderReserve;

if (equityReserve < riskBudget)
{
	LogWarning($"Command skipped because equity reserve {equityReserve} is below the risk budget {riskBudget}.");
	return false;
}

return true;
}

private decimal GetMinimalDistance()
{
	var step = Security?.PriceStep ?? 0m;
	return step > 0m ? step : 0m;
}

private void UpdatePeakEquity()
{
	var equity = Portfolio?.CurrentValue ?? 0m;
	if (equity <= 0m)
	return;

	if (equity > _peakEquity)
	_peakEquity = equity;
}

private void ResetCommandToWait()
{
	Command = ManualCommands.Wait;
	_commandHandled = true;
	_lastCommand = ManualCommands.Wait;
}
}
