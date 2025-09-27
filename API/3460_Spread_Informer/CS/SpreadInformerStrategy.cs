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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors bid-ask spread statistics and raises alerts when the spread exceeds the defined limit.
/// </summary>
public class SpreadInformerStrategy : Strategy
{
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<int> _alertIntervalSeconds;

	private decimal _priceStep;
	private decimal _prevSpreadPoints;
	private decimal _maxObservedSpread;
	private DateTimeOffset? _maxSpreadTime;
	private decimal _minObservedSpread;
	private DateTimeOffset? _minSpreadTime;
	private decimal _sumSpreadPoints;
	private long _spreadSamples;
	private DateTimeOffset _startTime;
	private DateTimeOffset _lastUpdateTime;
	private DateTimeOffset _lastAlertTime;
	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Maximum allowed spread in points. Zero disables alerts.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Minimal interval between consecutive alerts in seconds.
	/// </summary>
	public int AlertIntervalSeconds
	{
		get => _alertIntervalSeconds.Value;
		set => _alertIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpreadInformerStrategy"/> class.
	/// </summary>
	public SpreadInformerStrategy()
	{
		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 0)
		.SetDisplay("Max Spread", "Maximum allowed spread in points (0 disables alert)", "General")
		.SetCanOptimize(true)
		.SetOptimize(0, 100, 5);

		_alertIntervalSeconds = Param(nameof(AlertIntervalSeconds), 0)
		.SetDisplay("Alert Interval", "Minimum interval between alerts in seconds", "General")
		.SetCanOptimize(true)
		.SetOptimize(0, 300, 30);
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

		_priceStep = 0m;
		_prevSpreadPoints = 0m;
		_maxObservedSpread = 0m;
		_maxSpreadTime = null;
		_minObservedSpread = 0m;
		_minSpreadTime = null;
		_sumSpreadPoints = 0m;
		_spreadSamples = 0;
		_startTime = DateTimeOffset.MinValue;
		_lastUpdateTime = DateTimeOffset.MinValue;
		_lastAlertTime = DateTimeOffset.MinValue;
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		if (_priceStep <= 0m)
		{
			// Fallback to price scale if step is missing to avoid division by zero.
			_priceStep = Security?.MinPriceStep ?? 0m;
		}

		if (_priceStep <= 0m)
		{
			// Assume one point equals one price unit as a last resort.
			_priceStep = 1m;
		}

		_startTime = time;
		_lastUpdateTime = time;
		_lastAlertTime = DateTimeOffset.MinValue;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		if (_bestBid <= 0m || _bestAsk <= 0m)
		return;

		var spreadPrice = _bestAsk - _bestBid;

		if (spreadPrice <= 0m)
		return;

		var spreadPoints = spreadPrice / _priceStep;

		var time = level1.ServerTime;
		_lastUpdateTime = time;

		if (_spreadSamples == 0)
		{
			_minObservedSpread = spreadPoints;
			_minSpreadTime = time;
		}

		if (spreadPoints > _maxObservedSpread || _maxSpreadTime is null)
		{
			_maxObservedSpread = spreadPoints;
			_maxSpreadTime = time;
		}

		if (spreadPoints < _minObservedSpread || _minSpreadTime is null)
		{
			_minObservedSpread = spreadPoints;
			_minSpreadTime = time;
		}

		_sumSpreadPoints += spreadPoints;
		_spreadSamples++;

		RaiseAlertIfNeeded(spreadPoints, time);

		_prevSpreadPoints = spreadPoints;
	}

	private void RaiseAlertIfNeeded(decimal spreadPoints, DateTimeOffset time)
	{
		var limit = MaxSpreadPoints;

		if (limit <= 0)
		return;

		if (_prevSpreadPoints <= limit && spreadPoints > limit)
		{
			var interval = TimeSpan.FromSeconds(Math.Max(0, AlertIntervalSeconds));

			if (_lastAlertTime == DateTimeOffset.MinValue || time - _lastAlertTime >= interval)
			{
				LogInfo($"Spread {spreadPoints:F2} points exceeded limit {limit} points at {time:O}.");
				_lastAlertTime = time;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (_spreadSamples > 0)
		{
			var average = _sumSpreadPoints / _spreadSamples;
			var maxTime = _maxSpreadTime?.ToString("O") ?? "N/A";
			var minTime = _minSpreadTime?.ToString("O") ?? "N/A";

			LogInfo($"Spread statistics from {_startTime:O} to {_lastUpdateTime:O}:");
			LogInfo($"Maximum spread: {_maxObservedSpread:F2} points at {maxTime}.");
			LogInfo($"Minimum spread: {_minObservedSpread:F2} points at {minTime}.");
			LogInfo($"Average spread: {average:F2} points based on {_spreadSamples} samples.");
		}
		else
		{
			LogInfo("No spread samples collected.");
		}

		base.OnStopped();
	}
}

