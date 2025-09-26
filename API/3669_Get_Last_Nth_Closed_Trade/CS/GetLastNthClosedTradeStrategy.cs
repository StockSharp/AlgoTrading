using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reports the N-th most recent closed trade executed by the strategy.
/// </summary>
public class GetLastNthClosedTradeStrategy : Strategy
{
	private const int MaxStoredTrades = 100;

	private sealed class TradeDetail
	{
		public required string Symbol { get; init; }
		public required decimal Price { get; init; }
		public required decimal Volume { get; init; }
		public required DateTimeOffset Time { get; init; }
		public required Sides Side { get; init; }
		public long? OrderId { get; init; }
		public long? TradeId { get; init; }
		public string Comment { get; init; }
	}

	private sealed class PositionRecord
	{
		public required string Symbol { get; init; }
		public required Sides Direction { get; init; }
		public required DateTimeOffset EntryTime { get; init; }
		public required decimal AveragePrice { get; set; }
		public required decimal TotalVolume { get; set; }
		public string EntryComment { get; init; }
		public long? EntryOrderId { get; init; }
		public long? EntryTradeId { get; init; }
	}

	private sealed class ClosedTradeInfo
	{
		public required string Symbol { get; init; }
		public required Sides Direction { get; init; }
		public required DateTimeOffset EntryTime { get; init; }
		public required DateTimeOffset ExitTime { get; init; }
		public required decimal EntryPrice { get; init; }
		public required decimal ExitPrice { get; init; }
		public required decimal Volume { get; init; }
		public required decimal Profit { get; init; }
		public string EntryComment { get; init; }
		public string ExitComment { get; init; }
		public long? EntryOrderId { get; init; }
		public long? ExitOrderId { get; init; }
		public long? EntryTradeId { get; init; }
		public long? ExitTradeId { get; init; }
	}

	private readonly StrategyParam<bool> _enableStrategyIdFilter;
	private readonly StrategyParam<string> _strategyIdFilter;
	private readonly StrategyParam<bool> _enableSecurityFilter;
	private readonly StrategyParam<int> _tradeIndex;

	private readonly List<ClosedTradeInfo> _closedTrades = new();
	private PositionRecord? _openRecord;
	private TradeDetail? _lastTrade;
	private decimal _previousPosition;

