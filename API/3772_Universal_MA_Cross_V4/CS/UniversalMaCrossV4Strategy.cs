using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Universal MACross EA v4" MetaTrader expert advisor.
/// The strategy trades the crossover between configurable fast and slow moving averages
/// with optional session filters, stop-and-reverse behaviour and trailing stop management.
/// </summary>
public class UniversalMaCrossV4Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _fastMaType;
	private readonly StrategyParam<MovingAverageMethod> _slowMaType;
	private readonly StrategyParam<AppliedPrice> _fastPriceType;
	private readonly StrategyParam<AppliedPrice> _slowPriceType;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _minCrossDistancePoints;
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

	private IIndicator _fastMa;
	private IIndicator _slowMa;

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
	/// Method applied to the fast moving average.
	/// </summary>
	public MovingAverageMethod FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Method applied to the slow moving average.
	/// </summary>
	public MovingAverageMethod SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Price source for the fast moving average.
	/// </summary>
	public AppliedPrice FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	/// <summary>
	/// Price source for the slow moving average.
	/// </summary>
	public AppliedPrice SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance between moving averages to validate a crossover.
	/// </summary>
	public decimal MinCrossDistancePoints
	{
		get => _minCrossDistancePoints.Value;
		set => _minCrossDistancePoints.Value = value;
	}

	/// <summary>
	/// Swap bullish and bearish signals when set to <c>true</c>.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// Require the crossover to be confirmed on the previous closed bar.
	/// </summary>
	public bool ConfirmedOnEntry
	{
		get => _confirmedOnEntry.Value;
		set => _confirmedOnEntry.Value = value;
	}

	/// <summary>
	/// Allow only one new position per candle.
	/// </summary>
	public bool OneEntryPerBar
	{
		get => _oneEntryPerBar.Value;
		set => _oneEntryPerBar.Value = value;
	}

	/// <summary>
	/// Close and reverse the active position when the opposite signal appears.
	/// </summary>
	public bool StopAndReverse
	{
		get => _stopAndReverse.Value;
		set => _stopAndReverse.Value = value;
	}

	/// <summary>
	/// Disable stop-loss, take-profit and trailing stop logic.
	/// </summary>
	public bool PureSar
	{
		get => _pureSar.Value;
		set => _pureSar.Value = value;
	}

	/// <summary>
	/// Enable the hour-based trading session filter.
	/// </summary>
	public bool UseHourTrade
	{
		get => _useHourTrade.Value;
		set => _useHourTrade.Value = value;
	}

	/// <summary>
	/// Start hour of the trading window (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour of the trading window (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Order volume applied to each market order.
	/// </summary>
	public decimal TradeVolume
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
	/// Initializes a new instance of the <see cref="UniversalMaCrossV4Strategy"/> class.
	/// </summary>
	public UniversalMaCrossV4Strategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 200, 5);

		_fastMaType = Param(nameof(FastMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Fast MA Method", "Smoothing method applied to the fast moving average", "Indicators");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageMethod.Exponential)
			.SetDisplay("Slow MA Method", "Smoothing method applied to the slow moving average", "Indicators");

		_fastPriceType = Param(nameof(FastPriceType), AppliedPrice.Close)
			.SetDisplay("Fast MA Price", "Price source injected into the fast moving average", "Indicators");

		_slowPriceType = Param(nameof(SlowPriceType), AppliedPrice.Close)
			.SetDisplay("Slow MA Price", "Price source injected into the slow moving average", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Take-profit distance in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps", "Risk");

		_minCrossDistancePoints = Param(nameof(MinCrossDistancePoints), 0m)
			.SetNotNegative()
			.SetDisplay("Min Cross Distance (points)", "Minimum separation between the moving averages", "Filters");

		_reverseCondition = Param(nameof(ReverseCondition), false)
			.SetDisplay("Reverse Signals", "Swap bullish and bearish conditions", "General");

		_confirmedOnEntry = Param(nameof(ConfirmedOnEntry), true)
			.SetDisplay("Confirmed On Entry", "Validate signals on the previous closed bar", "General");

		_oneEntryPerBar = Param(nameof(OneEntryPerBar), true)
			.SetDisplay("One Entry Per Bar", "Allow at most one entry per candle", "General");

		_stopAndReverse = Param(nameof(StopAndReverse), true)
			.SetDisplay("Stop And Reverse", "Close and reverse when the opposite signal appears", "Risk");

		_pureSar = Param(nameof(PureSar), false)
			.SetDisplay("Pure SAR", "Disable protective stops and trailing", "Risk");

		_useHourTrade = Param(nameof(UseHourTrade), false)
			.SetDisplay("Use Hour Filter", "Restrict trading to a specific session", "Session");

		_startHour = Param(nameof(StartHour), 10)
			.SetDisplay("Start Hour", "Trading window start hour", "Session");

		_endHour = Param(nameof(EndHour), 11)
			.SetDisplay("End Hour", "Trading window end hour", "Session");

		_volume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for each market entry", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle subscription used by the strategy", "General");
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
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;

		Volume = TradeVolume;
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

		var minDistance = GetPriceOffset(MinCrossDistancePoints);

		var crossUp = false;
		var crossDown = false;

		if (ConfirmedOnEntry)
		{
			// Confirm signals using the previous completed bar (shift 2 -> 1 in MQL terms).
			if (prevFast.HasValue && prevSlow.HasValue && prevFastPrev.HasValue && prevSlowPrev.HasValue)
			{
				var diff = prevFast.Value - prevSlow.Value;
				crossUp = prevFastPrev.Value < prevSlowPrev.Value && prevFast.Value > prevSlow.Value && diff >= minDistance;
				crossDown = prevFastPrev.Value > prevSlowPrev.Value && prevFast.Value < prevSlow.Value && -diff >= minDistance;
			}
		}
		else
		{
			// Validate crossovers on the current finished bar.
			if (prevFast.HasValue && prevSlow.HasValue)
			{
				var diff = fastValue - slowValue;
				crossUp = prevFast.Value < prevSlow.Value && fastValue > slowValue && diff >= minDistance;
				crossDown = prevFast.Value > prevSlow.Value && fastValue < slowValue && -diff >= minDistance;
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

		if (!IsWithinTradingHours(candle))
			return;

		if (StopAndReverse && Position != 0)
		{
			var reverseToShort = _lastTrade == TradeDirection.Long && sellSignal;
			var reverseToLong = _lastTrade == TradeDirection.Short && buySignal;

			if (reverseToLong || reverseToShort)
			{
				ClosePosition();
				ResetProtection();
				_lastTrade = TradeDirection.None;
			}
		}

		if (Position != 0)
			return;

		if (OneEntryPerBar && _lastEntryBar == candle.OpenTime)
			return;

		if (buySignal)
		{
			BuyMarket(TradeVolume);
			SetProtectionLevels(candle.ClosePrice, true);
			_lastTrade = TradeDirection.Long;
			_lastEntryBar = candle.OpenTime;
		}
		else if (sellSignal)
		{
			SellMarket(TradeVolume);
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
		if (PureSar || TrailingStopPoints <= 0m || !_entryPrice.HasValue)
			return;

		var trailingDistance = GetPriceOffset(TrailingStopPoints);
		if (trailingDistance <= 0m)
			return;

		if (Position > 0)
		{
			var move = candle.ClosePrice - _entryPrice.Value;
			if (move > trailingDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (!_stopPrice.HasValue || candidate > _stopPrice.Value)
				{
					_stopPrice = candidate;
				}
			}
		}
		else if (Position < 0)
		{
			var move = _entryPrice.Value - candle.ClosePrice;
			if (move > trailingDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (!_stopPrice.HasValue || candidate < _stopPrice.Value)
				{
					_stopPrice = candidate;
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

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
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
			_ => candle.ClosePrice,
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

		var stopDistance = GetPriceOffset(StopLossPoints);
		var takeDistance = GetPriceOffset(TakeProfitPoints);

		_stopPrice = stopDistance > 0m ? (isLong ? entryPrice - stopDistance : entryPrice + stopDistance) : null;
		_takeProfitPrice = takeDistance > 0m ? (isLong ? entryPrice + takeDistance : entryPrice - takeDistance) : null;
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
			return points * step;

		var decimals = Security?.Decimals;
		if (decimals.HasValue && decimals.Value > 0)
		{
			decimal scale = 1m;
			for (var i = 0; i < decimals.Value; i++)
				scale /= 10m;

			return points * scale;
		}

		return points;
	}

	private enum TradeDirection
	{
		None,
		Long,
		Short
	}
}
