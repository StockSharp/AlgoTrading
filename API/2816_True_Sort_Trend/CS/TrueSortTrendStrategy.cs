using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that requires five exponential moving averages to be sorted in the same order for two consecutive candles.
/// Uses ADX to confirm trend strength and supports optional stop-loss, take-profit, and trailing stop distances expressed in price units.
/// </summary>
public class TrueSortTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _secondEmaLength;
	private readonly StrategyParam<int> _thirdEmaLength;
	private readonly StrategyParam<int> _fourthEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _trailingStopDistance;
	private readonly StrategyParam<decimal> _trailingStepDistance;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _secondEma;
	private ExponentialMovingAverage _thirdEma;
	private ExponentialMovingAverage _fourthEma;
	private ExponentialMovingAverage _slowEma;
	private AverageDirectionalIndex _adx;

	private decimal? _prevFast;
	private decimal? _prevSecond;
	private decimal? _prevThird;
	private decimal? _prevFourth;
	private decimal? _prevSlow;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private int _positionDirection;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fastest EMA.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the second EMA.
	/// </summary>
	public int SecondEmaLength
	{
		get => _secondEmaLength.Value;
		set => _secondEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the third EMA.
	/// </summary>
	public int ThirdEmaLength
	{
		get => _thirdEmaLength.Value;
		set => _thirdEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the fourth EMA.
	/// </summary>
	public int FourthEmaLength
	{
		get => _fourthEmaLength.Value;
		set => _fourthEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slowest EMA.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// ADX averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value that confirms a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss distance expressed in price units.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Absolute take-profit distance expressed in price units.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStopDistance
	{
		get => _trailingStopDistance.Value;
		set => _trailingStopDistance.Value = value;
	}

	/// <summary>
	/// Minimum price advance required before the trailing stop is activated or moved.
	/// </summary>
	public decimal TrailingStepDistance
	{
		get => _trailingStepDistance.Value;
		set => _trailingStepDistance.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrueSortTrendStrategy"/> class.
	/// </summary>
	public TrueSortTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period of the fastest EMA", "Moving Averages")
			
			.SetOptimize(5, 20, 1);

		_secondEmaLength = Param(nameof(SecondEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Second EMA Length", "Period of the second EMA", "Moving Averages")
			
			.SetOptimize(10, 40, 2);

		_thirdEmaLength = Param(nameof(ThirdEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Third EMA Length", "Period of the third EMA", "Moving Averages")
			
			.SetOptimize(30, 80, 5);

		_fourthEmaLength = Param(nameof(FourthEmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Fourth EMA Length", "Period of the fourth EMA", "Moving Averages")
			
			.SetOptimize(60, 140, 5);

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period of the slowest EMA", "Moving Averages")
			
			.SetOptimize(150, 250, 5);

		_adxPeriod = Param(nameof(AdxPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Averaging period for the ADX indicator", "Indicators")
			
			.SetOptimize(14, 40, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 10m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators")
			
			.SetOptimize(15m, 35m, 5m);

		_stopLossDistance = Param(nameof(StopLossDistance), 500m)
			.SetRange(0m, 1m)
			.SetDisplay("Stop Loss Distance", "Absolute distance for stop loss", "Risk")
			
			.SetOptimize(0.001m, 0.01m, 0.001m);

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 1500m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Distance", "Absolute distance for take profit", "Risk")
			
			.SetOptimize(0.002m, 0.03m, 0.002m);

		_trailingStopDistance = Param(nameof(TrailingStopDistance), 300m)
			.SetRange(0m, 1m)
			.SetDisplay("Trailing Stop Distance", "Trailing stop distance in price units", "Risk")
			
			.SetOptimize(0.0002m, 0.002m, 0.0002m);

		_trailingStepDistance = Param(nameof(TrailingStepDistance), 100m)
			.SetRange(0m, 1m)
			.SetDisplay("Trailing Step Distance", "Additional advance before trailing stop adjustment", "Risk")
			
			.SetOptimize(0m, 0.001m, 0.0001m);
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

		_prevFast = null;
		_prevSecond = null;
		_prevThird = null;
		_prevFourth = null;
		_prevSlow = null;

		ResetTradeState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_secondEma = new ExponentialMovingAverage { Length = SecondEmaLength };
		_thirdEma = new ExponentialMovingAverage { Length = ThirdEmaLength };
		_fourthEma = new ExponentialMovingAverage { Length = FourthEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandleRaw)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandleRaw(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastResult = _fastEma.Process(candle);
		var secondResult = _secondEma.Process(candle);
		var thirdResult = _thirdEma.Process(candle);
		var fourthResult = _fourthEma.Process(candle);
		var slowResult = _slowEma.Process(candle);
		var adxResult = _adx.Process(candle);

		if (!_slowEma.IsFormed || !_adx.IsFormed)
			return;

		if (fastResult.IsEmpty || secondResult.IsEmpty || thirdResult.IsEmpty || fourthResult.IsEmpty || slowResult.IsEmpty || adxResult.IsEmpty)
			return;

		var fast = fastResult.ToDecimal();
		var second = secondResult.ToDecimal();
		var third = thirdResult.ToDecimal();
		var fourth = fourthResult.ToDecimal();
		var slow = slowResult.ToDecimal();

		var adxData = (AverageDirectionalIndexValue)adxResult;
		var adxValue = adxData.MovingAverage ?? 0m;

		if (_prevFast is null || _prevSecond is null || _prevThird is null || _prevFourth is null || _prevSlow is null)
		{
			UpdatePreviousValues(fast, second, third, fourth, slow);
			return;
		}

		var ascendingCurrent = fast > second && second > third && third > fourth && fourth > slow;
		var descendingCurrent = fast < second && second < third && third < fourth && fourth < slow;
		var ascendingPrevious = _prevFast > _prevSecond && _prevSecond > _prevThird && _prevThird > _prevFourth && _prevFourth > _prevSlow;
		var descendingPrevious = _prevFast < _prevSecond && _prevSecond < _prevThird && _prevThird < _prevFourth && _prevFourth < _prevSlow;

		var openLongSignal = adxValue > AdxThreshold && ascendingCurrent && ascendingPrevious;
		var openShortSignal = adxValue > AdxThreshold && descendingCurrent && descendingPrevious;
		var breakLongStack = !ascendingCurrent;
		var breakShortStack = !descendingCurrent;

		var position = Position;

		if (position == 0 && _positionDirection != 0)
			ResetTradeState();

		if (position > 0)
		{
			_highestPrice = _highestPrice == 0m ? candle.HighPrice : Math.Max(_highestPrice, candle.HighPrice);
			_lowestPrice = candle.LowPrice;

			var exitVolume = Math.Abs(position);
			var shouldExit = false;

			if (!shouldExit && StopLossDistance > 0m && candle.LowPrice <= _entryPrice - StopLossDistance)
				shouldExit = true;

			if (!shouldExit && TakeProfitDistance > 0m && candle.HighPrice >= _entryPrice + TakeProfitDistance)
				shouldExit = true;

			if (!shouldExit && TrailingStopDistance > 0m)
			{
				var activation = TrailingStopDistance + Math.Max(0m, TrailingStepDistance);
				if (_highestPrice - _entryPrice >= activation)
				{
					var trailingLevel = _highestPrice - TrailingStopDistance;
					if (candle.ClosePrice <= trailingLevel)
						shouldExit = true;
				}
			}

			if (!shouldExit && breakLongStack)
				shouldExit = true;

			if (shouldExit && exitVolume > 0)
			{
				SellMarket(exitVolume);
				ResetTradeState();
				position = 0;
			}
		}
		else if (position < 0)
		{
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
			_highestPrice = candle.HighPrice;

			var exitVolume = Math.Abs(position);
			var shouldExit = false;

			if (!shouldExit && StopLossDistance > 0m && candle.HighPrice >= _entryPrice + StopLossDistance)
				shouldExit = true;

			if (!shouldExit && TakeProfitDistance > 0m && candle.LowPrice <= _entryPrice - TakeProfitDistance)
				shouldExit = true;

			if (!shouldExit && TrailingStopDistance > 0m)
			{
				var activation = TrailingStopDistance + Math.Max(0m, TrailingStepDistance);
				if (_entryPrice - _lowestPrice >= activation)
				{
					var trailingLevel = _lowestPrice + TrailingStopDistance;
					if (candle.ClosePrice >= trailingLevel)
						shouldExit = true;
				}
			}

			if (!shouldExit && breakShortStack)
				shouldExit = true;

			if (shouldExit && exitVolume > 0)
			{
				BuyMarket(exitVolume);
				ResetTradeState();
				position = 0;
			}
		}

		if (position <= 0 && openLongSignal)
		{
			var volumeToBuy = Volume + Math.Max(0m, -position);
			if (volumeToBuy > 0)
			{
				BuyMarket(volumeToBuy);
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.HighPrice;
				_lowestPrice = candle.LowPrice;
				_positionDirection = 1;
			}
		}
		else if (position >= 0 && openShortSignal)
		{
			var volumeToSell = Volume + Math.Max(0m, position);
			if (volumeToSell > 0)
			{
				SellMarket(volumeToSell);
				_entryPrice = candle.ClosePrice;
				_highestPrice = candle.HighPrice;
				_lowestPrice = candle.LowPrice;
				_positionDirection = -1;
			}
		}

		if (Position == 0 && _positionDirection != 0)
			ResetTradeState();

		UpdatePreviousValues(fast, second, third, fourth, slow);
	}

	private void UpdatePreviousValues(decimal fast, decimal second, decimal third, decimal fourth, decimal slow)
	{
		_prevFast = fast;
		_prevSecond = second;
		_prevThird = third;
		_prevFourth = fourth;
		_prevSlow = slow;
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_positionDirection = 0;
	}
}