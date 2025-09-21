using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weekly contrarian strategy that opens positions on Monday when price closes beyond recent extremes or diverges from a moving average.
/// </summary>
public class ContrarianTradeMaStrategy : Strategy
{
	private readonly StrategyParam<int> _calcPeriod;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<DataType> _tradeCandleType;
	private readonly StrategyParam<DataType> _maCandleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private LengthIndicator<decimal> _maIndicator = null!;
	private readonly Queue<decimal> _maValues = new();

	private decimal? _currentMaValue;
	private decimal? _previousMaValue;
	private decimal? _latestHighest;
	private decimal? _latestLowest;
	private decimal? _previousWeeklyClose;
	private decimal? _currentWeeklyOpen;
	private bool _weeklyDataReady;

	private DateTimeOffset? _positionEntryTime;
	private decimal? _stopLossPrice;

	/// <summary>
	/// Number of higher timeframe candles used for calculating extremes.
	/// </summary>
	public int CalcPeriod
	{
		get => _calcPeriod.Value;
		set => _calcPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in pips (price steps).
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Moving average period length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to shift the moving average forward.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used in the moving average calculation.
	/// </summary>
	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Primary candle type that triggers entries and exits.
	/// </summary>
	public DataType TradeCandleType
	{
		get => _tradeCandleType.Value;
		set => _tradeCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used for the moving average and extreme levels.
	/// </summary>
	public DataType MaCandleType
	{
		get => _maCandleType.Value;
		set => _maCandleType.Value = value;
	}

	/// <summary>
	/// Initializes configurable parameters with defaults matching the original MetaTrader expert.
	/// </summary>
	public ContrarianTradeMaStrategy()
	{
		_calcPeriod = Param(nameof(CalcPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Calc Period", "Lookback of higher timeframe candles for extremes", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_stopLossPips = Param(nameof(StopLossPips), 300)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100, 600, 50);

		_maPeriod = Param(nameof(MaPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length on the higher timeframe", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Horizontal shift applied to the moving average", "Moving Average");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.LinearWeighted)
			.SetDisplay("MA Method", "Moving average smoothing method", "Moving Average");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Weighted)
			.SetDisplay("Applied Price", "Price source fed into the moving average", "Moving Average");

		_tradeCandleType = Param(nameof(TradeCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Trade Candle Type", "Primary timeframe that triggers entries", "General");

		_maCandleType = Param(nameof(MaCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("MA Candle Type", "Higher timeframe used for the MA and extremes", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TradeCandleType), (Security, MaCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null!;
		_lowest = null!;
		_maIndicator = null!;
		_maValues.Clear();
		_currentMaValue = null;
		_previousMaValue = null;
		_latestHighest = null;
		_latestLowest = null;
		_previousWeeklyClose = null;
		_currentWeeklyOpen = null;
		_weeklyDataReady = false;
		_positionEntryTime = null;
		_stopLossPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Math.Max(1, CalcPeriod), CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = Math.Max(1, CalcPeriod), CandlePrice = CandlePrice.Low };
		_maIndicator = CreateMovingAverage(MaMethod, MaPeriod);

		var weeklySubscription = SubscribeCandles(MaCandleType);
		weeklySubscription
			.Bind(ProcessWeeklyCandle)
			.Start();

		var tradingSubscription = SubscribeCandles(TradeCandleType);
		tradingSubscription
			.Bind(ProcessTradingCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _maIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessWeeklyCandle(ICandleMessage candle)
	{
		var highestValue = _highest.Process(candle).ToNullableDecimal();
		var lowestValue = _lowest.Process(candle).ToNullableDecimal();
		var isFinished = candle.State == CandleStates.Finished;
		var price = GetAppliedPrice(candle, AppliedPrice);
		var maValue = _maIndicator.Process(price, candle.OpenTime, isFinished).ToNullableDecimal();

		if (!isFinished)
			return;

		if (highestValue == null || lowestValue == null || maValue == null)
			return;

		var shiftedMa = GetShiftedMaValue(maValue.Value);
		if (shiftedMa == null)
			return;

		_previousMaValue = _currentMaValue;
		_currentMaValue = shiftedMa;
		_latestHighest = highestValue.Value;
		_latestLowest = lowestValue.Value;
		_previousWeeklyClose = candle.ClosePrice;
		_weeklyDataReady = _highest.IsFormed && _lowest.IsFormed && _maIndicator.IsFormed && _previousMaValue != null;
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime.DayOfWeek == DayOfWeek.Monday)
			_currentWeeklyOpen = candle.OpenPrice;

		if (Position != 0)
		{
			if (IsFormedAndOnlineAndAllowTrading())
			{
				if (CheckStopLoss(candle))
					return;

				if (CheckTimeExit(candle))
					return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime.DayOfWeek == DayOfWeek.Monday)
			TryEnter(candle);
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (Volume <= 0)
			return;

		if (Position != 0)
			return;

		if (!_weeklyDataReady)
			return;

		if (_latestHighest is not decimal highest ||
			_latestLowest is not decimal lowest ||
			_previousWeeklyClose is not decimal prevClose ||
			_previousMaValue is not decimal maPrev ||
			_currentWeeklyOpen is not decimal weeklyOpen)
			return;

		var stopDistance = GetStopLossDistance();
		var closeTime = candle.CloseTime;
		var closePrice = candle.ClosePrice;

		if (highest < prevClose || maPrev > weeklyOpen)
		{
			BuyMarket(Volume);
			_positionEntryTime = closeTime;
			_stopLossPrice = stopDistance > 0m ? closePrice - stopDistance : null;
			return;
		}

		if (lowest > prevClose || maPrev < weeklyOpen)
		{
			SellMarket(Volume);
			_positionEntryTime = closeTime;
			_stopLossPrice = stopDistance > 0m ? closePrice + stopDistance : null;
		}
	}

	private bool CheckStopLoss(ICandleMessage candle)
	{
		if (_stopLossPrice == null)
			return false;

		if (Position > 0 && candle.LowPrice <= _stopLossPrice)
		{
			SellMarket(Position);
			ResetPositionState();
			return true;
		}

		if (Position < 0 && candle.HighPrice >= _stopLossPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return true;
		}

		return false;
	}

	private bool CheckTimeExit(ICandleMessage candle)
	{
		if (_positionEntryTime == null)
			return false;

		if (candle.CloseTime - _positionEntryTime < TimeSpan.FromDays(7))
			return false;

		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetPositionState();
		return true;
	}

	private void ResetPositionState()
	{
		_positionEntryTime = null;
		_stopLossPrice = null;
	}

	private decimal? GetShiftedMaValue(decimal maValue)
	{
		_maValues.Enqueue(maValue);

		var requiredCount = MaShift + 1;
		while (_maValues.Count > requiredCount)
			_maValues.Dequeue();

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

	private decimal GetStopLossDistance()
	{
		if (StopLossPips <= 0)
			return 0m;

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return 0m;

		return step.Value * StopLossPips;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + (2m * candle.ClosePrice)) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Moving average methods supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	/// <summary>
	/// Price sources that can be fed into the moving average.
	/// </summary>
	public enum AppliedPriceType
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
