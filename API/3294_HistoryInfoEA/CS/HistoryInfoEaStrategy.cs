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
/// StockSharp port of the MT4 HistoryInfo utility that aggregates historical trade information.
/// </summary>
public class HistoryInfoEaStrategy : Strategy
{
	private readonly StrategyParam<HistoryInfoFilterTypes> _filterType;
	private readonly StrategyParam<string> _magicNumberParam;
	private readonly StrategyParam<string> _orderCommentParam;
	private readonly StrategyParam<string> _securityIdParam;

	private decimal _totalProfit;
	private decimal _totalPips;
	private decimal _totalVolume;
	private int _tradeCount;
	private DateTimeOffset? _firstTradeTime;
	private DateTimeOffset? _lastTradeTime;
	private decimal _pipSize;

	/// <summary>
	/// Aggregated statistics for the latest processed trades.
	/// </summary>
	public HistoryInfoSnapshot LastSnapshot { get; private set; } = HistoryInfoSnapshot.Empty;

	/// <summary>
	/// Trade filtering mode that controls which deals enter the summary.
	/// </summary>
	public HistoryInfoFilterTypes FilterType
	{
		get => _filterType.Value;
		set => _filterType.Value = value;
	}

	/// <summary>
	/// Expected <see cref="Order.UserOrderId"/> when <see cref="FilterType"/> equals <see cref="HistoryInfoFilterTypes.CountByUserOrderId"/>.
	/// </summary>
	public string MagicNumber
	{
		get => _magicNumberParam.Value;
		set => _magicNumberParam.Value = value;
	}

	/// <summary>
	/// Expected prefix of <see cref="Order.Comment"/> when filtering by comments.
	/// </summary>
	public string OrderComment
	{
		get => _orderCommentParam.Value;
		set => _orderCommentParam.Value = value;
	}

	/// <summary>
	/// Expected <see cref="Security.Id"/> when filtering by symbol.
	/// </summary>
	public string SecurityId
	{
		get => _securityIdParam.Value;
		set => _securityIdParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HistoryInfoEaStrategy"/>.
	/// </summary>
	public HistoryInfoEaStrategy()
	{
		_filterType = Param(nameof(FilterType), HistoryInfoFilterTypes.CountBySecurity)
		.SetDisplay("Filter Type", "How trades are selected for aggregation", "General");

		_magicNumberParam = Param(nameof(MagicNumber), string.Empty)
		.SetDisplay("Magic Number", "Order.UserOrderId that must match", "Filters");

		_orderCommentParam = Param(nameof(OrderComment), "OrdersComment")
		.SetDisplay("Order Comment", "Order.Comment prefix that must match", "Filters");

		_securityIdParam = Param(nameof(SecurityId), "OrdersSymbol")
		.SetDisplay("Security Id", "Security identifier that must match", "Filters");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetStatistics();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Calculate the pip size using security metadata (PriceStep, Decimals, etc.).
		_pipSize = ResolvePipSize();
		ResetStatistics();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Trade == null)
		return;

		if (!IsTradeIncluded(trade))
		return;

		UpdateStatistics(trade);
	}

	private void ResetStatistics()
	{
		_totalProfit = 0m;
		_totalPips = 0m;
		_totalVolume = 0m;
		_tradeCount = 0;
		_firstTradeTime = null;
		_lastTradeTime = null;
		LastSnapshot = HistoryInfoSnapshot.Empty;
	}

	private bool IsTradeIncluded(MyTrade trade)
	{
		var order = trade.Order;

		return FilterType switch
		{
			HistoryInfoFilterTypes.CountByUserOrderId => !MagicNumber.IsEmpty() && order.UserOrderId.EqualsIgnoreCase(MagicNumber),
			HistoryInfoFilterTypes.CountByComment => !OrderComment.IsEmpty() && order.Comment != null && order.Comment.StartsWith(OrderComment, StringComparison.Ordinal),
			HistoryInfoFilterTypes.CountBySecurity => !SecurityId.IsEmpty() && order.Security?.Id != null && order.Security.Id.EqualsIgnoreCase(SecurityId),
			_ => false
		};
	}

