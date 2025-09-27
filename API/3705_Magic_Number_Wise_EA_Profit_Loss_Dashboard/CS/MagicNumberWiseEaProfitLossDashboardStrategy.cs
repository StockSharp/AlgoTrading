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

using System.Text;
using System.Threading;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dashboard strategy that groups realised and floating profit by the order identifier (magic number).
/// </summary>
public class MagicNumberWiseEaProfitLossDashboardStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _refreshInterval;
	private readonly StrategyParam<bool> _groupByComment;
	private readonly StrategyParam<bool> _includeOpenPositions;

	private readonly Dictionary<string, Summary> _summaries = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Summary> _symbolMap = new(StringComparer.OrdinalIgnoreCase);

	private Timer _timer;
	private int _isProcessing;

	/// <summary>
	/// Interval used to refresh the dashboard log.
	/// </summary>
	public TimeSpan RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// Determines whether order comments should be used instead of <see cref="Order.UserOrderId"/>.
	/// </summary>
	public bool GroupByComment
	{
		get => _groupByComment.Value;
		set => _groupByComment.Value = value;
	}

	/// <summary>
	/// Includes floating PnL computed from the portfolio positions when enabled.
	/// </summary>
	public bool IncludeOpenPositions
	{
		get => _includeOpenPositions.Value;
		set => _includeOpenPositions.Value = value;
	}

	/// <summary>
	/// Creates dashboard parameters.
	/// </summary>
	public MagicNumberWiseEaProfitLossDashboardStrategy()
	{
		_refreshInterval = Param(nameof(RefreshInterval), TimeSpan.FromSeconds(5))
			.SetDisplay("Refresh Interval", "How often the dashboard is updated", "General");

		_groupByComment = Param(nameof(GroupByComment), false)
			.SetDisplay("Group By Comment", "Use order comments as identifiers instead of UserOrderId", "General");

		_includeOpenPositions = Param(nameof(IncludeOpenPositions), true)
			.SetDisplay("Include Open Positions", "Add floating PnL coming from portfolio positions", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_summaries.Clear();
		_symbolMap.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (RefreshInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Refresh interval must be positive.");

		_timer = new Timer(_ => RefreshDashboard(), null, TimeSpan.Zero, RefreshInterval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		RegisterOrder(order);
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		RegisterOrder(order);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		RegisterTrade(trade);
	}

	private void RegisterOrder(Order order)
	{
		var identifier = GetIdentifier(order);
		var comment = order.Comment;
		var symbol = order.Security?.Id;

		var summary = GetOrCreateSummary(identifier);

		if (!comment.IsEmpty() && summary.Comment.IsEmpty())
			summary.Comment = comment;

		UpdateSymbol(summary, symbol);
	}

	private void RegisterTrade(MyTrade trade)
	{
		var identifier = GetIdentifier(trade.Order);
		var order = trade.Order;
		var tradeSecurity = trade.Trade?.Security?.Id;
		var orderSecurity = order?.Security?.Id;
		var comment = order?.Comment;

		var summary = GetOrCreateSummary(identifier);

		summary.DealCount++;
		summary.ClosedPnL += trade.PnL;

		if (GroupByComment && summary.Comment.IsEmpty())
			summary.Comment = identifier;
		else if (!comment.IsEmpty() && summary.Comment.IsEmpty())
			summary.Comment = comment;

		UpdateSymbol(summary, tradeSecurity);
		UpdateSymbol(summary, orderSecurity);
	}

	private void RefreshDashboard()
	{
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
			return;

		try
		{
			var snapshots = CreateSnapshots();

			if (snapshots.Length == 0)
			{
				LogInfo("No trade identifiers collected yet.");
				return;
			}

			Array.Sort(snapshots, (left, right) => string.CompareOrdinal(left.Identifier, right.Identifier));

			var builder = new StringBuilder();
			builder.AppendLine("Magic Id | Deals | Closed P/L | Floating P/L | Symbol | Comment");
			builder.AppendLine("-------------------------------------------------------------------------------");

			foreach (var snapshot in snapshots)
			{
				var floatingText = snapshot.HasFloating ? snapshot.FloatingPnL.ToString("0.##") : "-";
				var symbol = snapshot.Symbol.IsEmpty() ? "-" : snapshot.Symbol;
				var comment = snapshot.Comment.IsEmpty() ? "-" : snapshot.Comment;

				builder.AppendLine(string.Format(
					"{0,-10} | {1,5} | {2,11:0.##} | {3,12} | {4,-12} | {5}",
					snapshot.Identifier,
					snapshot.DealCount,
					snapshot.ClosedPnL,
					floatingText,
					symbol,
					comment));
			}

			LogInfo(builder.ToString().TrimEnd());
		}
		catch (Exception error)
		{
			LogError($"Failed to refresh dashboard: {error.Message}");
		}
		finally
		{
			Interlocked.Exchange(ref _isProcessing, 0);
		}
	}

	private SummarySnapshot[] CreateSnapshots()
	{
		RefreshFloatingPnLLocked();

		var result = new SummarySnapshot[_summaries.Count];
		var index = 0;

		foreach (var summary in _summaries.Values)
		{
			result[index++] = new SummarySnapshot(
				summary.Identifier,
				summary.DealCount,
				summary.ClosedPnL,
				summary.HasFloatingPnL,
				summary.FloatingPnL,
				summary.Symbol,
				summary.Comment);
		}

		return result;
	}

	private void RefreshFloatingPnLLocked()
	{
		foreach (var summary in _summaries.Values)
		{
			summary.HasFloatingPnL = false;
			summary.FloatingPnL = 0m;
		}

		if (!IncludeOpenPositions)
			return;

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		foreach (var position in portfolio.Positions)
		{
			var symbol = position.Security?.Id;
			if (symbol.IsEmpty())
				continue;

			if (!_symbolMap.TryGetValue(symbol, out var summary))
				continue;

			summary.HasFloatingPnL = true;
			summary.FloatingPnL = position.PnL ?? 0m;
		}
	}

	private Summary GetOrCreateSummary(string identifier)
	{
		if (!_summaries.TryGetValue(identifier, out var summary))
		{
			summary = new Summary(identifier);
			_summaries[identifier] = summary;
		}

		return summary;
	}

	private void UpdateSymbol(Summary summary, string symbol)
	{
		if (symbol.IsEmpty())
			return;

		if (!summary.Symbol.IsEmpty())
		{
			if (summary.Symbol.EqualsIgnoreCase(symbol))
				return;

			_symbolMap.Remove(summary.Symbol);
		}

		summary.Symbol = symbol;
		_symbolMap[symbol] = summary;
	}

	private string GetIdentifier(Order order)
	{
		if (order == null)
			return "Unspecified";

		var comment = order.Comment;
		var userOrderId = order.UserOrderId;

		if (GroupByComment)
		{
			if (!comment.IsEmpty())
				return comment;

			if (!userOrderId.IsEmpty())
				return userOrderId;
		}
		else
		{
			if (!userOrderId.IsEmpty())
				return userOrderId;

			if (!comment.IsEmpty())
				return comment;
		}

		return "Unspecified";
	}

	private sealed class Summary
	{
		public Summary(string identifier)
		{
			Identifier = identifier;
		}

		public string Identifier { get; }
		public int DealCount { get; set; }
		public decimal ClosedPnL { get; set; }
		public decimal FloatingPnL { get; set; }
		public bool HasFloatingPnL { get; set; }
		public string Symbol { get; set; }
		public string Comment { get; set; }
	}

	private readonly struct SummarySnapshot
	{
		public SummarySnapshot(string identifier, int dealCount, decimal closedPnL, bool hasFloating, decimal floatingPnL, string symbol, string comment)
		{
			Identifier = identifier;
			DealCount = dealCount;
			ClosedPnL = closedPnL;
			HasFloating = hasFloating;
			FloatingPnL = floatingPnL;
			Symbol = symbol;
			Comment = comment;
		}

		public string Identifier { get; }
		public int DealCount { get; }
		public decimal ClosedPnL { get; }
		public bool HasFloating { get; }
		public decimal FloatingPnL { get; }
		public string Symbol { get; }
		public string Comment { get; }
	}
}

