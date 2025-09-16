using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors candle direction across multiple timeframes similar to the MQL Piano indicator.
/// </summary>
public class PianoMultiTimeframeBarStateStrategy : Strategy
{
	private static readonly TimeFrameInfo[] TimeFrames =
	[
		new("M1", TimeSpan.FromMinutes(1).TimeFrame()),
		new("M2", TimeSpan.FromMinutes(2).TimeFrame()),
		new("M3", TimeSpan.FromMinutes(3).TimeFrame()),
		new("M4", TimeSpan.FromMinutes(4).TimeFrame()),
		new("M5", TimeSpan.FromMinutes(5).TimeFrame()),
		new("M6", TimeSpan.FromMinutes(6).TimeFrame()),
		new("M10", TimeSpan.FromMinutes(10).TimeFrame()),
		new("M12", TimeSpan.FromMinutes(12).TimeFrame()),
		new("M15", TimeSpan.FromMinutes(15).TimeFrame()),
		new("M20", TimeSpan.FromMinutes(20).TimeFrame()),
		new("M30", TimeSpan.FromMinutes(30).TimeFrame()),
		new("H1", TimeSpan.FromHours(1).TimeFrame()),
		new("H2", TimeSpan.FromHours(2).TimeFrame()),
		new("H3", TimeSpan.FromHours(3).TimeFrame()),
		new("H4", TimeSpan.FromHours(4).TimeFrame()),
		new("H6", TimeSpan.FromHours(6).TimeFrame()),
		new("H8", TimeSpan.FromHours(8).TimeFrame()),
		new("H12", TimeSpan.FromHours(12).TimeFrame()),
		new("D1", TimeSpan.FromDays(1).TimeFrame()),
		new("W1", TimeSpan.FromDays(7).TimeFrame()),
		new("MN1", TimeSpan.FromDays(30).TimeFrame())
	];

	private readonly StrategyParam<int> _barsToTrack;

	private PianoBuffer[] _buffers = Array.Empty<PianoBuffer>();
	private string _latestLine1 = string.Empty;
	private string _latestLine2 = string.Empty;
	private string _latestLine3 = string.Empty;

	/// <summary>
	/// Number of finished candles to keep for each timeframe.
	/// </summary>
	public int BarsToTrack
	{
		get => _barsToTrack.Value;
		set => _barsToTrack.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PianoMultiTimeframeBarStateStrategy"/> class.
	/// </summary>
	public PianoMultiTimeframeBarStateStrategy()
	{
		_barsToTrack = Param(nameof(BarsToTrack), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bars Per Timeframe", "How many recent candles are evaluated per timeframe.", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);
	}

	/// <summary>
	/// Combined state line for the most recent finished candles.
	/// </summary>
	public string LatestCurrentBarLine => _latestLine1;

	/// <summary>
	/// Combined state line for the previous finished candles.
	/// </summary>
	public string LatestPreviousBarLine => _latestLine2;

	/// <summary>
	/// Combined state line for the second previous finished candles.
	/// </summary>
	public string LatestSecondPreviousBarLine => _latestLine3;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var frame in TimeFrames)
		{
			yield return (Security, frame.Type);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buffers = Array.Empty<PianoBuffer>();
		_latestLine1 = string.Empty;
		_latestLine2 = string.Empty;
		_latestLine3 = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_buffers = new PianoBuffer[TimeFrames.Length];

		// Subscribe to every timeframe using the high-level API.
		for (var i = 0; i < TimeFrames.Length; i++)
		{
			_buffers[i] = new PianoBuffer(BarsToTrack);
			var index = i;
			var frame = TimeFrames[i];

			var subscription = SubscribeCandles(frame.Type);
			subscription
				.Do(candle => ProcessCandle(index, frame.Label, candle))
				.Start();
		}

		UpdateLines();
	}

	private void ProcessCandle(int index, string label, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Classify the candle based on its open and close values.
		var status = DetermineStatus(candle.OpenPrice, candle.ClosePrice);
		_buffers[index].Push(status);

		UpdateLines();

		LogInfo($"[{label}] Piano states updated. Latest: {_latestLine1}, Previous: {_latestLine2}, Older: {_latestLine3}");
	}

	private void UpdateLines()
	{
		// If subscriptions are not initialised there is nothing to build.
		if (_buffers.Length == 0)
		{
			_latestLine1 = string.Empty;
			_latestLine2 = string.Empty;
			_latestLine3 = string.Empty;
			return;
		}

		var builder1 = new StringBuilder();
		var builder2 = new StringBuilder();
		var builder3 = new StringBuilder();

		// Build the three Piano rows in the same order as the original script.
		for (var i = 0; i < _buffers.Length; i++)
		{
			builder1.Append('*').Append(_buffers[i].TryGet(0, out var first) ? first : '-');
			builder2.Append('*').Append(_buffers[i].TryGet(1, out var second) ? second : '-');
			builder3.Append('*').Append(_buffers[i].TryGet(2, out var third) ? third : '-');
		}

		_latestLine1 = builder1.ToString();
		_latestLine2 = builder2.ToString();
		_latestLine3 = builder3.ToString();
	}

	private static char DetermineStatus(decimal open, decimal close)
	{
		if (close > open)
			return '1';

		if (close < open)
			return '0';

		return '-';
	}

	private sealed class PianoBuffer
	{
		private readonly char[] _values;
		private int _count;

		public PianoBuffer(int capacity)
		{
			_values = new char[Math.Max(capacity, 0)];
			_count = 0;
		}

		public void Push(char value)
		{
			if (_values.Length == 0)
				return;

			if (_count > 0)
			{
				var length = Math.Min(_count, _values.Length - 1);
				Array.Copy(_values, 0, _values, 1, length);
			}

			_values[0] = value;

			if (_count < _values.Length)
				_count++;
		}

		public bool TryGet(int index, out char value)
		{
			if (index < _count)
			{
				value = _values[index];
				return true;
			}

			value = '-';
			return false;
		}
	}

	private sealed class TimeFrameInfo
	{
		public TimeFrameInfo(string label, DataType type)
		{
			Label = label;
			Type = type;
		}

		public string Label { get; }

		public DataType Type { get; }
	}
}
