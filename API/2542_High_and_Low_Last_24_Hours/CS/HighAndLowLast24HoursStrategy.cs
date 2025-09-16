using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that continuously tracks the highest and lowest prices inside a rolling window (24 hours by default).
/// The behaviour reproduces the original MQL example that displayed the last-day extremes when the user pressed a key.
/// </summary>
public class HighAndLowLast24HoursStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _windowLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest? _highest;
	private Lowest? _lowest;

	private TimeSpan _timeFrame;
	private TimeSpan _cachedWindowLength;

	private decimal? _trackedHigh;
	private decimal? _trackedLow;

	/// <summary>
	/// Duration of the rolling window used for high/low evaluation.
	/// </summary>
	public TimeSpan WindowLength
	{
		get => _windowLength.Value;
		set => _windowLength.Value = value;
	}

	/// <summary>
	/// Candle type used for the market data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public HighAndLowLast24HoursStrategy()
	{
		_windowLength = Param(nameof(WindowLength), TimeSpan.FromHours(24))
			.SetDisplay("Window Length", "Duration used to evaluate highs and lows", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of processed candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFrame = GetTimeFrame();
		_cachedWindowLength = WindowLength;

		var length = CalculateWindowLength(_cachedWindowLength, _timeFrame);

		_highest = new Highest
		{
			Length = length,
			CandlePrice = CandlePrice.High
		};

		_lowest = new Lowest
		{
			Length = length,
			CandlePrice = CandlePrice.Low
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
		}
	}

	private static int CalculateWindowLength(TimeSpan window, TimeSpan frame)
	{
		if (frame <= TimeSpan.Zero)
			return 1;

		var ratio = window.TotalSeconds / frame.TotalSeconds;
		if (double.IsNaN(ratio) || double.IsInfinity(ratio))
			return 1;

		var length = (int)Math.Ceiling(ratio);
		return Math.Max(1, length);
	}

	private TimeSpan GetTimeFrame()
	{
		if (CandleType.Arg is not TimeSpan frame)
			throw new InvalidOperationException("The candle type must define a time frame.");

		if (frame <= TimeSpan.Zero)
			throw new InvalidOperationException("The candle time frame must be positive.");

		return frame;
	}

	private void ProcessCandle(ICandleMessage candle, decimal windowHigh, decimal windowLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_highest == null || _lowest == null)
			return;

		if (WindowLength != _cachedWindowLength)
		{
			_cachedWindowLength = WindowLength;
			var length = CalculateWindowLength(_cachedWindowLength, _timeFrame);
			_highest.Length = length;
			_lowest.Length = length;
		}

		if (windowHigh <= 0m || windowLow <= 0m)
			return;

		var windowStart = candle.CloseTime - _cachedWindowLength;
		var windowEnd = candle.CloseTime;

		var startText = windowStart.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		var endText = windowEnd.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

		if (_trackedHigh != windowHigh)
		{
			_trackedHigh = windowHigh;
			this.AddInfoLog($"Highest price in window ({startText} - {endText}): {windowHigh.ToString("0.#####", CultureInfo.InvariantCulture)}");
		}

		if (_trackedLow != windowLow)
		{
			_trackedLow = windowLow;
			this.AddInfoLog($"Lowest price in window ({startText} - {endText}): {windowLow.ToString("0.#####", CultureInfo.InvariantCulture)}");
		}

		// Draw horizontal levels for the tracked high and low.
		DrawLine(windowStart, windowHigh, windowEnd, windowHigh);
		DrawLine(windowStart, windowLow, windowEnd, windowLow);

		// Draw a vertical marker at the beginning of the rolling window.
		DrawLine(windowStart, windowLow, windowStart, windowHigh);
	}
}
