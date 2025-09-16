using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with deep drawdown management ported from MetaTrader 5.
/// </summary>
public class DeepDrawdownMaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _closeLosses;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<PriceSource> _fastPriceType;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<PriceSource> _slowPriceType;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator? _fastMa;
	private IIndicator? _slowMa;
	private readonly Queue<decimal> _fastValues = new();
	private readonly Queue<decimal> _slowValues = new();
	private PositionDirection _lastEntryDirection = PositionDirection.None;
	private decimal _currentPositionPrice;
	private decimal _currentPositionVolume;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;

	/// <summary>
	/// Trading volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of aggregated positions per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Close losing positions immediately on a moving average reversal when true.
	/// </summary>
	public bool CloseLosses
	{
		get => _closeLosses.Value;
		set => _closeLosses.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average shift applied to historical values.
	/// </summary>
	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Price source used for the fast moving average.
	/// </summary>
	public PriceSource FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average shift applied to historical values.
	/// </summary>
	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// Price source used for the slow moving average.
	/// </summary>
	public PriceSource SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	/// <summary>
	/// Moving average method shared by the fast and slow averages.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Candle type used for subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the deep drawdown moving average strategy.
	/// </summary>
	public DeepDrawdownMaStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume used for each market order", "Trading")
			.SetGreaterThanZero();

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetDisplay("Max Positions", "Maximum number of aggregated entries", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_closeLosses = Param(nameof(CloseLosses), false)
			.SetDisplay("Close Losing Trades", "Close losing trades as soon as averages reverse", "Risk management");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_fastMaShift = Param(nameof(FastMaShift), 3)
			.SetDisplay("Fast MA Shift", "Shift applied to the fast moving average", "Indicators")
			.SetGreaterOrEqualZero();

		_fastPriceType = Param(nameof(FastPriceType), PriceSource.Close)
			.SetDisplay("Fast Price", "Price source for the fast moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(15, 90, 5);

		_slowMaShift = Param(nameof(SlowMaShift), 0)
			.SetDisplay("Slow MA Shift", "Shift applied to the slow moving average", "Indicators")
			.SetGreaterOrEqualZero();

		_slowPriceType = Param(nameof(SlowPriceType), PriceSource.Close)
			.SetDisplay("Slow Price", "Price source for the slow moving average", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Sma)
			.SetDisplay("MA Method", "Moving average smoothing method", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Market data");
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

		_fastMa = null;
		_slowMa = null;
		_fastValues.Clear();
		_slowValues.Clear();
		_lastEntryDirection = PositionDirection.None;
		_currentPositionPrice = 0m;
		_currentPositionVolume = 0m;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Align the default volume with the configured parameter.
		Volume = OrderVolume;
		_fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);

		// Subscribe to candle data and process finished candles.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Draw indicators together with price if a chart area is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		// Enable default position protection helpers.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip unfinished candles to avoid partial calculations.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Handle any pending break-even exits before new decisions are taken.
		HandleBreakEvenTargets(candle);

		// Calculate moving averages on the selected price sources.
		var fastPrice = GetPrice(candle, FastPriceType);
		var slowPrice = GetPrice(candle, SlowPriceType);

		var fastRaw = _fastMa!.Process(fastPrice, candle.OpenTime, true).ToDecimal();
		var slowRaw = _slowMa!.Process(slowPrice, candle.OpenTime, true).ToDecimal();

		var fastValue = GetShiftedValue(_fastValues, fastRaw, FastMaShift);
		var slowValue = GetShiftedValue(_slowValues, slowRaw, SlowMaShift);

		var fastAboveSlow = fastValue > slowValue;
		var fastBelowSlow = fastValue < slowValue;

		// Manage existing positions when a reversal is detected.
		if (Position > 0m && fastBelowSlow)
		{
			if (CloseLosses)
			{
				SellMarket(Position);
				_longBreakEvenActive = false;
			}
			else if (_currentPositionPrice > 0m)
			{
				if (candle.ClosePrice > _currentPositionPrice)
				{
					SellMarket(Position);
					_longBreakEvenActive = false;
				}
				else
				{
					_longBreakEvenActive = true;
				}
			}
		}
		else if (Position < 0m && fastAboveSlow)
		{
			if (CloseLosses)
			{
				BuyMarket(Math.Abs(Position));
				_shortBreakEvenActive = false;
			}
			else if (_currentPositionPrice > 0m)
			{
				if (candle.ClosePrice < _currentPositionPrice)
				{
					BuyMarket(Math.Abs(Position));
					_shortBreakEvenActive = false;
				}
				else
				{
					_shortBreakEvenActive = true;
				}
			}
		}

		// Evaluate entry conditions after risk checks are complete.
		if (fastAboveSlow && _lastEntryDirection != PositionDirection.Long && CanOpenLong())
		{
			var requiredVolume = OrderVolume + Math.Max(0m, -Position);
			BuyMarket(requiredVolume);
		}
		else if (fastBelowSlow && _lastEntryDirection != PositionDirection.Short && CanOpenShort())
		{
			var requiredVolume = OrderVolume + Math.Max(0m, Position);
			SellMarket(requiredVolume);
		}
	}

	private void HandleBreakEvenTargets(ICandleMessage candle)
	{
		// Close longs at break-even when the price recovers to the average entry.
		if (_longBreakEvenActive && Position > 0m && _currentPositionPrice > 0m && candle.ClosePrice >= _currentPositionPrice)
		{
			SellMarket(Position);
			_longBreakEvenActive = false;
		}

		// Close shorts at break-even once the price moves back in favor of the position.
		if (_shortBreakEvenActive && Position < 0m && _currentPositionPrice > 0m && candle.ClosePrice <= _currentPositionPrice)
		{
			BuyMarket(Math.Abs(Position));
			_shortBreakEvenActive = false;
		}
	}

	private bool CanOpenLong()
	{
		if (OrderVolume <= 0m || MaxPositions <= 0)
			return false;

		var maxVolume = OrderVolume * MaxPositions;
		var currentLong = Position > 0m ? Position : 0m;
		var projected = Position < 0m ? OrderVolume : currentLong + OrderVolume;

		return projected <= maxVolume;
	}

	private bool CanOpenShort()
	{
		if (OrderVolume <= 0m || MaxPositions <= 0)
			return false;

		var maxVolume = OrderVolume * MaxPositions;
		var currentShort = Position < 0m ? Math.Abs(Position) : 0m;
		var projected = Position > 0m ? OrderVolume : currentShort + OrderVolume;

		return projected <= maxVolume;
	}

	private static decimal GetPrice(ICandleMessage candle, PriceSource priceType)
	{
		return priceType switch
		{
			PriceSource.Open => candle.OpenPrice,
			PriceSource.High => candle.HighPrice,
			PriceSource.Low => candle.LowPrice,
			PriceSource.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceSource.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceSource.Weighted => (candle.HighPrice + candle.LowPrice + (candle.ClosePrice * 2m)) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Sma => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetShiftedValue(Queue<decimal> buffer, decimal value, int shift)
	{
		buffer.Enqueue(value);

		var maxSize = shift + 1;
		while (buffer.Count > maxSize)
			buffer.Dequeue();

		if (shift <= 0)
			return value;

		if (buffer.Count <= shift)
			return value;

		var targetIndex = buffer.Count - 1 - shift;
		var currentIndex = 0;
		decimal result = value;

		// Iterate manually to avoid LINQ and return the shifted value.
		foreach (var item in buffer)
		{
			if (currentIndex == targetIndex)
			{
				result = item;
				break;
			}

			currentIndex++;
		}

		return result;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var direction = trade.Order?.Direction;
		if (direction == null)
			return;

		var prevPosition = _currentPositionVolume;
		var newPosition = Position;
		var prevAbs = Math.Abs(prevPosition);
		var newAbs = Math.Abs(newPosition);
		var tradePrice = trade.Trade.Price;

		// Update the average entry price depending on whether the trade adds or reduces exposure.
		if (newAbs > prevAbs)
		{
			if (Math.Sign(prevPosition) == Math.Sign(newPosition) && prevAbs > 0m)
			{
				var addedVolume = newAbs - prevAbs;
				if (addedVolume > 0m)
				{
					_currentPositionPrice = ((prevAbs * _currentPositionPrice) + (addedVolume * tradePrice)) / newAbs;
				}
			}
			else
			{
				_currentPositionPrice = tradePrice;
			}

			_lastEntryDirection = direction == Sides.Buy ? PositionDirection.Long : PositionDirection.Short;
		}
		else if (newAbs < prevAbs)
		{
			_lastEntryDirection = PositionDirection.None;

			if (newPosition == 0m)
			{
				_currentPositionPrice = 0m;
				_longBreakEvenActive = false;
				_shortBreakEvenActive = false;
			}
			else if (Math.Sign(prevPosition) != Math.Sign(newPosition))
			{
				_currentPositionPrice = tradePrice;
			}
		}
		else if (newAbs == 0m)
		{
			_currentPositionPrice = 0m;
			_lastEntryDirection = PositionDirection.None;
			_longBreakEvenActive = false;
			_shortBreakEvenActive = false;
		}

		_currentPositionVolume = newPosition;

		// Reset the opposite break-even flag whenever the net position changes direction.
		if (newPosition > 0m)
		{
			_shortBreakEvenActive = false;
		}
		else if (newPosition < 0m)
		{
			_longBreakEvenActive = false;
		}
	}

	private enum PositionDirection
	{
		None,
		Long,
		Short
	}
}

/// <summary>
/// Price source types matching the original MetaTrader 5 implementation.
/// </summary>
public enum PriceSource
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}

/// <summary>
/// Moving average methods supported by the strategy.
/// </summary>
public enum MovingAverageMethod
{
	Sma,
	Ema,
	Smma,
	Lwma
}
