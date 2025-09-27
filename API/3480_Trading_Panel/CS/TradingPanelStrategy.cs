// TradingPanelStrategy.cs
// -----------------------------------------------------------------------------
// Strategy that mirrors a lightweight chart control panel from MetaTrader.
// Users can switch between favorite timeframes and securities at runtime.
// -----------------------------------------------------------------------------
// Date: 7 Aug 2025
// -----------------------------------------------------------------------------
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
using StockSharp.Algo.Candles;
using StockSharp.Localization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the Trading Panel utility from MetaTrader.
/// It allows switching the active security and timeframe without restarting the strategy.
/// </summary>
public class TradingPanelStrategy : Strategy
{
	private readonly StrategyParam<string> _timeFrameName;
	private readonly StrategyParam<string> _securityId;
	private readonly StrategyParam<bool> _autoLookupSecurity;
	private readonly StrategyParam<DataType> _defaultCandleType;

	private Security _resolvedSecurity;
	private DataType _activeCandleType;
	private CandleIndicatorSubscription _subscription;

	/// <summary>
	/// Preferred timeframe name like "M1", "H4", or "D1".
	/// </summary>
	public string TimeFrameName
	{
		get => _timeFrameName.Value;
		set
		{
			if (_timeFrameName.Value.EqualsIgnoreCase(value))
				return;

			_timeFrameName.Value = value;

			if (ProcessState == ProcessStates.Started)
				ApplyTimeFrame();
		}
	}

	/// <summary>
	/// Optional identifier of the security to control.
	/// Leave empty to use the security assigned by the host application.
	/// </summary>
	public string SecurityId
	{
		get => _securityId.Value;
		set
		{
			if (_securityId.Value.EqualsIgnoreCase(value))
				return;

			_securityId.Value = value;

			if (ProcessState == ProcessStates.Started)
				ApplySecurity();
		}
	}

	/// <summary>
	/// When <c>true</c>, the strategy looks up <see cref="SecurityId"/> in the provider automatically.
	/// </summary>
	public bool AutoLookupSecurity
	{
		get => _autoLookupSecurity.Value;
		set
		{
			if (_autoLookupSecurity.Value == value)
				return;

			_autoLookupSecurity.Value = value;

			if (ProcessState == ProcessStates.Started)
				ApplySecurity();
		}
	}

	/// <summary>
	/// Candle type that is used when <see cref="TimeFrameName"/> cannot be resolved.
	/// </summary>
	public DataType DefaultCandleType
	{
		get => _defaultCandleType.Value;
		set => _defaultCandleType.Value = value;
	}

	/// <summary>
	/// Stores the latest finished candle for visualization or diagnostics.
	/// </summary>
	public ICandleMessage LastFinishedCandle { get; private set; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TradingPanelStrategy()
	{
		_timeFrameName = Param(nameof(TimeFrameName), "M15")
		.SetDisplay("Timeframe", "Preferred chart timeframe", "Panel")
		.SetCanOptimize(false);

		_securityId = Param(nameof(SecurityId), string.Empty)
		.SetDisplay("Security Id", "Identifier of the instrument to control", "Panel")
		.SetCanOptimize(false);

		_autoLookupSecurity = Param(nameof(AutoLookupSecurity), true)
		.SetDisplay("Auto Lookup", "Automatically resolve security by identifier", "Panel");

		_defaultCandleType = Param(nameof(DefaultCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Fallback Candle", "Candle type used when timeframe is unknown", "Panel");

		_activeCandleType = DefaultCandleType;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = ResolveSecurity();

		return security != null
		? [(security, _activeCandleType)]
		: Enumerable.Empty<(Security, DataType)>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ApplySecurity();
		ApplyTimeFrame();
		StartSubscription();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		StopSubscription();

		base.OnStopped();
	}

	private void ApplySecurity()
	{
		_resolvedSecurity = ResolveSecurity();

		if (_subscription != null)
		{
			StopSubscription();
			StartSubscription();
		}
	}

	private void ApplyTimeFrame()
	{
		_activeCandleType = ResolveTimeFrame(TimeFrameName) ?? DefaultCandleType;

		if (_subscription != null)
		{
			StopSubscription();
			StartSubscription();
		}
	}

	private void StartSubscription()
	{
		var security = ResolveSecurity();

		if (security == null)
		{
			LogWarning(LocalizedStrings.Str1392Params.Put(nameof(Security)));
			return;
		}

		StopSubscription();

		_subscription = SubscribeCandles(_activeCandleType, false, security);
		_subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void StopSubscription()
	{
		_subscription?.Stop();
		_subscription = null;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles because the panel should react to completed data only.
		if (candle.State != CandleStates.Finished)
			return;

		LastFinishedCandle = candle;

		// Log the change to imitate visual feedback of the original panel.
		LogInfo($"{candle.SecurityId} {TimeFrameName} close: {candle.ClosePrice}");
	}

	private Security ResolveSecurity()
	{
		if (_resolvedSecurity != null)
			return _resolvedSecurity;

		if (!SecurityId.IsEmptyOrWhiteSpace() && AutoLookupSecurity)
			_resolvedSecurity = SecurityProvider?.LookupById(SecurityId) ?? Security;
		else
			_resolvedSecurity = Security;

		return _resolvedSecurity;
	}

	private static DataType ResolveTimeFrame(string name)
	{
		if (name.IsEmptyOrWhiteSpace())
			return null;

		switch (name.Trim().ToUpperInvariant())
		{
			case "M1":
				return TimeSpan.FromMinutes(1).TimeFrame();
			case "M5":
				return TimeSpan.FromMinutes(5).TimeFrame();
			case "M15":
				return TimeSpan.FromMinutes(15).TimeFrame();
			case "M30":
				return TimeSpan.FromMinutes(30).TimeFrame();
			case "H1":
				return TimeSpan.FromHours(1).TimeFrame();
			case "H4":
				return TimeSpan.FromHours(4).TimeFrame();
			case "D1":
				return TimeSpan.FromDays(1).TimeFrame();
			case "W1":
				return TimeSpan.FromDays(7).TimeFrame();
			default:
				return null;
		}
	}
}

