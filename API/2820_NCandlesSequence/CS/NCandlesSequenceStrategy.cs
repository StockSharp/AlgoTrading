namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that searches for N identical candles in a row and trades in the direction of the streak.
/// Implements optional take profit, stop loss, trailing stop, trading hours filter and profit lock.
/// </summary>
public class NCandlesSequenceStrategy : Strategy
{
	private readonly StrategyParam<int> _consecutiveCandles;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _maxPositionVolume;
	private readonly StrategyParam<bool> _useTradeHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<ClosingMode> _closingBehavior;
	private readonly StrategyParam<DataType> _candleType;

	private int _streakCount;
	private int _lastDirection;
	private int _patternDirection;
	private int _entriesInDirection;
	private bool _blackSheepTriggered;
	private bool _hasPosition;
	private decimal _entryPrice;
	private decimal _pipSize;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Defines how positions are closed when a "black sheep" candle appears.
	/// </summary>
	private enum ClosingMode
	{
		/// <summary>Close every open position.</summary>
		All,

		/// <summary>Close positions opposite to the previously detected streak.</summary>
		Opposite,

		/// <summary>Close positions that follow the previously detected streak.</summary>
		SameDirection
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NCandlesSequenceStrategy"/> class.
	/// </summary>
	public NCandlesSequenceStrategy()
	{
		_consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
		.SetGreaterThanZero()
		.SetDisplay("Consecutive Candles", "Number of candles with identical direction", "Pattern")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used for market orders", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Distance for the take profit target", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Distance for the protective stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
		.SetDisplay("Trailing Stop (pips)", "Distance used when trailing is active", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4)
		.SetDisplay("Trailing Step (pips)", "Additional distance before moving the trailing stop", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 2)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum number of sequential entries in the same direction", "Risk");

		_maxPositionVolume = Param(nameof(MaxPositionVolume), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Max Position Volume", "Maximum volume allowed for an open position", "Risk");

		_useTradeHours = Param(nameof(UseTradeHours), true)
		.SetDisplay("Use Trade Hours", "Enable intraday trading window", "Timing");

		_startHour = Param(nameof(StartHour), 11)
		.SetDisplay("Start Hour", "First trading hour (0-23)", "Timing");

		_endHour = Param(nameof(EndHour), 18)
		.SetDisplay("End Hour", "Last trading hour (0-23)", "Timing");

		_minProfit = Param(nameof(MinProfit), 3m)
		.SetDisplay("Min Profit", "Close positions when floating profit exceeds this value", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);

		_closingBehavior = Param(nameof(ClosingBehavior), ClosingMode.All)
		.SetDisplay("Black Sheep Closing", "Reaction when the streak is broken", "Pattern");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <summary>
	/// Required number of candles with the same direction.
	/// </summary>
	public int ConsecutiveCandles
	{
		get => _consecutiveCandles.Value;
		set => _consecutiveCandles.Value = value;
	}

	/// <summary>
	/// Volume for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Additional step before the trailing stop is moved.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive entries in the same direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume for an open position.
	/// </summary>
	public decimal MaxPositionVolume
	{
		get => _maxPositionVolume.Value;
		set => _maxPositionVolume.Value = value;
	}

	/// <summary>
	/// Enables the trading hours filter.
	/// </summary>
	public bool UseTradeHours
	{
		get => _useTradeHours.Value;
		set => _useTradeHours.Value = value;
	}

	/// <summary>
	/// First trading hour (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (inclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Minimum floating profit that forces the strategy to close all positions.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// How to handle positions when the streak is broken.
	/// </summary>
	public ClosingMode ClosingBehavior
	{
		get => _closingBehavior.Value;
		set => _closingBehavior.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
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
		ResetState();
	}

	private void ResetState()
	{
		_streakCount = 0;
		_lastDirection = 0;
		_patternDirection = 0;
		_entriesInDirection = 0;
		_blackSheepTriggered = false;
		_hasPosition = false;
		_entryPrice = 0m;
		_pipSize = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTradeHours && StartHour >= EndHour)
		throw new InvalidOperationException("Start hour must be less than end hour when the trading window is enabled.");

		StartProtection();

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateTrailingStops(candle);
		ManageFloatingProfit(candle);

		var direction = GetCandleDirection(candle);

		if (direction == 0)
		{
			HandlePatternBreak();
			_lastDirection = 0;
			_streakCount = 0;
			return;
		}

		if (_lastDirection == direction)
		{
			_streakCount++;
		}
		else
		{
			if (_lastDirection != 0)
			HandlePatternBreak();

			_lastDirection = direction;
			_streakCount = 1;
		}

		if (_streakCount >= ConsecutiveCandles)
		{
			if (_patternDirection != direction)
			{
				_patternDirection = direction;
				_entriesInDirection = 0;
				_blackSheepTriggered = false;
			}

			if (direction > 0)
			TryEnterLong(candle);
			else
			TryEnterShort(candle);
		}

		ManageExits(candle);
	}

	private void ManageFloatingProfit(ICandleMessage candle)
	{
		if (MinProfit <= 0m || Position == 0m || !_hasPosition)
		return;

		var floating = CalculateOpenProfit(candle.ClosePrice);
		if (floating >= MinProfit)
		ClosePosition();
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (OrderVolume <= 0m || OrderVolume > MaxPositionVolume)
		return;

		if (_entriesInDirection >= MaxPositions)
		return;

		if (UseTradeHours && !IsWithinTradeHours(candle.CloseTime))
		return;

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (Position != 0m)
		return;

		BuyMarket(OrderVolume);
		InitializeLongState(candle.ClosePrice);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (OrderVolume <= 0m || OrderVolume > MaxPositionVolume)
		return;

		if (_entriesInDirection >= MaxPositions)
		return;

		if (UseTradeHours && !IsWithinTradeHours(candle.CloseTime))
		return;

		if (Position > 0m)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (Position != 0m)
		return;

		SellMarket(OrderVolume);
		InitializeShortState(candle.ClosePrice);
	}

	private void InitializeLongState(decimal entryPrice)
	{
		_hasPosition = true;
		_entryPrice = entryPrice;
		_entriesInDirection = 1;
		_blackSheepTriggered = false;

		var stopDistance = StopLossPips > 0 ? ToPrice(StopLossPips) : (decimal?)null;
		var takeDistance = TakeProfitPips > 0 ? ToPrice(TakeProfitPips) : (decimal?)null;

		_longStop = stopDistance.HasValue ? entryPrice - stopDistance : null;
		_longTake = takeDistance.HasValue ? entryPrice + takeDistance : null;
		_shortStop = null;
		_shortTake = null;
	}

	private void InitializeShortState(decimal entryPrice)
	{
		_hasPosition = true;
		_entryPrice = entryPrice;
		_entriesInDirection = 1;
		_blackSheepTriggered = false;

		var stopDistance = StopLossPips > 0 ? ToPrice(StopLossPips) : (decimal?)null;
		var takeDistance = TakeProfitPips > 0 ? ToPrice(TakeProfitPips) : (decimal?)null;

		_shortStop = stopDistance.HasValue ? entryPrice + stopDistance : null;
		_shortTake = takeDistance.HasValue ? entryPrice - takeDistance : null;
		_longStop = null;
		_longTake = null;
	}

	private void ManageExits(ICandleMessage candle)
	{
		if (!_hasPosition)
		return;

		if (Position > 0m)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (!_hasPosition || TrailingStopPips <= 0 || _pipSize <= 0m)
		return;

		var distance = ToPrice(TrailingStopPips);
		var step = TrailingStepPips > 0 ? ToPrice(TrailingStepPips) : 0m;

		if (Position > 0m)
		{
			var threshold = candle.ClosePrice - (distance + step);
			if (candle.ClosePrice - _entryPrice > distance + step && (!_longStop.HasValue || _longStop.Value < threshold))
			_longStop = candle.ClosePrice - distance;
		}
		else if (Position < 0m)
		{
			var threshold = candle.ClosePrice + (distance + step);
			if (_entryPrice - candle.ClosePrice > distance + step && (!_shortStop.HasValue || _shortStop.Value > threshold))
			_shortStop = candle.ClosePrice + distance;
		}
	}

	private void HandlePatternBreak()
	{
		if (_patternDirection == 0 || _blackSheepTriggered)
		return;

		switch (ClosingBehavior)
		{
			case ClosingMode.All:
			ClosePosition();
			break;

			case ClosingMode.Opposite:
			if (_patternDirection > 0 && Position < 0m)
			ClosePosition();
			else if (_patternDirection < 0 && Position > 0m)
			ClosePosition();
			break;

			case ClosingMode.SameDirection:
			if (_patternDirection > 0 && Position > 0m)
			ClosePosition();
			else if (_patternDirection < 0 && Position < 0m)
			ClosePosition();
			break;
		}

		_blackSheepTriggered = true;
		_entriesInDirection = 0;
		_patternDirection = 0;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		SellMarket(Position);
		else if (Position < 0m)
		BuyMarket(Math.Abs(Position));

		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_hasPosition = Position != 0m;
		_entriesInDirection = _hasPosition ? 1 : 0;

		if (!_hasPosition)
		{
			_entryPrice = 0m;
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
		}
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		if (!_hasPosition || Position == 0m)
		return 0m;

		var volume = Math.Abs(Position);
		return Position > 0m ? (currentPrice - _entryPrice) * volume : (_entryPrice - currentPrice) * volume;
	}

	private static int GetCandleDirection(ICandleMessage candle)
	{
		if (candle.OpenPrice < candle.ClosePrice)
		return 1;

		if (candle.OpenPrice > candle.ClosePrice)
		return -1;

		return 0;
	}

	private bool IsWithinTradeHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= StartHour && hour <= EndHour;
	}

	private decimal ToPrice(int pips)
	{
		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0.0001m;

		var decimals = CountDecimals(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
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
