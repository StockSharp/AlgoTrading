namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Recreates the MetaTrader expert that draws two horizontal lines around the current price.
/// The strategy observes best bid/ask updates and reports when the market crosses those levels.
/// </summary>
public class HorizontalLineLevelsStrategy : Strategy
{
	private readonly StrategyParam<int> _timerPeriodMinutes;
	private readonly StrategyParam<int> _offsetPoints;

	private decimal _bestAsk;
	private decimal _bestBid;
	private bool _hasBestAsk;
	private bool _hasBestBid;
	private decimal _upperLevel;
	private decimal _lowerLevel;
	private decimal _pointSize;

	/// <summary>
	/// Minutes between consecutive level checks.
	/// </summary>
	public int TimerPeriodMinutes
	{
		get => _timerPeriodMinutes.Value;
		set => _timerPeriodMinutes.Value = value;
	}

	/// <summary>
	/// Distance from the current market measured in MetaTrader points.
	/// </summary>
	public int OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HorizontalLineLevelsStrategy"/>.
	/// </summary>
	public HorizontalLineLevelsStrategy()
	{
		_timerPeriodMinutes = Param(nameof(TimerPeriodMinutes), 1)
			.SetDisplay("Timer Period (minutes)", "Interval used to refresh the horizontal levels.", "General")
			.SetGreaterThanZero();

		_offsetPoints = Param(nameof(OffsetPoints), 50)
			.SetDisplay("Offset (points)", "Distance in MetaTrader points applied above and below the market price.", "Levels")
			.SetGreaterThanZero();
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

		_bestAsk = 0m;
		_bestBid = 0m;
		_hasBestAsk = false;
		_hasBestBid = false;
		_upperLevel = 0m;
		_lowerLevel = 0m;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TimerPeriodMinutes <= 0)
			throw new InvalidOperationException("Timer period must be positive.");

		// Subscribe to best bid/ask changes exactly like the original OnTick handler.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		Timer.Start(TimeSpan.FromMinutes(TimerPeriodMinutes), OnTimer);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		// Initialize the horizontal lines once a full quote snapshot is available.
		if (_hasBestAsk && _hasBestBid && _upperLevel == 0m && _lowerLevel == 0m)
			RecalculateLevels(true);
	}

	private void OnTimer()
	{
		if (!_hasBestAsk || !_hasBestBid)
			return;

		// Recreate the levels if they were not available earlier.
		if (_upperLevel == 0m || _lowerLevel == 0m)
			RecalculateLevels(true);

		var ask = _bestAsk;
		var bid = _bestBid;

		if (ask >= _upperLevel && _upperLevel > 0m)
			LogInfo($"Ask {ask:0.#####} traded above the upper level {_upperLevel:0.#####}.");

		if (_lowerLevel > 0m && bid <= _lowerLevel)
			LogInfo($"Bid {bid:0.#####} traded below the lower level {_lowerLevel:0.#####}.");
	}

	private void RecalculateLevels(bool logCreation)
	{
		var point = EnsurePointSize();
		if (point <= 0m)
			return;

		var offset = OffsetPoints * point;
		if (offset <= 0m)
			return;

		_upperLevel = _bestAsk + offset;
		_lowerLevel = _bestBid - offset;

		if (logCreation)
			LogInfo($"Horizontal levels updated. Upper: {_upperLevel:0.#####}, Lower: {_lowerLevel:0.#####}.");
	}

	private decimal EnsurePointSize()
	{
		if (_pointSize > 0m)
			return _pointSize;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var scaled = step;
		var digits = 0;

		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;

		_pointSize = step * adjust;
		return _pointSize;
	}
}

