namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Translated version of the "Average Pip Movement based on Tick & Seconds" expert advisor.
/// Calculates the average pip movement per tick together with the average spread and logs
/// a periodic summary at a user-defined interval.
/// </summary>
public class AveragePipMovementTickSecondsStrategy : Strategy
{
	private readonly StrategyParam<int> _maxTicks;
	private readonly StrategyParam<int> _checkIntervalSeconds;

	private SimpleMovingAverage? _pipMovementAverage;
	private SimpleMovingAverage? _spreadAverage;

	private decimal? _previousBid;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _averagePipMovementPerTick;
	private decimal? _averageSpreadPerTick;
	private decimal _pipSize;
	private DateTimeOffset _lastReportTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="AveragePipMovementTickSecondsStrategy"/> class.
	/// </summary>
	public AveragePipMovementTickSecondsStrategy()
	{
		_maxTicks = Param(nameof(MaxTicks), 100)
		.SetDisplay("Tick Buffer Size", "Number of ticks considered when computing averages.", "General")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_checkIntervalSeconds = Param(nameof(CheckIntervalSeconds), 1)
		.SetDisplay("Check Interval (seconds)", "Seconds between periodic summary reports.", "General")
		.SetGreaterThanZero()
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Number of ticks used for both movement and spread averages.
	/// </summary>
	public int MaxTicks
	{
		get => _maxTicks.Value;
		set => _maxTicks.Value = value;
	}

	/// <summary>
	/// Interval between summary reports expressed in seconds.
	/// </summary>
	public int CheckIntervalSeconds
	{
		get => _checkIntervalSeconds.Value;
		set => _checkIntervalSeconds.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		_pipSize = security.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
		{
			// Fallback for instruments without a configured price step.
			_pipSize = 0.0001m;
		}

		var ticks = Math.Max(2, MaxTicks);

		_pipMovementAverage = new SimpleMovingAverage
		{
			// Use length equal to the number of absolute differences (ticks - 1).
			Length = Math.Max(1, ticks - 1)
		};

		_spreadAverage = new SimpleMovingAverage
		{
			Length = ticks
		};

		_lastReportTime = time;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var time = message.ServerTime;

		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		{
			HandleBidUpdate(bid, time);
		}

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		{
			_currentAsk = ask;
		}

		TryProcessSpread(time);
		TryReport(time);
	}

	private void HandleBidUpdate(decimal bid, DateTimeOffset time)
	{
		_currentBid = bid;

		if (_pipMovementAverage is null)
		{
			return;
		}

		if (_previousBid is decimal previous)
		{
			// Convert price movement into pips before feeding the indicator.
			var pipMovement = Math.Abs((bid - previous) / _pipSize);
			var value = _pipMovementAverage.Process(new DecimalIndicatorValue(_pipMovementAverage, pipMovement, time));

			if (value.IsFinal)
			{
				_averagePipMovementPerTick = value.ToDecimal();
				LogTickAverages();
			}
		}

		_previousBid = bid;
	}

	private void TryProcessSpread(DateTimeOffset time)
	{
		if (_spreadAverage is null || _currentBid is not decimal bid || _currentAsk is not decimal ask)
		{
			return;
		}

		var spread = ask - bid;
		if (spread < 0m)
		{
			return;
		}

		var spreadPips = spread / _pipSize;
		var value = _spreadAverage.Process(new DecimalIndicatorValue(_spreadAverage, spreadPips, time));

		if (value.IsFinal)
		{
			_averageSpreadPerTick = value.ToDecimal();
		}
	}

	private void LogTickAverages()
	{
		if (_averagePipMovementPerTick is not decimal pipAverage || _averageSpreadPerTick is not decimal spreadAverage)
		{
			return;
		}

		// Mirror the original expert by logging tick-based averages continuously.
		LogInfo($"Average pip movement per tick: {pipAverage:0.#####} | Average spread per tick: {spreadAverage:0.#####}");
	}

	private void TryReport(DateTimeOffset time)
	{
		if (_averagePipMovementPerTick is not decimal pipAverage || _averageSpreadPerTick is not decimal spreadAverage)
		{
			return;
		}

		var interval = TimeSpan.FromSeconds(CheckIntervalSeconds);
		if (interval <= TimeSpan.Zero)
		{
			return;
		}

		if (time - _lastReportTime < interval)
		{
			return;
		}

		_lastReportTime = time;

		// Produce a detailed summary similar to the chart comment in the MQL version.
		LogInfo($"Interval report ({CheckIntervalSeconds}s) -> Avg pip movement: {pipAverage:0.#####}, Avg spread: {spreadAverage:0.#####}");
	}
}
