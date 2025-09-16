using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bridge strategy that mirrors the HistTraining MetaTrader helper by exposing buy, sell, and close commands.
/// </summary>
public class HistoryTrainingBridgeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _defaultVolume;
	private readonly StrategyParam<bool> _requestBuyParam;
	private readonly StrategyParam<bool> _requestSellParam;
	private readonly StrategyParam<bool> _requestCloseSelectedParam;
	private readonly StrategyParam<bool> _requestCloseAllParam;
	private readonly StrategyParam<int> _targetOrderNumberParam;
	private readonly StrategyParam<int> _lastOrderNumberParam;
	private readonly StrategyParam<int> _lastActionCodeParam;
	private readonly StrategyParam<decimal> _lastTradePriceParam;

	private readonly Dictionary<int, OrderRecord> _openOrders = new();
	private readonly object _syncRoot = new();

	private int _nextOrderNumber;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private bool _pendingBuyRequest;
	private bool _pendingSellRequest;
	private bool _pendingCloseRequest;
	private bool _pendingCloseAllRequest;

	/// <summary>
	/// Default volume used when external commands do not provide a custom size.
	/// </summary>
	public decimal DefaultVolume
	{
		get => _defaultVolume.Value;
		set => _defaultVolume.Value = value;
	}

	/// <summary>
	/// Set to <c>true</c> to request a long market order.
	/// </summary>
	public bool RequestBuy
	{
		get => _requestBuyParam.Value;
		set
		{
			_requestBuyParam.Value = value;
			if (value)
				_pendingBuyRequest = true;
		}
	}

	/// <summary>
	/// Set to <c>true</c> to request a short market order.
	/// </summary>
	public bool RequestSell
	{
		get => _requestSellParam.Value;
		set
		{
			_requestSellParam.Value = value;
			if (value)
				_pendingSellRequest = true;
		}
	}

	/// <summary>
	/// Set to <c>true</c> to close the entry identified by <see cref="TargetOrderNumber"/>.
	/// </summary>
	public bool RequestCloseSelected
	{
		get => _requestCloseSelectedParam.Value;
		set
		{
			_requestCloseSelectedParam.Value = value;
			if (value)
				_pendingCloseRequest = true;
		}
	}

	/// <summary>
	/// Set to <c>true</c> to close the entire position.
	/// </summary>
	public bool RequestCloseAll
	{
		get => _requestCloseAllParam.Value;
		set
		{
			_requestCloseAllParam.Value = value;
			if (value)
				_pendingCloseAllRequest = true;
		}
	}

	/// <summary>
	/// Identifier of the entry to close when <see cref="RequestCloseSelected"/> is triggered.
	/// </summary>
	public int TargetOrderNumber
	{
		get => _targetOrderNumberParam.Value;
		set => _targetOrderNumberParam.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Identifier assigned to the most recently processed command.
	/// </summary>
	public int LastOrderNumber
	{
		get => _lastOrderNumberParam.Value;
		private set => _lastOrderNumberParam.Value = value;
	}

	/// <summary>
	/// Last action code (1 = buy, 2 = sell, 3 = close, 4 = close all, 0 = idle).
	/// </summary>
	public int LastActionCode
	{
		get => _lastActionCodeParam.Value;
		private set => _lastActionCodeParam.Value = value;
	}

	/// <summary>
	/// Price observed on the last executed trade.
	/// </summary>
	public decimal LastTradePrice
	{
		get => _lastTradePriceParam.Value;
		private set => _lastTradePriceParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryTrainingBridgeStrategy"/> class.
	/// </summary>
	public HistoryTrainingBridgeStrategy()
	{
		_defaultVolume = Param(nameof(DefaultVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Default volume", "Order volume used when external command does not provide custom size", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_requestBuyParam = Param(nameof(RequestBuy), false)
			.SetDisplay("Request buy", "Set to true to submit a long market order", "External commands");

		_requestSellParam = Param(nameof(RequestSell), false)
			.SetDisplay("Request sell", "Set to true to submit a short market order", "External commands");

		_requestCloseSelectedParam = Param(nameof(RequestCloseSelected), false)
			.SetDisplay("Request close selected", "Set to true to close the position linked with TargetOrderNumber", "External commands");

		_requestCloseAllParam = Param(nameof(RequestCloseAll), false)
			.SetDisplay("Request close all", "Set to true to flatten the current position", "External commands");

		_targetOrderNumberParam = Param(nameof(TargetOrderNumber), 0)
			.SetDisplay("Target order #", "Identifier used when RequestCloseSelected is enabled", "External commands");

		_lastOrderNumberParam = Param(nameof(LastOrderNumber), 0)
			.SetDisplay("Last order #", "Identifier assigned to the most recent action", "Diagnostics");

		_lastActionCodeParam = Param(nameof(LastActionCode), 0)
			.SetDisplay("Last action code", "1 = Buy, 2 = Sell, 3 = Close, 4 = CloseAll, 0 = Idle", "Diagnostics");

		_lastTradePriceParam = Param(nameof(LastTradePrice), 0m)
			.SetDisplay("Last trade price", "Price of the last executed trade", "Diagnostics");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		Timer.Start(TimeSpan.FromMilliseconds(200), ProcessPendingRequests);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		lock (_syncRoot)
		{
			_openOrders.Clear();
			_nextOrderNumber = 0;
		}

		_lastBid = null;
		_lastAsk = null;
		_pendingBuyRequest = false;
		_pendingSellRequest = false;
		_pendingCloseRequest = false;
		_pendingCloseAllRequest = false;

		LastOrderNumber = 0;
		LastActionCode = 0;
		LastTradePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade is not null)
		{
			var price = trade.Trade.Price;
			if (price > 0m)
				LastTradePrice = price;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_lastBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_lastAsk = Convert.ToDecimal(askValue);
	}

	private void ProcessPendingRequests()
	{
		if (_requestBuyParam.Value)
			_pendingBuyRequest = true;

		if (_requestSellParam.Value)
			_pendingSellRequest = true;

		if (_requestCloseSelectedParam.Value)
			_pendingCloseRequest = true;

		if (_requestCloseAllParam.Value)
			_pendingCloseAllRequest = true;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingBuyRequest && ExecuteEntry(Sides.Buy))
		{
			_pendingBuyRequest = false;
			RequestBuy = false;
		}

		if (_pendingSellRequest && ExecuteEntry(Sides.Sell))
		{
			_pendingSellRequest = false;
			RequestSell = false;
		}

		if (_pendingCloseRequest && ExecuteCloseSelected())
		{
			_pendingCloseRequest = false;
			RequestCloseSelected = false;
		}

		if (_pendingCloseAllRequest && ExecuteCloseAll())
		{
			_pendingCloseAllRequest = false;
			RequestCloseAll = false;
		}
	}

	private bool ExecuteEntry(Sides side)
	{
		var baseVolume = DefaultVolume;

		if (baseVolume <= 0m)
		{
			LogWarning("DefaultVolume must be greater than zero before submitting orders.");
			return true;
		}

		var totalVolume = baseVolume;
		var currentPosition = Position;

		if (side == Sides.Buy && currentPosition < 0m)
		{
			totalVolume += Math.Abs(currentPosition);
		}
		else if (side == Sides.Sell && currentPosition > 0m)
		{
			totalVolume += Math.Abs(currentPosition);
		}

		if (totalVolume <= 0m)
			return true;

		int orderNumber;

		lock (_syncRoot)
		{
			if (side == Sides.Buy && currentPosition < 0m)
				RemoveRecordsInternal(Sides.Sell);
			else if (side == Sides.Sell && currentPosition > 0m)
				RemoveRecordsInternal(Sides.Buy);

			orderNumber = _nextOrderNumber++;
			_openOrders[orderNumber] = new OrderRecord
			{
				Number = orderNumber,
				Side = side,
				Volume = baseVolume
			};
		}

		var comment = FormatEntryComment(orderNumber);
		var order = CreateMarketOrder(side, totalVolume, comment);
		RegisterOrder(order);

		LastOrderNumber = orderNumber;
		LastActionCode = side == Sides.Buy ? 1 : 2;

		if (side == Sides.Buy)
		{
			if (_lastAsk is decimal ask)
				LastTradePrice = ask;
		}
		else
		{
			if (_lastBid is decimal bid)
				LastTradePrice = bid;
		}

		return true;
	}

	private bool ExecuteCloseSelected()
	{
		var orderNumber = TargetOrderNumber;
		OrderRecord record;

		lock (_syncRoot)
		{
			if (!_openOrders.TryGetValue(orderNumber, out record))
			{
				LogWarning($"Order #{orderNumber} is not active.");
				return true;
			}

			_openOrders.Remove(orderNumber);

			if (_openOrders.Count == 0)
				_nextOrderNumber = 0;
		}

		var volumeToClose = record.Volume;
		var longPosition = Math.Max(0m, Position);
		var shortPosition = Math.Max(0m, -Position);

		if (record.Side == Sides.Buy)
		{
			if (longPosition <= 0m)
				return true;

			if (volumeToClose > longPosition)
				volumeToClose = longPosition;

			if (volumeToClose <= 0m)
				return true;

			var comment = FormatExitComment(orderNumber);
			var order = CreateMarketOrder(Sides.Sell, volumeToClose, comment);
			RegisterOrder(order);

			if (_lastBid is decimal bid)
				LastTradePrice = bid;
		}
		else
		{
			if (shortPosition <= 0m)
				return true;

			if (volumeToClose > shortPosition)
				volumeToClose = shortPosition;

			if (volumeToClose <= 0m)
				return true;

			var comment = FormatExitComment(orderNumber);
			var order = CreateMarketOrder(Sides.Buy, volumeToClose, comment);
			RegisterOrder(order);

			if (_lastAsk is decimal ask)
				LastTradePrice = ask;
		}

		LastOrderNumber = orderNumber;
		LastActionCode = 3;

		return true;
	}

	private bool ExecuteCloseAll()
	{
		var position = Position;

		if (position == 0m)
		{
			lock (_syncRoot)
			{
				_openOrders.Clear();
				_nextOrderNumber = 0;
			}

			LastOrderNumber = 0;
			LastActionCode = 4;
			return true;
		}

		var volume = Math.Abs(position);
		if (volume <= 0m)
			return true;

		var exitSide = position > 0m ? Sides.Sell : Sides.Buy;
		var order = CreateMarketOrder(exitSide, volume, FormatCloseAllComment());
		RegisterOrder(order);

		if (exitSide == Sides.Sell)
		{
			if (_lastBid is decimal bid)
				LastTradePrice = bid;
		}
		else
		{
			if (_lastAsk is decimal ask)
				LastTradePrice = ask;
		}

		lock (_syncRoot)
		{
			_openOrders.Clear();
			_nextOrderNumber = 0;
		}

		LastOrderNumber = 0;
		LastActionCode = 4;

		return true;
	}

	private void RemoveRecordsInternal(Sides side)
	{
		if (_openOrders.Count == 0)
			return;

		var toRemove = new List<int>();

		foreach (var pair in _openOrders)
		{
			if (pair.Value.Side == side)
				toRemove.Add(pair.Key);
		}

		foreach (var number in toRemove)
		{
			_openOrders.Remove(number);
		}

		if (_openOrders.Count == 0)
			_nextOrderNumber = 0;
	}

	private Order CreateMarketOrder(Sides side, decimal volume, string comment)
	{
		return new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = volume,
			Side = side,
			Type = OrderTypes.Market,
			Comment = comment
		};
	}

	private static string FormatEntryComment(int number)
	{
		return $"HistoryTraining:Entry:{number}";
	}

	private static string FormatExitComment(int number)
	{
		return $"HistoryTraining:Exit:{number}";
	}

	private static string FormatCloseAllComment()
	{
		return "HistoryTraining:Exit:ALL";
	}

	private sealed class OrderRecord
	{
		public int Number { get; init; }
		public Sides Side { get; init; }
		public decimal Volume { get; init; }
	}
}
