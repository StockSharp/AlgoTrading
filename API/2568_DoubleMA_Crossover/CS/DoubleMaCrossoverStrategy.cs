using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double moving average crossover with breakout confirmation and trailing protection.
/// </summary>
public class DoubleMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _breakoutPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<TrailingType> _trailingMode;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _level1TriggerPips;
	private readonly StrategyParam<int> _level1OffsetPips;
	private readonly StrategyParam<int> _level2TriggerPips;
	private readonly StrategyParam<int> _level2OffsetPips;
	private readonly StrategyParam<int> _level3TriggerPips;
	private readonly StrategyParam<int> _level3OffsetPips;
	private readonly StrategyParam<bool> _useTimeLimit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;

	private decimal? _entryPrice;
	private decimal? _currentStop;
	private decimal? _currentTakeProfit;
	private decimal _maxPriceSinceEntry;
	private decimal _minPriceSinceEntry;

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int BreakoutPips
	{
		get => _breakoutPips.Value;
		set => _breakoutPips.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public TrailingType TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int Level1TriggerPips
	{
		get => _level1TriggerPips.Value;
		set => _level1TriggerPips.Value = value;
	}

	public int Level1OffsetPips
	{
		get => _level1OffsetPips.Value;
		set => _level1OffsetPips.Value = value;
	}

	public int Level2TriggerPips
	{
		get => _level2TriggerPips.Value;
		set => _level2TriggerPips.Value = value;
	}

	public int Level2OffsetPips
	{
		get => _level2OffsetPips.Value;
		set => _level2OffsetPips.Value = value;
	}

	public int Level3TriggerPips
	{
		get => _level3TriggerPips.Value;
		set => _level3TriggerPips.Value = value;
	}

	public int Level3OffsetPips
	{
		get => _level3OffsetPips.Value;
		set => _level3OffsetPips.Value = value;
	}

	public bool UseTimeLimit
	{
		get => _useTimeLimit.Value;
		set => _useTimeLimit.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DoubleMaCrossoverStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 2)
		.SetDisplay("Fast MA Period", "Period for the fast moving average.", "General")
		.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
		.SetDisplay("Slow MA Period", "Period for the slow moving average.", "General")
		.SetCanOptimize(true);

		_breakoutPips = Param(nameof(BreakoutPips), 45)
		.SetDisplay("Breakout Pips", "Distance in price steps added before submitting an entry.", "General")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 25)
		.SetDisplay("Stop Loss Pips", "Protective stop expressed in price steps.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0)
		.SetDisplay("Take Profit Pips", "Take profit distance expressed in price steps.", "Risk")
		.SetCanOptimize(true);

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing", "Enable trailing stop management.", "Risk");

		_trailingMode = Param(nameof(TrailingMode), TrailingType.Type3)
		.SetDisplay("Trailing Type", "Trailing stop behaviour.", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 40)
		.SetDisplay("Trailing Stop Pips", "Trailing distance used by type 2 trailing.", "Risk")
		.SetCanOptimize(true);

		_level1TriggerPips = Param(nameof(Level1TriggerPips), 20)
		.SetDisplay("Level 1 Trigger", "Profit in price steps required before the first trailing adjustment.", "Risk")
		.SetCanOptimize(true);

		_level1OffsetPips = Param(nameof(Level1OffsetPips), 20)
		.SetDisplay("Level 1 Offset", "Offset in price steps applied after the first trigger.", "Risk")
		.SetCanOptimize(true);

		_level2TriggerPips = Param(nameof(Level2TriggerPips), 30)
		.SetDisplay("Level 2 Trigger", "Profit in price steps required before the second trailing adjustment.", "Risk")
		.SetCanOptimize(true);

		_level2OffsetPips = Param(nameof(Level2OffsetPips), 20)
		.SetDisplay("Level 2 Offset", "Offset in price steps applied after the second trigger.", "Risk")
		.SetCanOptimize(true);

		_level3TriggerPips = Param(nameof(Level3TriggerPips), 50)
		.SetDisplay("Level 3 Trigger", "Profit in price steps required before the third trailing adjustment.", "Risk")
		.SetCanOptimize(true);

		_level3OffsetPips = Param(nameof(Level3OffsetPips), 20)
		.SetDisplay("Level 3 Offset", "Offset in price steps applied after the third trigger.", "Risk")
		.SetCanOptimize(true);

		_useTimeLimit = Param(nameof(UseTimeLimit), true)
		.SetDisplay("Use Time Limit", "Restrict the creation of new orders to a trading window.", "Schedule");

		_startHour = Param(nameof(StartHour), 11)
		.SetDisplay("Start Hour", "Hour when new setups become valid.", "Schedule")
		.SetCanOptimize(true);

		_stopHour = Param(nameof(StopHour), 16)
		.SetDisplay("Stop Hour", "Hour after which no new setups are created.", "Schedule")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for analysis.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Volume", "Order volume in lots.", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Reset internal buffers before processing market data.
		ResetState();
		Volume = TradeVolume;
		StartProtection();

		var fastMa = new SMA { Length = FastMaPeriod };
		var slowMa = new SMA { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!_hasPrev)
		{
			_prevFast = fastMa;
			_prevSlow = slowMa;
			_hasPrev = true;
			return;
		}

		// Identify crossovers based on the two moving averages.
		var crossUp = fastMa > slowMa && _prevFast <= _prevSlow;
		var crossDown = fastMa < slowMa && _prevFast >= _prevSlow;

		// Manage the current position before checking for fresh breakouts.
		ManageOpenPosition(candle, crossUp, crossDown);
		TriggerPendingEntries(candle);

		if (UseTimeLimit && !IsTradingTime(candle.OpenTime))
		{
			if (crossUp)
			{
				_pendingSellPrice = null;
			}

			if (crossDown)
			{
				_pendingBuyPrice = null;
			}

			_prevFast = fastMa;
			_prevSlow = slowMa;
			return;
		}

		if (crossDown)
		{
			_pendingBuyPrice = null;
		}

		if (crossUp)
		{
			_pendingSellPrice = null;
		}

		if (Position == 0)
		{
			var breakout = GetBreakoutDistance();

			if (crossUp)
			{
				_pendingBuyPrice = candle.ClosePrice + breakout;
			}
			else if (crossDown)
			{
				_pendingSellPrice = candle.ClosePrice - breakout;
			}
		}

		TriggerPendingEntries(candle);

		_prevFast = fastMa;
		_prevSlow = slowMa;
	}

	private void ManageOpenPosition(ICandleMessage candle, bool crossUp, bool crossDown)
	{
		if (Position == 0)
		{
			if (_entryPrice.HasValue)
			{
				// Clear trailing information once the position is flat.
				ResetPositionState();
			}

			return;
		}

		if (_entryPrice is null)
		{
			_entryPrice = candle.ClosePrice;
			_maxPriceSinceEntry = candle.ClosePrice;
			_minPriceSinceEntry = candle.ClosePrice;
		}

		UpdateExtremes(candle);
		UpdateTrailingStop(candle);

		if (CheckStopsAndTargets(candle))
		{
			return;
		}

		if (Position > 0 && crossDown)
		{
			ExitLong();
			return;
		}

		if (Position < 0 && crossUp)
		{
			ExitShort();
		}
	}

	private void UpdateExtremes(ICandleMessage candle)
	{
		var high = candle.HighPrice ?? candle.ClosePrice;
		var low = candle.LowPrice ?? candle.ClosePrice;
		_maxPriceSinceEntry = Math.Max(_maxPriceSinceEntry, high);
		_minPriceSinceEntry = Math.Min(_minPriceSinceEntry, low);
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (!UseTrailingStop || _entryPrice is null)
		{
			return;
		}

		var entryPrice = _entryPrice.Value;
		var closePrice = candle.ClosePrice;
		var step = GetPriceStep();

		switch (TrailingMode)
		{
			case TrailingType.Type1:
			{
				UpdateType1Trailing(entryPrice, closePrice, step);
				break;
			}
			case TrailingType.Type2:
			{
				UpdateType2Trailing(entryPrice, closePrice, step);
				break;
			}
			case TrailingType.Type3:
			{
				UpdateType3Trailing(entryPrice, closePrice, step);
				break;
			}
		}
	}

	private void UpdateType1Trailing(decimal entryPrice, decimal closePrice, decimal step)
	{
		var distance = step * Math.Abs(StopLossPips);
		if (distance == 0)
		{
			return;
		}

		if (Position > 0)
		{
			if (_maxPriceSinceEntry - entryPrice >= distance)
			{
				var candidate = closePrice - distance;
				UpdateStopForLong(candidate);
			}
		}
		else if (Position < 0)
		{
			if (entryPrice - _minPriceSinceEntry >= distance)
			{
				var candidate = closePrice + distance;
				UpdateStopForShort(candidate);
			}
		}
	}

	private void UpdateType2Trailing(decimal entryPrice, decimal closePrice, decimal step)
	{
		var distance = step * Math.Abs(TrailingStopPips);
		if (distance == 0)
		{
			return;
		}

		if (Position > 0)
		{
			if (_maxPriceSinceEntry - entryPrice >= distance)
			{
				var candidate = closePrice - distance;
				UpdateStopForLong(candidate);
			}
		}
		else if (Position < 0)
		{
			if (entryPrice - _minPriceSinceEntry >= distance)
			{
				var candidate = closePrice + distance;
				UpdateStopForShort(candidate);
			}
		}
	}

	private void UpdateType3Trailing(decimal entryPrice, decimal closePrice, decimal step)
	{
		var trigger1 = step * Math.Abs(Level1TriggerPips);
		if (trigger1 > 0)
		{
			if (Position > 0 && _maxPriceSinceEntry - entryPrice >= trigger1)
			{
				var candidate = entryPrice + trigger1 - step * Math.Abs(Level1OffsetPips);
				UpdateStopForLong(candidate);
			}
			else if (Position < 0 && entryPrice - _minPriceSinceEntry >= trigger1)
			{
				var candidate = entryPrice - trigger1 + step * Math.Abs(Level1OffsetPips);
				UpdateStopForShort(candidate);
			}
		}

		var trigger2 = step * Math.Abs(Level2TriggerPips);
		if (trigger2 > 0)
		{
			if (Position > 0 && _maxPriceSinceEntry - entryPrice >= trigger2)
			{
				var candidate = entryPrice + trigger2 - step * Math.Abs(Level2OffsetPips);
				UpdateStopForLong(candidate);
			}
			else if (Position < 0 && entryPrice - _minPriceSinceEntry >= trigger2)
			{
				var candidate = entryPrice - trigger2 + step * Math.Abs(Level2OffsetPips);
				UpdateStopForShort(candidate);
			}
		}

		var trigger3 = step * Math.Abs(Level3TriggerPips);
		if (trigger3 > 0)
		{
			if (Position > 0 && _maxPriceSinceEntry - entryPrice >= trigger3)
			{
				var candidate = closePrice - step * Math.Abs(Level3OffsetPips);
				UpdateStopForLong(candidate);
			}
			else if (Position < 0 && entryPrice - _minPriceSinceEntry >= trigger3)
			{
				var candidate = closePrice + step * Math.Abs(Level3OffsetPips);
				UpdateStopForShort(candidate);
			}
		}
	}

	private void UpdateStopForLong(decimal candidate)
	{
		if (!_currentStop.HasValue || candidate > _currentStop.Value)
		{
			_currentStop = candidate;
		}
	}

	private void UpdateStopForShort(decimal candidate)
	{
		if (!_currentStop.HasValue || candidate < _currentStop.Value)
		{
			_currentStop = candidate;
		}
	}

	private bool CheckStopsAndTargets(ICandleMessage candle)
	{
		// Simulate broker-side stop loss and take profit execution.
		if (Position > 0)
		{
			if (_currentTakeProfit.HasValue && candle.High >= _currentTakeProfit.Value)
			{
				ExitLong();
				return true;
			}

			if (_currentStop.HasValue && candle.Low <= _currentStop.Value)
			{
				ExitLong();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_currentTakeProfit.HasValue && candle.Low <= _currentTakeProfit.Value)
			{
				ExitShort();
				return true;
			}

			if (_currentStop.HasValue && candle.High >= _currentStop.Value)
			{
				ExitShort();
				return true;
			}
		}

		return false;
	}

	private void TriggerPendingEntries(ICandleMessage candle)
	{
		// Skip processing when a position already exists.
		if (Position != 0)
		{
			if (Position > 0)
			{
				_pendingBuyPrice = null;
			}
			else
			{
				_pendingSellPrice = null;
			}

			return;
		}

		if (_pendingBuyPrice is decimal buyPrice && candle.High >= buyPrice)
		{
			// Breakout confirmed on the long side.
			EnterLong(buyPrice);
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
		else if (_pendingSellPrice is decimal sellPrice && candle.Low <= sellPrice)
		{
			// Breakout confirmed on the short side.
			EnterShort(sellPrice);
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
		}
	}

	private void EnterLong(decimal price)
	{
		if (TradeVolume <= 0)
		{
			return;
		}

		Volume = TradeVolume;
		BuyMarket();

		// Initialize tracking variables for the new long trade.
		_entryPrice = price;
		_currentStop = StopLossPips > 0 ? price - GetPriceStep() * Math.Abs(StopLossPips) : null;
		_currentTakeProfit = TakeProfitPips > 0 ? price + GetPriceStep() * Math.Abs(TakeProfitPips) : null;
		_maxPriceSinceEntry = price;
		_minPriceSinceEntry = price;
	}

	private void EnterShort(decimal price)
	{
		if (TradeVolume <= 0)
		{
			return;
		}

		Volume = TradeVolume;
		SellMarket();

		// Initialize tracking variables for the new short trade.
		_entryPrice = price;
		_currentStop = StopLossPips > 0 ? price + GetPriceStep() * Math.Abs(StopLossPips) : null;
		_currentTakeProfit = TakeProfitPips > 0 ? price - GetPriceStep() * Math.Abs(TakeProfitPips) : null;
		_maxPriceSinceEntry = price;
		_minPriceSinceEntry = price;
	}

	private void ExitLong()
	{
		Volume = TradeVolume;
		SellMarket();
		ResetPositionState();
	}

	private void ExitShort()
	{
		Volume = TradeVolume;
		BuyMarket();
		ResetPositionState();
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!UseTimeLimit)
		{
			return true;
		}

		var hour = time.LocalDateTime.Hour;

		if (StartHour <= StopHour)
		{
			return hour >= StartHour && hour <= StopHour;
		}

		return hour >= StartHour || hour <= StopHour;
	}

	private decimal GetBreakoutDistance()
	{
		return GetPriceStep() * Math.Abs(BreakoutPips);
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0 ? step : 1m;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_currentStop = null;
		_currentTakeProfit = null;
		_maxPriceSinceEntry = 0m;
		_minPriceSinceEntry = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
	}

	private void ResetState()
	{
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		ResetPositionState();
	}

	public enum TrailingType
	{
		Type1,
		Type2,
		Type3
	}
}
