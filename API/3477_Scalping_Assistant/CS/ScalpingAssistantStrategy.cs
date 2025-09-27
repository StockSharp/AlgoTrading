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
/// Strategy that mirrors the "Scalping Assistant" MetaTrader expert.
/// It manages existing positions by placing initial protective orders
/// and shifting the stop-loss to break-even once price advances enough.
/// </summary>
public class ScalpingAssistantStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;

	private Order _stopOrder;
	private Order _takeOrder;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private bool _longBreakEvenApplied;
	private bool _shortBreakEvenApplied;
	private decimal _previousPosition;

	/// <summary>
	/// Initial stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit distance in price steps required before break-even activates.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Extra distance in price steps added when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ScalpingAssistantStrategy"/>.
	/// </summary>
	public ScalpingAssistantStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Stop-loss (points)", "Initial stop-loss distance in price steps.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Take-profit (points)", "Initial take-profit distance in price steps.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 20m);

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Break-even trigger (points)", "Profit in price steps required to move the stop to break-even.", "Break-even")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 5m)
			.SetNotNegative()
			.SetDisplay("Break-even offset (points)", "Extra distance added when the stop is shifted to break-even.", "Break-even")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 1m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelProtectionOrders();
		_lastBid = null;
		_lastAsk = null;
		_longBreakEvenApplied = false;
		_shortBreakEvenApplied = false;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security?.PriceStep is not decimal step || step <= 0m)
			throw new InvalidOperationException("Security price step must be specified and positive.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		if (Position > 0m)
		{
			// Attach protection to an already opened long position.
			RegisterInitialProtection(true);
		}
		else if (Position < 0m)
		{
			// Attach protection to an already opened short position.
			RegisterInitialProtection(false);
		}

		_previousPosition = Position;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_longBreakEvenApplied = false;
			_shortBreakEvenApplied = false;
		}
		else if (_previousPosition <= 0m && Position > 0m)
		{
			// A new long position appeared - re-register protection.
			CancelProtectionOrders();
			_shortBreakEvenApplied = false;
			_longBreakEvenApplied = false;
			RegisterInitialProtection(true);
		}
		else if (_previousPosition >= 0m && Position < 0m)
		{
			// A new short position appeared - re-register protection.
			CancelProtectionOrders();
			_longBreakEvenApplied = false;
			_shortBreakEvenApplied = false;
			RegisterInitialProtection(false);
		}

		_previousPosition = Position;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid != null)
			_lastBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask != null)
			_lastAsk = (decimal)ask;

		UpdateBreakEven();
	}

	private void UpdateBreakEven()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Security?.PriceStep is not decimal step || step <= 0m)
			return;

		if (Position > 0m)
		{
			if (_lastBid is not decimal bid)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			var triggerDistance = BreakEvenTriggerPoints * step;
			if (bid - entryPrice < triggerDistance)
				return;

			if (_longBreakEvenApplied)
				return;

			var stopPrice = entryPrice + BreakEvenOffsetPoints * step;
			ApplyBreakEven(true, stopPrice);
		}
		else if (Position < 0m)
		{
			if (_lastAsk is not decimal ask)
				return;

			var entryPrice = PositionPrice;
			if (entryPrice <= 0m)
				return;

			var triggerDistance = BreakEvenTriggerPoints * step;
			if (entryPrice - ask < triggerDistance)
				return;

			if (_shortBreakEvenApplied)
				return;

			var stopPrice = entryPrice - BreakEvenOffsetPoints * step;
			ApplyBreakEven(false, stopPrice);
		}
	}

	private void ApplyBreakEven(bool isLongPosition, decimal stopPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (stopPrice <= 0m)
			return;

		CancelStopOrder();

		_stopOrder = isLongPosition
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);

		if (isLongPosition)
		{
			_longBreakEvenApplied = true;
		}
		else
		{
			_shortBreakEvenApplied = true;
		}
	}

	private void RegisterInitialProtection(bool isLongPosition)
	{
		if (Security?.PriceStep is not decimal step || step <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		CancelProtectionOrders();

		if (StopLossPoints > 0m)
		{
			var stopPrice = isLongPosition
				? entryPrice - StopLossPoints * step
				: entryPrice + StopLossPoints * step;

			if (stopPrice > 0m)
			{
				_stopOrder = isLongPosition
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);
			}
		}

		if (TakeProfitPoints > 0m)
		{
			var takePrice = isLongPosition
				? entryPrice + TakeProfitPoints * step
				: entryPrice - TakeProfitPoints * step;

			if (takePrice > 0m)
			{
				_takeOrder = isLongPosition
					? SellLimit(volume, takePrice)
					: BuyLimit(volume, takePrice);
			}
		}
	}

	private void CancelProtectionOrders()
	{
		CancelStopOrder();
		CancelTakeOrder();
	}

	private void CancelStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
	}

	private void CancelTakeOrder()
	{
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_takeOrder = null;
	}
}

