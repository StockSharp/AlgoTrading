using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy converted from the "SV v.4.2.5" MetaTrader 5 expert advisor.
/// Evaluates one trade per day after a configurable start time using moving average filters.
/// </summary>
public class SvDailyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<bool> _useManualVolume;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<int> _interval;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<MovingAverageMethod> _fastMaMethod;
	private readonly StrategyParam<AppliedPrice> _fastAppliedPrice;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<MovingAverageMethod> _slowMaMethod;
	private readonly StrategyParam<AppliedPrice> _slowAppliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _fastMa;
	private LengthIndicator<decimal>? _slowMa;

	private readonly List<decimal> _fastMaValues = new();
	private readonly List<decimal> _slowMaValues = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopPrice;
	private DateTime? _currentDay;
	private bool _hasTradedToday;
	private decimal _pipSize;

	/// <summary>
	/// Use manual volume instead of the risk-based sizing model.
	/// </summary>
	public bool UseManualVolume
	{
		get => _useManualVolume.Value;
		set => _useManualVolume.Value = value;
	}

	/// <summary>
	/// Trading volume applied when <see cref="UseManualVolume"/> is enabled.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Risk percentage of account equity used when calculating the dynamic position size.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Hour of the day (exchange time) when the strategy starts searching for entries.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Minute of the hour when the strategy starts searching for entries.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Number of recent bars excluded from the high/low analysis window.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Number of bars that are analysed when computing the breakout range.
	/// </summary>
	public int Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
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
	/// Fast moving average horizontal shift.
	/// </summary>
	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Fast moving average calculation method.
	/// </summary>
	public MovingAverageMethod FastMaMethod
	{
		get => _fastMaMethod.Value;
		set => _fastMaMethod.Value = value;
	}

	/// <summary>
	/// Applied price used for the fast moving average.
	/// </summary>
	public AppliedPrice FastAppliedPrice
	{
		get => _fastAppliedPrice.Value;
		set => _fastAppliedPrice.Value = value;
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
	/// Slow moving average horizontal shift.
	/// </summary>
	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// Slow moving average calculation method.
	/// </summary>
	public MovingAverageMethod SlowMaMethod
	{
		get => _slowMaMethod.Value;
		set => _slowMaMethod.Value = value;
	}

	/// <summary>
	/// Applied price used for the slow moving average.
	/// </summary>
	public AppliedPrice SlowAppliedPrice
	{
		get => _slowAppliedPrice.Value;
		set => _slowAppliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults that match the original expert advisor.
	/// </summary>
	public SvDailyBreakoutStrategy()
	{
		_useManualVolume = Param(nameof(UseManualVolume), false)
			.SetDisplay("Use Manual Volume", "Use fixed volume instead of risk percentage", "Risk");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trading volume when manual sizing is enabled", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk percentage of account equity", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Trailing step increment in pips", "Risk");

		_startHour = Param(nameof(StartHour), 19)
			.SetDisplay("Start Hour", "Hour when trading may begin", "Trading Window");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Minute when trading may begin", "Trading Window");

		_shift = Param(nameof(Shift), 6)
			.SetGreaterOrEqualZero()
			.SetDisplay("Shift", "Number of newest bars excluded from range analysis", "Logic");

		_interval = Param(nameof(Interval), 27)
			.SetGreaterThanZero()
			.SetDisplay("Interval", "Number of historical bars analysed", "Logic");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Fast moving average length", "Indicators");

		_fastMaShift = Param(nameof(FastMaShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Fast MA Shift", "Horizontal shift for the fast moving average", "Indicators");

		_fastMaMethod = Param(nameof(FastMaMethod), MovingAverageMethod.Smma)
			.SetDisplay("Fast MA Method", "Calculation method for the fast moving average", "Indicators");

		_fastAppliedPrice = Param(nameof(FastAppliedPrice), AppliedPrice.Median)
			.SetDisplay("Fast Applied Price", "Price type used for the fast moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 41)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow moving average length", "Indicators");

		_slowMaShift = Param(nameof(SlowMaShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slow MA Shift", "Horizontal shift for the slow moving average", "Indicators");

		_slowMaMethod = Param(nameof(SlowMaMethod), MovingAverageMethod.Smma)
			.SetDisplay("Slow MA Method", "Calculation method for the slow moving average", "Indicators");

		_slowAppliedPrice = Param(nameof(SlowAppliedPrice), AppliedPrice.Median)
			.SetDisplay("Slow Applied Price", "Price type used for the slow moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations", "General");
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

		_fastMaValues.Clear();
		_slowMaValues.Clear();
		_highHistory.Clear();
		_lowHistory.Clear();
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
		_currentDay = null;
		_hasTradedToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security is null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		if (FastMaPeriod >= SlowMaPeriod)
			throw new InvalidOperationException("Fast moving average period must be less than the slow moving average period.");

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_fastMa = CreateMovingAverage(FastMaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(SlowMaMethod, SlowMaPeriod);

		var decimals = Security.Decimals;
		var step = Security.PriceStep ?? 0.0001m;
		var factor = decimals is 3 or 5 ? 10m : 1m;
		_pipSize = step * factor;
		if (_pipSize <= 0m)
			_pipSize = step > 0m ? step : 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyState(candle.CloseTime);

		UpdateRangeHistory(candle);

		if (_fastMa is null || _slowMa is null)
			return;

		var fastValue = ProcessMovingAverage(_fastMa, FastAppliedPrice, _fastMaValues, FastMaShift, candle);
		var slowValue = ProcessMovingAverage(_slowMa, SlowAppliedPrice, _slowMaValues, SlowMaShift, candle);

		if (fastValue is null || slowValue is null)
			return;

		UpdateTrailing(candle);

		if (CheckProtectiveExits(candle))
			return;

		if (Position != 0)
			return;

		if (_hasTradedToday)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var secondsSinceMidnight = (int)(candle.CloseTime - candle.CloseTime.Date).TotalSeconds;
		var startSeconds = StartHour * 3600 + StartMinute * 60;
		if (secondsSinceMidnight <= startSeconds)
			return;

		if (!TryGetRangeExtremes(out var lowest, out var highest))
			return;

		if (highest < slowValue && lowest < fastValue)
		{
			EnterPosition(true, candle);
			return;
		}

		if (lowest > slowValue && highest > fastValue)
		{
			EnterPosition(false, candle);
		}
	}

	private void EnterPosition(bool isLong, ICandleMessage candle)
	{
		var entryPrice = candle.ClosePrice;
		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;

		var volume = UseManualVolume
			? NormalizeVolume(Volume)
			: CalculateRiskBasedVolume(stopDistance);

		if (volume <= 0m)
			return;

		if (isLong)
		{
			var totalVolume = volume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (totalVolume <= 0m)
				return;

			BuyMarket(totalVolume);
			_entryPrice = entryPrice;
			_stopPrice = StopLossPips > 0 ? entryPrice - stopDistance : null;
			_takeProfitPrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : null;
		}
		else
		{
			var totalVolume = volume + (Position > 0 ? Position : 0m);
			if (totalVolume <= 0m)
				return;

			SellMarket(totalVolume);
			_entryPrice = entryPrice;
			_stopPrice = StopLossPips > 0 ? entryPrice + stopDistance : null;
			_takeProfitPrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : null;
		}

		_trailingStopPrice = TrailingStopPips > 0 ? _stopPrice : null;
		_hasTradedToday = true;
	}

	private void UpdateDailyState(DateTimeOffset time)
	{
		var day = time.Date;
		if (_currentDay != day)
		{
			_currentDay = day;
			_hasTradedToday = false;
		}
	}

	private void UpdateRangeHistory(ICandleMessage candle)
	{
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);

		var maxCount = Math.Max(Shift + Interval + 5, 50);
		if (_highHistory.Count > maxCount)
		{
			var remove = _highHistory.Count - maxCount;
			_highHistory.RemoveRange(0, remove);
			_lowHistory.RemoveRange(0, remove);
		}
	}

	private decimal? ProcessMovingAverage(LengthIndicator<decimal> indicator, AppliedPrice priceMode, List<decimal> buffer, int shift, ICandleMessage candle)
	{
		var price = GetAppliedPrice(candle, priceMode);
		var result = indicator.Process(price, candle.OpenTime, true);

		if (!result.IsFormed)
			return null;

		var value = result.ToDecimal();
		buffer.Add(value);

		var maxSize = Math.Max(shift + 1, indicator.Length + 5);
		if (buffer.Count > maxSize)
			buffer.RemoveAt(0);

		var index = buffer.Count - 1 - shift;
		if (index < 0 || index >= buffer.Count)
			return null;

		return buffer[index];
	}

	private bool TryGetRangeExtremes(out decimal lowest, out decimal highest)
	{
		lowest = 0m;
		highest = 0m;

		var required = Shift + Interval;
		if (required <= 0)
			return false;

		if (_lowHistory.Count < required || _highHistory.Count < required)
			return false;

		var low = decimal.MaxValue;
		var high = decimal.MinValue;
		var total = _lowHistory.Count;

		for (var offset = Shift; offset < Shift + Interval; offset++)
		{
			var index = total - 1 - offset;
			if (index < 0)
				return false;

			var lowValue = _lowHistory[index];
			var highValue = _highHistory[index];

			if (lowValue < low)
				low = lowValue;

			if (highValue > high)
				high = highValue;
		}

		if (low == decimal.MaxValue || high == decimal.MinValue)
			return false;

		lowest = low;
		highest = high;
		return true;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || _entryPrice is null)
			return;

		var trailDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var current = candle.ClosePrice;
			var entry = _entryPrice.Value;
			if (current - entry > trailDistance + stepDistance)
			{
				var threshold = current - (trailDistance + stepDistance);
				if (_stopPrice is null || _stopPrice < threshold)
				{
					var newStop = current - trailDistance;
					if (_stopPrice is null || newStop > _stopPrice)
					{
						_stopPrice = newStop;
						_trailingStopPrice = newStop;
						LogInfo($"Adjusted trailing stop for long position to {newStop}");
					}
				}
			}
		}
		else if (Position < 0)
		{
			var current = candle.ClosePrice;
			var entry = _entryPrice.Value;
			if (entry - current > trailDistance + stepDistance)
			{
				var threshold = current + trailDistance + stepDistance;
				if (_stopPrice is null || _stopPrice > threshold)
				{
					var newStop = current + trailDistance;
					if (_stopPrice is null || newStop < _stopPrice)
					{
						_stopPrice = newStop;
						_trailingStopPrice = newStop;
						LogInfo($"Adjusted trailing stop for short position to {newStop}");
					}
				}
			}
		}
	}

	private bool CheckProtectiveExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetTradeState();
				return true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				ResetTradeState();
				return true;
			}
		}
		else if (_entryPrice is not null)
		{
			ResetTradeState();
		}

		return false;
	}

	private decimal CalculateRiskBasedVolume(decimal stopDistance)
	{
		if (UseManualVolume || stopDistance <= 0m)
			return NormalizeVolume(Volume);

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
			return NormalizeVolume(Volume);

		var riskAmount = portfolioValue * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return NormalizeVolume(Volume);

		var step = Security?.PriceStep ?? _pipSize;
		if (step <= 0m)
			step = _pipSize > 0m ? _pipSize : 1m;

		var stepValue = Security?.StepPrice ?? step;
		if (stepValue <= 0m)
			stepValue = step;

		var steps = stopDistance / step;
		if (steps <= 0m)
			return NormalizeVolume(Volume);

		var riskPerUnit = steps * stepValue;
		if (riskPerUnit <= 0m)
			return NormalizeVolume(Volume);

		var rawVolume = riskAmount / riskPerUnit;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 1m;
		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		var minVolume = Security.VolumeMin ?? step;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.VolumeMax;
		if (maxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice mode)
	{
		return mode switch
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

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Sma => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
	}

	/// <summary>
	/// Available moving average calculation methods.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Smoothed moving average (SMMA).
		/// </summary>
		Smma,

		/// <summary>
		/// Linear weighted moving average (LWMA).
		/// </summary>
		Lwma
	}

	/// <summary>
	/// Price sources supported by the moving averages.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>
		/// Close price.
		/// </summary>
		Close,

		/// <summary>
		/// Open price.
		/// </summary>
		Open,

		/// <summary>
		/// High price.
		/// </summary>
		High,

		/// <summary>
		/// Low price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (high + low + close) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted close price (high + low + 2 * close) / 4.
		/// </summary>
		Weighted
	}
}
