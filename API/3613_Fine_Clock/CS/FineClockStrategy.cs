using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Decorative clock translated from the MetaTrader expert "FineClock".
/// Continuously draws a digital clock on the chart and keeps the strategy comment in sync with the selected time format.
/// </summary>
public class FineClockStrategy : Strategy
{
	/// <summary>
	/// Available clock formats.
	/// </summary>
	public enum ClockFormat
	{
		/// <summary>
		/// Displays hours, minutes and seconds.
		/// </summary>
		Seconds,

		/// <summary>
		/// Displays only hours and minutes.
		/// </summary>
		Minutes,
	}

	/// <summary>
	/// Supported time sources for the clock.
	/// </summary>
	public enum ClockTimeSource
	{
		/// <summary>
		/// Use the local computer time.
		/// </summary>
		Local,

		/// <summary>
		/// Use the connector server time.
		/// </summary>
		Server,

		/// <summary>
		/// Use the UTC time zone.
		/// </summary>
		Utc,
	}

	/// <summary>
	/// Preferred placement of the clock label.
	/// </summary>
	public enum ClockCorner
	{
		/// <summary>
		/// Upper left corner of the chart.
		/// </summary>
		LeftUpper,

		/// <summary>
		/// Lower left corner of the chart.
		/// </summary>
		LeftLower,

		/// <summary>
		/// Upper right corner of the chart.
		/// </summary>
		RightUpper,

		/// <summary>
		/// Lower right corner of the chart.
		/// </summary>
		RightLower,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ClockFormat> _format;
	private readonly StrategyParam<ClockTimeSource> _timeSource;
	private readonly StrategyParam<ClockCorner> _corner;
	private readonly StrategyParam<int> _horizontalOffset;
	private readonly StrategyParam<int> _verticalOffset;
	private readonly StrategyParam<int> _shadowOffset;
	private readonly StrategyParam<bool> _useShadow;
	private readonly StrategyParam<bool> _showInComment;

	private IChartArea? _chartArea;
	private decimal? _lastPrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private string _lastRenderedText;

	/// <summary>
	/// Initializes a new instance of the <see cref="FineClockStrategy"/> class.
	/// </summary>
	public FineClockStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromSeconds(1).TimeFrame())
		.SetDisplay("Candle Type", "Data type used to feed the clock and draw candles", "General");

		_format = Param(nameof(Format), ClockFormat.Seconds)
		.SetDisplay("Format", "Clock output format", "Visualization");

		_timeSource = Param(nameof(TimeSource), ClockTimeSource.Local)
		.SetDisplay("Time Source", "Select between local, server or UTC time", "Visualization");

		_corner = Param(nameof(Corner), ClockCorner.RightLower)
		.SetDisplay("Corner", "Preferred placement of the text label", "Visualization");

		_horizontalOffset = Param(nameof(HorizontalOffset), 0)
		.SetDisplay("Horizontal Offset", "Shift label horizontally in candle units", "Visualization");

		_verticalOffset = Param(nameof(VerticalOffset), 0)
		.SetDisplay("Vertical Offset", "Shift label vertically in price steps", "Visualization");

		_shadowOffset = Param(nameof(ShadowOffset), 1)
		.SetDisplay("Shadow Offset", "Offset applied to the optional shadow label", "Visualization");

		_useShadow = Param(nameof(UseShadow), true)
		.SetDisplay("Use Shadow", "Draw a secondary label to imitate a drop shadow", "Visualization");

