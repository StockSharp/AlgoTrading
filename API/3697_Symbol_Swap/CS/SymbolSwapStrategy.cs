namespace StockSharp.Samples.Strategies;

using System;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;

/// <summary>
/// Displays real-time market information for a user selected symbol and allows swapping the tracked security on demand.
/// Mirrors the MetaTrader 5 "Symbol Swap" panel that exposes time, prices, tick volume and spread.
/// </summary>
public class SymbolSwapStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _watchedSecurityId;
	private readonly StrategyParam<PanelOutputMode> _outputMode;

	private ISubscription _candleSubscription;
	private ISubscription _level1Subscription;
	private IChartArea _chartArea;

	private Security _trackedSecurity;

	private DateTimeOffset? _lastCandleTime;
	private decimal? _lastOpen;
	private decimal? _lastHigh;
	private decimal? _lastLow;
	private decimal? _lastClose;
	private decimal? _lastVolume;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	private string _pendingSecurityId;
	private string _lastLoggedText;

	/// <summary>
	/// Defines how the status panel is rendered.
	/// </summary>
	public enum PanelOutputMode
	{
		/// <summary>
		/// Write updates to the strategy log like MetaTrader's Comment window.
		/// </summary>
		Log,

		/// <summary>
		/// Draw the information block on the chart near the current price.
		/// </summary>
		Chart,
	}

	/// <summary>
	/// Initializes parameters with defaults equivalent to the MetaTrader panel behaviour.
	/// </summary>
	public SymbolSwapStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Time frame used to aggregate OHLC and tick volume information.", "General");

		_watchedSecurityId = Param(nameof(WatchedSecurityId), string.Empty)
		.SetDisplay("Watched security id", "Optional identifier resolved through the SecurityProvider to override Strategy.Security.", "General");

		_outputMode = Param(nameof(OutputMode), PanelOutputMode.Chart)
		.SetDisplay("Output mode", "Controls whether the panel is drawn on the chart or written to the log.", "Visualization");
	}

	/// <summary>
	/// Candle type used to gather OHLC values for the panel.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Explicit security identifier to monitor instead of the default Strategy.Security.
	/// </summary>
	public string WatchedSecurityId
	{
		get => _watchedSecurityId.Value;
		set => _watchedSecurityId.Value = value;
	}

	/// <summary>
	/// Controls the rendering destination of the information block.
	/// </summary>
	public PanelOutputMode OutputMode
	{
		get => _outputMode.Value;
		set => _outputMode.Value = value;
	}

	/// <summary>
	/// Requests to swap the tracked symbol during live execution.
	/// </summary>
	/// <param name="securityId">Identifier resolvable by the current SecurityProvider.</param>
	public void SwapSecurity(string securityId)
	{
		if (securityId.IsEmptyOrWhiteSpace())
			throw new ArgumentException("Security identifier must be provided.", nameof(securityId));

		_pendingSecurityId = securityId.Trim();
		TryApplyPendingSecurity();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candleSubscription = null;
		_level1Subscription = null;
		_chartArea = null;

		_trackedSecurity = null;

		_lastCandleTime = null;
		_lastOpen = null;
		_lastHigh = null;
		_lastLow = null;
		_lastClose = null;
		_lastVolume = null;
		_bestBid = null;
		_bestAsk = null;

		_pendingSecurityId = null;
		_lastLoggedText = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = ResolveTrackedSecurity();
		ApplyTrackedSecurity(security);

		TryApplyPendingSecurity();

		UpdatePanel();
	}

	private Security ResolveTrackedSecurity()
	{
		if (!WatchedSecurityId.IsEmptyOrWhiteSpace())
		{
			var resolved = SecurityProvider?.LookupById(WatchedSecurityId.Trim());
			if (resolved == null)
				throw new InvalidOperationException($"Security '{WatchedSecurityId}' was not found.");

			return resolved;
		}

		return Security ?? throw new InvalidOperationException("Security is not specified.");
	}

	private void TryApplyPendingSecurity()
	{
		if (string.IsNullOrEmpty(_pendingSecurityId))
			return;

		var candidate = SecurityProvider?.LookupById(_pendingSecurityId);
		if (candidate == null)
			return;

		_pendingSecurityId = null;

		ApplyTrackedSecurity(candidate);
		UpdatePanel();
	}

	private void ApplyTrackedSecurity(Security security)
	{
		_trackedSecurity = security;

		_lastCandleTime = null;
		_lastOpen = null;
		_lastHigh = null;
		_lastLow = null;
		_lastClose = null;
		_lastVolume = null;
		_bestBid = null;
		_bestAsk = null;

		_candleSubscription = SubscribeCandles(CandleType, true, security);
		_candleSubscription.Bind(candle => ProcessCandle(candle, security)).Start();

		_level1Subscription = SubscribeLevel1(security);
		_level1Subscription.Bind(level1 => ProcessLevel1(level1, security)).Start();

		if (OutputMode == PanelOutputMode.Chart)
		{
			_chartArea ??= CreateChartArea();

			if (_chartArea != null)
			{
				DrawCandles(_chartArea, _candleSubscription);
				DrawOwnTrades(_chartArea);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (security != _trackedSecurity)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		_lastCandleTime = candle.OpenTime;
		_lastOpen = candle.OpenPrice;
		_lastHigh = candle.HighPrice;
		_lastLow = candle.LowPrice;
		_lastClose = candle.ClosePrice;
		_lastVolume = candle.TotalVolume;

		UpdatePanel();
	}

	private void ProcessLevel1(Level1ChangeMessage level1, Security security)
	{
		if (security != _trackedSecurity)
			return;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		UpdatePanel();
	}

	private void UpdatePanel()
	{
		var text = BuildPanelText();
		if (string.IsNullOrEmpty(text))
			return;

		switch (OutputMode)
		{
			case PanelOutputMode.Log:
			{
				if (text == _lastLoggedText)
					return;

				AddInfo(text);
				_lastLoggedText = text;
				break;
			}
			case PanelOutputMode.Chart:
			{
				var area = _chartArea;
				if (area == null)
					return;

				var price = GetReferencePrice();
				DrawText(area, CurrentTime, price, text);
				break;
			}
		}
	}

	private string BuildPanelText()
	{
		var security = _trackedSecurity ?? Security;
		if (security == null)
			return string.Empty;

		var builder = new StringBuilder();

		builder.Append("Time: ");
		builder.AppendLine(CurrentTime.ToString("yyyy-MM-dd HH:mm:ss"));

		builder.Append("Period: ");
		builder.AppendLine(GetTimeFrameName());

		builder.Append("Symbol: ");
		builder.AppendLine(!string.IsNullOrEmpty(security.Code) ? security.Code : security.Id);

		builder.Append("Close Price: ");
		builder.AppendLine(FormatDecimal(_lastClose));

		builder.Append("Open Price: ");
		builder.AppendLine(FormatDecimal(_lastOpen));

		builder.Append("High: ");
		builder.AppendLine(FormatDecimal(_lastHigh));

		builder.Append("Low: ");
		builder.AppendLine(FormatDecimal(_lastLow));

		builder.Append("Tick Volume: ");
		builder.AppendLine(FormatDecimal(_lastVolume, "0"));

		builder.Append("Spread: ");
		builder.AppendLine(FormatDecimal(GetSpread(), "0.#####"));

		return builder.ToString();
	}

	private string GetTimeFrameName()
	{
		var tf = CandleType.Arg;
		if (tf is TimeSpan span && span > TimeSpan.Zero)
			return span.ToString();

		return CandleType.ToString();
	}

	private decimal? GetSpread()
	{
		if (_bestBid is null || _bestAsk is null)
			return null;

		var spread = _bestAsk.Value - _bestBid.Value;
		return spread > 0m ? spread : null;
	}

	private string FormatDecimal(decimal? value, string format = "0.#####")
	{
		return value is null ? "n/a" : value.Value.ToString(format);
	}

	private decimal GetReferencePrice()
	{
		if (_lastClose is decimal close && close > 0m)
			return close;

		if (_bestBid is decimal bid && bid > 0m && _bestAsk is decimal ask && ask > 0m)
			return (bid + ask) / 2m;

		return _trackedSecurity?.LastPrice ?? 0m;
	}
}