	private void UpdateStatistics(MyTrade trade)
	{
		var tradeTime = trade.Trade.ServerTime;

		if (_firstTradeTime == null || tradeTime < _firstTradeTime)
		_firstTradeTime = tradeTime;

		if (_lastTradeTime == null || tradeTime > _lastTradeTime)
		_lastTradeTime = tradeTime;

		var volume = trade.Volume ?? trade.Trade.Volume ?? 0m;
		if (volume <= 0m)
		return;

		_tradeCount++;
		_totalVolume += volume;

		var profit = (trade.PnL ?? 0m) - (trade.Commission ?? 0m);
		_totalProfit += profit;

		_totalPips += CalculatePips(profit);

		LastSnapshot = new HistoryInfoSnapshot(
		_firstTradeTime,
		_lastTradeTime,
		_totalVolume,
		_totalProfit,
		_totalPips,
		_tradeCount);

		// Mirror the original utility by reporting the summary in the log window.
		LogInfo($"History summary: trades={_tradeCount} volume={_totalVolume:0.#####} profit={_totalProfit:0.##} pips={_totalPips:0.##} first={_firstTradeTime:O} last={_lastTradeTime:O}");
	}

	private decimal CalculatePips(decimal profit)
	{
		if (profit == 0m || _pipSize <= 0m)
		return 0m;

		var security = Security;
		var priceStep = security?.PriceStep ?? security?.MinPriceStep;
		var stepPrice = security?.StepPrice;

		if (priceStep == null || priceStep.Value <= 0m || stepPrice == null || stepPrice.Value == 0m)
		return 0m;

		var pipValue = stepPrice.Value * (_pipSize / priceStep.Value);
		if (pipValue == 0m)
		return 0m;

		return profit / pipValue;
	}

	private decimal ResolvePipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		decimal pointSize = 0m;

		if (security.PriceStep is decimal step && step > 0m)
		{
			pointSize = step;
		}
		else if (security.MinPriceStep is decimal minStep && minStep > 0m)
		{
			pointSize = minStep;
		}
		else if (security.Decimals is int decimals && decimals > 0)
		{
			pointSize = (decimal)Math.Pow(10, -decimals);
		}

		if (pointSize <= 0m)
		return 0m;

		var decimalsCount = security.Decimals ?? 0;
		var multiplier = decimalsCount is 3 or 5 ? 10m : 1m;
		return pointSize * multiplier;
	}

	/// <summary>
	/// Determines how trades should be filtered before aggregating statistics.
	/// </summary>
	public enum HistoryInfoFilterTypes
	{
		/// <summary>
		/// Count trades whose <see cref="Order.UserOrderId"/> equals <see cref="HistoryInfoEaStrategy.MagicNumber"/>.
		/// </summary>
		CountByUserOrderId,

		/// <summary>
		/// Count trades whose <see cref="Order.Comment"/> starts with <see cref="HistoryInfoEaStrategy.OrderComment"/>.
		/// </summary>
		CountByComment,

		/// <summary>
		/// Count trades whose <see cref="Order.Security"/> identifier equals <see cref="HistoryInfoEaStrategy.SecurityId"/>.
		/// </summary>
		CountBySecurity
	}
}

/// <summary>
/// Snapshot that stores aggregated history information.
/// </summary>
/// <param name="FirstTrade">Timestamp of the earliest counted trade.</param>
/// <param name="LastTrade">Timestamp of the latest counted trade.</param>
/// <param name="TotalVolume">Cumulative filled volume.</param>
/// <param name="TotalProfit">Net profit including commissions.</param>
/// <param name="TotalPips">Net profit expressed in pips.</param>
/// <param name="TradeCount">Number of trades that satisfied the filter.</param>
public sealed record HistoryInfoSnapshot(
DateTimeOffset? FirstTrade,
DateTimeOffset? LastTrade,
decimal TotalVolume,
decimal TotalProfit,
decimal TotalPips,
int TradeCount)
{
	/// <summary>
	/// Empty snapshot that represents the initial state.
	/// </summary>
	public static readonly HistoryInfoSnapshot Empty = new(null, null, 0m, 0m, 0m, 0);
}
