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
/// Trailing stop manager that moves stops to breakeven and beyond once price advances.
/// Designed to trail any manually opened position using pip based distances.
/// </summary>
public class BreakevenTrailingStopTickStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _enableDemoEntries;

	private readonly StrategyParam<DataType> _candleType;
	private decimal _pointValue;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private bool _exitOrderPending;
	private decimal _entryPrice;
	private DateTimeOffset? _lastDemoEntryTime;

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
/// Initializes a new instance of <see cref="BreakevenTrailingStopTickStrategy"/>.
/// </summary>
public BreakevenTrailingStopTickStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Trailing")
			
			.SetOptimize(5m, 30m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Additional pips required before stop moves again", "Trailing")
			
			.SetOptimize(0.5m, 5m, 0.5m);

		_enableDemoEntries = Param(nameof(EnableDemoEntries), true)
			.SetDisplay("Enable Demo Entries", "Automatically open random trades in testing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_pointValue = 0m;
		_longStopPrice = null;
		_shortStopPrice = null;
		_exitOrderPending = false;
		_lastDemoEntryTime = null;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_pointValue = CalculateAdjustedPoint();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (EnableDemoEntries)
			TryCreateDemoEntry(candle, price);

		if (Position == 0)
		{
			ResetTrailingState();
			return;
		}

		if (TrailingStopPips <= 0m || _pointValue <= 0m)
			return;

		if (Position > 0)
			UpdateLongTrailing(price);
		else if (Position < 0)
			UpdateShortTrailing(price);
	}

	private void TryCreateDemoEntry(ICandleMessage candle, decimal price)
	{
		if (Position != 0 || _exitOrderPending)
			return;

		var serverTime = candle.CloseTime;
		if (_lastDemoEntryTime.HasValue && (serverTime - _lastDemoEntryTime.Value).TotalMinutes < 30)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		if (Random.Shared.NextDouble() < 0.5)
		{
			BuyMarket(volume);
			_entryPrice = price;
		}
		else
		{
			SellMarket(volume);
			_entryPrice = price;
		}

		_lastDemoEntryTime = serverTime;
	}

	private void UpdateLongTrailing(decimal currentPrice)
	{
		var entryPrice = _entryPrice;
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
				// log($"Long trailing stop moved to {newStop}.");
			}
		}

		if (_longStopPrice.HasValue && currentPrice <= _longStopPrice.Value)
			ExitLongPosition();
	}

	private void UpdateShortTrailing(decimal currentPrice)
	{
		var entryPrice = _entryPrice;
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
			// log($"Short trailing stop moved to {newStop}.");
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
		// log("Long position closed by trailing stop.");
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
		// log("Short position closed by trailing stop.");
	}


	private void ResetTrailingState()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
		_exitOrderPending = false;
		_entryPrice = 0m;
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