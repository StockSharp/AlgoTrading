using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the MetaTrader VR Watch List and Linker Lite script.
/// Synchronizes multiple linked securities so they share the same candle timeframe as the primary chart.
/// </summary>
public class VrWatchListLinkerLiteStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<IEnumerable<Security>> _linkedSecurities;
	private readonly StrategyParam<bool> _includePrimarySecurity;
	private readonly StrategyParam<bool> _restartOnMismatch;

	private readonly Dictionary<Security, MarketDataSubscription> _subscriptions = new();
	private readonly Dictionary<Security, DateTimeOffset> _lastCandleTimes = new();

	private List<Security> _activeSecurities = [];
	private DateTimeOffset? _primaryCandleTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="VrWatchListLinkerLiteStrategy"/> class.
	/// </summary>
	public VrWatchListLinkerLiteStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe applied to all linked securities", "General");

		_linkedSecurities = Param<IEnumerable<Security>>(nameof(LinkedSecurities), [])
			.SetDisplay("Linked Securities", "External securities that should follow the primary chart", "General");

		_includePrimarySecurity = Param(nameof(IncludePrimarySecurity), true)
			.SetDisplay("Include Primary", "Link the strategy security as the master chart", "General");

		_restartOnMismatch = Param(nameof(RestartOnMismatch), true)
			.SetDisplay("Restart On Mismatch", "Restart subscriptions when a linked chart drifts from the master timeframe", "General");
	}

	/// <summary>
	/// Timeframe that every linked security should follow.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// External securities that must stay synchronized with the primary chart.
	/// </summary>
	public IEnumerable<Security> LinkedSecurities
	{
		get => _linkedSecurities.Value;
		set => _linkedSecurities.Value = value;
	}

	/// <summary>
	/// Determines whether the strategy security acts as the master chart.
	/// </summary>
	public bool IncludePrimarySecurity
	{
		get => _includePrimarySecurity.Value;
		set => _includePrimarySecurity.Value = value;
	}

	/// <summary>
	/// When enabled the strategy restarts out-of-sync subscriptions automatically.
	/// </summary>
	public bool RestartOnMismatch
	{
		get => _restartOnMismatch.Value;
		set => _restartOnMismatch.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CandleType == null)
			yield break;

		if (IncludePrimarySecurity && Security != null)
			yield return (Security, CandleType);

		if (LinkedSecurities == null)
			yield break;

		var seen = new HashSet<Security>();

		foreach (var security in LinkedSecurities)
		{
			if (security == null)
				continue;

			if (!seen.Add(security))
				continue;

			yield return (security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		StopAllSubscriptions();

		_activeSecurities = [];
		_primaryCandleTime = null;
		_lastCandleTimes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		StopAllSubscriptions();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleType = CandleType ?? throw new InvalidOperationException("Candle type is not configured.");

		if (candleType.MessageType != typeof(TimeFrameCandleMessage))
			throw new InvalidOperationException("Only timeframe-based candles are supported.");

		var securities = new List<Security>();

		if (IncludePrimarySecurity)
		{
			if (Security == null)
				throw new InvalidOperationException("Primary security is not set.");

			securities.Add(Security);
		}

		if (LinkedSecurities != null)
		{
			foreach (var security in LinkedSecurities)
			{
				if (security == null || securities.Contains(security))
					continue;

				securities.Add(security);
			}
		}

		if (securities.Count == 0)
			throw new InvalidOperationException("No securities configured for linking.");

		_activeSecurities = securities;

		foreach (var security in securities)
		{
			SubscribeSecurity(security, candleType);
		}

		LogInfo($"Linker started with {securities.Count} security(ies). Timeframe: {DescribeTimeframe(candleType)}.");
	}

	private void SubscribeSecurity(Security security, DataType candleType)
	{
		MarketDataSubscription subscription;

		if (security == Security)
			subscription = SubscribeCandles(candleType);
		else
			subscription = SubscribeCandles(candleType, true, security);

		subscription.Bind(candle => OnLinkedCandle(security, candle)).Start();

		_subscriptions[security] = subscription;
		_lastCandleTimes.Remove(security);

		LogInfo($"Linked security {security.Id} to timeframe {DescribeTimeframe(candleType)}.");
	}

	private void OnLinkedCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastCandleTimes[security] = candle.OpenTime;

		if (security == Security)
		{
			_primaryCandleTime = candle.OpenTime;
			LogInfo($"Primary chart updated at {candle.OpenTime:O}. Close={candle.ClosePrice}.");
		}
		else
		{
			LogInfo($"Linked chart {security.Id} updated at {candle.OpenTime:O}. Close={candle.ClosePrice}.");
		}

		EnsureAlignment();
	}

	private void EnsureAlignment()
	{
		if (_primaryCandleTime is null)
			return;

		var expected = _primaryCandleTime.Value;

		foreach (var security in _activeSecurities)
		{
			if (security == Security)
				continue;

			if (!_lastCandleTimes.TryGetValue(security, out var linkedTime))
				continue;

			if (linkedTime == expected)
				continue;

			if (RestartOnMismatch)
			{
				LogInfo($"Resubscribing {security.Id} to align timeframe. Primary={expected:O}, Linked={linkedTime:O}.");
				RestartSubscription(security);
			}
			else
			{
				LogInfo($"Linked security {security.Id} is out of sync. Primary={expected:O}, Linked={linkedTime:O}.");
			}
		}
	}

	private void RestartSubscription(Security security)
	{
		if (!_subscriptions.TryGetValue(security, out var subscription))
			return;

		subscription.Dispose();
		_subscriptions.Remove(security);

		var candleType = CandleType;
		if (candleType == null)
			return;

		SubscribeSecurity(security, candleType);
	}

	private void StopAllSubscriptions()
	{
		foreach (var subscription in _subscriptions.Values)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private static string DescribeTimeframe(DataType dataType)
	{
		if (dataType.MessageType == typeof(TimeFrameCandleMessage) && dataType.Arg is TimeSpan span)
			return span.ToString();

		return dataType.ToString();
	}
}
