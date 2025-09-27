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
/// High-level implementation of the "ARD Order Management" expert advisor.
/// Allows manual commands (buy, sell, close, modify) while automatically managing stop-loss and take-profit orders.
/// </summary>
public class ArdOrderManagementCommandStrategy : Strategy
{
	private readonly StrategyParam<int> _slippageSteps;
	private readonly StrategyParam<int> _lotDecimals;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _lotSizeDivisor;
	private readonly StrategyParam<decimal> _modifyStopLossPoints;
	private readonly StrategyParam<decimal> _modifyTakeProfitPoints;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<string> _orderComment;
	private readonly StrategyParam<ArdOrderCommands> _command;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;
	private Sides? _pendingEntrySide;
	private decimal _pendingEntryVolume;
	private decimal _pendingStopPoints;
	private decimal _pendingTakePoints;

	/// <summary>
	/// Order commands supported by <see cref="ArdOrderManagementCommandStrategy"/>.
	/// Mirrors the constants used by the original MQL expert advisor.
	/// </summary>
	public enum ArdOrderCommands
	{
		/// <summary>
		/// No action requested.
		/// </summary>
		None = 0,

		/// <summary>
		/// Open a long position.
		/// </summary>
		Buy = 1,

		/// <summary>
		/// Open a short position.
		/// </summary>
		Sell = 2,

		/// <summary>
		/// Close every open position on the primary symbol.
		/// </summary>
		Close = 3,

		/// <summary>
		/// Rebuild stop-loss and take-profit orders around the active position.
		/// </summary>
		Modify = 4,
	}

	/// <summary>
	/// Accepted execution slippage in price steps.
	/// </summary>
	public int SlippageSteps
	{
		get => _slippageSteps.Value;
		set => _slippageSteps.Value = value;
	}

