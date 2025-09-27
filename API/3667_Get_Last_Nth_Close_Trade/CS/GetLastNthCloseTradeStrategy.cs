namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

/// <summary>
/// Strategy that reports the Nth most recent closed order handled by the strategy instance.
/// </summary>
public class GetLastNthCloseTradeStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableMagicNumber;
	private readonly StrategyParam<bool> _enableSymbolFilter;
	private readonly StrategyParam<long> _magicNumber;
	private readonly StrategyParam<int> _tradeIndex;

	private readonly List<OrderDetails> _closedOrders = new();
	private readonly Dictionary<long, OrderDetails> _pendingOrders = new();

	private string _lastMessage;

	/// <summary>
	/// Initializes a new instance of <see cref="GetLastNthCloseTradeStrategy"/>.
	/// </summary>
	public GetLastNthCloseTradeStrategy()
	{
		_enableMagicNumber = Param(nameof(EnableMagicNumber), false)
		.SetDisplay("Enable Magic Number", "Filter closed trades by a numeric comment", "Filters");

		_enableSymbolFilter = Param(nameof(EnableSymbolFilter), false)
		.SetDisplay("Enable Symbol Filter", "Restrict closed trades to the strategy security", "Filters");

		_magicNumber = Param(nameof(MagicNumber), 1234L)
		.SetDisplay("Magic Number", "Numeric identifier expected inside the order comment", "Filters")
		.SetCanOptimize(true);

		_tradeIndex = Param(nameof(TradeIndex), 0)
		.SetRange(0, 10_000)
		.SetDisplay("Trade Index", "Zero-based index counted from the most recent closed trade", "Display")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Enable filtering based on a numeric magic number stored in the order comment.
	/// </summary>
	public bool EnableMagicNumber
	{
		get => _enableMagicNumber.Value;
		set => _enableMagicNumber.Value = value;
	}

	/// <summary>
	/// Enable filtering by the strategy security.
	/// </summary>
	public bool EnableSymbolFilter
	{
		get => _enableSymbolFilter.Value;
		set => _enableSymbolFilter.Value = value;
	}

	/// <summary>
	/// Numeric identifier compared with the order comment when magic number filtering is enabled.
	/// </summary>
	public long MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Zero-based index of the closed trade counted from the most recent entry.
	/// </summary>
	public int TradeIndex
	{
		get => _tradeIndex.Value;
		set => _tradeIndex.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closedOrders.Clear();
		_pendingOrders.Clear();
		_lastMessage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateLastTradeInfo();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State != OrderStates.Done)
		return;

		if (!MatchesFilters(order))
		return;

		var details = BuildDetails(order);

		if (_pendingOrders.TryGetValue(order.Id, out var pending))
		{
			if (pending.FilledVolume > 0m)
			{
				details.OpenPrice = pending.OpenPrice;
				details.OpenTime = pending.OpenTime;
				details.ClosePrice = pending.ClosePrice;
				details.CloseTime = pending.CloseTime;
				details.FilledVolume = pending.FilledVolume;
				details.Profit = pending.Profit;
			}

			_pendingOrders.Remove(order.Id);
		}

		_closedOrders.Add(details);
		_closedOrders.Sort((left, right) => right.CloseTime.CompareTo(left.CloseTime));

		if (_closedOrders.Count > 1000)
		_closedOrders.RemoveRange(1000, _closedOrders.Count - 1000);

		UpdateLastTradeInfo();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var tradeInfo = trade.Trade;

		if (order is null || tradeInfo is null)
		return;

		if (!MatchesFilters(order))
		return;

		if (!_pendingOrders.TryGetValue(order.Id, out var details))
		details = BuildDetails(order);

		var volume = tradeInfo.Volume;
		var price = tradeInfo.Price;
		var time = tradeInfo.Time;

		if (details.FilledVolume <= 0m)
		{
			details.OpenPrice = price;
			details.OpenTime = time;
		}

		details.ClosePrice = price;
		details.CloseTime = time;
		details.FilledVolume += volume;

		if (details.Direction.HasValue)
		{
			var difference = details.Direction == Sides.Buy
			? price - details.OpenPrice
			: details.OpenPrice - price;

			details.Profit += difference * volume;
		}

		_pendingOrders[order.Id] = details;
	}

	private bool MatchesFilters(Order order)
	{
		if (EnableSymbolFilter)
		{
			var strategySecurity = Security;
			if (strategySecurity == null || order.Security != strategySecurity)
			return false;
		}

		if (EnableMagicNumber)
		{
			var comment = order.Comment?.Trim();
			if (string.IsNullOrEmpty(comment))
			return false;

			if (!long.TryParse(comment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
			return false;

			if (parsed != MagicNumber)
			return false;
		}

		return true;
	}

	private OrderDetails BuildDetails(Order order)
	{
		var details = new OrderDetails
		{
			Ticket = order.Id,
			Symbol = order.Security?.Id ?? string.Empty,
			Volume = order.Volume,
			StopLoss = order.StopPrice ?? 0m,
			TakeProfit = 0m,
			Comment = order.Comment ?? string.Empty,
			Type = order.Type,
			Direction = order.Direction,
			OpenPrice = order.Price ?? order.AveragePrice ?? 0m,
			ClosePrice = order.AveragePrice ?? order.Price ?? 0m,
			OpenTime = order.Time,
			CloseTime = order.LastChangeTime ?? order.Time,
			FilledVolume = 0m,
			Profit = 0m,
		};

		return details;
	}

	private void UpdateLastTradeInfo()
	{
		string message;

		if (_closedOrders.Count == 0)
		{
			message = "No closed trades available.";
		}
		else if (TradeIndex < 0 || TradeIndex >= _closedOrders.Count)
		{
			message = string.Format(
			CultureInfo.InvariantCulture,
			"Trade index {0} is outside the range of {1} closed trades.",
			TradeIndex,
			_closedOrders.Count);
		}
		else
		{
			var trade = _closedOrders[TradeIndex];
			var builder = new StringBuilder();

			builder.Append("ticket ").AppendLine(trade.Ticket.ToString(CultureInfo.InvariantCulture));
			builder.Append("symbol ").AppendLine(trade.Symbol);
			builder.Append("lots ").AppendLine(trade.Volume.ToString(CultureInfo.InvariantCulture));
			builder.Append("openPrice ").AppendLine(trade.OpenPrice.ToString(CultureInfo.InvariantCulture));
			builder.Append("closePrice ").AppendLine(trade.ClosePrice.ToString(CultureInfo.InvariantCulture));
			builder.Append("stopLoss ").AppendLine(trade.StopLoss.ToString(CultureInfo.InvariantCulture));
			builder.Append("takeProfit ").AppendLine(trade.TakeProfit.ToString(CultureInfo.InvariantCulture));
			builder.Append("comment ").AppendLine(trade.Comment);
			builder.Append("type ").AppendLine(trade.Type.ToString());
			builder.Append("orderOpenTime ").AppendLine(trade.OpenTime.ToString("O", CultureInfo.InvariantCulture));
			builder.Append("orderCloseTime ").AppendLine(trade.CloseTime.ToString("O", CultureInfo.InvariantCulture));
			builder.Append("profit ").Append(trade.Profit.ToString(CultureInfo.InvariantCulture));

			message = builder.ToString();
		}

		PublishMessage(message);
	}

	private void PublishMessage(string message)
	{
		if (_lastMessage.EqualsIgnoreCase(message))
		return;

		_lastMessage = message;
		AddInfo(message);
	}

	private struct OrderDetails
	{
		public long Ticket;
		public string Symbol;
		public decimal Volume;
		public decimal StopLoss;
		public decimal TakeProfit;
		public string Comment;
		public OrderTypes Type;
		public Sides? Direction;
		public decimal OpenPrice;
		public decimal ClosePrice;
		public DateTimeOffset OpenTime;
		public DateTimeOffset CloseTime;
		public decimal FilledVolume;
		public decimal Profit;
	}
}
