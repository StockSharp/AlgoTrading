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
/// Hedged tunnel strategy converted from the "Tunnel gen4" MetaTrader expert.
/// It opens a buy/sell pair, doubles the position when price travels by a fixed number of pips,
/// and closes the entire basket once the second anchor is breached again.
/// </summary>
public class TunnelGen4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _stepPips;

	private readonly Dictionary<Order, OrderIntents> _orderIntents = new();
	private readonly HashSet<Order> _entryOrders = new();

	private decimal _pipValue;
	private decimal _stepOffset;
	private decimal _firstEntryPrice;
	private decimal _secondEntryPrice;
	private bool _waitingForSecondEntry;
	private Order _secondEntryOrder;
	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private decimal _longExposure;
	private decimal _shortExposure;
	private bool _isClosing;

	private enum OrderIntents
	{
		OpenLong,
		OpenShort,
		CloseLong,
		CloseShort
	}

	/// <summary>
	/// Initial order volume for the hedged pair.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Step distance expressed in pips.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed difference when comparing exposure volumes.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public TunnelGen4Strategy()
	{
		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
			.SetNotNegative()
			.SetDisplay("Volume Tolerance", "Tolerance when comparing exposure volumes", "Trading");

		_startVolume = Param(nameof(StartVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Start Volume", "Initial hedge volume", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_stepPips = Param(nameof(StepPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Step (pips)", "Distance between tunnel anchors", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_orderIntents.Clear();
		_entryOrders.Clear();
		_pipValue = 0m;
		_stepOffset = 0m;
		_firstEntryPrice = 0m;
		_secondEntryPrice = 0m;
		_waitingForSecondEntry = false;
		_secondEntryOrder = null;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_longExposure = 0m;
		_shortExposure = 0m;
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipValue = CalculatePipValue();
		_stepOffset = StepPips * _pipValue;

		if (_stepOffset <= 0m)
		throw new InvalidOperationException("Step offset must be positive.");

		ValidateVolume(StartVolume);
		ValidateVolume(StartVolume * 2m);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order is not { } order || !_orderIntents.TryGetValue(order, out var intent))
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		switch (intent)
		{
			case OrderIntents.OpenLong:
				_longExposure += volume;
				if (_firstEntryPrice == 0m)
				_firstEntryPrice = price;
				break;
			case OrderIntents.OpenShort:
				_shortExposure += volume;
				if (_firstEntryPrice == 0m)
				_firstEntryPrice = price;
				break;
			case OrderIntents.CloseLong:
				_longExposure = Math.Max(0m, _longExposure - volume);
				break;
			case OrderIntents.CloseShort:
				_shortExposure = Math.Max(0m, _shortExposure - volume);
				break;
		}

		if (order == _secondEntryOrder && intent is OrderIntents.OpenLong or OrderIntents.OpenShort && _secondEntryPrice == 0m)
		{
			_secondEntryPrice = price;
			_waitingForSecondEntry = false;
		}

		if (order.Balance <= 0m || IsOrderCompleted(order))
		{
			_orderIntents.Remove(order);
			_entryOrders.Remove(order);
			if (order == _secondEntryOrder)
			_secondEntryOrder = null;
		}

		if (_isClosing && !HasExposure() && !HasActiveExitOrders())
		ResetCycle();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		if (_hasBestBid && _hasBestAsk)
		ProcessQuotes();
	}

	private void ProcessQuotes()
	{
		CleanupCompletedOrders();

		if (_isClosing)
		{
			if (!HasExposure() && !HasActiveExitOrders())
			ResetCycle();

			return;
		}

		if (!HasExposure() && !HasActiveEntryOrders())
		{
			EnterInitialHedge();
			return;
		}

		if (_firstEntryPrice == 0m)
		return;

		if (_secondEntryPrice == 0m && !_waitingForSecondEntry)
		{
			var upperTrigger = _firstEntryPrice + _stepOffset;
			var lowerTrigger = _firstEntryPrice - _stepOffset;

			if (_bestBid >= upperTrigger)
			OpenSecondStage(Sides.Sell);
			else if (_bestAsk <= lowerTrigger)
			OpenSecondStage(Sides.Buy);
		}
		else if (_secondEntryPrice > 0m)
		{
			var exitUpper = _secondEntryPrice + _stepOffset;
			var exitLower = _secondEntryPrice - _stepOffset;

			if (_bestBid >= exitUpper || _bestAsk <= exitLower)
			CloseCycle();
		}
	}

	private void EnterInitialHedge()
	{
		var volume = StartVolume;
		if (volume <= 0m)
		return;

		RegisterOrder(BuyMarket(volume), OrderIntents.OpenLong, true);
		RegisterOrder(SellMarket(volume), OrderIntents.OpenShort, true);
	}

	private void OpenSecondStage(Sides side)
	{
		var volume = StartVolume * 2m;
		if (volume <= 0m)
		return;

		Order order = side == Sides.Buy ? BuyMarket(volume) : SellMarket(volume);
		RegisterOrder(order, side == Sides.Buy ? OrderIntents.OpenLong : OrderIntents.OpenShort, true);

		if (order != null)
		{
			_waitingForSecondEntry = true;
			_secondEntryPrice = 0m;
			_secondEntryOrder = order;
		}
	}

	private void CloseCycle()
	{
		if (_isClosing)
		return;

		_isClosing = true;

		if (_longExposure > VolumeTolerance)
		RegisterOrder(SellMarket(_longExposure), OrderIntents.CloseLong, false);

		if (_shortExposure > VolumeTolerance)
		RegisterOrder(BuyMarket(_shortExposure), OrderIntents.CloseShort, false);

		if (!HasActiveExitOrders() && !HasExposure())
		ResetCycle();
	}

	private void RegisterOrder(Order order, OrderIntents intent, bool isEntry)
	{
		if (order == null)
		return;

		_orderIntents[order] = intent;

		if (isEntry)
		_entryOrders.Add(order);
	}

	private void CleanupCompletedOrders()
	{
		foreach (var order in _orderIntents.Keys.ToArray())
		{
			if (!IsOrderCompleted(order))
			continue;

			_orderIntents.Remove(order);
			_entryOrders.Remove(order);

			if (order == _secondEntryOrder)
			{
				_secondEntryOrder = null;
				if (_secondEntryPrice == 0m)
				_waitingForSecondEntry = false;
			}
		}
	}

	private bool HasActiveEntryOrders()
	{
		foreach (var order in _entryOrders)
		{
			if (!IsOrderCompleted(order))
			return true;
		}

		return false;
	}

	private bool HasActiveExitOrders()
	{
		foreach (var pair in _orderIntents)
		{
			if ((pair.Value == OrderIntents.CloseLong || pair.Value == OrderIntents.CloseShort) && !IsOrderCompleted(pair.Key))
			return true;
		}

		return false;
	}

	private bool HasExposure()
	{
		return _longExposure > VolumeTolerance || _shortExposure > VolumeTolerance;
	}

	private void ResetCycle()
	{
		_firstEntryPrice = 0m;
		_secondEntryPrice = 0m;
		_waitingForSecondEntry = false;
		_secondEntryOrder = null;
		_isClosing = false;
		_longExposure = 0m;
		_shortExposure = 0m;
	}

	private void ValidateVolume(decimal volume)
	{
		if (volume <= 0m || Security == null)
		return;

		if (Security.MinVolume is { } minVolume && volume < minVolume - VolumeTolerance)
		throw new InvalidOperationException($"Volume {volume} is less than the minimal allowed {minVolume}.");

		if (Security.MaxVolume is { } maxVolume && volume > maxVolume + VolumeTolerance)
		throw new InvalidOperationException($"Volume {volume} is greater than the maximal allowed {maxVolume}.");

		if (Security.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Round(volume / step);
			var normalized = steps * step;
			if (Math.Abs(normalized - volume) > VolumeTolerance)
			throw new InvalidOperationException($"Volume {volume} is not a multiple of the minimal step {step}.");
		}
	}

	private decimal CalculatePipValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var scaled = step;
		var digits = 0;
		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * adjust;
	}

	private static bool IsOrderCompleted(Order order)
	{
		return order.State == OrderStates.Done
		|| order.State == OrderStates.Failed
		|| order.State == OrderStates.Cancelled;
	}
}

