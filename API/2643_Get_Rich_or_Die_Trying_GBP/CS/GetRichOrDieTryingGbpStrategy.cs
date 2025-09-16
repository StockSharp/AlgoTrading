
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Get Rich or Die Trying GBP" Expert Advisor.
/// Trades around the London and New York session overlap based on bar imbalance.
/// Applies fixed and trailing exits to lock in profits or limit losses.
/// </summary>
public class GetRichOrDieTryingGbpStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _secondaryTakeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _countBars;
	private readonly StrategyParam<decimal> _additionalHour;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<int> _directionQueue = new();

	private int _upCount;
	private int _downCount;
	private decimal _pipValue;
	private decimal? _entryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private DateTimeOffset? _lastEntryTime;
	private bool _exitRequested;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Primary take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Secondary take-profit distance in pips for the early exit.
	/// </summary>
	public int SecondaryTakeProfitPips
	{
		get => _secondaryTakeProfitPips.Value;
		set => _secondaryTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal improvement (in pips) required before trailing stop moves.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Number of minute candles used to measure bar imbalance.
	/// </summary>
	public int CountBars
	{
		get => _countBars.Value;
		set => _countBars.Value = value;
	}

	/// <summary>
	/// Additional hour offset applied to the 19:00 and 22:00 checks.
	/// </summary>
	public decimal AdditionalHour
	{
		get => _additionalHour.Value;
		set => _additionalHour.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous positions allowed.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Candle type used for all calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="GetRichOrDieTryingGbpStrategy"/>.
	/// </summary>
	public GetRichOrDieTryingGbpStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Primary take-profit distance in pips", "Risk");

		_secondaryTakeProfitPips = Param(nameof(SecondaryTakeProfitPips), 40)
			.SetDisplay("Secondary TP (pips)", "Early exit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal price improvement before trailing", "Risk");

		_countBars = Param(nameof(CountBars), 18)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Number of candles for imbalance detection", "Logic");

		_additionalHour = Param(nameof(AdditionalHour), 2m)
			.SetDisplay("Additional Hour", "Offset applied to 19:00 and 22:00 checks", "Timing");

		_maxPositions = Param(nameof(MaxPositions), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum simultaneous positions", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for processing", "General");
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

		_directionQueue.Clear();
		_upCount = 0;
		_downCount = 0;
		_pipValue = 0m;
		_entryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_lastEntryTime = null;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipValue = CalculatePipValue();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.CloseTime == default ? candle.OpenTime : candle.CloseTime;

		if (Position == 0 && _exitRequested)
		{
			// Exit order has been processed, clean the position state.
			_exitRequested = false;
			ResetPositionState();
		}

		UpdateDirectionCounts(candle);

		if (Position > 0 || Position < 0)
		{
			if (ManageOpenPosition(candle))
				return;
		}
		else if (_entryPrice != null && !_exitRequested)
		{
			// No open position, clear stale state.
			ResetPositionState();
		}

		if (_exitRequested)
			return; // Wait for the pending exit order.

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_directionQueue.Count < CountBars)
			return; // Need full history to evaluate imbalance.

		if (MaxPositions <= 0)
			return;

		if (Position != 0)
			return; // Single-position implementation.

		if (!IsWithinTradingWindow(candleTime))
			return;

		if (_lastEntryTime.HasValue && (candleTime - _lastEntryTime.Value).TotalSeconds < 61)
			return; // Enforce 61-second cooldown between entries.

		if (Volume <= 0)
			return;

		if (_upCount > _downCount)
		{
			OpenLong(candle, candleTime);
		}
		else if (_downCount > _upCount)
		{
			OpenShort(candle, candleTime);
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (_exitRequested)
			return true; // Exit already requested, wait for fill.

		var entry = _entryPrice ?? candle.ClosePrice;
		var current = candle.ClosePrice;
		var volume = Math.Abs(Position);
		var pip = GetPipValue();
		var secondaryTarget = SecondaryTakeProfitPips * pip;
		var trailingDistance = TrailingStopPips * pip;
		var trailingStep = TrailingStepPips * pip;

		if (Position > 0)
		{
			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
				return CloseLongPosition(volume);

			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
				return CloseLongPosition(volume);

			if (secondaryTarget > 0m && current - entry >= secondaryTarget)
				return CloseLongPosition(volume);

			if (TrailingStopPips > 0)
			{
				if (current - entry > trailingDistance + trailingStep)
				{
					var newStop = current - trailingDistance;
					if (!_longTrailingStop.HasValue || newStop > _longTrailingStop.Value + trailingStep)
						_longTrailingStop = newStop;
				}

				if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
					return CloseLongPosition(volume);
			}
		}
		else if (Position < 0)
		{
			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
				return CloseShortPosition(volume);

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
				return CloseShortPosition(volume);

			if (secondaryTarget > 0m && entry - current >= secondaryTarget)
				return CloseShortPosition(volume);

			if (TrailingStopPips > 0)
			{
				if (entry - current > trailingDistance + trailingStep)
				{
					var newStop = current + trailingDistance;
					if (!_shortTrailingStop.HasValue || newStop < _shortTrailingStop.Value - trailingStep)
						_shortTrailingStop = newStop;
				}

				if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
					return CloseShortPosition(volume);
			}
		}

		return false;
	}

	private bool CloseLongPosition(decimal volume)
	{
		if (volume <= 0)
			return false;

		_exitRequested = true;
		SellMarket(volume);
		return true;
	}

	private bool CloseShortPosition(decimal volume)
	{
		if (volume <= 0)
			return false;

		_exitRequested = true;
		BuyMarket(volume);
		return true;
	}

	private void OpenLong(ICandleMessage candle, DateTimeOffset candleTime)
	{
		var volume = Volume;
		if (volume <= 0)
			return;

		var pip = GetPipValue();
		var entry = candle.ClosePrice;

		_entryPrice = entry;
		_stopLossPrice = StopLossPips > 0 ? entry - StopLossPips * pip : null;
		_takeProfitPrice = TakeProfitPips > 0 ? entry + TakeProfitPips * pip : null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_exitRequested = false;
		_lastEntryTime = candleTime;

		// Market order is used to follow the original EA behaviour.
		BuyMarket(volume);
	}

	private void OpenShort(ICandleMessage candle, DateTimeOffset candleTime)
	{
		var volume = Volume;
		if (volume <= 0)
			return;

		var pip = GetPipValue();
		var entry = candle.ClosePrice;

		_entryPrice = entry;
		_stopLossPrice = StopLossPips > 0 ? entry + StopLossPips * pip : null;
		_takeProfitPrice = TakeProfitPips > 0 ? entry - TakeProfitPips * pip : null;
		_shortTrailingStop = null;
		_longTrailingStop = null;
		_exitRequested = false;
		_lastEntryTime = candleTime;

		SellMarket(volume);
	}

	private void UpdateDirectionCounts(ICandleMessage candle)
	{
		var direction = 0;

		if (candle.OpenPrice > candle.ClosePrice)
		{
			direction = 1;
			_upCount++;
		}
		else if (candle.OpenPrice < candle.ClosePrice)
		{
			direction = -1;
			_downCount++;
		}

		_directionQueue.Enqueue(direction);

		while (_directionQueue.Count > CountBars)
		{
			var removed = _directionQueue.Dequeue();
			if (removed > 0)
				_upCount--;
			else if (removed < 0)
				_downCount--;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = (decimal)time.Hour;
		var minute = time.Minute;
		var firstHour = 22m + AdditionalHour;
		var secondHour = 19m + AdditionalHour;
		var tolerance = 0.0001m;

		var matchesFirst = Math.Abs(firstHour - hour) < tolerance;
		var matchesSecond = Math.Abs(secondHour - hour) < tolerance;

		return (matchesFirst || matchesSecond) && minute < 5;
	}

	private decimal CalculatePipValue()
	{
		if (Security == null)
			return 1m;

		var step = Security.PriceStep;
		if (step <= 0m)
			return 1m;

		var decimals = Security.Decimals;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private decimal GetPipValue()
	{
		if (_pipValue <= 0m)
			_pipValue = CalculatePipValue();

		return _pipValue;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}