	/// <summary>
	/// Number of decimal places used when rounding the trade volume.
	/// </summary>
	public int LotDecimals
	{
		get => _lotDecimals.Value;
		set => _lotDecimals.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Divisor used to transform free margin into volume units.
	/// </summary>
	public decimal LotSizeDivisor
	{
		get => _lotSizeDivisor.Value;
		set => _lotSizeDivisor.Value = value;
	}

	/// <summary>
	/// Stop-loss distance applied when executing a modify command.
	/// </summary>
	public decimal ModifyStopLossPoints
	{
		get => _modifyStopLossPoints.Value;
		set => _modifyStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance applied when executing a modify command.
	/// </summary>
	public decimal ModifyTakeProfitPoints
	{
		get => _modifyTakeProfitPoints.Value;
		set => _modifyTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum trade volume enforced by the money management rules.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Textual comment stored for every submitted order.
	/// </summary>
	public string OrderComment
	{
		get => _orderComment.Value;
		set => _orderComment.Value = value;
	}

	/// <summary>
	/// Command that will be executed on the next market data tick.
	/// </summary>
	public ArdOrderCommands Command
	{
		get => _command.Value;
		set => _command.Value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ArdOrderManagementCommandStrategy"/>.
	/// </summary>
	public ArdOrderManagementCommandStrategy()
	{
		_slippageSteps = Param(nameof(SlippageSteps), 4)
			.SetNotNegative()
			.SetDisplay("Slippage (steps)", "Accepted execution slippage expressed in price steps.", "Order management");

		_lotDecimals = Param(nameof(LotDecimals), 1)
			.SetNotNegative()
			.SetDisplay("Lot decimals", "Number of decimal places used to round the calculated volume.", "Order management");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Initial stop-loss distance from the entry price, measured in price points.", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Initial take-profit distance from the entry price, measured in price points.", "Protection");

		_lotSizeDivisor = Param(nameof(LotSizeDivisor), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Lot size divisor", "Divides free margin before converting the value into lots.", "Money management");

		_modifyStopLossPoints = Param(nameof(ModifyStopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Modify stop loss (points)", "New stop-loss distance applied by the modify command.", "Protection");

		_modifyTakeProfitPoints = Param(nameof(ModifyTakeProfitPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Modify take profit (points)", "New take-profit distance applied by the modify command.", "Protection");

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum volume", "Lowest volume allowed after rounding.", "Money management");

		_orderComment = Param(nameof(OrderComment), "Placing Order")
			.SetDisplay("Order comment", "Comment attached to every order for easier tracking.", "General");

		_command = Param(nameof(Command), ArdOrderCommands.None)
			.SetDisplay("Command", "Operation executed on the next tick (set back to None after completion).", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_pendingEntrySide = null;
		_pendingEntryVolume = 0m;
		_pendingStopPoints = 0m;
		_pendingTakePoints = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
			DrawOwnTrades(area);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_lastBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_lastAsk = Convert.ToDecimal(askValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var command = Command;
		if (command != ArdOrderCommands.None)
			ExecuteCommand(command);

		TrySubmitPendingEntry();
	}

	private void ExecuteCommand(ArdOrderCommands command)
	{
		switch (command)
		{
			case ArdOrderCommands.Buy:
				ScheduleEntry(Sides.Buy, StopLossPoints, TakeProfitPoints);
				break;
			case ArdOrderCommands.Sell:
				ScheduleEntry(Sides.Sell, StopLossPoints, TakeProfitPoints);
				break;
			case ArdOrderCommands.Close:
				HandleCloseCommand();
				break;
			case ArdOrderCommands.Modify:
				HandleModifyCommand();
				break;
			default:
				break;
		}

		Command = ArdOrderCommands.None;
	}

	private void ScheduleEntry(Sides side, decimal stopPoints, decimal takePoints)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarn($"Calculated volume {volume} is not positive; skipping {side} command.");
			return;
		}

		_pendingEntrySide = side;
		_pendingEntryVolume = volume;
		_pendingStopPoints = stopPoints;
		_pendingTakePoints = takePoints;

		RequestFlatPosition();
		TrySubmitPendingEntry();
	}

	private void RequestFlatPosition()
	{
		CancelProtectionOrders();
		CancelActiveOrders();

		if (Position != 0)
		{
			LogInfo("Requesting flat position before executing the next command.");
			ClosePosition();
		}
	}

	private void TrySubmitPendingEntry()
	{
		if (_pendingEntrySide is not Sides side)
			return;

		if (Position != 0)
			return;

		if (_pendingEntryVolume <= 0m)
		{
			LogWarn("Pending entry volume is not positive; clearing the request.");
			_pendingEntrySide = null;
			return;
		}

		if (!TryGetQuotePrices(out var bid, out var ask))
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarn("Price step is undefined; cannot submit the entry order.");
			return;
		}

		if (side == Sides.Buy)
		{
			LogInfo($"Submitting buy market order ({_pendingEntryVolume}) with comment '{OrderComment}'.");
			BuyMarket(_pendingEntryVolume);
		}
		else
		{
			LogInfo($"Submitting sell market order ({_pendingEntryVolume}) with comment '{OrderComment}'.");
			SellMarket(_pendingEntryVolume);
		}

		PlaceProtectionOrders(side, _pendingEntryVolume, _pendingStopPoints, _pendingTakePoints, bid, ask);

		_pendingEntrySide = null;
	}

	private void HandleCloseCommand()
	{
		_pendingEntrySide = null;
		_pendingEntryVolume = 0m;
		_pendingStopPoints = 0m;
		_pendingTakePoints = 0m;

		CancelProtectionOrders();
		CancelActiveOrders();

		if (Position != 0)
		{
			LogInfo("Closing active position on user request.");
			ClosePosition();
		}
		else
		{
			LogInfo("Close command received but there is no active position.");
		}
	}

	private void HandleModifyCommand()
	{
		if (Position == 0)
		{
			LogInfo("Modify command ignored because there is no position to protect.");
			return;
		}

		if (!TryGetQuotePrices(out var bid, out var ask))
			return;

		var side = Position > 0 ? Sides.Buy : Sides.Sell;
		var volume = Math.Abs(Position);

		LogInfo("Rebuilding protective orders using modify distances.");
		PlaceProtectionOrders(side, volume, ModifyStopLossPoints, ModifyTakeProfitPoints, bid, ask);
	}

	private void PlaceProtectionOrders(Sides side, decimal volume, decimal stopPoints, decimal takePoints, decimal bid, decimal ask)
	{
		CancelProtectionOrders();

		if (volume <= 0m)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarn("Cannot place protection orders without a valid price step.");
			return;
		}

		if (side == Sides.Buy)
		{
			if (stopPoints > 0m)
			{
				var stopPrice = bid - stopPoints * priceStep;
				if (stopPrice > 0m)
					_stopLossOrder = SellStop(volume, stopPrice);
			}

			if (takePoints > 0m)
			{
				var takePrice = ask + takePoints * priceStep;
				if (takePrice > 0m)
					_takeProfitOrder = SellLimit(volume, takePrice);
			}
		}
		else
		{
			if (stopPoints > 0m)
			{
				var stopPrice = ask + stopPoints * priceStep;
				if (stopPrice > 0m)
					_stopLossOrder = BuyStop(volume, stopPrice);
			}

			if (takePoints > 0m)
			{
				var takePrice = bid - takePoints * priceStep;
				if (takePrice > 0m)
					_takeProfitOrder = BuyLimit(volume, takePrice);
			}
		}

		if (_stopLossOrder != null)
			LogInfo($"Stop-loss order registered at {_stopLossOrder.Price}.");

		if (_takeProfitOrder != null)
			LogInfo($"Take-profit order registered at {_takeProfitOrder.Price}.");
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null && _stopLossOrder.State == OrderStates.Active)
			CancelOrder(_stopLossOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_stopLossOrder = null;
		_takeProfitOrder = null;
	}

	private bool TryGetQuotePrices(out decimal bid, out decimal ask)
	{
		bid = _lastBid ?? 0m;
		ask = _lastAsk ?? 0m;

		if (bid <= 0m && ask <= 0m)
			return false;

		if (bid <= 0m)
			bid = ask;

		if (ask <= 0m)
			ask = bid;

		return bid > 0m && ask > 0m;
	}

	private decimal CalculateOrderVolume()
	{
		var freeMargin = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (freeMargin <= 0m)
			return MinimumVolume;

		var divisor = LotSizeDivisor;
		if (divisor <= 0m)
		{
			LogWarn("Lot size divisor must be greater than zero; fallback to minimum volume.");
			return MinimumVolume;
		}

		var rawVolume = freeMargin / divisor / 1000m;
		var decimals = Math.Max(0, LotDecimals);
		var rounded = Math.Round(rawVolume, decimals, MidpointRounding.AwayFromZero);

		if (rounded < MinimumVolume)
			rounded = MinimumVolume;

		return rounded;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelProtectionOrders();
			TrySubmitPendingEntry();
		}
	}
}
