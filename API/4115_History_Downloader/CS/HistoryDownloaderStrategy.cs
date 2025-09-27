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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the MetaTrader "HistoryDownloader" expert by requesting candle history until a target date.
/// </summary>
public class HistoryDownloaderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _targetDate;
	private readonly StrategyParam<TimeSpan> _requestTimeout;
	private readonly StrategyParam<int> _maxFailures;

	private MarketDataSubscription _subscription;
	private DateTimeOffset? _earliestCandleTime;
	private DateTimeOffset _executionStart;
	private DateTimeOffset _lastUpdateTime;
	private int _receivedCandles;
	private int _timeoutCounter;
	private bool _watchdogActive;

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryDownloaderStrategy"/> class.
	/// </summary>
	public HistoryDownloaderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that should be downloaded", "General");

		_targetDate = Param(nameof(TargetDate), new DateTimeOffset(2009, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Target Date", "Earliest candle timestamp that must be fetched", "Download");

		_requestTimeout = Param(nameof(RequestTimeout), TimeSpan.FromSeconds(1))
			.SetDisplay("Request Timeout", "Maximum wait for additional history before counting a failure", "Download");

		_maxFailures = Param(nameof(MaxFailures), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Failures", "Consecutive timeouts allowed before aborting", "Download");
	}

	/// <summary>
	/// Candle type used when requesting history.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Earliest acceptable candle timestamp.
	/// </summary>
	public DateTimeOffset TargetDate
	{
		get => _targetDate.Value;
		set => _targetDate.Value = value;
	}

	/// <summary>
	/// Maximum delay between incoming candles before a failure is recorded.
	/// </summary>
	public TimeSpan RequestTimeout
	{
		get => _requestTimeout.Value;
		set => _requestTimeout.Value = value;
	}

	/// <summary>
	/// Consecutive timeouts tolerated before cancelling the download.
	/// </summary>
	public int MaxFailures
	{
		get => _maxFailures.Value;
		set => _maxFailures.Value = value;
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

		_subscription?.Dispose();
		_subscription = null;
		_earliestCandleTime = null;
		_executionStart = default;
		_lastUpdateTime = default;
		_receivedCandles = 0;
		_timeoutCounter = 0;
		_watchdogActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_executionStart = time;
		_lastUpdateTime = time;
		_receivedCandles = 0;
		_timeoutCounter = 0;
		_earliestCandleTime = null;
		_watchdogActive = true;

		_subscription = SubscribeCandles(CandleType);
		_subscription.Bind(OnCandle).Start();

		Timer.Start(RequestTimeout, OnTimerTick);

		LogInfo($"History download started. Target date: {TargetDate:O}. Candle type: {CandleType}.");
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		_watchdogActive = false;
		_subscription?.Dispose();
		_subscription = null;
	}

	private void OnCandle(ICandleMessage candle)
	{
		if (!_watchdogActive)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		_receivedCandles++;
		_lastUpdateTime = CurrentTime;
		_timeoutCounter = 0;

		if (_earliestCandleTime is null || candle.OpenTime < _earliestCandleTime.Value)
			_earliestCandleTime = candle.OpenTime;

		var progressMessage = BuildProgressMessage();
		LogInfo(progressMessage);

		if (_earliestCandleTime.HasValue && _earliestCandleTime.Value <= TargetDate)
		{
			var successMessage = $"History download completed. {progressMessage}";
			CompleteDownload(true, successMessage);
		}
	}

	private void OnTimerTick()
	{
		if (!_watchdogActive)
			return;

		var elapsed = CurrentTime - _lastUpdateTime;

		if (elapsed < RequestTimeout)
			return;

		_timeoutCounter++;

		if (_timeoutCounter >= MaxFailures)
		{
			var failureMessage = $"History download failed after {_timeoutCounter} timeouts. {BuildProgressMessage()}";
			CompleteDownload(false, failureMessage);
			return;
		}

		LogWarning($"Waiting for additional history. No new candles for {FormatDuration(elapsed)} (failure {_timeoutCounter}/{MaxFailures}).");
	}

	private void CompleteDownload(bool success, string message)
	{
		if (!_watchdogActive)
			return;

		_watchdogActive = false;

		_subscription?.Dispose();
		_subscription = null;

		if (success)
			LogInfo(message);
		else
			LogError(message);

		Stop();
	}

	private string BuildProgressMessage()
	{
		var earliest = _earliestCandleTime.HasValue
			? _earliestCandleTime.Value.ToString("O", CultureInfo.InvariantCulture)
			: "n/a";

		var elapsed = CurrentTime - _executionStart;

		return $"Bars received: {_receivedCandles}. Earliest bar: {earliest}. Elapsed: {FormatDuration(elapsed)}.";
	}

	private static string FormatDuration(TimeSpan duration)
	{
		if (duration < TimeSpan.Zero)
			duration = TimeSpan.Zero;

		var totalHours = (int)duration.TotalHours;
		var minutes = duration.Minutes;
		var seconds = duration.Seconds;

		return $"{totalHours}h {minutes}m {seconds}s";
	}
}
