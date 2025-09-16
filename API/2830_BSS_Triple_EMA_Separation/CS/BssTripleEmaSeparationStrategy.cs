using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted,
}

public class BssTripleEmaSeparationStrategy : Strategy
{
	// Small epsilon used to compare decimal volumes without floating point noise.
	private const decimal VolumeTolerance = 1e-8m;

	// User configurable parameters.
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minimumDistance;
	private readonly StrategyParam<int> _minimumPauseSeconds;
	private readonly StrategyParam<int> _firstMaPeriod;
	private readonly StrategyParam<int> _secondMaPeriod;
	private readonly StrategyParam<int> _thirdMaPeriod;
	private readonly StrategyParam<MaMethod> _firstMaMethod;
	private readonly StrategyParam<MaMethod> _secondMaMethod;
	private readonly StrategyParam<MaMethod> _thirdMaMethod;
	private readonly StrategyParam<DataType> _candleType;

	// Indicator instances created according to the selected parameters.
	private IIndicator _firstMa = null!;
	private IIndicator _secondMa = null!;
	private IIndicator _thirdMa = null!;

	// Timestamp of the last position entry used to enforce the pause between trades.
	private DateTimeOffset? _lastEntryTime;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public decimal MinimumDistance
	{
		get => _minimumDistance.Value;
		set => _minimumDistance.Value = value;
	}

	public int MinimumPauseSeconds
	{
		get => _minimumPauseSeconds.Value;
		set => _minimumPauseSeconds.Value = value;
	}

	public int FirstMaPeriod
	{
		get => _firstMaPeriod.Value;
		set => _firstMaPeriod.Value = value;
	}

	public int SecondMaPeriod
	{
		get => _secondMaPeriod.Value;
		set => _secondMaPeriod.Value = value;
	}

	public int ThirdMaPeriod
	{
		get => _thirdMaPeriod.Value;
		set => _thirdMaPeriod.Value = value;
	}

	public MaMethod FirstMaMethod
	{
		get => _firstMaMethod.Value;
		set => _firstMaMethod.Value = value;
	}

	public MaMethod SecondMaMethod
	{
		get => _secondMaMethod.Value;
		set => _secondMaMethod.Value = value;
	}

	public MaMethod ThirdMaMethod
	{
		get => _thirdMaMethod.Value;
		set => _thirdMaMethod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BssTripleEmaSeparationStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for each entry order", "Trading");

		_maxPositions = Param(nameof(MaxPositions), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum simultaneous entries per direction", "Risk");

		_minimumDistance = Param(nameof(MinimumDistance), 0.0005m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Distance", "Minimum price gap between moving averages", "Signals");

		_minimumPauseSeconds = Param(nameof(MinimumPauseSeconds), 600)
			.SetGreaterOrEqualZero()
			.SetDisplay("Minimum Pause (sec)", "Pause between new entries in seconds", "Risk");

		_firstMaPeriod = Param(nameof(FirstMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("First MA Period", "Period for the fastest moving average", "Indicators");

		_firstMaMethod = Param(nameof(FirstMaMethod), MaMethod.Exponential)
			.SetDisplay("First MA Method", "Smoothing method for the fastest moving average", "Indicators");

		_secondMaPeriod = Param(nameof(SecondMaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Second MA Period", "Period for the medium moving average", "Indicators");

		_secondMaMethod = Param(nameof(SecondMaMethod), MaMethod.Exponential)
			.SetDisplay("Second MA Method", "Smoothing method for the medium moving average", "Indicators");

		_thirdMaPeriod = Param(nameof(ThirdMaPeriod), 125)
			.SetGreaterThanZero()
			.SetDisplay("Third MA Period", "Period for the slowest moving average", "Indicators");

		_thirdMaMethod = Param(nameof(ThirdMaMethod), MaMethod.Exponential)
			.SetDisplay("Third MA Method", "Smoothing method for the slowest moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_lastEntryTime = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (FirstMaPeriod >= SecondMaPeriod)
			throw new InvalidOperationException("First MA period must be less than second MA period.");

		if (SecondMaPeriod >= ThirdMaPeriod)
			throw new InvalidOperationException("Second MA period must be less than third MA period.");

		_firstMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod);
		_secondMa = CreateMovingAverage(SecondMaMethod, SecondMaPeriod);
		_thirdMa = CreateMovingAverage(ThirdMaMethod, ThirdMaPeriod);

		_lastEntryTime = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_firstMa, _secondMa, _thirdMa, ProcessCandle).Start();

		StartProtection();
	}

	private static IIndicator CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SMA { Length = period },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new ExponentialMovingAverage { Length = period },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal firstValue, decimal secondValue, decimal thirdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_firstMa.IsFormed || !_secondMa.IsFormed || !_thirdMa.IsFormed)
			return;

		var minDistance = MinimumDistance;

		var longSpreadOk = thirdValue - secondValue >= minDistance && secondValue - firstValue >= minDistance;
		var shortSpreadOk = firstValue - secondValue >= minDistance && secondValue - thirdValue >= minDistance;

		if (!longSpreadOk && !shortSpreadOk)
			return;

		var time = candle.OpenTime;

		if (longSpreadOk)
		{
			if (TryCloseOppositePositions(true))
				return;

			if (CanEnterPosition(time, true))
				BuyMarket(OrderVolume);

			return;
		}

		if (shortSpreadOk)
		{
			if (TryCloseOppositePositions(false))
				return;

			if (CanEnterPosition(time, false))
				SellMarket(OrderVolume);
		}
	}

	private bool CanEnterPosition(DateTimeOffset time, bool isLong)
	{
		// Trading is allowed only when the strategy is ready, the pause elapsed, and exposure stays within bounds.
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		if (!IsPauseElapsed(time))
			return false;

		var targetPosition = Position + (isLong ? OrderVolume : -OrderVolume);
		var maxExposure = MaxPositions * OrderVolume;

		return Math.Abs(targetPosition) <= maxExposure + VolumeTolerance;
	}

	private bool IsPauseElapsed(DateTimeOffset time)
	{
		var pauseSeconds = MinimumPauseSeconds;

		if (pauseSeconds <= 0)
			return true;

		if (_lastEntryTime is null)
			return true;

		return time - _lastEntryTime.Value >= TimeSpan.FromSeconds(pauseSeconds);
	}

	private bool TryCloseOppositePositions(bool isLong)
	{
		// Close active trades in the opposite direction before opening a new position.
		if (isLong)
		{
			if (Position < -VolumeTolerance)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}
		else
		{
			if (Position > VolumeTolerance)
			{
				SellMarket(Position);
				return true;
			}
		}

		return false;
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var previousPosition = Position - delta;

		// Record the fill time whenever the absolute exposure increases (new entry or scale in).
		if (Math.Abs(Position) > Math.Abs(previousPosition) + VolumeTolerance)
			_lastEntryTime = CurrentTime;
	}
}
