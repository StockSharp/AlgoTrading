namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that places both buy stop and sell stop orders around the current spread.
/// It keeps one of the pending orders active until filled, then manages the resulting position
/// with fixed stop loss, take profit and optional trailing stop levels.
/// </summary>
public class OpenTwoPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<decimal> _slippagePoints;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	private decimal? _entryPrice;
	private decimal? _stopLevel;
	private decimal? _takeLevel;

	/// <summary>
	/// Enables risk-based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percent applied when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed order volume used when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Distance in price steps used to place the pending orders away from the spread.
	/// </summary>
	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Extra slippage reserve in price steps (currently informational).
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OpenTwoPendingOrdersStrategy"/>.
	/// </summary>
	public OpenTwoPendingOrdersStrategy()
	{
		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Money Management", "Use risk based position sizing", "Position")
		.SetCanOptimize(false);

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetDisplay("Risk Percent", "Portfolio percent risk per trade", "Position")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetDisplay("Fixed Volume", "Lot size used without money management", "Position")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 300m, 20m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300m)
		.SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50m, 600m, 50m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
		.SetDisplay("Trailing Stop (steps)", "Trailing stop distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 50m)
		.SetDisplay("Entry Offset (steps)", "Offset from the spread for stop entries", "Execution")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 10m);

		_slippagePoints = Param(nameof(SlippagePoints), 5m)
		.SetDisplay("Slippage Reserve", "Additional distance reserved for fills", "Execution")
		.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeOrderBook()
		.Bind(OnOrderBookReceived)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
		return;

		_entryPrice = null;
		_stopLevel = null;
		_takeLevel = null;

		TryPlacePendingOrders();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
		return;

		var fillPrice = trade.Trade.Price;

		if (trade.Order == _buyStopOrder)
		{
			CancelOppositeOrder(ref _sellStopOrder);
			_buyStopOrder = null;
			InitializePositionLevels(true, fillPrice);
			return;
		}

		if (trade.Order == _sellStopOrder)
		{
			CancelOppositeOrder(ref _buyStopOrder);
			_sellStopOrder = null;
			InitializePositionLevels(false, fillPrice);
		}
	}

	private void OnOrderBookReceived(IOrderBookMessage orderBook)
	{
		var bestBid = orderBook.GetBestBid();
		var bestAsk = orderBook.GetBestAsk();

		if (bestBid != null)
		_bestBidPrice = bestBid.Value.Price;

		if (bestAsk != null)
		_bestAskPrice = bestAsk.Value.Price;

		if (Position == 0)
		{
			TryPlacePendingOrders();
			return;
		}

		ManagePosition();
	}

	private void TryPlacePendingOrders()
	{
		if (Security == null)
		return;

		if (Position != 0)
		return;

		if (_buyStopOrder != null || _sellStopOrder != null)
		return;

		if (_bestBidPrice == null || _bestAskPrice == null)
		return;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		var offset = EntryOffsetPoints * step;
		var buyPrice = _bestAskPrice.Value + offset;
		var sellPrice = _bestBidPrice.Value - offset;

		if (buyPrice <= 0m || sellPrice <= 0m)
		return;

		_buyStopOrder = BuyStop(volume, buyPrice);
		_sellStopOrder = SellStop(volume, sellPrice);
	}

	private decimal CalculateVolume()
	{
		var volume = FixedVolume;

		if (!UseMoneyManagement)
		return AdjustVolume(volume);

		var equity = Portfolio?.CurrentValue;
		var step = Security?.PriceStep ?? 0m;
		if (equity == null || equity <= 0m || step <= 0m || StopLossPoints <= 0m)
		return AdjustVolume(volume);

		var stopDistance = StopLossPoints * step;
		if (stopDistance <= 0m)
		return AdjustVolume(volume);

		var riskAmount = equity.Value * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return AdjustVolume(volume);

		var rawVolume = riskAmount / stopDistance;
		if (rawVolume <= 0m)
		rawVolume = volume;

		return AdjustVolume(rawVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
		return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		var min = Security.MinVolume ?? step;
		var max = Security.MaxVolume ?? decimal.MaxValue;

		var rounded = Math.Round(volume / step) * step;
		if (rounded < min)
		rounded = min;
		if (rounded > max)
		rounded = max;

		return rounded;
	}

	private void InitializePositionLevels(bool isLong, decimal entryPrice)
	{
		_entryPrice = entryPrice;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		_stopLevel = StopLossPoints > 0m
		? entryPrice + (isLong ? -StopLossPoints : StopLossPoints) * step
		: null;

		_takeLevel = TakeProfitPoints > 0m
		? entryPrice + (isLong ? TakeProfitPoints : -TakeProfitPoints) * step
		: null;
	}

	private void ManagePosition()
	{
		if (Security == null)
		return;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		if (Position > 0)
		{
			if (_bestBidPrice != null)
			{
				if (_stopLevel != null && _bestBidPrice.Value <= _stopLevel.Value)
				{
					SellMarket(Position);
					return;
				}

				if (_takeLevel != null && _bestBidPrice.Value >= _takeLevel.Value)
				{
					SellMarket(Position);
					return;
				}

				UpdateTrailingStop(true, step, _bestBidPrice.Value);
			}
		}
		else if (Position < 0)
		{
			if (_bestAskPrice != null)
			{
				if (_stopLevel != null && _bestAskPrice.Value >= _stopLevel.Value)
				{
					BuyMarket(Math.Abs(Position));
					return;
				}

				if (_takeLevel != null && _bestAskPrice.Value <= _takeLevel.Value)
				{
					BuyMarket(Math.Abs(Position));
					return;
				}

				UpdateTrailingStop(false, step, _bestAskPrice.Value);
			}
		}
	}

	private void UpdateTrailingStop(bool isLong, decimal step, decimal currentPrice)
	{
		if (TrailingStopPoints <= 0m || _entryPrice == null)
		return;

		var trailingDistance = TrailingStopPoints * step;
		if (trailingDistance <= 0m)
		return;

		if (isLong)
		{
			if (currentPrice - _entryPrice.Value >= trailingDistance)
			{
				var desiredStop = currentPrice - trailingDistance;
				if (_stopLevel == null || desiredStop > _stopLevel.Value)
				_stopLevel = desiredStop;
			}
		}
		else
		{
			if (_entryPrice.Value - currentPrice >= trailingDistance)
			{
				var desiredStop = currentPrice + trailingDistance;
				if (_stopLevel == null || desiredStop < _stopLevel.Value)
				_stopLevel = desiredStop;
			}
		}
	}

	private void CancelOppositeOrder(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}
}
