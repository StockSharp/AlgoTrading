using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that compares bullish and bearish MACD areas to determine trend direction.
/// </summary>
public class AreaMacdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _historyLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;
	private readonly Queue<decimal> _macdHistory = new();

	private decimal _positiveArea;
	private decimal _negativeArea;
	private decimal _entryPrice;
	private decimal _longTrailingStop;
	private decimal _shortTrailingStop;

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of candles to accumulate MACD area.
	/// </summary>
	public int HistoryLength
	{
		get => _historyLength.Value;
		set => _historyLength.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Invert long and short entry conditions.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Minimal favorable movement required before trailing adjusts.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AreaMacdStrategy"/>.
	/// </summary>
	public AreaMacdStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume to trade", "Trading");

		_historyLength = Param(nameof(HistoryLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("History Length", "Number of candles for MACD area comparison", "Indicators")
			.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short signals", "General");

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal favorable move before trailing adjusts", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used by the strategy", "General");
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
		_macdHistory.Clear();
		_positiveArea = 0m;
		_negativeArea = 0m;
		_entryPrice = 0m;
		_longTrailingStop = 0m;
		_shortTrailingStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastLength,
			Slow = MacdSlowLength,
			Signal = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macd = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdLine = macd.Macd;

		UpdateMacdAreas(macdLine);

		var isLongSignal = _positiveArea > _negativeArea;
		if (ReverseSignals)
			isLongSignal = !isLongSignal;

		if (isLongSignal)
		{
			if (Position <= 0m)
			{
				var volume = OrderVolume + Math.Abs(Position);
				if (volume > 0m)
				{
					BuyMarket(volume);
					InitializePosition(true, candle.ClosePrice);
				}
			}
		}
		else
		{
			if (Position >= 0m)
			{
				var volume = OrderVolume + Math.Abs(Position);
				if (volume > 0m)
				{
					SellMarket(volume);
					InitializePosition(false, candle.ClosePrice);
				}
			}
		}

		ManageRisk(candle);
	}

	private void UpdateMacdAreas(decimal macdValue)
	{
		_macdHistory.Enqueue(macdValue);

		if (macdValue > 0m)
			_positiveArea += macdValue;
		else if (macdValue < 0m)
			_negativeArea += Math.Abs(macdValue);

		while (_macdHistory.Count > HistoryLength)
		{
			var removed = _macdHistory.Dequeue();
			if (removed > 0m)
				_positiveArea -= removed;
			else if (removed < 0m)
				_negativeArea -= Math.Abs(removed);
		}

		if (_positiveArea < 0m)
			_positiveArea = 0m;
		if (_negativeArea < 0m)
			_negativeArea = 0m;
	}

	private void InitializePosition(bool isLong, decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_longTrailingStop = 0m;
		_shortTrailingStop = 0m;

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
		{
			LogWarning("Trailing stop is enabled but trailing step is zero. Trailing will be ignored.");
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (TryExitLong(candle))
				ResetAfterExit();
		}
		else if (Position < 0m)
		{
			if (TryExitShort(candle))
				ResetAfterExit();
		}
		else
		{
			ResetAfterExit();
		}
	}

	private bool TryExitLong(ICandleMessage candle)
	{
		var exitVolume = Position;
		if (exitVolume <= 0m)
			return false;

		var stopDistance = GetPriceOffset(StopLossPips);
		var targetDistance = GetPriceOffset(TakeProfitPips);
		var trailingDistance = TrailingStopPips > 0 && TrailingStepPips > 0 ? GetPriceOffset(TrailingStopPips) : 0m;
		var trailingStep = TrailingStopPips > 0 && TrailingStepPips > 0 ? GetPriceOffset(TrailingStepPips) : 0m;

		if (stopDistance > 0m)
		{
			var stopPrice = _entryPrice - stopDistance;
			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(exitVolume);
				LogInfo($"Long stop loss hit at {stopPrice}");
				return true;
			}
		}

		if (targetDistance > 0m)
		{
			var targetPrice = _entryPrice + targetDistance;
			if (candle.HighPrice >= targetPrice)
			{
				SellMarket(exitVolume);
				LogInfo($"Long take profit hit at {targetPrice}");
				return true;
			}
		}

		if (trailingDistance > 0m)
		{
			var activationDistance = trailingDistance + trailingStep;
			var gain = candle.ClosePrice - _entryPrice;

			if (gain > activationDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (candidate > _longTrailingStop)
					_longTrailingStop = candidate;
			}

			if (_longTrailingStop > 0m && candle.LowPrice <= _longTrailingStop)
			{
				SellMarket(exitVolume);
				LogInfo($"Long trailing stop hit at {_longTrailingStop}");
				return true;
			}
		}

		return false;
	}

	private bool TryExitShort(ICandleMessage candle)
	{
		var exitVolume = -Position;
		if (exitVolume <= 0m)
			return false;

		var stopDistance = GetPriceOffset(StopLossPips);
		var targetDistance = GetPriceOffset(TakeProfitPips);
		var trailingDistance = TrailingStopPips > 0 && TrailingStepPips > 0 ? GetPriceOffset(TrailingStopPips) : 0m;
		var trailingStep = TrailingStopPips > 0 && TrailingStepPips > 0 ? GetPriceOffset(TrailingStepPips) : 0m;

		if (stopDistance > 0m)
		{
			var stopPrice = _entryPrice + stopDistance;
			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(exitVolume);
				LogInfo($"Short stop loss hit at {stopPrice}");
				return true;
			}
		}

		if (targetDistance > 0m)
		{
			var targetPrice = _entryPrice - targetDistance;
			if (candle.LowPrice <= targetPrice)
			{
				BuyMarket(exitVolume);
				LogInfo($"Short take profit hit at {targetPrice}");
				return true;
			}
		}

		if (trailingDistance > 0m)
		{
			var activationDistance = trailingDistance + trailingStep;
			var gain = _entryPrice - candle.ClosePrice;

			if (gain > activationDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (_shortTrailingStop == 0m || candidate < _shortTrailingStop)
					_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop > 0m && candle.HighPrice >= _shortTrailingStop)
			{
				BuyMarket(exitVolume);
				LogInfo($"Short trailing stop hit at {_shortTrailingStop}");
				return true;
			}
		}

		return false;
	}

	private void ResetAfterExit()
	{
		if (Position == 0m)
		{
			_entryPrice = 0m;
			_longTrailingStop = 0m;
			_shortTrailingStop = 0m;
		}
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		var step = Security?.PriceStep;
		if (step == null || step == 0m)
			return pips;

		return step.Value * pips;
	}
}
