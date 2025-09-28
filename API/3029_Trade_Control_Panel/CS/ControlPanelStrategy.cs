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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading helper that mirrors the original MQL control panel behavior.
/// </summary>
public class ControlPanelStrategy : Strategy
{

	private readonly StrategyParam<string> _volumeList;
	private readonly StrategyParam<decimal> _currentVolumeParam;
	private readonly StrategyParam<decimal> _breakEvenSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<int> _maxVolumePresets;

	private readonly HashSet<int> _selectedIndexes = new();
	private decimal[] _volumeOptions = Array.Empty<decimal>();
	private Order _stopOrder;
	private Order _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="ControlPanelStrategy"/> class.
	/// </summary>
	public ControlPanelStrategy()
	{
		_volumeList = Param(nameof(VolumeList), "0.01; 0.02; 0.05; 0.10; 0.20; 0.50; 1.00; 2.00; 5.00;")
			.SetDisplay("Volume Presets", "Semicolon separated lot presets", "Trading");

		_currentVolumeParam = Param(nameof(CurrentVolume), 0m)
			.SetDisplay("Current Volume", "Aggregated volume selected for next action", "Trading");

		_breakEvenSteps = Param(nameof(BreakEvenSteps), 10m)
			.SetDisplay("Break-even Steps", "Price steps added to entry when moving stop to break-even", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 0m)
			.SetDisplay("Stop-loss Steps", "Initial stop-loss distance expressed in price steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 0m)
			.SetDisplay("Take-profit Steps", "Initial take-profit distance expressed in price steps", "Risk");

		_maxVolumePresets = Param(nameof(MaxVolumePresets), 9)
			.SetRange(1, 20)
			.SetDisplay("Max Volume Presets", "Maximum number of presets parsed from the list", "Trading");

		UpdateVolumeOptions();
	}

	/// <summary>
	/// Preset volumes that can be toggled in the manual panel.
	/// </summary>
	public string VolumeList
	{
		get => _volumeList.Value;
		set
		{
			_volumeList.Value = value;
			UpdateVolumeOptions();
		}
	}

	/// <summary>
	/// Current aggregated volume that will be used for the next trade action.
	/// </summary>
	public decimal CurrentVolume
	{
		get => _currentVolumeParam.Value;
		set => _currentVolumeParam.Value = RoundVolume(Math.Max(0m, value));
	}

	/// <summary>
	/// Number of price steps added to the entry price when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenSteps
	{
		get => _breakEvenSteps.Value;
		set => _breakEvenSteps.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Initial stop-loss distance expressed in price steps. Zero disables automatic stops.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Initial take-profit distance expressed in price steps. Zero disables automatic targets.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Maximum number of volume presets parsed from the list.
	/// </summary>
	public int MaxVolumePresets
	{
		get => _maxVolumePresets.Value;
		set
		{
			var sanitized = Math.Max(1, value);
			if (_maxVolumePresets.Value == sanitized)
				return;
			_maxVolumePresets.Value = sanitized;
			UpdateVolumeOptions();
		}
	}

	/// <summary>
	/// Read-only snapshot of parsed volume presets.
	/// </summary>
	public IReadOnlyList<decimal> VolumeOptions => _volumeOptions;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Enable built-in protection manager so protective orders are handled correctly.
		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_selectedIndexes.Clear();
		_stopOrder = null;
		_takeProfitOrder = null;
		CurrentVolume = 0m;
	}

	/// <summary>
	/// Toggle a preset volume. Selected presets are accumulated just like checkbox buttons in the original panel.
	/// </summary>
	/// <param name="index">Zero-based index of the preset to toggle.</param>
	public void ToggleVolumeSelection(int index)
	{
		if ((uint)index >= (uint)_volumeOptions.Length)
			throw new ArgumentOutOfRangeException(nameof(index));

		var volume = _volumeOptions[index];
		if (volume <= 0m)
			return;

		if (_selectedIndexes.Remove(index))
		{
			// Removing the preset subtracts its volume from the aggregate value.
			CurrentVolume = Math.Max(0m, CurrentVolume - volume);
		}
		else
		{
			// Adding the preset increases the volume that the next action will send to the exchange.
			_selectedIndexes.Add(index);
			CurrentVolume = CurrentVolume + volume;
		}
	}

	/// <summary>
	/// Clear all presets so the next action will not send any additional volume.
	/// </summary>
	public void ResetVolumeSelection()
	{
		_selectedIndexes.Clear();
		CurrentVolume = 0m;
	}

	/// <summary>
	/// Send a market buy order using the currently accumulated volume.
	/// </summary>
	/// <returns><c>true</c> if an order was submitted; otherwise, <c>false</c>.</returns>
	public bool ExecuteBuy()
	{
		var volume = CurrentVolume;
		if (volume <= 0m)
			return false;

		BuyMarket(volume);
		return true;
	}

	/// <summary>
	/// Send a market sell order using the currently accumulated volume.
	/// </summary>
	/// <returns><c>true</c> if an order was submitted; otherwise, <c>false</c>.</returns>
	public bool ExecuteSell()
	{
		var volume = CurrentVolume;
		if (volume <= 0m)
			return false;

		SellMarket(volume);
		return true;
	}

	/// <summary>
	/// Close any open position by sending a market order in the opposite direction.
	/// </summary>
	public void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	/// <summary>
	/// Reverse the current position and immediately open a new one with the selected volume.
	/// </summary>
	public void ReversePosition()
	{
		if (Position > 0m)
		{
			var positionVolume = Math.Abs(Position);
			SellMarket(positionVolume);
			if (CurrentVolume > 0m)
				SellMarket(CurrentVolume);
		}
		else if (Position < 0m)
		{
			var positionVolume = Math.Abs(Position);
			BuyMarket(positionVolume);
			if (CurrentVolume > 0m)
				BuyMarket(CurrentVolume);
		}
	}

	/// <summary>
	/// Move the protective stop to break-even with an additional offset defined by <see cref="BreakEvenSteps"/>.
	/// </summary>
	/// <returns><c>true</c> if a stop order was placed, <c>false</c> otherwise.</returns>
	public bool ApplyBreakEven()
	{
		if (Position == 0m)
			return false;

		var offset = GetOffsetPrice(BreakEvenSteps);
		if (offset <= 0m)
			return false;

		var entryPrice = PositionAvgPrice;

		// Replace any existing stop before submitting the new break-even order.
		CancelStopOrder();

		var volume = Math.Abs(Position);
		_stopOrder = Position > 0m
			? SellStop(volume, entryPrice + offset)
			: BuyStop(volume, entryPrice - offset);

		return true;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			return;
		}

		UpdateProtectionOrders();
	}

