using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending order grid translated from the MetaTrader expert "Pending_tread".
/// Places layered stop or limit orders above and below the market with optional equity protection.
/// </summary>
public class PendingTreadStrategy : Strategy
{
	private const int OrdersPerSide = 10;
	private static readonly TimeSpan SubmissionThrottle = TimeSpan.FromSeconds(5);
	private const string CommentPrefix = "PendingTread";

	/// <summary>
	/// Order direction used for a pending grid.
	/// </summary>
	public enum PendingGridDirection
	{
		/// <summary>
		/// Long side orders.
		/// </summary>
		Buy,

		/// <summary>
		/// Short side orders.
		/// </summary>
		Sell,
	}

	private readonly StrategyParam<decimal> _pipStepPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<bool> _enableBuyGrid;
	private readonly StrategyParam<PendingGridDirection> _aboveMarketDirection;
	private readonly StrategyParam<bool> _enableSellGrid;
	private readonly StrategyParam<PendingGridDirection> _belowMarketDirection;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _minimumEquity;
	private readonly StrategyParam<bool> _enableEquityGuard;
	private readonly StrategyParam<decimal> _maxLossPercent;

	private decimal _pointSize;
	private decimal _pipDistance;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;

	private DateTimeOffset _lastSubmissionTime;
	private decimal? _startingBalance;
	private bool _equityGuardTriggered;

