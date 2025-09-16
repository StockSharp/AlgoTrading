using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Universal moving average crossover strategy converted from the original MQL version.
/// The strategy trades based on a fast and a slow moving average with optional signal confirmation,
/// stop-and-reverse behaviour, trailing stop management and time filtering.
/// </summary>
public class UniversalMaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _fastMaType;
	private readonly StrategyParam<MovingAverageMethod> _slowMaType;
	private readonly StrategyParam<AppliedPrice> _fastPriceType;
	private readonly StrategyParam<AppliedPrice> _slowPriceType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _minCrossDistance;
	private readonly StrategyParam<bool> _reverseCondition;
	private readonly StrategyParam<bool> _confirmedOnEntry;
	private readonly StrategyParam<bool> _oneEntryPerBar;
	private readonly StrategyParam<bool> _stopAndReverse;
	private readonly StrategyParam<bool> _pureSar;
	private readonly StrategyParam<bool> _useHourTrade;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator? _fastMa;
	private IIndicator? _slowMa;

	private decimal? _fastPrev;
	private decimal? _fastPrevPrev;
	private decimal? _slowPrev;
	private decimal? _slowPrevPrev;

	private DateTimeOffset? _lastEntryBar;
	private TradeDirection _lastTrade = TradeDirection.None;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
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
	/// Fast moving average method.
	/// </summary>
	public MovingAverageMethod FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Slow moving average method.
	/// </summary>
	public MovingAverageMethod SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Price type used for the fast moving average.
	/// </summary>
	public AppliedPrice FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	/// <summary>
	/// Price type used for the slow moving average.
	/// </summary>
	public AppliedPrice SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Additional move required before shifting the trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Minimum distance between the averages to validate a crossover.
	/// </summary>
	public decimal MinCrossDistance
	{
		get => _minCrossDistance.Value;
		set => _minCrossDistance.Value = value;
	}

	/// <summary>
	/// Reverse buy and sell conditions.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// Confirm signals on closed candles only.
	/// </summary>
	public bool ConfirmedOnEntry
	{
		get => _confirmedOnEntry.Value;
		set => _confirmedOnEntry.Value = value;
	}

	/// <summary>
	/// Limit the strategy to a single entry per bar.
	/// </summary>
	public bool OneEntryPerBar
	{
		get => _oneEntryPerBar.Value;
		set => _oneEntryPerBar.Value = value;
	}

	/// <summary>
	/// Close the current trade and reverse when an opposite signal appears.
	/// </summary>
	public bool StopAndReverse
	{
		get => _stopAndReverse.Value;
		set => _stopAndReverse.Value = value;
	}

	/// <summary>
	/// Disable protective orders and rely purely on signal reversals.
	/// </summary>
	public bool PureSar
	{
		get => _pureSar.Value;
		set => _pureSar.Value = value;
	}

	/// <summary>
	/// Enable trading only within the selected hours.
	/// </summary>
	public bool UseHourTrade
	{
		get => _useHourTrade.Value;
		set => _useHourTrade.Value = value;
	}

	/// <summary>
	/// Hour when trading can start (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when trading must end (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Initializes <see cref="UniversalMaCrossStrategy"/>.
	/// </summary>
	public UniversalMaCrossStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Fast moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 200, 5);

		_fastMaType = Param(nameof(FastMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Fast MA Type", "Method for fast average", "Indicators");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Slow MA Type", "Method for slow average", "Indicators");

		_fastPriceType = Param(nameof(FastPriceType), AppliedPrice.Close)
			.SetDisplay("Fast Price Type", "Price source for fast MA", "Indicators");

		_slowPriceType = Param(nameof(SlowPriceType), AppliedPrice.Close)
			.SetDisplay("Slow Price Type", "Price source for slow MA", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take-profit distance in price", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 0m)
			.SetDisplay("Trailing Step", "Additional move before trailing", "Risk");

		_minCrossDistance = Param(nameof(MinCrossDistance), 0m)
			.SetDisplay("Min Cross Distance", "Minimum distance between averages", "Filters");

		_reverseCondition = Param(nameof(ReverseCondition), false)
			.SetDisplay("Reverse Signals", "Swap long and short conditions", "General");

		_confirmedOnEntry = Param(nameof(ConfirmedOnEntry), true)
			.SetDisplay("Confirmed On Entry", "Use closed candles for signals", "General");

		_oneEntryPerBar = Param(nameof(OneEntryPerBar), true)
			.SetDisplay("One Entry Per Bar", "Allow only one entry per candle", "General");

		_stopAndReverse = Param(nameof(StopAndReverse), true)
			.SetDisplay("Stop And Reverse", "Close and reverse on opposite signal", "Risk");

		_pureSar = Param(nameof(PureSar), false)
			.SetDisplay("Pure SAR", "Disable stop-loss, take-profit and trailing", "Risk");

		_useHourTrade = Param(nameof(UseHourTrade), false)
			.SetDisplay("Use Hour Filter", "Limit trading by session hours", "Session");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading window start hour", "Session");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading window end hour", "Session");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle subscription", "General");
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
		_fastPrev = null;
		_fastPrevPrev = null;
		_slowPrev = null;
		_slowPrevPrev = null;
		_lastEntryBar = null;
		_lastTrade = TradeDirection.None;
		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastMaType, FastMaPeriod);
		_slowMa = CreateMovingAverage(SlowMaType, SlowMaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageExistingPosition(candle);

		if (_fastMa is null || _slowMa is null)
			return;

		var fastPrice = GetPrice(candle, FastPriceType);
		var slowPrice = GetPrice(candle, SlowPriceType);

		var fastValue = _fastMa.Process(fastPrice, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowMa.Process(slowPrice, candle.OpenTime, true).ToDecimal();

		var prevFast = _fastPrev;
		var prevSlow = _slowPrev;
		var prevFastPrev = _fastPrevPrev;
		var prevSlowPrev = _slowPrevPrev;

		_fastPrevPrev = prevFast;
		_slowPrevPrev = prevSlow;
		_fastPrev = fastValue;
		_slowPrev = slowValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		bool crossUp = false;
		bool crossDown = false;

		if (ConfirmedOnEntry)
		{
			if (prevFast.HasValue && prevSlow.HasValue && prevFastPrev.HasValue && prevSlowPrev.HasValue)
			{
				var fastPrevPrevValue = prevFastPrev.Value;
				var slowPrevPrevValue = prevSlowPrev.Value;
				var fastPrevValue = prevFast.Value;
				var slowPrevValue = prevSlow.Value;
				var diff = fastPrevValue - slowPrevValue;

				crossUp = fastPrevPrevValue < slowPrevPrevValue && fastPrevValue > slowPrevValue && diff >= MinCrossDistance;
				crossDown = fastPrevPrevValue > slowPrevPrevValue && fastPrevValue < slowPrevValue && -diff >= MinCrossDistance;
			}
		}
		else
		{
			if (prevFast.HasValue && prevSlow.HasValue)
			{
				var fastPrevValue = prevFast.Value;
				var slowPrevValue = prevSlow.Value;
				var diff = fastValue - slowValue;

				crossUp = fastPrevValue < slowPrevValue && fastValue > slowValue && diff >= MinCrossDistance;
				crossDown = fastPrevValue > slowPrevValue && fastValue < slowValue && -diff >= MinCrossDistance;
			}
		}

		bool buySignal;
		bool sellSignal;

		if (!ReverseCondition)
		{
			buySignal = crossUp;
			sellSignal = crossDown;
		}
		else
		{
			buySignal = crossDown;
			sellSignal = crossUp;
		}

		var canTrade = IsWithinTradingHours(candle);

		if (!canTrade)
			return;

		if (StopAndReverse && Position != 0)
		{
			if ((_lastTrade == TradeDirection.Long && sellSignal) || (_lastTrade == TradeDirection.Short && buySignal))
			{
				ClosePosition();
				ResetProtection();
			}
		}

		if (Position != 0)
			return;

		var entryAllowed = !OneEntryPerBar || _lastEntryBar != candle.OpenTime;

		if (!entryAllowed)
			return;

		if (buySignal)
		{
			BuyMarket(Volume);
			SetProtectionLevels(candle.ClosePrice, true);
			_lastTrade = TradeDirection.Long;
			_lastEntryBar = candle.OpenTime;
		}
		else if (sellSignal)
		{
			SellMarket(Volume);
			SetProtectionLevels(candle.ClosePrice, false);
			_lastTrade = TradeDirection.Short;
			_lastEntryBar = candle.OpenTime;
		}
	}

	private void ManageExistingPosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetProtection();
			return;
		}

		UpdateTrailingStop(candle);

		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				ClosePosition();
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				ClosePosition();
				ResetProtection();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				ClosePosition();
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				ClosePosition();
				ResetProtection();
			}
		}
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (PureSar || TrailingStop <= 0m || !_entryPrice.HasValue)
			return;

		var activationDistance = TrailingStop + TrailingStep;

		if (Position > 0)
		{
			if (candle.ClosePrice - _entryPrice.Value > activationDistance)
			{
				var activationLevel = candle.ClosePrice - activationDistance;
				if (!_stopPrice.HasValue || _stopPrice.Value < activationLevel)
				{
					var newStop = candle.ClosePrice - TrailingStop;
					_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, newStop) : newStop;
				}
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice.Value - candle.ClosePrice > activationDistance)
			{
				var activationLevel = candle.ClosePrice + activationDistance;
				if (!_stopPrice.HasValue || _stopPrice.Value > activationLevel)
				{
					var newStop = candle.ClosePrice + TrailingStop;
					_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, newStop) : newStop;
				}
			}
		}
	}

	private bool IsWithinTradingHours(ICandleMessage candle)
	{
		if (!UseHourTrade)
			return true;

		var hour = candle.OpenTime.Hour;
		var start = StartHour;
		var end = EndHour;

		if (start <= end)
			return hour >= start && hour <= end;

		return hour >= start || hour <= end;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPrice priceType)
	{
		return priceType switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private void SetProtectionLevels(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (PureSar)
		{
			_stopPrice = null;
			_takeProfitPrice = null;
			return;
		}

		var stop = StopLoss;
		var take = TakeProfit;

		_stopPrice = stop > 0m ? (isLong ? entryPrice - stop : entryPrice + stop) : null;
		_takeProfitPrice = take > 0m ? (isLong ? entryPrice + take : entryPrice - take) : null;
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private enum TradeDirection
	{
		None,
		Long,
		Short
	}
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MovingAverageMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

/// <summary>
/// Price sources that can feed the moving averages.
/// </summary>
public enum AppliedPrice
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
