using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mirrors trades from an external strategy or manual trading by opening an opposite hedge position.
/// The implementation reproduces the behaviour of the "EES Hedger" MetaTrader expert advisor.
/// </summary>
public class EesHedgerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hedgeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<string> _originalOrderComment;
	private readonly StrategyParam<string> _hedgerOrderComment;

	private readonly HashSet<long> _processedTradeIds = new();
	private readonly HashSet<long> _ownOrderTransactions = new();

	private Order? _stopOrder;
	private Order? _takeProfitOrder;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private decimal _pipSize;

	/// <summary>
	/// Hedge position volume.
	/// </summary>
	public decimal HedgeVolume
	{
		get => _hedgeVolume.Value;
		set => _hedgeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum step between trailing stop updates in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Comment of the original orders that should be hedged. Leave empty to react to any trade.
	/// </summary>
	public string OriginalOrderComment
	{
		get => _originalOrderComment.Value;
		set => _originalOrderComment.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Comment that will be assigned to hedge orders.
	/// </summary>
	public string HedgerOrderComment
	{
		get => _hedgerOrderComment.Value;
		set => _hedgerOrderComment.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EesHedgerStrategy()
	{
		_hedgeVolume = Param(nameof(HedgeVolume), 0.1m)
		.SetDisplay("Hedge Volume", "Volume used for hedge orders", "General")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance per hedge", "Risk Management")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit (pips)", "Take-profit distance per hedge", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk Management")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Minimum trailing stop increment", "Risk Management")
		.SetCanOptimize(true);

		_originalOrderComment = Param(nameof(OriginalOrderComment), string.Empty)
		.SetDisplay("Original Comment", "Orders with this comment will be mirrored", "Filters");

		_hedgerOrderComment = Param(nameof(HedgerOrderComment), "EES Hedger")
		.SetDisplay("Hedge Comment", "Comment applied to hedge orders", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_processedTradeIds.Clear();
		_ownOrderTransactions.Clear();
		_stopOrder = null;
		_takeProfitOrder = null;
		_currentStopPrice = null;
		_currentTakeProfitPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		{
		LogError("Trailing Step must be greater than zero when Trailing Stop is enabled.");
		Stop();
		return;
		}

		if (Security == null)
		{
		LogError("Security is not assigned to the strategy.");
		Stop();
		return;
		}

		if (Connector == null)
		{
		LogError("Connector is not available for subscription.");
		Stop();
		return;
		}

		_pipSize = CalculatePipSize();

		Connector.NewMyTrade += OnConnectorNewMyTrade;

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
	if (Connector != null)
	Connector.NewMyTrade -= OnConnectorNewMyTrade;

	base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order?.Security != Security)
		return;

		if (trade.Order.TransactionId != 0)
		_ownOrderTransactions.Add(trade.Order.TransactionId);

		RefreshProtection();
	}

	private void OnConnectorNewMyTrade(MyTrade trade)
	{
		if (trade.Order == null || trade.Order.Security != Security)
		return;

		if (trade.Order.TransactionId != 0 && _ownOrderTransactions.Contains(trade.Order.TransactionId))
		return;

		if (!string.IsNullOrEmpty(HedgerOrderComment))
		{
			var hedgeComment = trade.Order.Comment ?? string.Empty;
			if (hedgeComment.Equals(HedgerOrderComment, StringComparison.InvariantCultureIgnoreCase))
				return;
		}

		if (!string.IsNullOrEmpty(OriginalOrderComment))
		{
		var comment = trade.Order.Comment ?? string.Empty;
		if (!comment.Equals(OriginalOrderComment, StringComparison.InvariantCultureIgnoreCase))
		return;
		}

		var tradeId = trade.Trade?.Id ?? 0;
		if (tradeId != 0 && !_processedTradeIds.Add(tradeId))
		return;

		if (trade.Order.Side == Sides.Buy)
		{
		OpenHedge(Sides.Sell);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
		OpenHedge(Sides.Buy);
		}
	}

	private void OpenHedge(Sides side)
	{
		var volume = HedgeVolume;
		if (volume <= 0m)
		{
		LogWarning("Hedge volume must be greater than zero.");
		return;
		}


		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null)
		return;

		UpdateTrailingStop(price.Value);
	}

	private void RefreshProtection()
	{
		CancelOrderIfActive(ref _stopOrder);
		CancelOrderIfActive(ref _takeProfitOrder);

		_currentStopPrice = null;
		_currentTakeProfitPrice = null;

		var position = Position;
		if (position == 0m)
		return;

		var volume = Math.Abs(position);
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (position > 0m)
		{
		if (StopLossPips > 0)
		{
		var price = PositionPrice - stopDistance;
		_stopOrder = SellStop(volume, price);
		_currentStopPrice = price;
		}

		if (TakeProfitPips > 0)
		{
		var price = PositionPrice + takeDistance;
		_takeProfitOrder = SellLimit(volume, price);
		_currentTakeProfitPrice = price;
		}
		}
		else
		{
		if (StopLossPips > 0)
		{
		var price = PositionPrice + stopDistance;
		_stopOrder = BuyStop(volume, price);
		_currentStopPrice = price;
		}

		if (TakeProfitPips > 0)
		{
		var price = PositionPrice - takeDistance;
		_takeProfitOrder = BuyLimit(volume, price);
		_currentTakeProfitPrice = price;
		}
		}
	}

	private void UpdateTrailingStop(decimal currentPrice)
	{
		if (TrailingStopPips <= 0 || Position == 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
		var move = currentPrice - PositionPrice;
		if (move <= trailingDistance + trailingStep)
		return;

		var newStop = currentPrice - trailingDistance;
		if (!_currentStopPrice.HasValue || _currentStopPrice.Value < currentPrice - (trailingDistance + trailingStep))
		UpdateStopOrder(true, newStop, Math.Abs(Position));
		}
		else
		{
		var move = PositionPrice - currentPrice;
		if (move <= trailingDistance + trailingStep)
		return;

		var newStop = currentPrice + trailingDistance;
		if (!_currentStopPrice.HasValue || _currentStopPrice.Value > currentPrice + trailingDistance + trailingStep)
		UpdateStopOrder(false, newStop, Math.Abs(Position));
		}
	}

	private void UpdateStopOrder(bool isLongPosition, decimal price, decimal volume)
	{
		CancelOrderIfActive(ref _stopOrder);

		if (volume <= 0m)
		{
		_currentStopPrice = null;
		return;
		}

		_stopOrder = isLongPosition
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_currentStopPrice = price;
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	return 1m;

	var decimals = GetDecimalPlaces(step);
	return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
	var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
	var index = text.IndexOf('.');
	return index >= 0 ? text.Length - index - 1 : 0;
	}
}
