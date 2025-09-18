namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Recreates the ASCV expert advisor using StockSharp high level components.
/// </summary>
public class AscvStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<decimal> _stdDevThreshold;
	private readonly StrategyParam<decimal> _volumeDeltaThreshold;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticExitDelta;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _minPivotDistanceSteps;
	private readonly StrategyParam<decimal> _stopFallbackSteps;
	private readonly StrategyParam<decimal> _takeProfitBufferSteps;
	private readonly StrategyParam<decimal> _orderVolume;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private StandardDeviation _stdDev = null!;
	private StochasticOscillator _stochastic = null!;

	private DateTime? _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	private decimal? _pivot;
	private decimal? _resistance1;
	private decimal? _resistance2;
	private decimal? _support1;
	private decimal? _support2;

	private decimal? _previousVolume;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private int? _currentHour;
	private bool _longOpenedThisHour;
	private bool _shortOpenedThisHour;

	/// <summary>
	/// Initializes strategy parameters that mirror the configurable inputs of the EA.
	/// </summary>
	public AscvStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Data");

		_startHour = Param(nameof(StartHour), 2)
		.SetDisplay("Start Hour", "First trading hour (inclusive)", "Session");

		_endHour = Param(nameof(EndHour), 20)
		.SetDisplay("End Hour", "Last trading hour (inclusive)", "Session");

		_fastMaLength = Param(nameof(FastMaLength), 10)
		.SetDisplay("Fast MA", "Length of the fast moving average", "Trend")
		.SetGreaterThanZero();

		_slowMaLength = Param(nameof(SlowMaLength), 40)
		.SetDisplay("Slow MA", "Length of the slow moving average", "Trend")
		.SetGreaterThanZero();

		_stdDevLength = Param(nameof(StdDevLength), 10)
		.SetDisplay("StdDev Length", "Lookback for the standard deviation filter", "Volatility")
		.SetGreaterThanZero();

		_stdDevThreshold = Param(nameof(StdDevThreshold), 0.0005m)
		.SetDisplay("StdDev Threshold", "Minimum volatility required for entries", "Volatility")
		.SetGreaterThanZero();

		_volumeDeltaThreshold = Param(nameof(VolumeDeltaThreshold), 30m)
		.SetDisplay("Volume Delta", "Minimum increase of candle volume", "Volume")
		.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetDisplay("Stochastic %K", "Main period of the stochastic oscillator", "Oscillator")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stochastic %D", "Signal period of the stochastic oscillator", "Oscillator")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Stochastic Slowing", "Additional smoothing applied to %K", "Oscillator")
		.SetGreaterThanZero();

		_stochasticExitDelta = Param(nameof(StochasticExitDelta), 5m)
		.SetDisplay("Stochastic Exit Delta", "|%K-%D| level that triggers exits", "Oscillator")
		.SetGreaterThanZero();

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 30m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk")
		.SetGreaterThanZero();

		_minPivotDistanceSteps = Param(nameof(MinPivotDistanceSteps), 50m)
		.SetDisplay("Min Pivot Distance", "Minimum distance to pivot-based targets", "Risk")
		.SetGreaterThanZero();

		_stopFallbackSteps = Param(nameof(StopFallbackSteps), 33m)
		.SetDisplay("Fallback Stop", "Fallback stop distance when pivots are unavailable", "Risk")
		.SetGreaterThanZero();

		_takeProfitBufferSteps = Param(nameof(TakeProfitBufferSteps), 50m)
		.SetDisplay("Fallback Take Profit", "Fallback target distance when pivots are close", "Risk")
		.SetGreaterThanZero();

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Volume for market orders", "Trading")
		.SetGreaterThanZero();
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int StdDevLength
	{
		get => _stdDevLength.Value;
		set => _stdDevLength.Value = value;
	}

	public decimal StdDevThreshold
	{
		get => _stdDevThreshold.Value;
		set => _stdDevThreshold.Value = value;
	}

	public decimal VolumeDeltaThreshold
	{
		get => _volumeDeltaThreshold.Value;
		set => _volumeDeltaThreshold.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	public decimal StochasticExitDelta
	{
		get => _stochasticExitDelta.Value;
		set => _stochasticExitDelta.Value = value;
	}

	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	public decimal MinPivotDistanceSteps
	{
		get => _minPivotDistanceSteps.Value;
		set => _minPivotDistanceSteps.Value = value;
	}

	public decimal StopFallbackSteps
	{
		get => _stopFallbackSteps.Value;
		set => _stopFallbackSteps.Value = value;
	}

	public decimal TakeProfitBufferSteps
	{
		get => _takeProfitBufferSteps.Value;
		set => _takeProfitBufferSteps.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDay = null;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayClose = 0m;
		_pivot = null;
		_resistance1 = null;
		_resistance2 = null;
		_support1 = null;
		_support2 = null;
		_previousVolume = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_currentHour = null;
		_longOpenedThisHour = false;
		_shortOpenedThisHour = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_stdDev = new StandardDeviation { Length = StdDevLength };
		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Slowing = StochasticSlowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([_fastMa, _slowMa, _stdDev, _stochastic], ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Main trading routine executed for every finished candle.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (values.Length != 4)
		return;

		if (!values[0].IsFinal || !values[1].IsFinal || !values[2].IsFinal || !values[3].IsFinal)
		return;

		var fast = values[0].ToDecimal();
		var slow = values[1].ToDecimal();
		var stdDev = values[2].ToDecimal();

		var stochTyped = (StochasticOscillatorValue)values[3];
		if (stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
		return;

		// Synchronize internal state with the latest data snapshot.
		ResetHourlyFlags(candle.OpenTime);
		UpdateDailyLevels(candle);

		var volumeDelta = GetVolumeDelta(candle);

		if (!IsWithinTradingWindow(candle.OpenTime))
		return;

		if (_pivot is not decimal pivot ||
		_resistance1 is not decimal r1 ||
		_resistance2 is not decimal r2 ||
		_support1 is not decimal s1 ||
		_support2 is not decimal s2)
		{
			return;
		}

		var volatilityOk = stdDev >= StdDevThreshold;
		var volumeOk = volumeDelta >= VolumeDeltaThreshold;
		var longTrend = fast > slow;
		var shortTrend = fast < slow;
		var stochSpread = stochK - stochD;

		// Entry filters approximate the MQL combination of ASCTrend and BrainTrend signals.
		var longSignal = volatilityOk && volumeOk && longTrend && candle.ClosePrice > pivot && stochSpread > 0m;
		var shortSignal = volatilityOk && volumeOk && shortTrend && candle.ClosePrice < pivot && stochSpread < 0m;

		var exitLong = shortSignal || stochSpread <= -StochasticExitDelta;
		var exitShort = longSignal || stochSpread >= StochasticExitDelta;

		if (Position > 0m)
		{
			if (exitLong)
			{
				SellMarket(Position);
				_longTrailingStop = null;
				return;
			}

			UpdateTrailingStop(candle);
			return;
		}

		if (Position < 0m)
		{
			if (exitShort)
			{
				BuyMarket(Math.Abs(Position));
				_shortTrailingStop = null;
				return;
			}

			UpdateTrailingStop(candle);
			return;
		}

		if (OrderVolume <= 0m)
		return;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return;

		if (longSignal && !_longOpenedThisHour)
		{
			EnterLong(candle, pivot, r1, r2, s1, step);
			return;
		}

		if (shortSignal && !_shortOpenedThisHour)
		{
			EnterShort(candle, pivot, r1, r2, s2, step);
		}
	}

	/// <summary>
	/// Executes a long entry and configures protective orders.
	/// </summary>
	private void EnterLong(ICandleMessage candle, decimal pivot, decimal r1, decimal r2, decimal s1, decimal step)
	{
		var volume = OrderVolume;
		var entryPrice = candle.ClosePrice;
		var resultingPosition = Position + volume;

		BuyMarket(volume);
		_longOpenedThisHour = true;
		_shortOpenedThisHour = false;
		_longTrailingStop = null;

		var stopPrice = s1 < entryPrice ? s1 : entryPrice - StopFallbackSteps * step;
		if (stopPrice < entryPrice)
		{
			var stopSteps = (entryPrice - stopPrice) / step;
			if (stopSteps > 0m)
			SetStopLoss(stopSteps, entryPrice, resultingPosition);
		}

		var targetPrice = GetLongTarget(entryPrice, pivot, r1, r2, step);
		if (targetPrice > entryPrice)
		{
			var takeSteps = (targetPrice - entryPrice) / step;
			if (takeSteps > 0m)
			SetTakeProfit(takeSteps, entryPrice, resultingPosition);
		}
	}

	/// <summary>
	/// Executes a short entry and configures protective orders.
	/// </summary>
	private void EnterShort(ICandleMessage candle, decimal pivot, decimal r1, decimal r2, decimal s2, decimal step)
	{
		var volume = OrderVolume;
		var entryPrice = candle.ClosePrice;
		var resultingPosition = Position - volume;

		SellMarket(volume);
		_shortOpenedThisHour = true;
		_longOpenedThisHour = false;
		_shortTrailingStop = null;

		var stopPrice = s2 > entryPrice ? s2 : entryPrice + StopFallbackSteps * step;
		if (stopPrice > entryPrice)
		{
			var stopSteps = (stopPrice - entryPrice) / step;
			if (stopSteps > 0m)
			SetStopLoss(stopSteps, entryPrice, resultingPosition);
		}

		var targetPrice = GetShortTarget(entryPrice, pivot, step);
		if (targetPrice < entryPrice)
		{
			var takeSteps = (entryPrice - targetPrice) / step;
			if (takeSteps > 0m)
			SetTakeProfit(takeSteps, entryPrice, resultingPosition);
		}
	}

	/// <summary>
	/// Calculates the increase in candle volume used to emulate Volume[0]-Volume[1].
	/// </summary>
	private decimal GetVolumeDelta(ICandleMessage candle)
	{
		var volume = candle.TotalVolume;
		decimal delta;

		if (_previousVolume is decimal prevVolume)
		delta = volume - prevVolume;
		else
		delta = 0m;

		_previousVolume = volume;
		return delta;
	}

	/// <summary>
	/// Resets hourly trading flags to mimic the EA's minute == 0 logic.
	/// </summary>
	private void ResetHourlyFlags(DateTimeOffset time)
	{
		var hour = time.Hour;
		if (_currentHour == hour)
		return;

		_currentHour = hour;
		_longOpenedThisHour = false;
		_shortOpenedThisHour = false;
	}

	/// <summary>
	/// Tracks daily high, low, and close to derive pivot levels.
	/// </summary>
	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var day = candle.OpenTime.Date;

		if (_currentDay is null)
		{
			InitializeDay(candle, day);
			return;
		}

		if (_currentDay != day)
		{
			FinalizeDay();
			InitializeDay(candle, day);
			return;
		}

		_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
		_dayLow = Math.Min(_dayLow, candle.LowPrice);
		_dayClose = candle.ClosePrice;
	}

	/// <summary>
	/// Initializes daily accumulators when a new day begins.
	/// </summary>
	private void InitializeDay(ICandleMessage candle, DateTime day)
	{
		_currentDay = day;
		_dayHigh = candle.HighPrice;
		_dayLow = candle.LowPrice;
		_dayClose = candle.ClosePrice;
	}

	/// <summary>
	/// Computes classical floor trader pivot levels for the finished day.
	/// </summary>
	private void FinalizeDay()
	{
		var high = _dayHigh;
		var low = _dayLow;
		var close = _dayClose;

		var pivot = (high + low + close) / 3m;
		var range = high - low;

		_pivot = pivot;
		_resistance1 = 2m * pivot - low;
		_support1 = 2m * pivot - high;
		_resistance2 = pivot + range;
		_support2 = pivot - range;
	}

	/// <summary>
	/// Restricts trading to the configured time window.
	/// </summary>
	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= StartHour && hour <= EndHour;
	}

	/// <summary>
	/// Chooses the take profit price for long trades based on pivot hierarchy.
	/// </summary>
	private decimal GetLongTarget(decimal entryPrice, decimal pivot, decimal r1, decimal r2, decimal step)
	{
		var minDistance = MinPivotDistanceSteps * step;

		if (r2 - entryPrice >= minDistance)
		return r2;

		if (r1 - entryPrice >= minDistance)
		return r1;

		if (pivot - entryPrice >= minDistance)
		return pivot;

		return entryPrice + TakeProfitBufferSteps * step;
	}

	/// <summary>
	/// Chooses the take profit price for short trades based on pivot hierarchy.
	/// </summary>
	private decimal GetShortTarget(decimal entryPrice, decimal pivot, decimal step)
	{
		var minDistance = MinPivotDistanceSteps * step;

		if (_support2 is decimal s2 && entryPrice - s2 >= minDistance)
		return s2;

		if (_support1 is decimal s1 && entryPrice - s1 >= minDistance)
		return s1;

		if (entryPrice - pivot >= minDistance)
		return pivot;

		return entryPrice - TakeProfitBufferSteps * step;
	}

	/// <summary>
	/// Maintains a trailing stop that mirrors the dynamic modifications from the EA.
	/// </summary>
	private void UpdateTrailingStop(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || TrailingStopSteps <= 0m)
		return;

		if (Position > 0m)
		{
			var desiredStop = candle.ClosePrice - TrailingStopSteps * step;
			if (_longTrailingStop is null || desiredStop > _longTrailingStop.Value)
			{
				var steps = (candle.ClosePrice - desiredStop) / step;
				if (steps > 0m)
				{
					SetStopLoss(steps, candle.ClosePrice, Position);
					_longTrailingStop = desiredStop;
				}
			}
		}
		else
		{
			_longTrailingStop = null;
		}

		if (Position < 0m)
		{
			var desiredStop = candle.ClosePrice + TrailingStopSteps * step;
			if (_shortTrailingStop is null || desiredStop < _shortTrailingStop.Value)
			{
				var steps = (desiredStop - candle.ClosePrice) / step;
				if (steps > 0m)
				{
					SetStopLoss(steps, candle.ClosePrice, Position);
					_shortTrailingStop = desiredStop;
				}
			}
		}
		else
		{
			_shortTrailingStop = null;
		}
	}
}