	/// <summary>
	/// Distance between stacked pending orders expressed in MetaTrader pips.
	/// </summary>
	public decimal PipStep
	{
		get => _pipStepPips.Value;
		set => _pipStepPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance applied to every pending order in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance applied to every pending order in MetaTrader pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Volume used when placing each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Optional slippage placeholder kept for feature parity with the original expert.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Enables the grid that submits orders above the current market price.
	/// </summary>
	public bool EnableBuyGrid
	{
		get => _enableBuyGrid.Value;
		set => _enableBuyGrid.Value = value;
	}

	/// <summary>
	/// Direction (buy or sell) for pending orders located above the market.
	/// </summary>
	public PendingGridDirection AboveMarketTradeDirection
	{
		get => _aboveMarketDirection.Value;
		set => _aboveMarketDirection.Value = value;
	}

	/// <summary>
	/// Enables the grid that submits orders below the current market price.
	/// </summary>
	public bool EnableSellGrid
	{
		get => _enableSellGrid.Value;
		set => _enableSellGrid.Value = value;
	}

	/// <summary>
	/// Direction (buy or sell) for pending orders located below the market.
	/// </summary>
	public PendingGridDirection BelowMarketTradeDirection
	{
		get => _belowMarketDirection.Value;
		set => _belowMarketDirection.Value = value;
	}

	/// <summary>
	/// Enables stop-loss prices for pending orders.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Minimum portfolio equity required to maintain the pending grids.
	/// </summary>
	public decimal MinimumEquity
	{
		get => _minimumEquity.Value;
		set => _minimumEquity.Value = value;
	}

	/// <summary>
	/// Enables forced liquidation once the configured equity drawdown is reached.
	/// </summary>
	public bool EnableEquityLossProtection
	{
		get => _enableEquityGuard.Value;
		set => _enableEquityGuard.Value = value;
	}

	/// <summary>
	/// Maximum percentage drawdown tolerated before cancelling all orders.
	/// </summary>
	public decimal MaxLossPercent
	{
		get => _maxLossPercent.Value;
		set => _maxLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PendingTreadStrategy"/> class.
	/// </summary>
	public PendingTreadStrategy()
	{
		_pipStepPips = Param(nameof(PipStep), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Step", "Distance between layered orders in MetaTrader pips.", "Grid");

		_takeProfitPips = Param(nameof(TakeProfitPips), 75m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Optional take-profit distance for pending orders.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Optional stop-loss distance for pending orders.", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.10m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade volume submitted with each pending order.", "Trading");

		_slippage = Param(nameof(Slippage), 3)
		.SetNotNegative()
		.SetDisplay("Slippage", "Placeholder parameter for compatibility with the original EA.", "Trading");

		_enableBuyGrid = Param(nameof(EnableBuyGrid), true)
		.SetDisplay("Enable Above-Market Grid", "Maintains layered pending orders above the market price.", "Grid");

		_aboveMarketDirection = Param(nameof(AboveMarketTradeDirection), PendingGridDirection.Buy)
		.SetDisplay("Above-Market Direction", "Order side used for the above-market pending grid.", "Grid");

		_enableSellGrid = Param(nameof(EnableSellGrid), true)
		.SetDisplay("Enable Below-Market Grid", "Maintains layered pending orders below the market price.", "Grid");

		_belowMarketDirection = Param(nameof(BelowMarketTradeDirection), PendingGridDirection.Sell)
		.SetDisplay("Below-Market Direction", "Order side used for the below-market pending grid.", "Grid");

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
		.SetDisplay("Use Stop Loss", "Attaches stop-loss prices to pending orders when enabled.", "Risk");

		_minimumEquity = Param(nameof(MinimumEquity), 100m)
		.SetNotNegative()
		.SetDisplay("Minimum Equity", "Minimum equity required to submit new pending orders.", "Protection");

		_enableEquityGuard = Param(nameof(EnableEquityLossProtection), true)
		.SetDisplay("Enable Equity Guard", "Cancels all activity if portfolio equity drops below the threshold.", "Protection");

		_maxLossPercent = Param(nameof(MaxLossPercent), 20m)
		.SetNotNegative()
		.SetDisplay("Max Loss Percent", "Drawdown percentage that triggers the equity guard.", "Protection");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointSize = 0m;
		_pipDistance = 0m;
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;

		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;

		_lastSubmissionTime = default;
		_startingBalance = null;
		_equityGuardTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		UpdateDistances();

		_startingBalance = Portfolio?.CurrentValue;
		_equityGuardTriggered = false;

		StartProtection();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		if (!_hasBestBid && !_hasBestAsk)
		return;

		var currentTime = message.ServerTime != default ? message.ServerTime : message.LocalTime;
		MaintainAllPendingGrids(currentTime);
	}

	private void MaintainAllPendingGrids(DateTimeOffset currentTime)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateDistances();

		if (!CheckEquityGuard())
		return;

	var minimumEquity = MinimumEquity;
		if (minimumEquity > 0m)
		{
			var currentEquity = Portfolio?.CurrentValue ?? 0m;
			if (currentEquity < minimumEquity)
			return;
		}

		if (_lastSubmissionTime != default && currentTime - _lastSubmissionTime < SubmissionThrottle)
		return;

		_lastSubmissionTime = currentTime;

		if (EnableBuyGrid)
		{
			if (_hasBestAsk)
			MaintainPendingGrid(true, AboveMarketTradeDirection, _bestAsk);
		}
		else
		{
			CancelGrid(true);
		}

		if (EnableSellGrid)
		{
			if (_hasBestBid)
			MaintainPendingGrid(false, BelowMarketTradeDirection, _bestBid);
		}
		else
		{
			CancelGrid(false);
		}
	}

	private void MaintainPendingGrid(bool aboveMarket, PendingGridDirection direction, decimal referencePrice)
	{
		if (referencePrice <= 0m)
		return;

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
		return;

		var pipDistance = _pipDistance;
		if (pipDistance <= 0m)
		return;

		var comment = BuildComment(aboveMarket, direction);
		CancelMismatchedOrders(aboveMarket, comment);

		var (side, orderType) = GetOrderParameters(aboveMarket, direction);

		var existingCount = Orders
		.Where(o => o.Security == Security && o.State == OrderStates.Active && o.Comment != null && o.Comment.Equals(comment, StringComparison.Ordinal))
		.Count(o => o.Side == side && o.Type == orderType);

		for (var index = existingCount; index < OrdersPerSide; index++)
		{
			var offset = pipDistance * (index + 1);
			var price = aboveMarket ? referencePrice + offset : referencePrice - offset;

			if (price <= 0m)
			break;

			var stopLoss = CalculateStopLoss(direction, price);
			var takeProfit = CalculateTakeProfit(direction, price);

			Order order = (side, orderType) switch
			{
			(Sides.Buy, OrderTypes.Limit) => BuyLimit(volume, price, stopLoss: stopLoss, takeProfit: takeProfit),
			(Sides.Buy, OrderTypes.Stop) => BuyStop(volume, price, stopLoss: stopLoss, takeProfit: takeProfit),
			(Sides.Sell, OrderTypes.Limit) => SellLimit(volume, price, stopLoss: stopLoss, takeProfit: takeProfit),
			(Sides.Sell, OrderTypes.Stop) => SellStop(volume, price, stopLoss: stopLoss, takeProfit: takeProfit),
			_ => null,
			};

			if (order == null)
			continue;

			order.Comment = comment;
		}
	}

	private void CancelGrid(bool aboveMarket)
	{
		var prefix = BuildCommentPrefix(aboveMarket);
		foreach (var order in Orders.Where(o => o.Security == Security && o.State == OrderStates.Active && o.Comment != null && o.Comment.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
		CancelOrder(order);
	}

	private void CancelMismatchedOrders(bool aboveMarket, string expectedComment)
	{
		var prefix = BuildCommentPrefix(aboveMarket);
		foreach (var order in Orders.Where(o => o.Security == Security && o.State == OrderStates.Active && o.Comment != null && o.Comment.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
		{
			if (!order.Comment.Equals(expectedComment, StringComparison.Ordinal))
			CancelOrder(order);
		}
	}

	private bool CheckEquityGuard()
	{
		if (!EnableEquityLossProtection)
		return true;

		var startingBalance = _startingBalance;
		if (startingBalance is null || startingBalance <= 0m)
		return true;

		var percent = MaxLossPercent;
		if (percent <= 0m)
		return true;

		var currentEquity = Portfolio?.CurrentValue;
		if (currentEquity is null)
		return true;

		var limitEquity = startingBalance.Value * (1m - percent / 100m);
		if (currentEquity.Value > limitEquity)
		{
			_equityGuardTriggered = false;
			return true;
		}

		if (!_equityGuardTriggered)
		{
			_equityGuardTriggered = true;
			LogInfo($"Equity guard triggered. Current value {currentEquity.Value:0.##}, minimum allowed {limitEquity:0.##}. Closing all positions and pending orders.");
			CloseAllTradesAndOrders();
		}

		return false;
	}

	private void CloseAllTradesAndOrders()
	{
		var position = Position;
		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}

		foreach (var order in Orders.Where(o => o.Security == Security && o.State == OrderStates.Active && o.Comment != null && o.Comment.StartsWith(CommentPrefix, StringComparison.Ordinal)).ToArray())
		CancelOrder(order);
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = CountDecimals(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private void UpdateDistances()
	{
		var point = _pointSize;
		if (point <= 0m)
		{
			point = CalculatePointSize();
			_pointSize = point;
		}

		_pipDistance = PipStep > 0m ? PipStep * point : 0m;
		_takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * point : 0m;
		_stopLossDistance = EnableStopLoss && StopLossPips > 0m ? StopLossPips * point : 0m;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
		var index = text.IndexOf('.');
		return index < 0 ? 0 : text.Length - index - 1;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var minVolume = Security?.VolumeMin ?? 0m;
		var maxVolume = Security?.VolumeMax;
		var step = Security?.VolumeStep ?? 0m;

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
		volume = maxVolume.Value;

		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
		volume = steps * step;
		}

		if (volume <= 0m && minVolume > 0m)
		volume = minVolume;

		return volume > 0m ? volume : 0m;
	}

	private string BuildComment(bool aboveMarket, PendingGridDirection direction)
	{
		return $"{BuildCommentPrefix(aboveMarket)}|{direction}";
	}

	private static string BuildCommentPrefix(bool aboveMarket)
	{
		return $"{CommentPrefix}|{(aboveMarket ? "Above" : "Below")}";
	}

	private (Sides side, OrderTypes type) GetOrderParameters(bool aboveMarket, PendingGridDirection direction)
	{
		return direction switch
		{
			PendingGridDirection.Buy => aboveMarket ? (Sides.Buy, OrderTypes.Stop) : (Sides.Buy, OrderTypes.Limit),
			PendingGridDirection.Sell => aboveMarket ? (Sides.Sell, OrderTypes.Limit) : (Sides.Sell, OrderTypes.Stop),
			_ => (Sides.Buy, OrderTypes.Stop),
		};
	}

	private decimal? CalculateStopLoss(PendingGridDirection direction, decimal entryPrice)
	{
		var distance = _stopLossDistance;
		if (distance <= 0m)
		return null;

		return direction == PendingGridDirection.Buy ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakeProfit(PendingGridDirection direction, decimal entryPrice)
	{
		var distance = _takeProfitDistance;
		if (distance <= 0m)
		return null;

		return direction == PendingGridDirection.Buy ? entryPrice + distance : entryPrice - distance;
	}
}
