using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that records average bid-ask spread over specified time intervals.
/// </summary>
public class SpreadProbeStrategy : Strategy
{
	private readonly StrategyParam<int> _interval;

	private DateTimeOffset _beginTime;
	private DateTimeOffset _nextInterval;
	private decimal _sumSpread;
	private int _ticks;
	private StreamWriter _writer;
	private decimal _bid;
	private decimal _ask;

	/// <summary>
	/// Interval in minutes for averaging spread.
	/// </summary>
	public int Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SpreadProbeStrategy"/>.
	/// </summary>
	public SpreadProbeStrategy()
	{
		_interval = Param(nameof(Interval), 15)
			.SetDisplay("Interval", "Interval in minutes for averaging spread", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sumSpread = 0;
		_ticks = 0;
		_bid = _ask = 0;
		_writer = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Ensure the interval is within [5, 60] and multiple of 5.
		if (Interval < 5)
			Interval = 5;
		else if (Interval > 60)
			Interval = 60;
		else
			Interval = (Interval / 5) * 5;

		_beginTime = time;
		_nextInterval = time
			.AddMinutes(Interval - time.Minute % Interval)
			.AddSeconds(-time.Second)
			.AddMilliseconds(-time.Millisecond);

		var fileName = $"{Security.Id}_BidAskSpread({Interval})_{time:yyyyMMdd}.txt";
		_writer = new StreamWriter(File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			AutoFlush = true
		};

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_ask = (decimal)ask;

		if (_bid == default || _ask == default || _writer is null)
			return;

		var time = level1.ServerTime;

		if (time > _nextInterval)
		{
			var start = _nextInterval - TimeSpan.FromMinutes(Interval);
			if (start < _beginTime)
				start = _beginTime;

			var avg = _ticks > 0 ? _sumSpread / _ticks : 0m;
			_writer.WriteLine($"{start:HH:mm:ss},{_nextInterval:HH:mm:ss},{avg.ToString(CultureInfo.InvariantCulture)}");

			_sumSpread = 0;
			_ticks = 0;

			while (time > _nextInterval)
				_nextInterval = _nextInterval.AddMinutes(Interval);
		}

		_sumSpread += _ask - _bid;
		_ticks++;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (_writer != null)
		{
			var start = _nextInterval - TimeSpan.FromMinutes(Interval);
			if (start < _beginTime)
				start = _beginTime;

			var end = CurrentTime;
			var avg = _ticks > 0 ? _sumSpread / _ticks : 0m;
			_writer.WriteLine($"{start:HH:mm:ss},{end:HH:mm:ss},{avg.ToString(CultureInfo.InvariantCulture)}");
			_writer.Dispose();
			_writer = null;
		}

		base.OnStopped();
	}
}
