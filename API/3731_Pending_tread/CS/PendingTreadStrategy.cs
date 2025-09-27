using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending grid strategy converted from the MetaTrader 4 expert advisor "Pending_tread".
/// Maintains two independent ladders of pending orders above and below the market with configurable direction and spacing.
/// </summary>
public class PendingTreadStrategy : Strategy
{
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _ordersPerSide;
	private readonly StrategyParam<decimal> _minStopDistancePoints;
	private readonly StrategyParam<decimal> _throttleSeconds;
	private readonly StrategyParam<Sides> _aboveMarketSide;
	private readonly StrategyParam<Sides> _belowMarketSide;
	private readonly StrategyParam<int> _slippagePoints;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pipSize;
	private decimal _pointValue;
	private decimal _minStopOffset;
	private TimeSpan _throttleInterval;
	private DateTimeOffset? _lastMaintenanceTime;

	/// <summary>
	/// Initializes a new instance of the strategy with defaults that mirror the MQL inputs.
	/// </summary>
	public PendingTreadStrategy()
	{
		_pipStep = Param(nameof(PipStep), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Grid step (pips)", "Distance between adjacent pending orders expressed in pips", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Individual take-profit distance assigned to every pending order", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume sent with each pending order", "Trading");

		_ordersPerSide = Param(nameof(OrdersPerSide), 10)
			.SetGreaterThanZero()
			.SetDisplay("Orders per side", "Maximum number of active pending orders maintained above and below the market", "Trading");

		_minStopDistancePoints = Param(nameof(MinStopDistancePoints), 0m)
			.SetDisplay("Min stop distance (points)", "Broker stop-level distance in raw price points (MODE_STOPLEVEL analogue)", "Risk");

		_throttleSeconds = Param(nameof(ThrottleSeconds), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Throttle (seconds)", "Minimum delay between maintenance cycles to avoid trade context congestion", "Execution");

		_aboveMarketSide = Param(nameof(AboveMarketSide), Sides.Buy)
			.SetDisplay("Above market side", "Type of orders stacked above the current price", "Orders");

		_belowMarketSide = Param(nameof(BelowMarketSide), Sides.Sell)
			.SetDisplay("Below market side", "Type of orders stacked below the current price", "Orders");

		_slippagePoints = Param(nameof(SlippagePoints), 3)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Slippage (points)", "Retained for parity with the MT4 input; pending orders ignore slippage in StockSharp", "Execution");
	}

	/// <summary>
	/// Grid spacing expressed in pips.
	/// </summary>
	public decimal PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	/// <summary>
	/// Take-profit distance assigned to each pending order.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Volume sent with each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of orders maintained above and below the market.
	/// </summary>
	public int OrdersPerSide
	{
		get => _ordersPerSide.Value;
		set => _ordersPerSide.Value = value;
	}

	/// <summary>
	/// Minimal allowed distance between the market price and pending orders expressed in price points.
	/// </summary>
	public decimal MinStopDistancePoints
	{
		get => _minStopDistancePoints.Value;
		set => _minStopDistancePoints.Value = value;
	}

	/// <summary>
	/// Delay between maintenance cycles expressed in seconds.
	/// </summary>
	public decimal ThrottleSeconds
	{
		get => _throttleSeconds.Value;
		set => _throttleSeconds.Value = value;
	}

	/// <summary>
	/// Direction used for orders stacked above the current price.
	/// </summary>
	public Sides AboveMarketSide
	{
		get => _aboveMarketSide.Value;
		set => _aboveMarketSide.Value = value;
	}

	/// <summary>
	/// Direction used for orders stacked below the current price.
	/// </summary>
	public Sides BelowMarketSide
	{
		get => _belowMarketSide.Value;
		set => _belowMarketSide.Value = value;
	}

	/// <summary>
	/// MetaTrader slippage input maintained for documentation purposes.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
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

		_bestBid = null;
		_bestAsk = null;
		_pipSize = 0m;
		_pointValue = 0m;
		_minStopOffset = 0m;
		_throttleInterval = TimeSpan.Zero;
		_lastMaintenanceTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0.0001m;
		if (_pointValue <= 0m)
		{
			_pointValue = 0.0001m;
		}

		_pipSize = GetPipSize();
		_minStopOffset = MinStopDistancePoints > 0m ? MinStopDistancePoints * _pointValue : 0m;
		_throttleInterval = TimeSpan.FromSeconds((double)ThrottleSeconds);

		this.LogInfo($"Pending_tread grid initialized. Pip size={_pipSize}, point={_pointValue}, throttle={_throttleInterval}.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		{
			_bestBid = bid;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		{
			_bestAsk = ask;
		}

		if (_bestBid is null || _bestAsk is null)
			return;

		var now = level1.ServerTime != default ? level1.ServerTime : CurrentTime ?? DateTimeOffset.UtcNow;
		if (_lastMaintenanceTime is DateTimeOffset last && now - last < _throttleInterval)
			return;

		_lastMaintenanceTime = now;

		MaintainPendingGrid(true, AboveMarketSide);
		MaintainPendingGrid(false, BelowMarketSide);
	}

	private void MaintainPendingGrid(bool aboveMarket, Sides side)
	{
		var security = Security;
		if (security == null)
			return;

		if (side != Sides.Buy && side != Sides.Sell)
			return;

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
			return;

		var distance = PipStep * _pipSize;
		if (distance <= 0m)
			return;

		var takeProfitOffset = TakeProfitPips * _pipSize;
		var bestBid = _bestBid;
		var bestAsk = _bestAsk;

		if (bestBid is null || bestAsk is null)
			return;

		var expectedType = GetOrderType(aboveMarket, side);
		var existingCount = 0;

		foreach (var order in Orders)
		{
			if (order == null || order.Security != security)
				continue;

			if (!order.State.IsActive())
				continue;

			if (order.Direction != side)
				continue;

			if (order.Type != expectedType)
				continue;

			existingCount++;
		}

		for (var index = existingCount; index < OrdersPerSide; index++)
		{
			var offset = distance * (index + 1);
			var orderPrice = CalculateOrderPrice(aboveMarket, side, bestBid.Value, bestAsk.Value, offset);
			if (orderPrice <= 0m)
				continue;

			var anchorPrice = aboveMarket ? (side == Sides.Buy ? bestAsk.Value : bestBid.Value) : (side == Sides.Buy ? bestAsk.Value : bestBid.Value);
			if (_minStopOffset > 0m && Math.Abs(orderPrice - anchorPrice) < _minStopOffset)
			{
				this.LogWarning($"Skipping order too close to market. Side={side}, price={orderPrice}, anchor={anchorPrice}.");
				continue;
			}

			var normalizedPrice = NormalizePrice(orderPrice);
			var takeProfit = takeProfitOffset > 0m ? NormalizePrice(GetTakeProfitPrice(side, normalizedPrice, takeProfitOffset)) : (decimal?)null;

			var order = PlacePendingOrder(aboveMarket, side, volume, normalizedPrice, takeProfit);
			if (order != null)
			{
				this.LogInfo($"Pending order placed -> Side={side}, Type={order.Type}, Price={normalizedPrice}, TP={(takeProfit.HasValue ? takeProfit.Value.ToString() : "none")}");
			}
		}
	}

	private static OrderTypes GetOrderType(bool aboveMarket, Sides side)
	{
		if (aboveMarket)
			return side == Sides.Buy ? OrderTypes.Stop : OrderTypes.Limit;

		return side == Sides.Buy ? OrderTypes.Limit : OrderTypes.Stop;
	}

	private decimal CalculateOrderPrice(bool aboveMarket, Sides side, decimal bid, decimal ask, decimal offset)
	{
		if (aboveMarket)
		{
			if (side == Sides.Buy)
				return ask + offset;

			return bid + offset;
		}

		if (side == Sides.Buy)
			return ask - offset;

		return bid - offset;
	}

	private decimal GetTakeProfitPrice(Sides side, decimal orderPrice, decimal offset)
	{
		return side == Sides.Buy ? orderPrice + offset : orderPrice - offset;
	}

	private Order PlacePendingOrder(bool aboveMarket, Sides side, decimal volume, decimal price, decimal? takeProfit)
	{
		if (aboveMarket)
		{
			if (side == Sides.Buy)
				return BuyStop(volume, price, null, takeProfit);

			return SellLimit(volume, price, null, takeProfit);
		}

		if (side == Sides.Buy)
			return BuyLimit(volume, price, null, takeProfit);

		return SellStop(volume, price, null, takeProfit);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = decimal.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals ?? 0;

		if (decimals >= 4)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}
}