	/// <summary>
	/// Gets or sets whether trades should be filtered by strategy identifier.
	/// </summary>
	public bool EnableStrategyIdFilter
	{
		get => _enableStrategyIdFilter.Value;
		set => _enableStrategyIdFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets the strategy identifier that must match the executed trade.
	/// </summary>
	public string StrategyIdFilter
	{
		get => _strategyIdFilter.Value;
		set => _strategyIdFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets whether only trades for the current security should be processed.
	/// </summary>
	public bool EnableSecurityFilter
	{
		get => _enableSecurityFilter.Value;
		set => _enableSecurityFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets the zero-based index of the closed trade to report.
	/// </summary>
	public int TradeIndex
	{
		get => _tradeIndex.Value;
		set => _tradeIndex.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GetLastNthClosedTradeStrategy"/> class.
	/// </summary>
	public GetLastNthClosedTradeStrategy()
	{
		_enableStrategyIdFilter = Param(nameof(EnableStrategyIdFilter), false)
		.SetDisplay("Enable Strategy Id Filter", "Match trades with the configured strategy identifier", "General");

		_strategyIdFilter = Param(nameof(StrategyIdFilter), string.Empty)
		.SetDisplay("Strategy Id", "Strategy identifier to match when filtering is enabled", "General");

		_enableSecurityFilter = Param(nameof(EnableSecurityFilter), false)
		.SetDisplay("Enable Security Filter", "Process only trades for the current security", "General");

		_tradeIndex = Param(nameof(TradeIndex), 0)
		.SetDisplay("Trade Index", "Zero-based index of the closed trade snapshot", "General")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closedTrades.Clear();
		_openRecord = null;
		_lastTrade = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		var previousPosition = Position;

		base.OnNewMyTrade(trade);

		if (!IsTradeAccepted(trade))
		return;

		var info = trade.Trade;
		if (info == null)
		return;

		var price = info.Price ?? 0m;
		var volume = info.Volume ?? 0m;
		if (price <= 0m || volume <= 0m)
		return;

		var side = trade.Order.Direction ?? Sides.Buy;
		var symbol = trade.Order.Security?.Id ?? string.Empty;

		_lastTrade = new TradeDetail
		{
		Symbol = symbol,
		Price = price,
		Volume = volume,
		Time = info.ServerTime,
		Side = side,
		OrderId = trade.Order.Id,
		TradeId = info.TradeId,
		Comment = trade.Order.Comment,
		};

		ProcessPositionChange(previousPosition, Position, side, volume);
	}

	private bool IsTradeAccepted(MyTrade trade)
	{
		if (trade?.Order == null)
		return false;

		if (EnableSecurityFilter && Security != null && !Equals(trade.Order.Security, Security))
		return false;

		if (EnableStrategyIdFilter)
		{
		var filter = StrategyIdFilter;
		var orderStrategyId = trade.Order.StrategyId;
		var target = string.IsNullOrEmpty(filter) ? Id.ToString() : filter;

		if (!string.Equals(orderStrategyId, target, StringComparison.Ordinal))
		return false;
		}

		return true;
	}

	private void ProcessPositionChange(decimal previousPosition, decimal currentPosition, Sides side, decimal volume)
	{
		var trade = _lastTrade;
		if (trade == null)
		return;

		var signedVolume = side == Sides.Buy ? volume : -volume;

		if (previousPosition == 0m && currentPosition != 0m)
		{
		StartNewPosition(trade, signedVolume);
		_previousPosition = currentPosition;
		return;
		}

		if (currentPosition == 0m && previousPosition != 0m)
		{
		ClosePosition(Math.Abs(previousPosition));
		_previousPosition = 0m;
		return;
		}

		if (Math.Sign(previousPosition) == Math.Sign(currentPosition))
		{
		if (Math.Abs(currentPosition) > Math.Abs(previousPosition))
		{
		IncreasePosition(volume);
		}
		else if (Math.Abs(currentPosition) < Math.Abs(previousPosition))
		{
		ReducePosition(Math.Abs(previousPosition) - Math.Abs(currentPosition));
		}
		}
		else
		{
		var closedVolume = Math.Abs(previousPosition);
		if (closedVolume > 0m)
		ClosePosition(closedVolume);

		var remainingVolume = Math.Abs(currentPosition);
		if (remainingVolume > 0m)
		StartNewPosition(trade, currentPosition);
		}

		_previousPosition = currentPosition;
	}

	private void StartNewPosition(TradeDetail trade, decimal signedVolume)
	{
		var direction = signedVolume >= 0m ? Sides.Buy : Sides.Sell;
		var volume = Math.Abs(signedVolume);

		_openRecord = new PositionRecord
		{
		Symbol = trade.Symbol,
		Direction = direction,
		EntryTime = trade.Time,
		AveragePrice = trade.Price,
		TotalVolume = volume,
		EntryComment = trade.Comment,
		EntryOrderId = trade.OrderId,
		EntryTradeId = trade.TradeId,
		};
	}

	private void IncreasePosition(decimal volume)
	{
		var trade = _lastTrade;
		if (trade == null || _openRecord == null)
		return;

		var newVolume = volume;
		var existingVolume = _openRecord.TotalVolume;
		var totalVolume = existingVolume + newVolume;
		if (totalVolume <= 0m)
		return;

		var weightedPrice = (_openRecord.AveragePrice * existingVolume) + (trade.Price * newVolume);
		_openRecord.AveragePrice = weightedPrice / totalVolume;
		_openRecord.TotalVolume = totalVolume;
	}

	private void ReducePosition(decimal closedVolume)
	{
		if (closedVolume <= 0m)
		return;

		ClosePosition(closedVolume);
	}

	private void ClosePosition(decimal closedVolume)
	{
		var trade = _lastTrade;
		if (trade == null || _openRecord == null)
		return;

		var volume = Math.Min(closedVolume, _openRecord.TotalVolume);
		if (volume <= 0m)
		return;

		var entryPrice = _openRecord.AveragePrice;
		var exitPrice = trade.Price;
		var direction = _openRecord.Direction;
		var profitPerUnit = direction == Sides.Buy ? exitPrice - entryPrice : entryPrice - exitPrice;
		var profit = profitPerUnit * volume;

		var closed = new ClosedTradeInfo
		{
		Symbol = _openRecord.Symbol,
		Direction = direction,
		EntryTime = _openRecord.EntryTime,
		ExitTime = trade.Time,
		EntryPrice = entryPrice,
		ExitPrice = exitPrice,
		Volume = volume,
		Profit = profit,
		EntryComment = _openRecord.EntryComment,
		ExitComment = trade.Comment,
		EntryOrderId = _openRecord.EntryOrderId,
		ExitOrderId = trade.OrderId,
		EntryTradeId = _openRecord.EntryTradeId,
		ExitTradeId = trade.TradeId,
		};

		_closedTrades.Insert(0, closed);
		if (_closedTrades.Count > MaxStoredTrades)
		_closedTrades.RemoveAt(_closedTrades.Count - 1);

		_openRecord.TotalVolume -= volume;
		if (_openRecord.TotalVolume <= 0m)
		_openRecord = null;

		PublishReport();
	}

	private void PublishReport()
	{
		var index = TradeIndex;
		if (index < 0 || index >= _closedTrades.Count)
		{
		LogInfo("Closed trade index is out of range or there are not enough closed trades yet.");
		return;
		}

		var trade = _closedTrades[index];
		var builder = new StringBuilder();
		builder.AppendLine($"symbol {trade.Symbol}");
		builder.AppendLine($"lots {trade.Volume.ToString("0.###", CultureInfo.InvariantCulture)}");
		builder.AppendLine($"openPrice {trade.EntryPrice.ToString("0.#####", CultureInfo.InvariantCulture)}");
		builder.AppendLine($"closePrice {trade.ExitPrice.ToString("0.#####", CultureInfo.InvariantCulture)}");
		builder.AppendLine($"profit {trade.Profit.ToString("0.##", CultureInfo.InvariantCulture)}");
		builder.AppendLine($"type {(trade.Direction == Sides.Buy ? 0 : 1)}");
		builder.AppendLine($"typeDescription {(trade.Direction == Sides.Buy ? "buy" : "sell")}");
		builder.AppendLine($"orderOpenTime {trade.EntryTime:yyyy-MM-dd HH:mm:ss}");
		builder.AppendLine($"orderCloseTime {trade.ExitTime:yyyy-MM-dd HH:mm:ss}");
		builder.AppendLine($"entryOrderId {trade.EntryOrderId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
		builder.AppendLine($"exitOrderId {trade.ExitOrderId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
		builder.AppendLine($"entryTradeId {trade.EntryTradeId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
		builder.AppendLine($"exitTradeId {trade.ExitTradeId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
		builder.AppendLine($"entryComment {trade.EntryComment ?? string.Empty}");
		builder.AppendLine($"exitComment {trade.ExitComment ?? string.Empty}");

		LogInfo(builder.ToString().TrimEnd());
	}
}
