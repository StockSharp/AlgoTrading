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
/// Strategy that monitors how far the market trades relative to a moving average.
/// Buys when the market opens and trades below a rising moving average.
/// Sells when the market opens and trades above a falling moving average.
/// Only one position is allowed at a time and the opposite side is closed before reversing.
/// </summary>
public class AboveBelowMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethods> _maMethod;
	private readonly StrategyParam<AppliedPriceTypes> _appliedPrice;
	private readonly StrategyParam<int> _minimumDistancePips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private LengthIndicator<decimal> _ma;
	private readonly Queue<decimal> _maValues = new();
	private decimal? _currentMa;
	private decimal? _previousMa;
	private bool _isInitialized;

	/// <summary>
	/// Moving average period length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars that the moving average is shifted forward.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethods MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used in the moving average calculation.
	/// </summary>
	public AppliedPriceTypes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and the moving average measured in pips.
	/// </summary>
	public int MinimumDistancePips
	{
		get => _minimumDistancePips.Value;
		set => _minimumDistancePips.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Position size for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with default values.
	/// </summary>
	public AboveBelowMaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(3, 60, 1);

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Number of bars to shift the MA forward", "Moving Average");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethods.Exponential)
			.SetDisplay("MA Method", "Moving average smoothing method", "Moving Average");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceTypes.Typical)
			.SetDisplay("Applied Price", "Price type passed to the moving average", "Moving Average");

		_minimumDistancePips = Param(nameof(MinimumDistancePips), 5)
			.SetNotNegative()
			.SetDisplay("Minimum Distance (pips)", "Required distance between price and the MA", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "General");
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

		_ma = null;
		_maValues.Clear();
		_currentMa = null;
		_previousMa = null;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaMethod, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var indicator = _ma;
		if (indicator == null)
			return;

		var isFinished = candle.State == CandleStates.Finished;
		var price = GetAppliedPrice(candle);
		var maValue = indicator.Process(price, candle.OpenTime, isFinished).ToNullableDecimal();

		if (!isFinished || maValue == null)
			return;

		var shiftedValue = GetShiftedValue(maValue.Value);
		if (shiftedValue == null)
			return;

		_previousMa = _currentMa;
		_currentMa = shiftedValue;

		if (!_isInitialized)
		{
			_isInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousMa == null)
			return;

		var distanceFilter = GetMinimumDistance();
		var openPrice = candle.OpenPrice;
		var closePrice = candle.ClosePrice;
		var currentMa = _currentMa.Value;
		var previousMa = _previousMa.Value;

		// Long setup: price is below the MA and the MA is rising.
		if (ShouldEnterLong(openPrice, closePrice, currentMa, previousMa, distanceFilter))
		{
			EnterLong();
			return;
		}

		// Short setup: price is above the MA and the MA is falling.
		if (ShouldEnterShort(openPrice, closePrice, currentMa, previousMa, distanceFilter))
		{
			EnterShort();
		}
	}

	private void EnterLong()
	{
		// Close a short position before opening a long one.
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Position <= 0)
		{
			BuyMarket(TradeVolume);
		}
	}

	private void EnterShort()
	{
		// Close a long position before opening a short one.
		if (Position > 0)
		{
			SellMarket(Position);
		}

		if (Position >= 0)
		{
			SellMarket(TradeVolume);
		}
	}

	private bool ShouldEnterLong(decimal openPrice, decimal closePrice, decimal currentMa, decimal previousMa, decimal distanceFilter)
	{
		if (TradeVolume <= 0)
			return false;

		if (currentMa <= previousMa)
			return false;

		if (currentMa - openPrice < distanceFilter)
			return false;

		if (currentMa - closePrice < distanceFilter)
			return false;

		return Position <= 0;
	}

	private bool ShouldEnterShort(decimal openPrice, decimal closePrice, decimal currentMa, decimal previousMa, decimal distanceFilter)
	{
		if (TradeVolume <= 0)
			return false;

		if (currentMa >= previousMa)
			return false;

		if (openPrice - currentMa < distanceFilter)
			return false;

		if (closePrice - currentMa < distanceFilter)
			return false;

		return Position >= 0;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceTypes.Close => candle.ClosePrice,
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private decimal GetMinimumDistance()
	{
		if (MinimumDistancePips <= 0)
			return 0m;

		var step = Security?.PriceStep;
		if (step == null || step == 0m)
			return 0m;

		return step.Value * MinimumDistancePips;
	}

	private decimal? GetShiftedValue(decimal maValue)
	{
		_maValues.Enqueue(maValue);

		var requiredCount = MaShift + 1;
		while (_maValues.Count > requiredCount)
		{
			_maValues.Dequeue();
		}

		if (_maValues.Count < requiredCount)
			return null;

		var index = _maValues.Count - MaShift - 1;
		var counter = 0;
		decimal selected = 0m;
		foreach (var value in _maValues)
		{
			if (counter == index)
			{
				selected = value;
				break;
			}

			counter++;
		}

		return selected;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethods method, int length)
	{
		return method switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethods.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported moving average methods.
	/// </summary>
	public enum MovingAverageMethods
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	/// <summary>
	/// Supported price inputs for the moving average calculation.
	/// </summary>
	public enum AppliedPriceTypes
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}