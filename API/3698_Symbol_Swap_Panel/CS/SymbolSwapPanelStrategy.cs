using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Market data monitor converted from the "Symbol Swap Panel" MQL panel.
/// It allows switching the watched security on demand and logs real-time price metrics
/// similar to the original chart widget that displayed symbol information.
/// </summary>
public class SymbolSwapPanelStrategy : Strategy
{
	private readonly StrategyParam<string> _targetSecurityId;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _swapRequested;

	private MarketDataSubscription? _candleSubscription;
	private MarketDataSubscription? _level1Subscription;

	private Security _activeSecurity;
	private string _appliedSecurityId;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _lastSpread;

	/// <summary>
	/// Security identifier that should replace the current chart when a swap is requested.
	/// </summary>
	public string TargetSecurityId
	{
		get => _targetSecurityId.Value;
		set => _targetSecurityId.Value = value;
	}

	/// <summary>
	/// Candle type used to sample price snapshots that emulate the MQL panel updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Manual flag that triggers switching to <see cref="TargetSecurityId"/>.
	/// It automatically resets to <c>false</c> after the request is processed.
	/// </summary>
	public bool SwapRequested
	{
		get => _swapRequested.Value;
		set => _swapRequested.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SymbolSwapPanelStrategy"/> class.
	/// </summary>
	public SymbolSwapPanelStrategy()
	{
		_targetSecurityId = Param(nameof(TargetSecurityId), string.Empty)
			.SetDisplay("Target Security", "Security ID that will be monitored after the swap", "Panel")
			.SetCanOptimize(false);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle aggregation used to refresh panel metrics", "Panel");

		_swapRequested = Param(nameof(SwapRequested), false)
			.SetDisplay("Swap Request", "Set to true to apply the target security", "Panel")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = _activeSecurity ?? Security;

		if (security != null)
		{
			yield return (security, CandleType);
			yield return (security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candleSubscription = null;
		_level1Subscription = null;
		_activeSecurity = null;
		_appliedSecurityId = null;
		_lastBid = null;
		_lastAsk = null;
		_lastSpread = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_activeSecurity = Security ?? ResolveTargetSecurity(TargetSecurityId);

		if (_activeSecurity == null)
			throw new InvalidOperationException("No security assigned to SymbolSwapPanelStrategy. Provide Strategy.Security or TargetSecurityId.");

		SubscribeToSecurity(_activeSecurity);

		TryHandleSwapRequest("initialization");
	}

	private void SubscribeToSecurity(Security security)
	{
		_candleSubscription?.Stop();
		_level1Subscription?.Stop();

		if (!ReferenceEquals(Security, security))
			Security = security;

		var candleSubscription = SubscribeCandles(CandleType, security: security);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();
		_candleSubscription = candleSubscription;

		var level1Subscription = SubscribeLevel1(security);
		level1Subscription
			.Bind(ProcessLevel1)
			.Start();
		_level1Subscription = level1Subscription;

		_appliedSecurityId = security.Id;
		_activeSecurity = security;

		LogInfo($"Monitoring security '{security.Id}' using candle type {CandleType}.");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		TryHandleSwapRequest("candle update");

		var totalVolume = candle.TotalVolume ?? 0m;
		var spread = _lastSpread ?? CalculateSpread();

		_lastSpread = spread;

		LogInfo(
			$"Time: {candle.CloseTime:O}, Symbol: {_activeSecurity?.Id ?? "N/A"}, " +
			$"Open: {candle.OpenPrice}, High: {candle.HighPrice}, Low: {candle.LowPrice}, Close: {candle.ClosePrice}, " +
			$"Volume: {totalVolume}, Spread: {(spread.HasValue ? spread.Value.ToString() : "n/a")}."
		);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BidPrice);
		if (bid is decimal bidPrice && bidPrice > 0m)
			_lastBid = bidPrice;

		var ask = message.TryGetDecimal(Level1Fields.AskPrice);
		if (ask is decimal askPrice && askPrice > 0m)
			_lastAsk = askPrice;

		TryHandleSwapRequest("level1 update");
	}

	private void TryHandleSwapRequest(string reason)
	{
		var requested = SwapRequested;
		var target = TargetSecurityId?.Trim();

		if (!requested)
			return;

		if (string.IsNullOrEmpty(target))
		{
			LogWarning("Swap requested but the target security ID is empty.");

			SwapRequested = false;
			return;
		}

		var security = ResolveTargetSecurity(target);

		if (security == null)
		{
			LogWarning($"Security '{target}' cannot be resolved.");

			SwapRequested = false;
			return;
		}

		if (_appliedSecurityId != null && string.Equals(_appliedSecurityId, security.Id, StringComparison.InvariantCultureIgnoreCase))
		{
			LogInfo($"Security '{security.Id}' is already active; swap request was ignored.");
			SwapRequested = false;
			return;
		}

		SubscribeToSecurity(security);
		LogInfo($"Switched to '{security.Id}' due to {reason}.");

		SwapRequested = false;
	}

	private Security ResolveTargetSecurity(string securityId)
	{
		var security = this.GetSecurity(securityId);

		if (security != null)
			return security;

		security = SecurityProvider?.LookupById(securityId);

		return security;
	}

	private decimal? CalculateSpread()
	{
		if (_lastBid is not decimal bid || _lastAsk is not decimal ask)
			return null;

		if (bid <= 0m || ask <= 0m)
			return null;

		var spread = ask - bid;
		return spread >= 0m ? spread : null;
	}
}
