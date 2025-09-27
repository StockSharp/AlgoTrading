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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple hedge panel style strategy.
/// Opens market positions for configured slots when the strategy starts
/// and closes them when requested or when the strategy stops.
/// </summary>
public class SimpleHedgePanelStrategy : Strategy
{
	private readonly StrategyParam<int> _maxSlots;
	private readonly StrategyParam<int> _slotCount;
	private StrategyParam<Security>[] _slotSecurities = Array.Empty<StrategyParam<Security>>();
	private StrategyParam<decimal>[] _slotVolumes = Array.Empty<StrategyParam<decimal>>();
	private StrategyParam<bool>[] _slotIsBuy = Array.Empty<StrategyParam<bool>>();

	private bool _positionsOpened;

	/// <summary>
	/// Initializes strategy parameters that mirror the original panel controls.
	/// </summary>
	public SimpleHedgePanelStrategy()
	{
		_maxSlots = Param(nameof(MaxSlots), 5)
			.SetDisplay("Max Slots", "Maximum number of deal slots", "General")
			.SetRange(1, 50);

		EnsureSlotParameters(MaxSlots);

		_slotCount = Param(nameof(SlotCount), Math.Min(3, MaxSlots))
			.SetDisplay("Slots", "Number of deal slots", "General")
			.SetRange(1, MaxSlots);
	}

	public int MaxSlots
	{
		get => _maxSlots.Value;
		set
		{
			if (value == MaxSlots)
				return;

			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), "Max slots must be positive.");

			_maxSlots.Value = value;
			EnsureSlotParameters(value);
			_slotCount.SetRange(1, value);

			if (_slotCount.Value > value)
				_slotCount.Value = value;
		}
	}

	/// <summary>
	/// Number of slots to process, limited to the original panel range of 1-5.
	/// </summary>
	public int SlotCount
	{
		get => _slotCount.Value;
		set => _slotCount.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_positionsOpened = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		OpenConfiguredPositions();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CloseConfiguredPositions();
		base.OnStopped();
	}

	/// <summary>
	/// Opens market positions for each configured slot.
	/// </summary>
	public void OpenConfiguredPositions()
	{
		if (Portfolio == null)
		{
			LogError("Portfolio is not assigned. Unable to submit hedge orders.");
			return;
		}

		if (_positionsOpened)
		{
			LogInfo("Hedge positions already requested. Close them before opening again.");
			return;
		}

		var slotTotal = GetValidatedSlotCount();
		var hasOrders = false;

		for (var i = 0; i < slotTotal; i++)
		{
			var slotNumber = i + 1;
			var security = GetConfiguredSecurity(i);
			if (security == null)
			{
				LogInfo($"Slot {slotNumber}: no security selected. Order skipped.");
				continue;
			}

			var volume = _slotVolumes[i].Value;
			if (volume <= 0m)
			{
				LogInfo($"Slot {slotNumber}: volume must be greater than zero. Order skipped.");
				continue;
			}

			if (_slotIsBuy[i].Value)
			{
				BuyMarket(volume, security);
				LogInfo($"Slot {slotNumber}: submitted buy market order for {volume} units of {security.Id}.");
			}
			else
			{
				SellMarket(volume, security);
				LogInfo($"Slot {slotNumber}: submitted sell market order for {volume} units of {security.Id}.");
			}

			hasOrders = true;
		}

		_positionsOpened = hasOrders;

		if (!hasOrders)
		{
			LogInfo("No hedge orders were submitted because all slots were invalid.");
		}
	}

	/// <summary>
	/// Closes positions for the configured slots using market orders.
	/// </summary>
	public void CloseConfiguredPositions()
	{
		if (Portfolio == null)
		{
			return;
		}

		var slotTotal = GetValidatedSlotCount();
		var closedAny = false;

		for (var i = 0; i < slotTotal; i++)
		{
			var security = GetConfiguredSecurity(i);
			if (security == null)
			{
				continue;
			}

			var position = GetCurrentPosition(security);
			if (position == 0m)
			{
				continue;
			}

			ClosePosition(security);
			LogInfo($"Requested to close position of {position} units in {security.Id}.");
			closedAny = true;
		}

		if (closedAny)
		{
			_positionsOpened = false;
		}
	}

	private void EnsureSlotParameters(int slotLimit)
	{
		var current = _slotSecurities.Length;

		if (current >= slotLimit)
		{
			return;
		}

		Array.Resize(ref _slotSecurities, slotLimit);
		Array.Resize(ref _slotVolumes, slotLimit);
		Array.Resize(ref _slotIsBuy, slotLimit);

		for (var i = current; i < slotLimit; i++)
		{
			var slotNumber = i + 1;
			var group = $"Slot {slotNumber}";

			_slotSecurities[i] = Param<Security>($"Slot{slotNumber}Security")
				.SetDisplay($"Slot {slotNumber} Security", "Security to trade in the slot", group);

			_slotVolumes[i] = Param($"Slot{slotNumber}Volume", 0m)
				.SetDisplay($"Slot {slotNumber} Volume", "Order volume for the slot", group)
				.SetNotNegative();

			_slotIsBuy[i] = Param($"Slot{slotNumber}IsBuy", true)
				.SetDisplay($"Slot {slotNumber} Buy", "True to buy, false to sell", group);
		}
	}

/// <summary>
/// Returns a sanitized slot count within the allowed range and logs adjustments.
/// </summary>
	private int GetValidatedSlotCount()
	{
		var count = SlotCount;
		if (count < 1)
		{
			LogInfo("SlotCount is below 1. Using 1 slot.");
			return 1;
		}

		if (count > MaxSlots)
		{
			LogInfo($"SlotCount exceeds {MaxSlots}. Using {MaxSlots} slots.");
			return MaxSlots;
		}

		return count;
	}

	/// <summary>
	/// Resolves a slot security or falls back to the main strategy security.
	/// </summary>
	private Security GetConfiguredSecurity(int slotIndex)
	{
		var security = _slotSecurities[slotIndex].Value;
		if (security != null)
		{
			return security;
		}

		return Security;
	}

	/// <summary>
	/// Gets the current position for the specified security.
	/// </summary>
	private decimal GetCurrentPosition(Security security)
	{
		if (Portfolio == null)
		{
			return 0m;
		}

		var position = GetPositionValue(security, Portfolio);
		return position ?? 0m;
	}
}