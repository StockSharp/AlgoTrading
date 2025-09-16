namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy combining Bollinger Bands, two moving averages, and swing points from a ZigZag-like detector.
/// Replicates the Bollinger Band Two MA and Zig-Zag expert logic with two staged entries.
/// </summary>
public class BollingerBandTwoMaZigZagStrategy : Strategy
{
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _secondVolume;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _spacingFromPivot;
	private readonly StrategyParam<bool> _useStopLossProtection;
	private readonly StrategyParam<decimal> _stopLossFromPoints;
	private readonly StrategyParam<decimal> _stopLossLevelPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _ma1CandleType;
	private readonly StrategyParam<DataType> _ma2CandleType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviation;
	private readonly StrategyParam<int> _zigZagBackstep;

	private decimal? _ma1Value;
	private decimal? _ma2Value;
	private decimal? _prevClose1;
	private decimal? _prevClose2;
	private decimal? _prevLowerBand;
	private decimal? _prevUpperBand;

	private decimal? _lastPivotLow;
	private decimal? _lastPivotHigh;
	private int _zigZagDirection;
	private int _barsSincePivot;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTarget;
	private decimal? _shortTarget;
	private bool _longPartialClosed;
	private bool _shortPartialClosed;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Volume for the position that uses a fixed take profit.
	/// </summary>
	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	/// <summary>
	/// Runner volume that trails without a fixed profit target.
	/// </summary>
	public decimal SecondVolume
	{
		get => _secondVolume.Value;
		set => _secondVolume.Value = value;
	}

	/// <summary>
	/// Take profit percent relative to the distance between entry and stop.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Extra points added beyond the pivot for stop placement.
	/// </summary>
	public decimal SpacingFromPivot
	{
		get => _spacingFromPivot.Value;
		set => _spacingFromPivot.Value = value;
	}

	/// <summary>
	/// Enable break-even style stop adjustment.
	/// </summary>
	public bool UseStopLossProtection
	{
		get => _useStopLossProtection.Value;
		set => _useStopLossProtection.Value = value;
	}

	/// <summary>
	/// Points locked in once break-even triggers.
	/// </summary>
	public decimal StopLossFromPoints
	{
		get => _stopLossFromPoints.Value;
		set => _stopLossFromPoints.Value = value;
	}

	/// <summary>
	/// Additional points of profit required before moving the stop.
	/// </summary>
	public decimal StopLossLevelPoints
	{
		get => _stopLossLevelPoints.Value;
		set => _stopLossLevelPoints.Value = value;
	}

