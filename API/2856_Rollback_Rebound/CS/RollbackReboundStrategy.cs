using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rollback Rebound strategy that follows the TST (barabashkakvn's edition) expert advisor logic.
/// The strategy buys after a bullish bar pulls back from its high and sells after a bearish bar rebounds from its low.
/// Protective orders are managed in pips and include optional trailing logic with a rollback filter.
/// </summary>
public class RollbackReboundStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _rollbackRatePips;
	private readonly StrategyParam<bool> _reverseSignal;

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;
	private decimal _rollbackOffset;

	private decimal _longEntryPrice;
	private decimal _longStopPrice;
	private decimal _longTakeProfitPrice;

	private decimal _shortEntryPrice;
	private decimal _shortStopPrice;
	private decimal _shortTakeProfitPrice;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
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
	/// Additional profit in pips required before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Pullback threshold in pips to validate signals.
	/// </summary>
	public decimal RollbackRatePips
	{
		get => _rollbackRatePips.Value;
		set => _rollbackRatePips.Value = value;
	}

	/// <summary>
	/// Inverts entry direction.
	/// </summary>
	public bool ReverseSignal
	{
		get => _reverseSignal.Value;
		set => _reverseSignal.Value = value;
	}

	/// <summary>
	/// Initialize parameters with defaults derived from the original MQL expert.
	/// </summary>
	public RollbackReboundStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations.", "General");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 90m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Distance of the take profit in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop offset in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 15m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Additional profit required before trailing adjusts.", "Risk");

		_rollbackRatePips = Param(nameof(RollbackRatePips), 15m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Rollback Threshold (pips)", "Minimum pullback from the bar extreme to trigger entries.", "Signal");

		_reverseSignal = Param(nameof(ReverseSignal), false)
			.SetDisplay("Reverse Signal", "Invert entry logic (buy becomes sell).", "Signal");
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
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
		_rollbackOffset = 0m;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Validate that trailing configuration matches the behaviour of the original expert.
		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		// Convert pip-based parameters into absolute price offsets.
		_pipSize = Security?.PriceStep ?? 1m;
		if (Security != null && (Security.Decimals == 3 || Security.Decimals == 5))
			_pipSize *= 10m;

		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;
		_trailingStopOffset = TrailingStopPips * _pipSize;
		_trailingStepOffset = TrailingStepPips * _pipSize;
		_rollbackOffset = RollbackRatePips * _pipSize;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles to emulate the IsNewBar check from the MQL expert.
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing stops and exit conditions before generating new signals.
		ManageOpenPosition(candle);

		// Skip signal generation until the strategy is online and allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Translate the rollback filters from the original EA using candle statistics.
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var longCondition = open > close && high - close > _rollbackOffset;
		var shortCondition = close > open && close - low > _rollbackOffset;

		if (ReverseSignal)
		{
			(longCondition, shortCondition) = (shortCondition, longCondition);
		}

		if (longCondition && Position <= 0)
		{
			// Enter long when the rollback condition is met and the strategy is not already in a long position.
			var volume = Volume + Math.Abs(Position);
			if (volume <= 0m)
				return;

			BuyMarket(volume);
			InitializeLongState(candle);
		}
		else if (shortCondition && Position >= 0)
		{
			// Enter short when the bearish rollback occurs and we are not currently short.
			var volume = Volume + Math.Abs(Position);
			if (volume <= 0m)
				return;

			SellMarket(volume);
			InitializeShortState(candle);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		// Mirror the trailing and exit logic from the MetaTrader expert to keep behaviour identical.
		if (Position > 0)
		{
			// Use the candle high as the most optimistic price for long positions.
			var extreme = candle.HighPrice;

			if (_longEntryPrice == 0m)
				// Store the actual entry price once the trade is filled.
				_longEntryPrice = candle.ClosePrice;

			if (_trailingStopOffset > 0m)
			{
				// Apply the trailing algorithm for the active position.
				// Move the stop only when profit exceeds trailing stop plus step, exactly as in the MQL code.
				if (extreme - _longEntryPrice > _trailingStopOffset + _trailingStepOffset)
				{
					var threshold = extreme - (_trailingStopOffset + _trailingStepOffset);
					if (_longStopPrice == 0m || _longStopPrice < threshold)
						_longStopPrice = extreme - _trailingStopOffset;
				}
			}

			if (_longTakeProfitPrice > 0m && candle.HighPrice >= _longTakeProfitPrice)
			{
				// Exit the long position once the take-profit level is touched.
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}

			if (_longStopPrice > 0m && candle.LowPrice <= _longStopPrice)
			{
				// Close the long position if the initial or trailing stop is triggered.
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}
		}
		else if (Position < 0)
		{
			// Use the candle low as the best price in favour of the short position.
			var extreme = candle.LowPrice;

			if (_shortEntryPrice == 0m)
				// Capture the short entry price after execution.
				_shortEntryPrice = candle.ClosePrice;

			if (_trailingStopOffset > 0m)
			{
				// Apply the trailing algorithm for short positions.
				// Move the stop only when profit exceeds trailing stop plus step, exactly as in the MQL code.
				if (_shortEntryPrice - extreme > _trailingStopOffset + _trailingStepOffset)
				{
					var threshold = extreme + (_trailingStopOffset + _trailingStepOffset);
					if (_shortStopPrice == 0m || _shortStopPrice > threshold)
						_shortStopPrice = extreme + _trailingStopOffset;
				}
			}

			if (_shortTakeProfitPrice > 0m && candle.LowPrice <= _shortTakeProfitPrice)
			{
				// Exit the short position when the take-profit is hit.
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			if (_shortStopPrice > 0m && candle.HighPrice >= _shortStopPrice)
			{
				// Cover the short position if the stop level is breached.
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}
		}
		else
		{
			// Clear cached levels when no position is open.
			ResetLongState();
			ResetShortState();
		}
	}

	private void InitializeLongState(ICandleMessage candle)
	{
		// Clear short-side state because the strategy operates in netting mode.
		ResetShortState();

		var entry = candle.ClosePrice;
		// Save reference prices for managing the long position.
		_longEntryPrice = entry;
		_longStopPrice = StopLossPips > 0m ? entry - _stopLossOffset : 0m;
		_longTakeProfitPrice = TakeProfitPips > 0m ? entry + _takeProfitOffset : 0m;
	}

	private void InitializeShortState(ICandleMessage candle)
	{
		// Clear long-side state before opening a short position.
		ResetLongState();

		var entry = candle.ClosePrice;
		// Store price references for the short position.
		_shortEntryPrice = entry;
		_shortStopPrice = StopLossPips > 0m ? entry + _stopLossOffset : 0m;
		_shortTakeProfitPrice = TakeProfitPips > 0m ? entry - _takeProfitOffset : 0m;
	}

	private void ResetLongState()
	{
		_longEntryPrice = 0m;
		_longStopPrice = 0m;
		_longTakeProfitPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = 0m;
		_shortStopPrice = 0m;
		_shortTakeProfitPrice = 0m;
	}
}
