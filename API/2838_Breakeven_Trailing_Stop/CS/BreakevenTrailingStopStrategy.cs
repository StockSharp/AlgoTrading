using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager that moves stops to breakeven and beyond once price advances.
/// Designed to trail any manually opened position using pip based distances.
/// </summary>
public class BreakevenTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _enableDemoEntries;

	private decimal _pointValue;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private bool _exitOrderPending;
	private DateTimeOffset? _lastDemoEntryTime;
	private readonly Random _random = new();

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips before the stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable random demo entries to showcase the trailing behaviour in testing.
	/// </summary>
	public bool EnableDemoEntries
	{
		get => _enableDemoEntries.Value;
		set => _enableDemoEntries.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BreakevenTrailingStopStrategy"/>.
	/// </summary>
	public BreakevenTrailingStopStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step", "Additional pips required before stop moves again", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_enableDemoEntries = Param(nameof(EnableDemoEntries), false)
			.SetDisplay("Enable Demo Entries", "Automatically open random trades in testing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointValue = 0m;
		_longStopPrice = null;
		_shortStopPrice = null;
		_exitOrderPending = false;
		_lastDemoEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_pointValue = CalculateAdjustedPoint();

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price is null || price <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (EnableDemoEntries)
			TryCreateDemoEntry(trade, price.Value);

		if (TrailingStopPips <= 0m || _pointValue <= 0m || Position == 0)
			return;

		if (Position > 0)
			UpdateLongTrailing(price.Value);
		else if (Position < 0)
			UpdateShortTrailing(price.Value);
	}

	private void TryCreateDemoEntry(ExecutionMessage trade, decimal price)
	{
		if (Position != 0 || _exitOrderPending)
			return;

		var serverTime = trade.ServerTime;
		if (_lastDemoEntryTime.HasValue && serverTime <= _lastDemoEntryTime.Value)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		if (_random.NextDouble() < 0.5)
		{
			BuyMarket(volume);
			LogInfo($"Demo long entry opened at {price}.");
		}
		else
		{
			SellMarket(volume);
			LogInfo($"Demo short entry opened at {price}.");
		}

		_lastDemoEntryTime = serverTime;
	}

	private void UpdateLongTrailing(decimal currentPrice)
	{
		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		var stopOffset = TrailingStopPips * _pointValue;
		var stepOffset = TrailingStepPips * _pointValue;
		if (stopOffset <= 0m)
			return;

		var activationOffset = stopOffset + stepOffset;
		if (currentPrice - entryPrice <= activationOffset)
			return;

		var threshold = currentPrice - activationOffset;
		if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
		{
			var newStop = currentPrice - stopOffset;
			if (newStop > 0m)
			{
				_longStopPrice = newStop;
				LogInfo($"Long trailing stop moved to {newStop}.");
			}
		}

		if (_longStopPrice.HasValue && currentPrice <= _longStopPrice.Value)
			ExitLongPosition();
	}

	private void UpdateShortTrailing(decimal currentPrice)
	{
		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		var stopOffset = TrailingStopPips * _pointValue;
		var stepOffset = TrailingStepPips * _pointValue;
		if (stopOffset <= 0m)
			return;

		var activationOffset = stopOffset + stepOffset;
		if (entryPrice - currentPrice <= activationOffset)
			return;

		var threshold = currentPrice + activationOffset;
		if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold)
		{
			var newStop = currentPrice + stopOffset;
			_shortStopPrice = newStop;
			LogInfo($"Short trailing stop moved to {newStop}.");
		}

		if (_shortStopPrice.HasValue && currentPrice >= _shortStopPrice.Value)
			ExitShortPosition();
	}

	private void ExitLongPosition()
	{
		if (_exitOrderPending)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_exitOrderPending = true;
		LogInfo("Long position closed by trailing stop.");
	}

	private void ExitShortPosition()
	{
		if (_exitOrderPending)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_exitOrderPending = true;
		LogInfo("Short position closed by trailing stop.");
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			ResetTrailingState();
			return;
		}

		if ((Position > 0 && delta > 0m) || (Position < 0 && delta < 0m))
		{
			_exitOrderPending = false;
			if (Position > 0)
				_shortStopPrice = null;
			else
				_longStopPrice = null;
		}
	}

	private void ResetTrailingState()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
		_exitOrderPending = false;
	}

	private decimal CalculateAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
