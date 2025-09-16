using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors external trades on the same account using a configurable multiplier.
/// It listens to account trades, replicates positions tagged with a specific prefix, and keeps
/// the copied exposure synchronized with the source exposure.
/// </summary>
public class TradeCopierStrategy : Strategy
{
	private sealed class PendingManualTrade
	{
		public DateTimeOffset Time { get; set; }

		public decimal Price { get; set; }
	}

	private readonly StrategyParam<decimal> _slippage;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<TimeSpan> _maxOrderAge;
	private readonly StrategyParam<string> _commentPrefix;
	private readonly StrategyParam<TimeSpan> _checkInterval;

	private readonly Dictionary<Security, decimal> _sourcePositions = new();
	private readonly Dictionary<Security, decimal> _copiedPositions = new();
	private readonly Dictionary<Security, decimal> _inFlightAdjustments = new();
	private readonly Dictionary<Security, PendingManualTrade> _pendingManualTrades = new();
	private readonly HashSet<string> _processedTrades = new();
	private readonly Queue<(string key, DateTimeOffset time)> _processedHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeCopierStrategy"/> class.
	/// </summary>
	public TradeCopierStrategy()
	{
		_slippage = Param(nameof(Slippage), 3m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Slippage", "Allowed price deviation in ticks", "General");

		_multiplier = Param(nameof(Multiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Multiplier", "Volume multiplier for copied trades", "General");

		_maxOrderAge = Param(nameof(MaxOrderAge), TimeSpan.FromSeconds(30))
		.SetDisplay("Max Order Age", "Maximum age of the source trade", "General");

		_commentPrefix = Param(nameof(CommentPrefix), "VCPY_")
		.SetDisplay("Comment Prefix", "Prefix used to mark copied trades", "General");

		_checkInterval = Param(nameof(CheckInterval), TimeSpan.FromSeconds(1))
		.SetDisplay("Check Interval", "Legacy timer interval from MQL script", "General");
	}

	/// <summary>
	/// Maximum price deviation in ticks allowed for a copy order.
	/// </summary>
	public decimal Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Trade volume multiplier for the copied position.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Maximum acceptable age of the source trade.
	/// </summary>
	public TimeSpan MaxOrderAge
	{
		get => _maxOrderAge.Value;
		set => _maxOrderAge.Value = value;
	}

	/// <summary>
	/// Prefix added to comments of generated copy orders.
	/// </summary>
	public string CommentPrefix
	{
		get => _commentPrefix.Value;
		set => _commentPrefix.Value = value;
	}

	/// <summary>
	/// Optional throttle value retained from the original script.
	/// The strategy reacts instantly, but the value is used for housekeeping timers.
	/// </summary>
	public TimeSpan CheckInterval
	{
		get => _checkInterval.Value;
		set => _checkInterval.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sourcePositions.Clear();
		_copiedPositions.Clear();
		_inFlightAdjustments.Clear();
		_pendingManualTrades.Clear();
		_processedTrades.Clear();
		_processedHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Connector.NewMyTrade += OnNewMyTrade;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Connector.NewMyTrade -= OnNewMyTrade;

		base.OnStopped();
	}

	private void OnNewMyTrade(MyTrade myTrade)
	{
		if (myTrade.Order is null || myTrade.Trade is null)
		return;

		if (myTrade.Order.Portfolio != Portfolio)
		return;

		var security = myTrade.Order.Security;

		if (security is null)
		return;

		var tradeKey = BuildTradeKey(myTrade);

		lock (_processedTrades)
		{
			CleanProcessedTrades(CurrentTime);

			if (!_processedTrades.Add(tradeKey))
			return;

			_processedHistory.Enqueue((tradeKey, CurrentTime));
		}

		var signedVolume = myTrade.Trade.Volume;

		if (myTrade.Order.Side == Sides.Sell)
		signedVolume = -signedVolume;

		var comment = myTrade.Order.Comment ?? string.Empty;

		if (comment.StartsWith(CommentPrefix, StringComparison.Ordinal))
		{
			UpdateCopiedPosition(security, signedVolume);
			ProcessPendingTrades();
			return;
		}

		UpdateSourcePosition(security, signedVolume);

		var tradeAge = CurrentTime - myTrade.Trade.ServerTime;

		if (tradeAge > MaxOrderAge)
		{
			LogInfo($"Skip copying trade for {security.Id} because it is older than {MaxOrderAge.TotalSeconds:F0} seconds.");
			return;
		}

		lock (_pendingManualTrades)
		{
			_pendingManualTrades[security] = new PendingManualTrade
			{
				Time = myTrade.Trade.ServerTime,
				Price = myTrade.Trade.Price
			};
		}

		ProcessPendingTrades();
	}

	private void ProcessPendingTrades()
	{
		KeyValuePair<Security, PendingManualTrade>[] pending;

		lock (_pendingManualTrades)
		{
			if (_pendingManualTrades.Count == 0)
			return;

			pending = _pendingManualTrades.ToArray();
			_pendingManualTrades.Clear();
		}

		foreach (var pair in pending)
		AlignCopiedPosition(pair.Key, pair.Value);
	}

	private void AlignCopiedPosition(Security security, PendingManualTrade manualTrade)
	{
		var sourcePosition = _sourcePositions.TryGetValue(security, out var src) ? src : 0m;
		var copiedPosition = _copiedPositions.TryGetValue(security, out var copy) ? copy : 0m;
		var inFlight = _inFlightAdjustments.TryGetValue(security, out var pending) ? pending : 0m;

		var target = sourcePosition * Multiplier;
		var delta = target - (copiedPosition + inFlight);

		if (delta == 0m)
		return;

		var volume = NormalizeVolume(security, Math.Abs(delta));

		if (volume <= 0m)
		return;

		if (!IsSlippageAcceptable(security, manualTrade.Price))
		{
			LogInfo($"Skip copying trade for {security.Id} because slippage is too high.");
			return;
		}

		var side = delta > 0 ? Sides.Buy : Sides.Sell;
		var signedVolume = side == Sides.Buy ? volume : -volume;
		var comment = $"{CommentPrefix}{security.Id}-{manualTrade.Time.UtcTicks}";

		RegisterOrder(new Order
		{
			Portfolio = Portfolio,
			Security = security,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = comment
		});

		if (_inFlightAdjustments.TryGetValue(security, out var value))
		_inFlightAdjustments[security] = value + signedVolume;
		else
		_inFlightAdjustments[security] = signedVolume;

		LogInfo($"Submitted copy order for {security.Id}. Target position: {target}, current: {copiedPosition}, delta: {delta}, volume: {volume}.");
	}

	private void UpdateSourcePosition(Security security, decimal signedVolume)
	{
		if (_sourcePositions.TryGetValue(security, out var value))
		{
			value += signedVolume;

			if (value == 0m)
			_sourcePositions.Remove(security);
			else
			_sourcePositions[security] = value;
		}
		else if (signedVolume != 0m)
		{
			_sourcePositions[security] = signedVolume;
		}
	}

	private void UpdateCopiedPosition(Security security, decimal signedVolume)
	{
		if (_copiedPositions.TryGetValue(security, out var value))
		{
			value += signedVolume;

			if (value == 0m)
			_copiedPositions.Remove(security);
			else
			_copiedPositions[security] = value;
		}
		else if (signedVolume != 0m)
		{
			_copiedPositions[security] = signedVolume;
		}

		if (_inFlightAdjustments.TryGetValue(security, out var pending))
		{
			pending -= signedVolume;

			if (pending == 0m)
			_inFlightAdjustments.Remove(security);
			else
			_inFlightAdjustments[security] = pending;
		}
	}

	private bool IsSlippageAcceptable(Security security, decimal referencePrice)
	{
		if (Slippage <= 0m)
		return true;

		var lastTradePrice = security.LastTrade?.Price;

		if (lastTradePrice is null)
		return true;

		var priceStep = security.PriceStep ?? 0m;
		var allowedDiff = priceStep > 0m ? Slippage * priceStep : Slippage;
		var diff = Math.Abs(lastTradePrice.Value - referencePrice);

		return diff <= allowedDiff;
	}

	private static decimal NormalizeVolume(Security security, decimal volume)
	{
		var step = security.VolumeStep ?? 0m;

		if (step <= 0m)
		return volume;

		return Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
	}

	private static string BuildTradeKey(MyTrade myTrade)
	{
		var trade = myTrade.Trade;
		var order = myTrade.Order;
		var tradeId = trade?.Id?.ToString() ?? trade?.StringId ?? string.Empty;
		return $"{order?.TransactionId}:{tradeId}:{trade?.ServerTime.UtcTicks}";
	}

	private void CleanProcessedTrades(DateTimeOffset now)
	{
		var retention = GetRetentionWindow();

		while (_processedHistory.Count > 0)
		{
			var (key, time) = _processedHistory.Peek();

			if (now - time <= retention)
			break;

			_processedHistory.Dequeue();
			_processedTrades.Remove(key);
		}
	}

	private TimeSpan GetRetentionWindow()
	{
		if (CheckInterval <= TimeSpan.Zero)
		return TimeSpan.FromMinutes(10);

		var ticks = CheckInterval.Ticks;
		var minTicks = TimeSpan.FromMinutes(1).Ticks;
		var maxTicks = TimeSpan.FromHours(1).Ticks;

		if (ticks <= 0)
		return TimeSpan.FromMinutes(10);

		if (ticks >= maxTicks / 60)
		return TimeSpan.FromHours(1);

		var scaled = ticks * 60;

		if (scaled < minTicks)
		return TimeSpan.FromMinutes(1);

		return scaled > maxTicks ? TimeSpan.FromHours(1) : TimeSpan.FromTicks(scaled);
	}
}
