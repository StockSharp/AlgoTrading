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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Visual clock strategy inspired by the original LoongClock.mq5 expert.
/// Displays hour, minute, and second labels arranged in a circular layout on the chart.
/// </summary>
public class LoongClockStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ClockTimeSource> _timeSource;
	private readonly StrategyParam<TimeSpan> _hourTimeRadius;
	private readonly StrategyParam<TimeSpan> _minuteTimeRadius;
	private readonly StrategyParam<TimeSpan> _secondTimeRadius;
	private readonly StrategyParam<decimal> _hourPriceRadius;
	private readonly StrategyParam<decimal> _minutePriceRadius;
	private readonly StrategyParam<decimal> _secondPriceRadius;

	private IChartArea? _area;
	private DateTimeOffset? _anchorTime;
	private decimal? _anchorPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="LoongClockStrategy"/> class.
	/// </summary>
	public LoongClockStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used to anchor the drawing area.", "General");

		_timeSource = Param(nameof(TimeSource), ClockTimeSource.Local)
		.SetDisplay("Time Source", "Select which time base should be shown by the clock.", "Clock");

		_hourTimeRadius = Param(nameof(HourTimeRadius), TimeSpan.FromMinutes(8))
		.SetDisplay("Hour Time Radius", "Horizontal radius (time axis) for the hour label.", "Clock");

		_minuteTimeRadius = Param(nameof(MinuteTimeRadius), TimeSpan.FromMinutes(10))
		.SetDisplay("Minute Time Radius", "Horizontal radius (time axis) for the minute label.", "Clock");

		_secondTimeRadius = Param(nameof(SecondTimeRadius), TimeSpan.FromMinutes(12))
		.SetDisplay("Second Time Radius", "Horizontal radius (time axis) for the second label.", "Clock");

		_hourPriceRadius = Param(nameof(HourPriceRadius), 0.2m)
		.SetDisplay("Hour Price Radius", "Vertical radius (price axis) for the hour label.", "Clock");

		_minutePriceRadius = Param(nameof(MinutePriceRadius), 0.32m)
		.SetDisplay("Minute Price Radius", "Vertical radius (price axis) for the minute label.", "Clock");

		_secondPriceRadius = Param(nameof(SecondPriceRadius), 0.52m)
		.SetDisplay("Second Price Radius", "Vertical radius (price axis) for the second label.", "Clock");
	}

	/// <summary>
	/// Candle type subscription used to provide a chart axis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Selects which time base should be visualized.
	/// </summary>
	public ClockTimeSource TimeSource
	{
		get => _timeSource.Value;
		set => _timeSource.Value = value;
	}

	/// <summary>
	/// Horizontal radius for the hour label measured on the time axis.
	/// </summary>
	public TimeSpan HourTimeRadius
	{
		get => _hourTimeRadius.Value;
		set => _hourTimeRadius.Value = value;
	}

	/// <summary>
	/// Horizontal radius for the minute label measured on the time axis.
	/// </summary>
	public TimeSpan MinuteTimeRadius
	{
		get => _minuteTimeRadius.Value;
		set => _minuteTimeRadius.Value = value;
	}

	/// <summary>
	/// Horizontal radius for the second label measured on the time axis.
	/// </summary>
	public TimeSpan SecondTimeRadius
	{
		get => _secondTimeRadius.Value;
		set => _secondTimeRadius.Value = value;
	}

	/// <summary>
	/// Vertical radius for the hour label measured on the price axis.
	/// </summary>
	public decimal HourPriceRadius
	{
		get => _hourPriceRadius.Value;
		set => _hourPriceRadius.Value = value;
	}

	/// <summary>
	/// Vertical radius for the minute label measured on the price axis.
	/// </summary>
	public decimal MinutePriceRadius
	{
		get => _minutePriceRadius.Value;
		set => _minutePriceRadius.Value = value;
	}

	/// <summary>
	/// Vertical radius for the second label measured on the price axis.
	/// </summary>
	public decimal SecondPriceRadius
	{
		get => _secondPriceRadius.Value;
		set => _secondPriceRadius.Value = value;
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

		_area = null;
		_anchorTime = null;
		_anchorPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_anchorTime = time;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(OnCandle)
		.Start();

		_area = CreateChartArea();

		if (_area != null)
		{
			DrawCandles(_area, subscription);
		}

		// Use a one-second timer to update the clock continuously.
		Timer.Start(TimeSpan.FromSeconds(1), OnTimer);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	private void OnCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Remember the last closing price to position the clock vertically.
		_anchorPrice = candle.ClosePrice;

		// Initialize the anchor time using completed market data if it was not set yet.
		_anchorTime ??= candle.OpenTime;
	}

	private void OnTimer()
	{
		var area = _area;

		if (area == null)
		return;

		var anchorTime = _anchorTime ?? CurrentTime;
		var anchorPrice = GetAnchorPrice();
		var clockTime = GetClockTime();

		// Draw the clock center marker.
		DrawText(area, anchorTime, anchorPrice, "@");

		DrawClockLabel(area, anchorTime, anchorPrice, clockTime.Hour % 12 + clockTime.Minute / 60m + clockTime.Second / 3600m,
		12, HourTimeRadius, HourPriceRadius, clockTime.ToString("HH"));

		DrawClockLabel(area, anchorTime, anchorPrice, clockTime.Minute + clockTime.Second / 60m,
		60, MinuteTimeRadius, MinutePriceRadius, clockTime.ToString("mm"));

		DrawClockLabel(area, anchorTime, anchorPrice, clockTime.Second,
		60, SecondTimeRadius, SecondPriceRadius, clockTime.ToString("ss"));
	}

	private void DrawClockLabel(IChartArea area, DateTimeOffset anchorTime, decimal anchorPrice, decimal value,
	int extent, TimeSpan timeRadius, decimal priceRadius, string text)
	{
		// Convert the current hand value into radians.
		var angle = Math.PI / 2 - 2 * Math.PI * (double)value / extent;

		// Map the polar coordinates into the chart axes (time on X, price on Y).
		var timeOffsetTicks = (long)Math.Round(timeRadius.Ticks * Math.Cos(angle));
		var time = anchorTime + TimeSpan.FromTicks(timeOffsetTicks);
		var price = anchorPrice + (decimal)Math.Sin(angle) * priceRadius;

		DrawText(area, time, price, text);
	}

	private decimal GetAnchorPrice()
	{
		if (_anchorPrice != null)
		return _anchorPrice.Value;

		var lastTrade = Security?.LastTrade;

		if (lastTrade != null)
		return lastTrade.Price;

		// Fallback value keeps the clock visible even when no market data arrived yet.
		return 0m;
	}

	private DateTimeOffset GetClockTime()
	{
		return TimeSource switch
		{
			ClockTimeSource.Server => CurrentTime,
			ClockTimeSource.Utc => DateTimeOffset.UtcNow,
			_ => DateTimeOffset.Now,
		};
	}
}

/// <summary>
/// Defines available sources for the displayed clock time.
/// </summary>
public enum ClockTimeSource
{
	/// <summary>
	/// Uses the local machine time.
	/// </summary>
	Local,

	/// <summary>
	/// Uses the server time provided by the trading connection.
	/// </summary>
	Server,

	/// <summary>
	/// Uses Coordinated Universal Time.
	/// </summary>
	Utc
}