	/// <summary>
	/// Distance of the trailing stop in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Step that price must move before the trailing stop is advanced.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger width multiplier.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Main candle type for Bollinger calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the first moving average filter.
	/// </summary>
	public DataType Ma1CandleType
	{
		get => _ma1CandleType.Value;
		set => _ma1CandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the second moving average filter.
	/// </summary>
	public DataType Ma2CandleType
	{
		get => _ma2CandleType.Value;
		set => _ma2CandleType.Value = value;
	}

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Lookback for swing detection.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Minimum deviation between swings in points.
	/// </summary>
	public decimal ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	/// <summary>
	/// Minimum bars between successive pivots.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="BollingerBandTwoMaZigZagStrategy"/>.
	/// </summary>
	public BollingerBandTwoMaZigZagStrategy()
	{
		_firstVolume = Param(nameof(FirstVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("First Volume", "Volume with take profit", "Trading")
			.SetCanOptimize(true);

		_secondVolume = Param(nameof(SecondVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Second Volume", "Runner volume", "Trading")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 50m)
			.SetDisplay("Take Profit %", "Percent of stop distance for TP", "Risk")
			.SetCanOptimize(true);

		_spacingFromPivot = Param(nameof(SpacingFromPivot), 10m)
			.SetDisplay("Pivot Offset (pts)", "Extra points beyond swing", "Risk")
			.SetCanOptimize(true);

		_useStopLossProtection = Param(nameof(UseStopLossProtection), true)
			.SetDisplay("Use Break-even Move", "Enable stop pull-up", "Risk");

		_stopLossFromPoints = Param(nameof(StopLossFromPoints), 80m)
			.SetDisplay("Break-even Offset (pts)", "Points locked after trigger", "Risk")
			.SetCanOptimize(true);

		_stopLossLevelPoints = Param(nameof(StopLossLevelPoints), 10m)
			.SetDisplay("Break-even Threshold (pts)", "Extra profit before moving stop", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 80m)
			.SetDisplay("Trailing Stop (pts)", "Trailing distance", "Risk")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 120m)
			.SetDisplay("Trailing Step (pts)", "Minimum move before trailing", "Risk")
			.SetCanOptimize(true);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bars for Bollinger Bands", "Bollinger")
			.SetCanOptimize(true);

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Deviation multiplier", "Bollinger")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Base Candle", "Primary timeframe", "General");

		_ma1CandleType = Param(nameof(Ma1CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("MA1 Candle", "Higher timeframe for MA1", "General");

		_ma2CandleType = Param(nameof(Ma2CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("MA2 Candle", "Higher timeframe for MA2", "General");

		_ma1Period = Param(nameof(Ma1Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Bars for first MA", "Filters")
			.SetCanOptimize(true);

		_ma2Period = Param(nameof(Ma2Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Bars for second MA", "Filters")
			.SetCanOptimize(true);

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Depth", "Lookback for pivots", "ZigZag")
			.SetCanOptimize(true);

		_zigZagDeviation = Param(nameof(ZigZagDeviation), 5m)
			.SetDisplay("ZigZag Deviation (pts)", "Minimum pivot distance", "ZigZag")
			.SetCanOptimize(true);

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Backstep", "Bars between pivots", "ZigZag")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!Equals(CandleType, Ma1CandleType))
			yield return (Security, Ma1CandleType);

		if (!Equals(CandleType, Ma2CandleType) && !Equals(Ma1CandleType, Ma2CandleType))
			yield return (Security, Ma2CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1Value = null;
		_ma2Value = null;
		_prevClose1 = null;
		_prevClose2 = null;
		_prevLowerBand = null;
		_prevUpperBand = null;

		_lastPivotLow = null;
		_lastPivotHigh = null;
		_zigZagDirection = 0;
		_barsSincePivot = 0;

		_longStop = null;
		_shortStop = null;
		_longTarget = null;
		_shortTarget = null;
		_longPartialClosed = false;
		_shortPartialClosed = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var highest = new Highest { Length = Math.Max(1, ZigZagDepth) };
		var lowest = new Lowest { Length = Math.Max(1, ZigZagDepth) };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(bollinger, highest, lowest, ProcessMain).Start();

		var ma1 = new SimpleMovingAverage { Length = Ma1Period };
		var ma1Subscription = SubscribeCandles(Ma1CandleType);
		ma1Subscription.Bind(ma1, ProcessMa1).Start();

		var ma2 = new SimpleMovingAverage { Length = Ma2Period };
		var ma2Subscription = SubscribeCandles(Ma2CandleType);
		ma2Subscription.Bind(ma2, ProcessMa2).Start();
	}

	private void ProcessMa1(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_ma1Value = maValue;
	}

	private void ProcessMa2(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_ma2Value = maValue;
	}

	private void ProcessMain(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = GetPriceStep();

		UpdateZigZag(candle, highest, lowest, step);

		ManageOpenPositions(candle, step);

		var longSignal = ShouldEnterLong(candle);
		var shortSignal = ShouldEnterShort(candle);

		if (longSignal)
			TryEnterLong(candle, step);
		else if (shortSignal)
			TryEnterShort(candle, step);

		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
		_prevLowerBand = lower;
		_prevUpperBand = upper;
	}

	private bool ShouldEnterLong(ICandleMessage candle)
	{
		if (!_ma1Value.HasValue || !_ma2Value.HasValue || !_prevClose1.HasValue || !_prevClose2.HasValue || !_prevLowerBand.HasValue)
			return false;

		var maFilter = candle.ClosePrice >= _ma1Value.Value && candle.ClosePrice >= _ma2Value.Value;
		var crossUp = _prevClose2.Value <= _prevLowerBand.Value && _prevClose1.Value >= _prevLowerBand.Value && candle.ClosePrice >= _prevLowerBand.Value;

		return maFilter && crossUp;
	}

	private bool ShouldEnterShort(ICandleMessage candle)
	{
		if (!_ma1Value.HasValue || !_ma2Value.HasValue || !_prevClose1.HasValue || !_prevClose2.HasValue || !_prevUpperBand.HasValue)
			return false;

		var maFilter = candle.ClosePrice <= _ma1Value.Value && candle.ClosePrice <= _ma2Value.Value;
		var crossDown = _prevClose2.Value >= _prevUpperBand.Value && _prevClose1.Value <= _prevUpperBand.Value && candle.ClosePrice <= _prevUpperBand.Value;

		return maFilter && crossDown;
	}

	private void TryEnterLong(ICandleMessage candle, decimal step)
	{
		if (Position > 0 || !_lastPivotLow.HasValue)
			return;

		if (FirstVolume <= 0m || SecondVolume <= 0m)
			return;

		var stop = _lastPivotLow.Value - SpacingFromPivot * step;
		if (step <= 0m)
			return;

		if (stop >= candle.ClosePrice)
			return;

		CloseShortPositions();

		_longStop = stop;
		_longTarget = CalculateLongTarget(candle.ClosePrice, stop);
		_longEntryPrice = candle.ClosePrice;
		_longPartialClosed = false;

		BuyMarket(FirstVolume);
		BuyMarket(SecondVolume);

		ClearShortState();
	}

	private void TryEnterShort(ICandleMessage candle, decimal step)
	{
		if (Position < 0 || !_lastPivotHigh.HasValue)
			return;

		if (FirstVolume <= 0m || SecondVolume <= 0m)
			return;

		var stop = _lastPivotHigh.Value + SpacingFromPivot * step;
		if (step <= 0m)
			return;

		if (stop <= candle.ClosePrice)
			return;

		CloseLongPositions();

		_shortStop = stop;
		_shortTarget = CalculateShortTarget(candle.ClosePrice, stop);
		_shortEntryPrice = candle.ClosePrice;
		_shortPartialClosed = false;

		SellMarket(FirstVolume);
		SellMarket(SecondVolume);

		ClearLongState();
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal step)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ClearLongState();
			}
			else
			{
				if (_longTarget.HasValue && !_longPartialClosed && candle.HighPrice >= _longTarget.Value)
				{
					var volume = Math.Min(Position, FirstVolume);
					if (volume > 0m)
					{
						SellMarket(volume);
						_longPartialClosed = true;
					}
				}

				if (UseStopLossProtection && _longEntryPrice.HasValue && step > 0m)
				{
					var profitPoints = (candle.ClosePrice - _longEntryPrice.Value) / step;
					var threshold = StopLossLevelPoints + StopLossFromPoints;
					if (profitPoints >= threshold)
					{
						var newStop = _longEntryPrice.Value + StopLossFromPoints * step;
						if (_longStop is null || _longStop.Value < newStop)
							_longStop = newStop;
					}
				}

				if (TrailingStopPoints > 0m && _longStop.HasValue && step > 0m)
				{
					var profitPoints = (candle.ClosePrice - _longStop.Value) / step;
					var minAdvance = (TrailingStopPoints + TrailingStepPoints) * step;
					if (profitPoints >= TrailingStopPoints && candle.ClosePrice > _longStop.Value + minAdvance)
						_longStop = candle.ClosePrice - TrailingStopPoints * step;
				}
			}
		}
		else
		{
			ClearLongState();
		}

		if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(-Position);
				ClearShortState();
			}
			else
			{
				if (_shortTarget.HasValue && !_shortPartialClosed && candle.LowPrice <= _shortTarget.Value)
				{
					var volume = Math.Min(-Position, FirstVolume);
					if (volume > 0m)
					{
						BuyMarket(volume);
						_shortPartialClosed = true;
					}
				}

				if (UseStopLossProtection && _shortEntryPrice.HasValue && step > 0m)
				{
					var profitPoints = (_shortEntryPrice.Value - candle.ClosePrice) / step;
					var threshold = StopLossLevelPoints + StopLossFromPoints;
					if (profitPoints >= threshold)
					{
						var newStop = _shortEntryPrice.Value - StopLossFromPoints * step;
						if (_shortStop is null || _shortStop.Value > newStop)
							_shortStop = newStop;
					}
				}

				if (TrailingStopPoints > 0m && _shortStop.HasValue && step > 0m)
				{
					var profitPoints = (_shortStop.Value - candle.ClosePrice) / step;
					var minAdvance = (TrailingStopPoints + TrailingStepPoints) * step;
					if (profitPoints >= TrailingStopPoints && candle.ClosePrice < _shortStop.Value - minAdvance)
						_shortStop = candle.ClosePrice + TrailingStopPoints * step;
				}
			}
		}
		else
		{
			ClearShortState();
		}
	}

	private void UpdateZigZag(ICandleMessage candle, decimal highest, decimal lowest, decimal step)
	{
		_barsSincePivot++;

		var deviation = ZigZagDeviation * step;
		var backstep = Math.Max(1, ZigZagBackstep);

		if (_barsSincePivot < backstep)
			return;

		var updated = false;

		if (candle.HighPrice >= highest && _zigZagDirection != 1)
		{
			if (!_lastPivotLow.HasValue || deviation <= 0m || candle.HighPrice - _lastPivotLow.Value >= deviation)
			{
				_lastPivotHigh = candle.HighPrice;
				_zigZagDirection = 1;
				updated = true;
			}
		}

		if (candle.LowPrice <= lowest && _zigZagDirection != -1)
		{
			if (!_lastPivotHigh.HasValue || deviation <= 0m || _lastPivotHigh.Value - candle.LowPrice >= deviation)
			{
				_lastPivotLow = candle.LowPrice;
				_zigZagDirection = -1;
				updated = true;
			}
		}

		if (updated)
			_barsSincePivot = 0;
	}

	private void CloseShortPositions()
	{
		if (Position < 0)
		{
			BuyMarket(-Position);
			ClearShortState();
		}
	}

	private void CloseLongPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			ClearLongState();
		}
	}

	private void ClearLongState()
	{
		if (Position > 0)
			return;

		_longStop = null;
		_longTarget = null;
		_longPartialClosed = false;
		_longEntryPrice = null;
	}

	private void ClearShortState()
	{
		if (Position < 0)
			return;

		_shortStop = null;
		_shortTarget = null;
		_shortPartialClosed = false;
		_shortEntryPrice = null;
	}

	private decimal? CalculateLongTarget(decimal entryPrice, decimal stopPrice)
	{
		if (TakeProfitPercent <= 0m || stopPrice >= entryPrice)
			return null;

		return entryPrice + (entryPrice - stopPrice) * (TakeProfitPercent / 100m);
	}

	private decimal? CalculateShortTarget(decimal entryPrice, decimal stopPrice)
	{
		if (TakeProfitPercent <= 0m || stopPrice <= entryPrice)
			return null;

		return entryPrice - (stopPrice - entryPrice) * (TakeProfitPercent / 100m);
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? Security?.MinPriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}
}