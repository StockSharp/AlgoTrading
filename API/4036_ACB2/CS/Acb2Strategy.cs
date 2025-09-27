using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tick frame monitor converted from the MetaTrader "ACB2" expert.
/// </summary>
public class Acb2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DataType _activeCandleType;
	private TimeSpan _frameTimeSpan;
	private string _timeframeName = string.Empty;

	private DateTimeOffset? _currentFrameOpen;
	private DateTimeOffset? _previousFrameOpen;
	private DateTimeOffset? _currentReportedOpen;
	private DateTimeOffset? _previousReportedOpen;

	private int _currentTickCount;
	private int _previousTickCount;

	private decimal _currentReportedVolume;
	private decimal _previousReportedVolume;

	private bool _frameInitialized;
	private bool _startupLogged;

	public Acb2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to aggregate ticks into frames.", "General");
	}

	/// <summary>
	/// Candle type used to define the monitored frame.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		yield return (Security, CandleType);
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_frameInitialized = false;
		_startupLogged = false;
		_activeCandleType = default;
		_frameTimeSpan = TimeSpan.Zero;
		_timeframeName = string.Empty;

		_currentFrameOpen = null;
		_previousFrameOpen = null;
		_currentReportedOpen = null;
		_previousReportedOpen = null;

		_currentTickCount = 0;
		_previousTickCount = 0;
		_currentReportedVolume = 0m;
		_previousReportedVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_activeCandleType = CandleType;
		_frameTimeSpan = GetTimeFrame(_activeCandleType);
		_timeframeName = DescribeDataType(_activeCandleType);
		_frameInitialized = false;
		_startupLogged = false;

		SubscribeCandles(_activeCandleType)
		.Bind(ProcessCandle)
		.Start();

		SubscribeTicks()
		.Bind(ProcessTick)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;
		var tickVolume = ExtractTickVolume(candle);

		// Update the latest reported volume snapshot for the candle represented by openTime.
		if (!_currentReportedOpen.HasValue || openTime >= _currentReportedOpen.Value)
		{
			if (_currentReportedOpen.HasValue && openTime > _currentReportedOpen.Value)
			{
				_previousReportedOpen = _currentReportedOpen;
				_previousReportedVolume = _currentReportedVolume;
			}

			_currentReportedOpen = openTime;
			_currentReportedVolume = tickVolume;
		}
		else if (!_previousReportedOpen.HasValue || openTime >= _previousReportedOpen.Value)
		{
			_previousReportedOpen = openTime;
			_previousReportedVolume = tickVolume;
		}

		if (_frameInitialized)
		return;

		if (!_currentReportedOpen.HasValue)
		return;

		_currentFrameOpen = _currentReportedOpen;
		_currentTickCount = (int)Math.Round(_currentReportedVolume, MidpointRounding.AwayFromZero);

		if (_previousReportedOpen.HasValue)
		{
			_previousFrameOpen = _previousReportedOpen;
			_previousTickCount = (int)Math.Round(_previousReportedVolume, MidpointRounding.AwayFromZero);
		}

		_frameInitialized = true;
	}

	private void ProcessTick(ITickTradeMessage trade)
	{
		if (!_frameInitialized)
		return;

		var tickTime = trade.ServerTime;
		if (tickTime == default)
		return;

		var frameOpen = AlignToFrame(tickTime, _frameTimeSpan);

		_currentFrameOpen ??= frameOpen;

		if (_currentReportedOpen is null)
		{
			_currentReportedOpen = _currentFrameOpen;
			_currentReportedVolume = _currentTickCount;
		}

		if (!_startupLogged)
		{
			_startupLogged = true;
			LogInfo("Start");
			return;
		}

		if (_currentFrameOpen!.Value == frameOpen)
		{
			_currentTickCount++;
		}
		else
		{
			var previousOpen = _currentFrameOpen.Value;
			var previousCount = _currentTickCount;
			var previousReported = GetReportedVolume(previousOpen);

			if (previousReported <= 0m && previousCount > 0)
			previousReported = previousCount;

			_previousFrameOpen = previousOpen;
			_previousTickCount = previousCount;
			_previousReportedOpen = previousOpen;
			_previousReportedVolume = previousReported;

			_currentFrameOpen = frameOpen;

			var currentReported = GetReportedVolume(frameOpen);
			_currentReportedOpen = frameOpen;
			_currentReportedVolume = currentReported;

			_currentTickCount = 1;

			if (_currentReportedVolume < _currentTickCount)
			_currentReportedVolume = _currentTickCount;

			LogInfo(
			"Frame 0 closed: Open {0} Count: {1} Report: {2}",
			FormatTime(previousOpen),
			previousCount,
			FormatVolume(previousReported));
		}

		if (_currentReportedOpen.Value == _currentFrameOpen.Value && _currentReportedVolume < _currentTickCount)
		_currentReportedVolume = _currentTickCount;

		var frameLengthSeconds = _currentFrameOpen.HasValue
		? (int)Math.Max(0, (tickTime - _currentFrameOpen.Value).TotalSeconds)
		: 0;

		var symbol = Security?.Id ?? string.Empty;

		LogInfo(
		"{0} {1}: Frame 0: Open: {2} Seconds: {3} Count: {4} Report: {5} // Frame 1: Open: {6} Count: {7} Report: {8}",
		symbol,
		_timeframeName,
		FormatTime(_currentFrameOpen),
		frameLengthSeconds,
		_currentTickCount,
		FormatVolume(_currentReportedVolume),
		FormatTime(_previousFrameOpen),
		_previousTickCount,
		FormatVolume(_previousReportedVolume));
	}

	private static decimal ExtractTickVolume(ICandleMessage candle)
	{
		if (candle.TotalTicks.HasValue)
		return candle.TotalTicks.Value;

		if (candle.TotalVolume.HasValue)
		return candle.TotalVolume.Value;

		if (candle.Volume.HasValue)
		return candle.Volume.Value;

		return 0m;
	}

	private decimal GetReportedVolume(DateTimeOffset openTime)
	{
		if (_currentReportedOpen.HasValue && _currentReportedOpen.Value == openTime)
		return _currentReportedVolume;

		if (_previousReportedOpen.HasValue && _previousReportedOpen.Value == openTime)
		return _previousReportedVolume;

		return 0m;
	}

	private static DateTimeOffset AlignToFrame(DateTimeOffset time, TimeSpan frame)
	{
		if (frame <= TimeSpan.Zero)
		return time;

		var utc = time.UtcDateTime;
		var ticks = utc.Ticks / frame.Ticks * frame.Ticks;
		var alignedUtc = new DateTime(ticks, DateTimeKind.Utc);
		var aligned = new DateTimeOffset(alignedUtc);
		return aligned.ToOffset(time.Offset);
	}

	private static TimeSpan GetTimeFrame(DataType dataType)
	{
		if (dataType.MessageType == typeof(TimeFrameCandleMessage) && dataType.Arg is TimeSpan span)
		return span;

		return TimeSpan.Zero;
	}

	private static string DescribeDataType(DataType dataType)
	{
		if (dataType.MessageType == typeof(TimeFrameCandleMessage) && dataType.Arg is TimeSpan timeFrame)
		{
			var minutes = timeFrame.TotalMinutes;
			switch (minutes)
			{
				case 1: return "M1";
				case 2: return "M2";
				case 3: return "M3";
				case 4: return "M4";
				case 5: return "M5";
				case 6: return "M6";
				case 10: return "M10";
				case 12: return "M12";
				case 15: return "M15";
				case 20: return "M20";
				case 30: return "M30";
			}

			var hours = timeFrame.TotalHours;
			switch (hours)
			{
				case 1: return "H1";
				case 2: return "H2";
				case 3: return "H3";
				case 4: return "H4";
				case 6: return "H6";
				case 8: return "H8";
			}

			var days = timeFrame.TotalDays;
			switch (days)
			{
				case 1: return "D1";
				case 7: return "W1";
			}

			return timeFrame.ToString();
		}

		return dataType.ToString() ?? string.Empty;
	}

	private static string FormatTime(DateTimeOffset? time)
	=> time?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "N/A";

	private static string FormatVolume(decimal volume)
	=> volume.ToString("0.####", CultureInfo.InvariantCulture);
}