	private void UpdateProtectionOrders()
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entryPrice = PositionAvgPrice;

		var stopOffset = GetOffsetPrice(StopLossSteps);
		if (stopOffset > 0m)
		{
			_stopOrder = Position > 0m
				? SellStop(volume, entryPrice - stopOffset)
				: BuyStop(volume, entryPrice + stopOffset);
		}

		var takeOffset = GetOffsetPrice(TakeProfitSteps);
		if (takeOffset > 0m)
		{
			_takeProfitOrder = Position > 0m
				? SellLimit(volume, entryPrice + takeOffset)
				: BuyLimit(volume, entryPrice - takeOffset);
		}
	}

	private void CancelProtectionOrders()
	{
		CancelStopOrder();

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
	}

	private void CancelStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
	}

	private void UpdateVolumeOptions()
	{
		_volumeOptions = ParseVolumeList(_volumeList.Value);

		if (_selectedIndexes.Count == 0)
		{
			CurrentVolume = 0m;
			return;
		}

		decimal sum = 0m;
		var invalid = new List<int>();

		foreach (var index in _selectedIndexes)
		{
			if (index >= 0 && index < _volumeOptions.Length)
			{
				sum += _volumeOptions[index];
			}
			else
			{
				invalid.Add(index);
			}
		}

		foreach (var index in invalid)
			_selectedIndexes.Remove(index);

		CurrentVolume = sum;
	}

	private decimal[] ParseVolumeList(string value)
	{
		if (value.IsEmptyOrWhiteSpace())
			return Array.Empty<decimal>();

		var parts = value.Split(';');
		var result = new List<decimal>(Math.Min(parts.Length, _maxVolumePresets.Value));

		foreach (var part in parts)
		{
			if (result.Count >= _maxVolumePresets.Value)
				break;

			var trimmed = part.Trim();
			if (trimmed.Length == 0)
				continue;

			if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed > 0m)
				result.Add(parsed);
		}

		return result.ToArray();
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Round(volume / step.Value, MidpointRounding.AwayFromZero);
			return steps * step.Value;
		}

		return Math.Round(volume, 2, MidpointRounding.AwayFromZero);
	}

	private decimal GetOffsetPrice(decimal stepCount)
	{
		if (stepCount <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep;
		if (priceStep.HasValue && priceStep.Value > 0m)
			return stepCount * priceStep.Value;

		return stepCount;
	}
}

