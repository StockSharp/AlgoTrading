using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of the Gaps strategy that trades opening price gaps.
/// </summary>
public class GapsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _gapPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _gapSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPreviousCandle;

	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;
	private decimal _entryPrice;

	/// <summary>
	/// Order size in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
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
	/// Minimum price advance required to move the trailing stop, in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum opening gap size in pips to trigger trades.
	/// </summary>
	public decimal GapPips
	{
		get => _gapPips.Value;
		set => _gapPips.Value = value;
	}

	/// <summary>
	/// Candle type used for detecting gaps.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GapsStrategy"/> class.
	/// </summary>
	public GapsStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Order Volume", "Trading volume in lots", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Trailing Step (pips)", "Increment required to move the trailing stop", "Risk");

		_gapPips = Param(nameof(GapPips), 1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Gap Threshold (pips)", "Minimum opening gap measured in pips", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for gap detection", "General");
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

		_pipSize = 0m;
		_gapSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;

		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPreviousCandle = false;

		_activeStopPrice = null;
		_activeTakePrice = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var decimals = GetDecimalPlaces(step);
		_pipSize = (decimals == 3 || decimals == 5) ? step * 10m : step;

		_gapSize = GapPips * _pipSize;
		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_trailingStopOffset = TrailingStopPips * _pipSize;
		_trailingStepOffset = TrailingStepPips * _pipSize;

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage existing position before evaluating new signals.
		if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0)
		{
			ManageShortPosition(candle);
		}
		else
		{
			ResetProtection();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (_hasPreviousCandle)
		{
			var gapDown = candle.OpenPrice < _previousLow - _gapSize;
			var gapUp = candle.OpenPrice > _previousHigh + _gapSize;

			if (gapDown && Position <= 0)
			{
				EnterLong(candle.OpenPrice);
			}
			else if (gapUp && Position >= 0)
			{
				EnterShort(candle.OpenPrice);
			}
		}

		// Manage the updated position so protective levels react within the same candle.
		if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0)
		{
			ManageShortPosition(candle);
		}

		UpdatePreviousCandle(candle);
	}

	private void EnterLong(decimal entryPrice)
	{
		var volume = OrderVolume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Cancel pending orders before reversing the position.
		CancelActiveOrders();

		// Enter long using a market order so the gap is captured immediately.
		BuyMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = StopLossPips > 0m ? entryPrice - _stopLossOffset : null;
		_activeTakePrice = TakeProfitPips > 0m ? entryPrice + _takeProfitOffset : null;
	}

	private void EnterShort(decimal entryPrice)
	{
		var volume = OrderVolume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Cancel pending orders before reversing the position.
		CancelActiveOrders();

		// Enter short using a market order at the opening gap.
		SellMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = StopLossPips > 0m ? entryPrice + _stopLossOffset : null;
		_activeTakePrice = TakeProfitPips > 0m ? entryPrice - _takeProfitOffset : null;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		// Close the long trade if the stop-loss level is violated inside the candle range.
		if (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			var volume = Math.Abs(Position);

			if (volume <= 0m)
				return;

			SellMarket(volume);
			ResetProtection();
			return;
		}

		// Lock in profits once the candle traded through the take-profit level.
		if (_activeTakePrice is decimal take && candle.HighPrice >= take)
		{
			var volume = Math.Abs(Position);

			if (volume <= 0m)
				return;

			SellMarket(volume);
			ResetProtection();
			return;
		}

		// Update the trailing stop only when both distance and step conditions are satisfied.
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m || _entryPrice <= 0m)
			return;

		var maxPrice = candle.HighPrice;
		var advance = maxPrice - _entryPrice;

		if (advance > _trailingStopOffset + _trailingStepOffset)
		{
			var minAllowed = maxPrice - (_trailingStopOffset + _trailingStepOffset);
			if (_activeStopPrice == null || _activeStopPrice < minAllowed)
			{
				var newStop = maxPrice - _trailingStopOffset;
				if (_activeStopPrice == null || newStop > _activeStopPrice.Value)
					_activeStopPrice = newStop;
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		// Exit the short trade if price touches the protective stop.
		if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			var volume = Math.Abs(Position);

			if (volume <= 0m)
				return;

			BuyMarket(volume);
			ResetProtection();
			return;
		}

		// Cover the short position when the take-profit level is hit.
		if (_activeTakePrice is decimal take && candle.LowPrice <= take)
		{
			var volume = Math.Abs(Position);

			if (volume <= 0m)
				return;

			BuyMarket(volume);
			ResetProtection();
			return;
		}

		// Adjust the trailing stop for short positions when conditions are met.
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m || _entryPrice <= 0m)
			return;

		var minPrice = candle.LowPrice;
		var advance = _entryPrice - minPrice;

		if (advance > _trailingStopOffset + _trailingStepOffset)
		{
			var maxAllowed = minPrice + (_trailingStopOffset + _trailingStepOffset);
			if (_activeStopPrice == null || _activeStopPrice > maxAllowed)
			{
				var newStop = minPrice + _trailingStopOffset;
				if (_activeStopPrice == null || newStop < _activeStopPrice.Value)
					_activeStopPrice = newStop;
			}
		}
	}

	private void ResetProtection()
	{
		_activeStopPrice = null;
		_activeTakePrice = null;
		_entryPrice = 0m;
	}

	private void UpdatePreviousCandle(ICandleMessage candle)
	{
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_hasPreviousCandle = true;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
