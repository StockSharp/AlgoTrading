using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy that uses a Donchian style price channel for risk management.
/// </summary>
public class TripleMaChannelCrossoverStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<bool> _useAutoTargets;
	private readonly StrategyParam<bool> _tradeOnClose;
	private readonly StrategyParam<int> _maxPositionCount;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<MovingAverageMode> _fastMaType;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _middleShift;
	private readonly StrategyParam<MovingAverageMode> _middleMaType;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<MovingAverageMode> _slowMaType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _fastMa = null!;
	private MovingAverage _middleMa = null!;
	private MovingAverage _slowMa = null!;
	private DonchianChannels _channel = null!;
	private Shift? _fastShiftIndicator;
	private Shift? _middleShiftIndicator;
	private Shift? _slowShiftIndicator;

	private decimal _prevFast;
	private decimal _prevMiddle;
	private decimal _prevSlow;
	private bool _hasPreviousValues;

	private decimal _tickSize;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal _longEntryPrice;
	private bool _longBreakEvenActivated;

	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _shortEntryPrice;
	private bool _shortBreakEvenActivated;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Minimal step for trailing stop adjustments in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Profit in pips required to move the stop loss to break-even.
	/// </summary>
	public int BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Enable automatic SL/TP placement based on the price channel.
	/// </summary>
	public bool UseAutoTargets
	{
		get => _useAutoTargets.Value;
		set => _useAutoTargets.Value = value;
	}

	/// <summary>
	/// Trade only when the crossover is confirmed on the closed bar.
	/// </summary>
	public bool TradeOnClose
	{
		get => _tradeOnClose.Value;
		set => _tradeOnClose.Value = value;
	}

	/// <summary>
	/// Maximum number of scaled-in positions.
	/// </summary>
	public int MaxPositionCount
	{
		get => _maxPositionCount.Value;
		set => _maxPositionCount.Value = value;
	}

	/// <summary>
	/// Period for the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the fast moving average.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Type of the fast moving average.
	/// </summary>
	public MovingAverageMode FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Period for the middle moving average.
	/// </summary>
	public int MiddlePeriod
	{
		get => _middlePeriod.Value;
		set => _middlePeriod.Value = value;
	}

	/// <summary>
	/// Shift for the middle moving average.
	/// </summary>
	public int MiddleShift
	{
		get => _middleShift.Value;
		set => _middleShift.Value = value;
	}

	/// <summary>
	/// Type of the middle moving average.
	/// </summary>
	public MovingAverageMode MiddleMaType
	{
		get => _middleMaType.Value;
		set => _middleMaType.Value = value;
	}

	/// <summary>
	/// Period for the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the slow moving average.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Type of the slow moving average.
	/// </summary>
	public MovingAverageMode SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Lookback period for the Donchian price channel.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="TripleMaChannelCrossoverStrategy"/>.
	/// </summary>
	public TripleMaChannelCrossoverStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 0)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 145)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal trailing adjustment", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 15)
			.SetDisplay("Break Even (pips)", "Profit to move stop to break-even", "Risk");

		_useAutoTargets = Param(nameof(UseAutoTargets), false)
			.SetDisplay("Auto SL/TP", "Use channel for stop & take", "Risk");

		_tradeOnClose = Param(nameof(TradeOnClose), true)
			.SetDisplay("Trade On Close", "Confirm cross on closed bar", "Signals");

		_maxPositionCount = Param(nameof(MaxPositionCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum scaling steps", "Trading");

		_fastPeriod = Param(nameof(FastPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "First moving average", "Moving Averages");

		_fastShift = Param(nameof(FastShift), 0)
			.SetDisplay("Fast MA Shift", "Bars to shift fast MA", "Moving Averages");

		_fastMaType = Param(nameof(FastMaType), MovingAverageMode.Smoothed)
			.SetDisplay("Fast MA Type", "Method for fast MA", "Moving Averages");

		_middlePeriod = Param(nameof(MiddlePeriod), 61)
			.SetGreaterThanZero()
			.SetDisplay("Middle MA Period", "Second moving average", "Moving Averages");

		_middleShift = Param(nameof(MiddleShift), 0)
			.SetDisplay("Middle MA Shift", "Bars to shift middle MA", "Moving Averages");

		_middleMaType = Param(nameof(MiddleMaType), MovingAverageMode.Smoothed)
			.SetDisplay("Middle MA Type", "Method for middle MA", "Moving Averages");

		_slowPeriod = Param(nameof(SlowPeriod), 122)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Third moving average", "Moving Averages");

		_slowShift = Param(nameof(SlowShift), 0)
			.SetDisplay("Slow MA Shift", "Bars to shift slow MA", "Moving Averages");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageMode.Smoothed)
			.SetDisplay("Slow MA Type", "Method for slow MA", "Moving Averages");

		_channelPeriod = Param(nameof(ChannelPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Price channel lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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
		_fastShiftIndicator = null;
		_middleShiftIndicator = null;
		_slowShiftIndicator = null;
		_prevFast = 0m;
		_prevMiddle = 0m;
		_prevSlow = 0m;
		_hasPreviousValues = false;
		_longStop = null;
		_longTake = null;
		_longEntryPrice = 0m;
		_longBreakEvenActivated = false;
		_shortStop = null;
		_shortTake = null;
		_shortEntryPrice = 0m;
		_shortBreakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastMaType, FastPeriod);
		_middleMa = CreateMovingAverage(MiddleMaType, MiddlePeriod);
		_slowMa = CreateMovingAverage(SlowMaType, SlowPeriod);
		_channel = new DonchianChannels { Length = ChannelPeriod };

		_fastShiftIndicator = FastShift > 0 ? new Shift { Length = FastShift } : null;
		_middleShiftIndicator = MiddleShift > 0 ? new Shift { Length = MiddleShift } : null;
		_slowShiftIndicator = SlowShift > 0 ? new Shift { Length = SlowShift } : null;

		_tickSize = Security.PriceStep ?? 1m;
		if (_tickSize <= 0)
			_tickSize = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _middleMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _channel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastValue = _fastMa.Process(candle).ToDecimal();
		var middleValue = _middleMa.Process(candle).ToDecimal();
		var slowValue = _slowMa.Process(candle).ToDecimal();

		if (_fastShiftIndicator != null)
			fastValue = _fastShiftIndicator.Process(fastValue, candle.OpenTime, true).ToDecimal();
		if (_middleShiftIndicator != null)
			middleValue = _middleShiftIndicator.Process(middleValue, candle.OpenTime, true).ToDecimal();
		if (_slowShiftIndicator != null)
			slowValue = _slowShiftIndicator.Process(slowValue, candle.OpenTime, true).ToDecimal();

		var channelValue = (DonchianChannelsValue)_channel.Process(candle);
		var channelUpper = channelValue.UpBand as decimal?;
		var channelLower = channelValue.LowBand as decimal?;

		if (!_fastMa.IsFormed || !_middleMa.IsFormed || !_slowMa.IsFormed || !_channel.IsFormed)
			return;

		if ((_fastShiftIndicator != null && !_fastShiftIndicator.IsFormed) ||
			(_middleShiftIndicator != null && !_middleShiftIndicator.IsFormed) ||
			(_slowShiftIndicator != null && !_slowShiftIndicator.IsFormed))
			return;

		UpdateLongTargets(candle, channelUpper, channelLower);
		UpdateShortTargets(candle, channelUpper, channelLower);
		CheckExits(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastValue;
			_prevMiddle = middleValue;
			_prevSlow = slowValue;
			_hasPreviousValues = true;
			return;
		}

		var crossUp = CalculateCrossUp(fastValue, middleValue, slowValue);
		var crossDown = CalculateCrossDown(fastValue, middleValue, slowValue);

		if (crossUp)
		{
			TryEnterLong(candle, channelUpper, channelLower);
		}
		else if (crossDown)
		{
			TryEnterShort(candle, channelUpper, channelLower);
		}

		_prevFast = fastValue;
		_prevMiddle = middleValue;
		_prevSlow = slowValue;
		_hasPreviousValues = true;
	}

	private bool CalculateCrossUp(decimal fastValue, decimal middleValue, decimal slowValue)
	{
		if (TradeOnClose)
		{
			if (!_hasPreviousValues)
				return false;

			var crossMiddle = _prevFast <= _prevMiddle && fastValue > middleValue;
			var crossSlow = _prevFast <= _prevSlow && fastValue > slowValue;
			return crossMiddle && crossSlow;
		}

		return fastValue > middleValue && fastValue > slowValue;
	}

	private bool CalculateCrossDown(decimal fastValue, decimal middleValue, decimal slowValue)
	{
		if (TradeOnClose)
		{
			if (!_hasPreviousValues)
				return false;

			var crossMiddle = _prevFast >= _prevMiddle && fastValue < middleValue;
			var crossSlow = _prevFast >= _prevSlow && fastValue < slowValue;
			return crossMiddle && crossSlow;
		}

		return fastValue < middleValue && fastValue < slowValue;
	}

	private void TryEnterLong(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		if (Position >= 0)
		{
			var maxVolume = Volume * MaxPositionCount;
			var currentLong = Position;
			if (currentLong >= maxVolume)
				return;

			var targetVolume = Math.Min(Volume, maxVolume - currentLong);
			if (targetVolume <= 0m)
				return;

			BuyMarket(targetVolume);
		}
		else
		{
			var requiredVolume = Volume + Math.Abs(Position);
			BuyMarket(requiredVolume);
			ResetShortState();
		}

		_longEntryPrice = candle.ClosePrice;
		_longBreakEvenActivated = false;
		SetLongTargets(candle, channelUpper, channelLower);
	}

	private void TryEnterShort(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		if (Position <= 0)
		{
			var maxVolume = Volume * MaxPositionCount;
			var currentShort = -Position;
			if (currentShort >= maxVolume)
				return;

			var targetVolume = Math.Min(Volume, maxVolume - currentShort);
			if (targetVolume <= 0m)
				return;

			SellMarket(targetVolume);
		}
		else
		{
			var requiredVolume = Volume + Position;
			SellMarket(requiredVolume);
			ResetLongState();
		}

		_shortEntryPrice = candle.ClosePrice;
		_shortBreakEvenActivated = false;
		SetShortTargets(candle, channelUpper, channelLower);
	}

	private void SetLongTargets(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		var entryPrice = candle.ClosePrice;
		var stopDistance = GetDistance(StopLossPips);
		var takeDistance = GetDistance(TakeProfitPips);
		var breakEvenDistance = GetDistance(BreakEvenPips);

		if (UseAutoTargets)
		{
			if (channelLower is decimal lower)
			{
				var candidate = lower;
				if (BreakEvenPips > 0)
					candidate = Math.Max(candidate, entryPrice - breakEvenDistance);
				_longStop = _longStop.HasValue ? Math.Max(_longStop.Value, candidate) : candidate;
			}
			else if (stopDistance > 0m)
			{
				_longStop = entryPrice - stopDistance;
			}

			if (channelUpper is decimal upper)
			{
				var candidate = upper;
				if (BreakEvenPips > 0)
					candidate = Math.Max(candidate, entryPrice + breakEvenDistance);
				_longTake = _longTake.HasValue ? Math.Max(_longTake.Value, candidate) : candidate;
			}
			else if (takeDistance > 0m)
			{
				_longTake = entryPrice + takeDistance;
			}
		}
		else
		{
			_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;
			_longTake = takeDistance > 0m ? entryPrice + takeDistance : null;
		}
	}

	private void SetShortTargets(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		var entryPrice = candle.ClosePrice;
		var stopDistance = GetDistance(StopLossPips);
		var takeDistance = GetDistance(TakeProfitPips);
		var breakEvenDistance = GetDistance(BreakEvenPips);

		if (UseAutoTargets)
		{
			if (channelUpper is decimal upper)
			{
				var candidate = upper;
				if (BreakEvenPips > 0)
					candidate = Math.Min(candidate, entryPrice + breakEvenDistance);
				_shortStop = _shortStop.HasValue ? Math.Min(_shortStop.Value, candidate) : candidate;
			}
			else if (stopDistance > 0m)
			{
				_shortStop = entryPrice + stopDistance;
			}

			if (channelLower is decimal lower)
			{
				var candidate = lower;
				if (BreakEvenPips > 0)
					candidate = Math.Min(candidate, entryPrice - breakEvenDistance);
				_shortTake = _shortTake.HasValue ? Math.Min(_shortTake.Value, candidate) : candidate;
			}
			else if (takeDistance > 0m)
			{
				_shortTake = entryPrice - takeDistance;
			}
		}
		else
		{
			_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;
			_shortTake = takeDistance > 0m ? entryPrice - takeDistance : null;
		}
	}

	private void UpdateLongTargets(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		if (Position <= 0)
		{
			ResetLongState();
			return;
		}

		var breakEvenDistance = GetDistance(BreakEvenPips);
		var trailingDistance = GetDistance(TrailingStopPips);
		var trailingStep = GetDistance(TrailingStepPips);
		var entryPrice = PositionPrice != 0m ? PositionPrice : _longEntryPrice;

		if (UseAutoTargets && channelLower is decimal lower)
		{
			var candidate = lower;
			if (BreakEvenPips > 0)
				candidate = Math.Max(candidate, entryPrice - breakEvenDistance);
			_longStop = _longStop.HasValue ? Math.Max(_longStop.Value, candidate) : candidate;
		}

		if (UseAutoTargets && channelUpper is decimal upper)
		{
			var candidate = upper;
			if (BreakEvenPips > 0)
				candidate = Math.Max(candidate, entryPrice + breakEvenDistance);
			_longTake = _longTake.HasValue ? Math.Max(_longTake.Value, candidate) : candidate;
		}

		if (trailingDistance > 0m)
		{
			var candidate = candle.ClosePrice - trailingDistance;
			if (_longStop is decimal currentStop)
			{
				if (candidate - currentStop >= Math.Max(trailingStep, _tickSize))
					_longStop = candidate;
			}
			else
			{
				_longStop = candidate;
			}
		}

		if (BreakEvenPips > 0 && !_longBreakEvenActivated)
		{
			var activationPrice = entryPrice + breakEvenDistance + Math.Max(0m, trailingStep);
			var targetStop = entryPrice + breakEvenDistance;
			if (candle.ClosePrice >= activationPrice)
			{
				_longBreakEvenActivated = true;
				_longStop = _longStop.HasValue ? Math.Max(_longStop.Value, targetStop) : targetStop;
			}
		}
	}

	private void UpdateShortTargets(ICandleMessage candle, decimal? channelUpper, decimal? channelLower)
	{
		if (Position >= 0)
		{
			ResetShortState();
			return;
		}

		var breakEvenDistance = GetDistance(BreakEvenPips);
		var trailingDistance = GetDistance(TrailingStopPips);
		var trailingStep = GetDistance(TrailingStepPips);
		var entryPrice = PositionPrice != 0m ? PositionPrice : _shortEntryPrice;

		if (UseAutoTargets && channelUpper is decimal upper)
		{
			var candidate = upper;
			if (BreakEvenPips > 0)
				candidate = Math.Min(candidate, entryPrice + breakEvenDistance);
			_shortStop = _shortStop.HasValue ? Math.Min(_shortStop.Value, candidate) : candidate;
		}

		if (UseAutoTargets && channelLower is decimal lower)
		{
			var candidate = lower;
			if (BreakEvenPips > 0)
				candidate = Math.Min(candidate, entryPrice - breakEvenDistance);
			_shortTake = _shortTake.HasValue ? Math.Min(_shortTake.Value, candidate) : candidate;
		}

		if (trailingDistance > 0m)
		{
			var candidate = candle.ClosePrice + trailingDistance;
			if (_shortStop is decimal currentStop)
			{
				if (currentStop - candidate >= Math.Max(trailingStep, _tickSize))
					_shortStop = candidate;
			}
			else
			{
				_shortStop = candidate;
			}
		}

		if (BreakEvenPips > 0 && !_shortBreakEvenActivated)
		{
			var activationPrice = entryPrice - breakEvenDistance - Math.Max(0m, trailingStep);
			var targetStop = entryPrice - breakEvenDistance;
			if (candle.ClosePrice <= activationPrice)
			{
				_shortBreakEvenActivated = true;
				_shortStop = _shortStop.HasValue ? Math.Min(_shortStop.Value, targetStop) : targetStop;
			}
		}
	}

	private void CheckExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetLongState();
			}
			else if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
		else if (Position < 0)
		{
			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
			else if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
		}
	}

	private decimal GetDistance(int pips)
	{
		return pips <= 0 ? 0m : pips * _tickSize;
	}

	private void ResetLongState()
	{
		_longStop = null;
		_longTake = null;
		_longEntryPrice = 0m;
		_longBreakEvenActivated = false;
	}

	private void ResetShortState()
	{
		_shortStop = null;
		_shortTake = null;
		_shortEntryPrice = 0m;
		_shortBreakEvenActivated = false;
	}

	private MovingAverage CreateMovingAverage(MovingAverageMode mode, int length)
	{
		return mode switch
		{
			MovingAverageMode.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMode.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageMode.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}

/// <summary>
/// Moving average calculation modes supported by <see cref="TripleMaChannelCrossoverStrategy"/>.
/// </summary>
public enum MovingAverageMode
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average (SMMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Weighted,
}
