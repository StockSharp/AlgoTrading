using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when a configurable number of consecutive candles share the same direction.
/// Applies fixed stop-loss, take-profit and optional trailing stop in price steps.
/// </summary>
public class NCandlesV2Strategy : Strategy
{
	private readonly StrategyParam<int> _candlesCount;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private int _streakLength;
	private int _streakDirection;
	private int _currentPositionDirection;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public int CandlesCount
	{
		get => _candlesCount.Value;
		set => _candlesCount.Value = value;
	}

	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NCandlesV2Strategy()
	{
		_candlesCount = Param(nameof(CandlesCount), 3)
			.SetGreaterThanZero()
			.SetDisplay("Candles in Row", "Number of identical candles required", "Entry");

		_lotSize = Param(nameof(LotSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Position size used for entries", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in price steps", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in price steps", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in price steps", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Additional move required to tighten trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only completed candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until the strategy is fully initialized and allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update trailing logic and close the position if protective levels are hit.
		if (ManageOpenPosition(candle))
			return;

		var direction = GetCandleDirection(candle);

		// Doji candles reset the streak because they do not show clear direction.
		if (direction == 0)
		{
			ResetStreak();
			return;
		}

		// Maintain the running count of identical candles.
		if (direction == _streakDirection)
			_streakLength++;
		else
		{
			_streakDirection = direction;
			_streakLength = 1;
		}

		// Enter only after the required number of matching candles is observed.
		if (_streakLength < CandlesCount)
			return;

		if (direction > 0)
			TryOpenLong(candle);
		else
			TryOpenShort(candle);
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		// Reset cached values once the position is flat.
		if (Position == 0)
		{
			_currentPositionDirection = 0;
			_stopPrice = null;
			_takePrice = null;
			_entryPrice = 0m;
			return false;
		}

		var pip = GetPipSize();
		var trailingStep = TrailingStepPips * pip;

		if (_currentPositionDirection > 0)
		{
			// Raise the stop for long trades when price advances far enough.
			if (TrailingStopPips > 0)
			{
				var desired = candle.ClosePrice - TrailingStopPips * pip;
				if (_stopPrice is decimal stop && desired - trailingStep > stop)
					_stopPrice = desired;
			}

			// Close long positions if take-profit or stop-loss levels are reached.
			if (_takePrice is decimal take && candle.HighPrice >= take)
				return ExitPosition();

			if (_stopPrice is decimal stopLoss && candle.LowPrice <= stopLoss)
				return ExitPosition();
		}
		else if (_currentPositionDirection < 0)
		{
			// Lower the stop for short trades when price keeps moving down.
			if (TrailingStopPips > 0)
			{
				var desired = candle.ClosePrice + TrailingStopPips * pip;
				if (_stopPrice is decimal stop && desired + trailingStep < stop)
					_stopPrice = desired;
			}

			// Close short positions if take-profit or stop-loss levels are reached.
			if (_takePrice is decimal take && candle.LowPrice <= take)
				return ExitPosition();

			if (_stopPrice is decimal stopLoss && candle.HighPrice >= stopLoss)
				return ExitPosition();
		}

		return false;
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		// Combine the opposite exposure with the desired lot to get the net volume.
		var volume = LotSize + (Position < 0 ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		SetPositionState(candle.ClosePrice, 1);
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		// Combine the opposite exposure with the desired lot to get the net volume.
		var volume = LotSize + (Position > 0 ? Position : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		SetPositionState(candle.ClosePrice, -1);
	}

	private bool ExitPosition()
	{
		// Close the active position and clear the cached trade state.
		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			if (_currentPositionDirection > 0)
				SellMarket(volume);
			else if (_currentPositionDirection < 0)
				BuyMarket(volume);
		}

		ResetState();
		return true;
	}

	private void SetPositionState(decimal price, int direction)
	{
		// Remember the entry direction and compute initial protective levels.
		_currentPositionDirection = direction;
		_entryPrice = price;

		var pip = GetPipSize();

		if (direction > 0)
		{
			_stopPrice = StopLossPips > 0 ? price - StopLossPips * pip : (TrailingStopPips > 0 ? price : null);
			_takePrice = TakeProfitPips > 0 ? price + TakeProfitPips * pip : null;
		}
		else
		{
			_stopPrice = StopLossPips > 0 ? price + StopLossPips * pip : (TrailingStopPips > 0 ? price : null);
			_takePrice = TakeProfitPips > 0 ? price - TakeProfitPips * pip : null;
		}
	}

	private void ResetState()
	{
		ResetStreak();
		_currentPositionDirection = 0;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private void ResetStreak()
	{
		_streakLength = 0;
		_streakDirection = 0;
	}

	private static int GetCandleDirection(ICandleMessage candle)
	{
		return candle.ClosePrice > candle.OpenPrice ? 1 : candle.ClosePrice < candle.OpenPrice ? -1 : 0;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}
}