		_showInComment = Param(nameof(ShowInComment), true)
		.SetDisplay("Show In Comment", "Mirror the clock text inside the strategy comment", "Visualization");
	}

	/// <summary>
	/// Candle type used for data subscription and chart drawing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Clock display format.
	/// </summary>
	public ClockFormat Format
	{
		get => _format.Value;
		set => _format.Value = value;
	}

	/// <summary>
	/// Selected time source for the clock.
	/// </summary>
	public ClockTimeSource TimeSource
	{
		get => _timeSource.Value;
		set => _timeSource.Value = value;
	}

	/// <summary>
	/// Preferred chart corner used to position the clock.
	/// </summary>
	public ClockCorner Corner
	{
		get => _corner.Value;
		set => _corner.Value = value;
	}

	/// <summary>
	/// Horizontal shift in candle units.
	/// </summary>
	public int HorizontalOffset
	{
		get => _horizontalOffset.Value;
		set => _horizontalOffset.Value = value;
	}

	/// <summary>
	/// Vertical shift expressed in price steps.
	/// </summary>
	public int VerticalOffset
	{
		get => _verticalOffset.Value;
		set => _verticalOffset.Value = value;
	}

	/// <summary>
	/// Offset used for the optional shadow text.
	/// </summary>
	public int ShadowOffset
	{
		get => _shadowOffset.Value;
		set => _shadowOffset.Value = value;
	}

	/// <summary>
	/// Enables rendering of the drop shadow label.
	/// </summary>
	public bool UseShadow
	{
		get => _useShadow.Value;
		set => _useShadow.Value = value;
	}

	/// <summary>
	/// Mirrors the clock text inside the strategy comment field.
	/// </summary>
	public bool ShowInComment
	{
		get => _showInComment.Value;
		set => _showInComment.Value = value;
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

		Timer.Stop();

		_chartArea = null;
		_lastPrice = null;
		_bestBid = null;
		_bestAsk = null;
		_lastRenderedText = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		_chartArea = CreateChartArea();
		if (_chartArea != null)
		{
			DrawCandles(_chartArea, candleSubscription);
		}

		// Render the first clock value immediately.
		UpdateClock(force: true);

		Timer.Start(GetTimerInterval(), OnTimerTick);
	}

	private void OnTimerTick()
	{
		// Timer keeps the clock running even when no market data arrives.
		UpdateClock();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.ClosePrice > 0m)
		{
			_lastPrice = candle.ClosePrice;
		}

		// Update the display when a candle finishes to refresh the anchor price.
		if (candle.State == CandleStates.Finished)
		{
			UpdateClock(force: true);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var last = level1.TryGetDecimal(Level1Fields.LastTradePrice);
		if (last != null && last.Value > 0m)
		{
			_lastPrice = last.Value;
		}

		var bid = level1.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null && bid.Value > 0m)
		{
			_bestBid = bid.Value;
		}

		var ask = level1.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null && ask.Value > 0m)
		{
			_bestAsk = ask.Value;
		}
	}

	private void UpdateClock(bool force = false)
	{
		var now = GetClockTime();
		var text = FormatTime(now);

		if (!force && text == _lastRenderedText)
		{
			return;
		}

		_lastRenderedText = text;

		if (ShowInComment)
		{
			// Synchronize the strategy comment with the visible clock.
			Comment = text;
		}

		if (_chartArea == null)
		{
			return;
		}

		var price = GetPriceForDisplay();
		if (price <= 0m)
		{
			return;
		}

		DrawClockText(now, price, text);
	}

	private void DrawClockText(DateTimeOffset baseTime, decimal basePrice, string text)
	{
		var placement = CalculatePlacement(baseTime, basePrice);

		if (UseShadow && placement.shadowTime != null && placement.shadowPrice != null)
		{
			// Draw the shadow first so the main label stays on top.
			DrawText(_chartArea!, placement.shadowTime.Value, placement.shadowPrice.Value, text);
		}

		DrawText(_chartArea!, placement.mainTime, placement.mainPrice, text);
	}

	private (DateTimeOffset mainTime, decimal mainPrice, DateTimeOffset? shadowTime, decimal? shadowPrice) CalculatePlacement(DateTimeOffset baseTime, decimal basePrice)
	{
		var timeUnit = GetTimeFrame();
		if (timeUnit <= TimeSpan.Zero)
		{
			timeUnit = Format == ClockFormat.Seconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);
		}

		var priceStep = Security.PriceStep;
		if (priceStep == null || priceStep <= 0m)
		{
			priceStep = Math.Abs(basePrice) > 0m ? Math.Abs(basePrice) * 0.001m : 1m;
		}

		var horizontalDirection = Corner is ClockCorner.RightUpper or ClockCorner.RightLower ? 1 : -1;
		var verticalDirection = Corner is ClockCorner.LeftUpper or ClockCorner.RightUpper ? 1 : -1;

		var mainTime = baseTime + TimeSpan.FromTicks(timeUnit.Ticks * HorizontalOffset * horizontalDirection);
		var mainPrice = basePrice + priceStep.Value * VerticalOffset * verticalDirection;

		DateTimeOffset? shadowTime = null;
		decimal? shadowPrice = null;

		if (UseShadow && ShadowOffset != 0)
		{
			shadowTime = mainTime - TimeSpan.FromTicks(timeUnit.Ticks * ShadowOffset * horizontalDirection);
			shadowPrice = mainPrice - priceStep.Value * ShadowOffset * verticalDirection;
		}

		return (mainTime, mainPrice, shadowTime, shadowPrice);
	}

	private decimal GetPriceForDisplay()
	{
		if (_lastPrice != null && _lastPrice.Value > 0m)
		{
			return _lastPrice.Value;
		}

		if (_bestBid != null && _bestAsk != null)
		{
			return (_bestBid.Value + _bestAsk.Value) / 2m;
		}

		if (Security.LastPrice != null && Security.LastPrice.Value > 0m)
		{
			return Security.LastPrice.Value;
		}

		return Security.PriceStep ?? 1m;
	}

	private TimeSpan GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan frame ? frame : TimeSpan.Zero;
	}

	private DateTimeOffset GetClockTime()
	{
		var now = CurrentTime;
		if (now == default)
		{
			now = DateTimeOffset.UtcNow;
		}

		return TimeSource switch
		{
			ClockTimeSource.Local => now.ToLocalTime(),
			ClockTimeSource.Server => now,
			ClockTimeSource.Utc => now.ToUniversalTime(),
			_ => now,
		};
	}

	private string FormatTime(DateTimeOffset time)
	{
		return Format switch
		{
			ClockFormat.Minutes => time.ToString(" HH:mm "),
			_ => time.ToString(" HH:mm:ss "),
		};
	}

	private TimeSpan GetTimerInterval()
	{
		return Format == ClockFormat.Seconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);
	}
}
