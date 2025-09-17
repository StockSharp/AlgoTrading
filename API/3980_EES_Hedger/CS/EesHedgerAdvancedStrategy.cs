using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Creates an opposite hedge whenever an external strategy or manual trader opens a position.
/// It reproduces the behaviour of the MetaTrader "EES Hedger" expert advisor with break-even and trailing logic.
/// </summary>
public class EesHedgerAdvancedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hedgeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingActivationPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<string> _originalOrderComment;
	private readonly StrategyParam<string> _hedgerOrderComment;

	private readonly HashSet<long> _processedTradeIds = new();
	private readonly HashSet<long> _ownOrderTransactions = new();

	private Order? _stopOrder;
	private Order? _takeProfitOrder;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private decimal _pipSize;
	private bool _breakEvenApplied;

	/// <summary>
	/// Hedge position volume.
	/// </summary>
	public decimal HedgeVolume
	{
		get => _hedgeVolume.Value;
		set => _hedgeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips for the hedge order.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips for the hedge order.
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
	/// Minimum profit in pips before the trailing stop can move.
	/// </summary>
	public int TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Profit in pips required before the stop-loss is moved to break-even.
	/// </summary>
	public int BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Comment attached to the original orders. Leave blank to hedge any external trade.
	/// </summary>
	public string OriginalOrderComment
	{
		get => _originalOrderComment.Value;
		set => _originalOrderComment.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Comment assigned to hedge orders produced by this strategy.
	/// </summary>
	public string HedgerOrderComment
	{
		get => _hedgerOrderComment.Value;
		set => _hedgerOrderComment.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public EesHedgerAdvancedStrategy()
	{
		_hedgeVolume = Param(nameof(HedgeVolume), 0.1m)
		.SetDisplay("Hedge Volume", "Volume used for hedge orders", "General")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance applied to hedges", "Risk Management")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit (pips)", "Take-profit distance applied to hedges", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
		.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained once profit grows", "Risk Management")
		.SetCanOptimize(true);

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 0)
		.SetDisplay("Trailing Activation (pips)", "Minimum profit before trailing stop updates", "Risk Management")
		.SetCanOptimize(true);

		_breakEvenPips = Param(nameof(BreakEvenPips), 25)
		.SetDisplay("Break-even (pips)", "Profit required before the stop is moved to the entry price", "Risk Management")
		.SetCanOptimize(true);

		_originalOrderComment = Param(nameof(OriginalOrderComment), string.Empty)
		.SetDisplay("Original Comment", "Filter external trades by comment", "Filters");

		_hedgerOrderComment = Param(nameof(HedgerOrderComment), "EES Hedger")
		.SetDisplay("Hedge Comment", "Comment attached to hedge orders", "General");
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
		_breakEvenApplied = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			LogError("Security must be assigned before starting the strategy.");
			Stop();
			return;
		}

		if (Connector == null)
		{
			LogError("Connector is required to subscribe for account trades.");
			Stop();
			return;
		}

		_pipSize = CalculatePipSize();

		Connector.NewMyTrade += OnConnectorNewMyTrade;

		SubscribeTrades()
		.Bind(ProcessTrade)
		.Start();
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

		_breakEvenApplied = false;
		RefreshProtection();
	}

	private void OnConnectorNewMyTrade(MyTrade trade)
	{
		if (trade.Order?.Security != Security)
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
		OpenHedge(Sides.Sell);
		else if (trade.Order.Side == Sides.Sell)
		OpenHedge(Sides.Buy);
	}

	private void OpenHedge(Sides side)
	{
		var volume = HedgeVolume;
		if (volume <= 0m)
		{
			LogWarning("Hedge volume must be positive to place offsetting orders.");
			return;
		}

		if (side == Sides.Buy)
		BuyMarket(volume, comment: HedgerOrderComment);
		else
		SellMarket(volume, comment: HedgerOrderComment);
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null)
		return;

		UpdateRiskManagement(price.Value);
	}

	private void RefreshProtection()
	{
		CancelOrderIfActive(ref _stopOrder);
		CancelOrderIfActive(ref _takeProfitOrder);

		_currentStopPrice = null;
		_currentTakeProfitPrice = null;

		var position = Position;
		if (position == 0m)
		{
			_breakEvenApplied = false;
			return;
		}

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

	private void UpdateRiskManagement(decimal currentPrice)
	{
		var position = Position;
		if (position == 0m)
		{
			if (_stopOrder != null || _takeProfitOrder != null)
			RefreshProtection();
			return;
		}

		var volume = Math.Abs(position);

		if (BreakEvenPips > 0 && !_breakEvenApplied)
		{
			var profitPips = CalculateProfitInPips(position, currentPrice);
			if (profitPips >= BreakEvenPips)
			{
				var breakEvenOffset = Math.Max(_pipSize, Security?.PriceStep ?? 0m);
				var newStop = position > 0m
				? PositionPrice + breakEvenOffset
				: PositionPrice - breakEvenOffset;

				if (IsBetterStop(position > 0m, newStop))
				{
					UpdateStopOrder(position > 0m, newStop, volume);
					_breakEvenApplied = true;
				}
			}
		}

		UpdateTrailingStop(position, volume, currentPrice);
	}

	private void UpdateTrailingStop(decimal position, decimal volume, decimal currentPrice)
	{
		if (TrailingStopPips <= 0)
		return;

		var activationDistance = TrailingActivationPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var minimumImprovement = Math.Max(_pipSize, Security?.PriceStep ?? 0m);

		if (position > 0m)
		{
			var profit = currentPrice - PositionPrice;
			if (profit < Math.Max(activationDistance, trailingDistance))
			return;

			var newStop = currentPrice - trailingDistance;
			if (IsBetterStop(true, newStop, minimumImprovement))
			UpdateStopOrder(true, newStop, volume);
		}
		else
		{
			var profit = PositionPrice - currentPrice;
			if (profit < Math.Max(activationDistance, trailingDistance))
			return;

			var newStop = currentPrice + trailingDistance;
			if (IsBetterStop(false, newStop, minimumImprovement))
			UpdateStopOrder(false, newStop, volume);
		}
	}

	private bool IsBetterStop(bool isLongPosition, decimal candidatePrice, decimal? minimumImprovement = null)
	{
		if (!_currentStopPrice.HasValue)
		return true;

		var current = _currentStopPrice.Value;

		if (isLongPosition)
		{
			if (candidatePrice <= current)
			return false;
			return minimumImprovement == null || candidatePrice - current >= minimumImprovement.Value;
		}

		if (candidatePrice >= current)
		return false;
		return minimumImprovement == null || current - candidatePrice >= minimumImprovement.Value;
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
		? SellStop(volume, price, comment: HedgerOrderComment)
		: BuyStop(volume, price, comment: HedgerOrderComment);

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

	private decimal CalculateProfitInPips(decimal position, decimal currentPrice)
	{
		var pip = _pipSize <= 0m ? 1m : _pipSize;
		var profit = position > 0m ? currentPrice - PositionPrice : PositionPrice - currentPrice;
		return profit / pip;
	}
}
