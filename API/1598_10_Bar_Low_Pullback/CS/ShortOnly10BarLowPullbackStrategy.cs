using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short Only 10 Bar Low Pullback Strategy - sells on new lows with IBS filter and optional EMA trend confirmation.
/// </summary>
public class ShortOnly10BarLowPullbackStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lowestPeriod;
	private readonly StrategyParam<decimal> _ibsThreshold;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private Lowest _lowest;
	private ExponentialMovingAverage _ema;

	private decimal _prevLowest;
	private decimal _prevLow;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for lowest low.
	/// </summary>
	public int LowestPeriod
	{
		get => _lowestPeriod.Value;
		set => _lowestPeriod.Value = value;
	}

	/// <summary>
	/// IBS threshold for entry.
	/// </summary>
	public decimal IbsThreshold
	{
		get => _ibsThreshold.Value;
		set => _ibsThreshold.Value = value;
	}

	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// EMA period for trend filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Start of trading window.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of trading window.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ShortOnly10BarLowPullbackStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_lowestPeriod = Param(nameof(LowestPeriod), 10)
			.SetDisplay("Lowest Low Period", "Lookback for lowest low", "Indicators")
			.SetCanOptimize(true);

		_ibsThreshold = Param(nameof(IbsThreshold), 0.85m)
			.SetDisplay("IBS Threshold", "Internal bar strength threshold", "Signals")
			.SetCanOptimize(true);

		_useEmaFilter = Param(nameof(UseEmaFilter), true)
			.SetDisplay("Use EMA Filter", "Enable trend filter", "Trend Filter");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetDisplay("EMA Period", "EMA period for filter", "Trend Filter")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2014,1,1), TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of trading window", "Time");
		_endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(2099,1,1), TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading window", "Time");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lowest = new Lowest { Length = LowestPeriod };
		if (UseEmaFilter)
			_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartTime || candle.OpenTime > EndTime)
			return;

		var prevLowest = _prevLowest;
		var lowest = _lowest.Process(candle.LowPrice).ToDecimal();
		_prevLowest = lowest;

		var prevLow = _prevLow;
		_prevLow = candle.LowPrice;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
			return;

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;

		var shortCondition = candle.LowPrice < prevLowest && ibs > IbsThreshold;

		if (UseEmaFilter)
		{
			var ma = _ema.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
			shortCondition &= candle.ClosePrice < ma;
		}

		if (shortCondition && Position >= 0)
			SellMarket();

		if (Position < 0 && candle.ClosePrice < prevLow)
			BuyMarket();
	}
}

