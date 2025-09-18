using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy converted from the MetaTrader expert located in <c>MQL/8147</c>.
/// Places symmetric pending limit orders around the market price and resets the grid
/// when a profit target or a maximum drawdown is reached.
/// </summary>
public class PendingLimitGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetCurrency;
	private readonly StrategyParam<decimal> _maxDrawdownCurrency;
	private readonly StrategyParam<decimal> _gridStepPoints;
	private readonly StrategyParam<int> _levelsPerSide;
	private readonly StrategyParam<decimal> _orderVolume;

	private decimal _pointValue;
	private decimal _baselineEquity;
	private bool _baselineCaptured;
	private bool _gridPlaced;
	private bool _flattenRequested;
	private bool _closePositionRequested;

	private Order?[] _sellOrders = Array.Empty<Order?>();
	private Order?[] _buyOrders = Array.Empty<Order?>();
	private bool[] _sellLevelPlaced = Array.Empty<bool>();
	private bool[] _buyLevelPlaced = Array.Empty<bool>();

	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Net profit in account currency that triggers a full reset of the grid.
	/// </summary>
	public decimal ProfitTargetCurrency
	{
		get => _profitTargetCurrency.Value;
		set => _profitTargetCurrency.Value = value;
	}

	/// <summary>
	/// Maximum floating loss tolerated before all orders are cancelled and positions are closed.
	/// </summary>
	public decimal MaxDrawdownCurrency
	{
		get => _maxDrawdownCurrency.Value;
		set => _maxDrawdownCurrency.Value = value;
	}

	/// <summary>
	/// Distance between consecutive grid levels expressed in broker points.
	/// </summary>
	public decimal GridStepPoints
	{
		get => _gridStepPoints.Value;
		set => _gridStepPoints.Value = value;
	}

	/// <summary>
	/// Number of pending orders placed above and below the current market price.
	/// </summary>
	public int LevelsPerSide
	{
		get => _levelsPerSide.Value;
		set => _levelsPerSide.Value = value;
	}

	/// <summary>
	/// Volume used for each pending limit order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters that replicate the inputs of the original MetaTrader expert.
	/// </summary>
	public PendingLimitGridStrategy()
	{
		_profitTargetCurrency = Param(nameof(ProfitTargetCurrency), 500m)
		.SetDisplay("Profit Target", "Net profit (currency units) before the grid is reset", "Risk")
		.SetGreaterOrEqualZero();

		_maxDrawdownCurrency = Param(nameof(MaxDrawdownCurrency), 150m)
		.SetDisplay("Max Drawdown", "Maximum floating loss (currency units) tolerated", "Risk")
		.SetGreaterOrEqualZero();

		_gridStepPoints = Param(nameof(GridStepPoints), 30m)
		.SetDisplay("Grid Step (points)", "Distance in broker points between pending limits", "Grid")
		.SetGreaterThanZero();

		_levelsPerSide = Param(nameof(LevelsPerSide), 15)
		.SetDisplay("Levels Per Side", "Number of limit orders placed on each side of price", "Grid")
		.SetGreaterThanZero();

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetDisplay("Order Volume", "Volume for every pending limit order", "Trading")
		.SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointValue = 0m;
		_baselineEquity = 0m;
		_baselineCaptured = false;
		_gridPlaced = false;
		_flattenRequested = false;
		_closePositionRequested = false;

		_sellOrders = Array.Empty<Order?>();
		_buyOrders = Array.Empty<Order?>();
		_sellLevelPlaced = Array.Empty<bool>();
		_buyLevelPlaced = Array.Empty<bool>();

		_lastBid = null;
		_lastAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();
		Volume = OrderVolume;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bidPrice)
		_lastBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal askPrice)
		_lastAsk = askPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_baselineCaptured)
		{
			var currentValue = Portfolio?.CurrentValue;
			if (currentValue is null)
			return;

			_baselineEquity = currentValue.Value;
			_baselineCaptured = true;
		}

		var equity = Portfolio?.CurrentValue ?? _baselineEquity;
		var floatingProfit = equity - _baselineEquity;

		var canTrade = floatingProfit < ProfitTargetCurrency && floatingProfit > -MaxDrawdownCurrency;

		if (!canTrade)
		{
			RequestFlatten();

			if (!HasOpenExposure())
			ResetGridState();

			return;
		}

		_flattenRequested = false;

		if (!_gridPlaced)
		TryPlaceGrid();
	}

	private void TryPlaceGrid()
	{
		var bid = _lastBid ?? _lastAsk;
		var ask = _lastAsk ?? _lastBid;

		if (bid is null || ask is null)
		return;

		var security = Security;
		if (security is null)
		return;

		var step = GridStepPoints * _pointValue;
		if (step <= 0m)
		return;

		EnsureArrays();

		var firstSell = security.ShrinkPrice(bid.Value + step);
		var firstBuy = security.ShrinkPrice(ask.Value - step);

		if (firstSell <= 0m || firstBuy <= 0m)
		return;

		for (var i = 0; i < LevelsPerSide; i++)
		{
			if (!_sellLevelPlaced[i])
			{
				var price = i == 0 ? firstSell : firstSell + (i + 1) * step;
				price = security.ShrinkPrice(price);

				if (price > 0m)
				{
					var order = SellLimit(OrderVolume, price);
					if (order != null)
					{
						_sellOrders[i] = order;
						_sellLevelPlaced[i] = true;
					}
				}
			}

			if (!_buyLevelPlaced[i])
			{
				var price = i == 0 ? firstBuy : firstBuy - (i + 1) * step;
				price = security.ShrinkPrice(price);

				if (price > 0m)
				{
					var order = BuyLimit(OrderVolume, price);
					if (order != null)
					{
						_buyOrders[i] = order;
						_buyLevelPlaced[i] = true;
					}
				}
			}
		}

		_gridPlaced = true;
	}

	private void RequestFlatten()
	{
		if (_flattenRequested)
		return;

		_flattenRequested = true;

		CancelOrderArray(_sellOrders);
		CancelOrderArray(_buyOrders);

		if (Position != 0m && !_closePositionRequested)
		{
			ClosePosition();
			_closePositionRequested = true;
		}
	}

	private void ResetGridState()
	{
		_gridPlaced = false;
		_flattenRequested = false;
		_closePositionRequested = false;

		Array.Clear(_sellOrders, 0, _sellOrders.Length);
		Array.Clear(_buyOrders, 0, _buyOrders.Length);
		Array.Clear(_sellLevelPlaced, 0, _sellLevelPlaced.Length);
		Array.Clear(_buyLevelPlaced, 0, _buyLevelPlaced.Length);

		_baselineCaptured = false;
		_lastBid = null;
		_lastAsk = null;
	}

	private bool HasOpenExposure()
	{
		if (Position != 0m)
		return true;

		for (var i = 0; i < _sellOrders.Length; i++)
		{
			var order = _sellOrders[i];
			if (order != null && !order.State.IsFinal())
			return true;
		}

		for (var i = 0; i < _buyOrders.Length; i++)
		{
			var order = _buyOrders[i];
			if (order != null && !order.State.IsFinal())
			return true;
		}

		return false;
	}

	private void CancelOrderArray(Order?[] orders)
	{
		for (var i = 0; i < orders.Length; i++)
		{
			var order = orders[i];
			if (order != null && !order.State.IsFinal())
			CancelOrder(order);
		}
	}

	private void EnsureArrays()
	{
		if (_sellOrders.Length != LevelsPerSide)
		{
			_sellOrders = new Order?[LevelsPerSide];
			_sellLevelPlaced = new bool[LevelsPerSide];
		}

		if (_buyOrders.Length != LevelsPerSide)
		{
			_buyOrders = new Order?[LevelsPerSide];
			_buyLevelPlaced = new bool[LevelsPerSide];
		}
	}

	private decimal CalculatePointValue()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		return 1m;

		var digits = 0;
		var temp = step;

		while (temp < 1m && digits < 10)
		{
			temp *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
		step *= 10m;

		return step;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (!order.State.IsFinal())
		return;

		if (Position == 0m)
		_closePositionRequested = false;
	}
}
