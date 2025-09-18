using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quotes multiple limit orders around a reference price in a market-making fashion.
/// </summary>
public class BitexOneMarketMakerStrategy : Strategy
{
	private const decimal PriceToleranceRatio = 0.0005m;
	private const decimal VolumeTolerance = 0.0000001m;

	private readonly StrategyParam<decimal> _maxVolumePerLevel;
	private readonly StrategyParam<decimal> _shiftCoefficient;
	private readonly StrategyParam<int> _levelCount;
	private readonly StrategyParam<LeadPriceSource> _priceSource;
	private readonly StrategyParam<Security> _leadSecurityParam;

	private Order[] _buyOrders = Array.Empty<Order>();
	private Order[] _sellOrders = Array.Empty<Order>();

	private decimal _tickSize;
	private decimal _volumeStep;
	private decimal _minVolume;

	private decimal _leadPrice;
	private bool _hasLeadPrice;

	private Security? _leadSecurity;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Maximum volume quoted at each price level.
	/// </summary>
	public decimal MaxVolumePerLevel
	{
		get => _maxVolumePerLevel.Value;
		set => _maxVolumePerLevel.Value = value;
	}

	/// <summary>
	/// Relative distance from the reference price used to build the grid.
	/// </summary>
	public decimal ShiftCoefficient
	{
		get => _shiftCoefficient.Value;
		set => _shiftCoefficient.Value = value;
	}

	/// <summary>
	/// Number of quote levels placed on each side of the book.
	/// </summary>
	public int LevelCount
	{
		get => _levelCount.Value;
		set => _levelCount.Value = value;
	}

	/// <summary>
	/// Source that supplies the reference price for the quoting ladder.
	/// </summary>
	public LeadPriceSource PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Optional security that provides external reference prices.
	/// </summary>
	public Security? LeadSecurity
	{
		get => _leadSecurityParam.Value;
		set => _leadSecurityParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BitexOneMarketMakerStrategy"/>.
	/// </summary>
	public BitexOneMarketMakerStrategy()
	{
		_maxVolumePerLevel = Param(nameof(MaxVolumePerLevel), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume per Level", "Maximum volume that can be quoted at a single price level.", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_shiftCoefficient = Param(nameof(ShiftCoefficient), 0.001m)
		.SetGreaterThanZero()
		.SetDisplay("Shift Coefficient", "Relative displacement from the lead price applied to each level.", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_levelCount = Param(nameof(LevelCount), 1)
		.SetGreaterThanZero()
		.SetDisplay("Level Count", "Number of price levels quoted above and below the reference.", "Orders");

		_priceSource = Param(nameof(PriceSource), LeadPriceSource.MarkPrice)
		.SetDisplay("Price Source", "Defines where the reference quote is taken from.", "General");

		_leadSecurityParam = Param<Security>(nameof(LeadSecurity))
		.SetDisplay("Lead Security", "Instrument that supplies mark or index prices when needed.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
		{
			yield return (Security, DataType.Level1);
			yield return (Security, DataType.MarketDepth);
		}

		if (LeadSecurity != null && LeadSecurity != Security)
		{
			yield return (LeadSecurity, DataType.Level1);
			yield return (LeadSecurity, DataType.MarketDepth);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyOrders = Array.Empty<Order>();
		_sellOrders = Array.Empty<Order>();
		_tickSize = 0m;
		_volumeStep = 0m;
		_minVolume = 0m;
		_leadPrice = 0m;
		_hasLeadPrice = false;
		_leadSecurity = null;
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Strategy security is not specified.");

		if (LevelCount <= 0)
		throw new InvalidOperationException("Level count must be greater than zero.");

		_buyOrders = new Order[LevelCount];
		_sellOrders = new Order[LevelCount];

		_tickSize = Security.PriceStep ?? 0m;
		_volumeStep = Security.VolumeStep ?? 0m;
		_minVolume = Security.MinVolume ?? 0m;

		_leadSecurity = PriceSource == LeadPriceSource.OrderBook ? Security : LeadSecurity ?? Security;

		var initialLeadBid = _leadSecurity?.BestBid?.Price ?? Security.BestBid?.Price;
		if (initialLeadBid is decimal bid && bid > 0m)
		{
			_leadPrice = bid;
			_hasLeadPrice = true;
		}

		SubscribeOrderBook()
		.Bind(ProcessPrimaryOrderBook)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessPrimaryLevel1)
		.Start();

		if (_leadSecurity != null && _leadSecurity != Security)
		{
			SubscribeOrderBook(security: _leadSecurity)
			.Bind(ProcessLeadOrderBook)
			.Start();

			SubscribeLevel1(security: _leadSecurity)
			.Bind(ProcessLeadLevel1)
			.Start();
		}

		ProcessMarketMaking();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelOrders(_buyOrders);
		CancelOrders(_sellOrders);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		ProcessMarketMaking();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		return;

		if (IsStrategyOrder(order))
		ProcessMarketMaking();
	}

	private void ProcessPrimaryOrderBook(IOrderBookMessage depth)
	{
		var bestBid = depth.GetBestBid()?.Price;
		if (bestBid is decimal bid && bid > 0m)
		{
			_bestBid = bid;
			if (_leadSecurity == Security)
			UpdateLeadPrice(bid);
		}

		var bestAsk = depth.GetBestAsk()?.Price;
		if (bestAsk is decimal ask && ask > 0m)
		_bestAsk = ask;

		ProcessMarketMaking();
	}

	private void ProcessPrimaryLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			_bestBid = (decimal)bidValue;
			if (_leadSecurity == Security && _bestBid > 0m)
			UpdateLeadPrice(_bestBid);
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		_bestAsk = (decimal)askValue;

		ProcessMarketMaking();
	}

	private void ProcessLeadOrderBook(IOrderBookMessage depth)
	{
		var bestBid = depth.GetBestBid()?.Price;
		if (bestBid is decimal bid && bid > 0m)
		{
			UpdateLeadPrice(bid);
			ProcessMarketMaking();
		}
	}

	private void ProcessLeadLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				UpdateLeadPrice(bid);
				ProcessMarketMaking();
			}
		}
		else if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var lastValue))
		{
			var last = (decimal)lastValue;
			if (last > 0m)
			{
				UpdateLeadPrice(last);
				ProcessMarketMaking();
			}
		}
	}

	private void UpdateLeadPrice(decimal price)
	{
		_leadPrice = price;
		_hasLeadPrice = price > 0m;
	}

	private void ProcessMarketMaking()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_hasLeadPrice)
		return;

		var leadPrice = _leadPrice;
		if (leadPrice <= 0m)
		return;

		if (MaxVolumePerLevel <= 0m)
		return;

		if (ShiftCoefficient <= 0m)
		return;

		var shiftPrice = leadPrice * ShiftCoefficient;
		if (shiftPrice <= 0m)
		return;

		var totalExposurePerSide = MaxVolumePerLevel * LevelCount;

		var remainingBuy = totalExposurePerSide - Position;
		var remainingSell = totalExposurePerSide + Position;

		for (var level = LevelCount - 1; level >= 0; level--)
		{
			var levelIndex = level + 1;

			var targetBuyPrice = RoundPrice(leadPrice - shiftPrice * levelIndex);
			var targetSellPrice = RoundPrice(leadPrice + shiftPrice * levelIndex);

			var targetBuyVolume = CalculateTargetVolume(remainingBuy);
			remainingBuy -= targetBuyVolume;
			if (remainingBuy < 0m)
			remainingBuy = 0m;

			var targetSellVolume = CalculateTargetVolume(remainingSell);
			remainingSell -= targetSellVolume;
			if (remainingSell < 0m)
			remainingSell = 0m;

			ManageOrder(ref _buyOrders[level], Sides.Buy, targetBuyPrice, targetBuyVolume, leadPrice);
			ManageOrder(ref _sellOrders[level], Sides.Sell, targetSellPrice, targetSellVolume, leadPrice);
		}
	}

	private decimal CalculateTargetVolume(decimal available)
	{
		if (available <= 0m)
		return 0m;

		var desired = Math.Min(available, MaxVolumePerLevel);
		if (desired <= 0m)
		return 0m;

		return RoundVolume(desired);
	}

	private void ManageOrder(ref Order? order, Sides side, decimal price, decimal volume, decimal leadPrice)
	{
		CleanupOrder(ref order);

		if (volume <= 0m || price <= 0m)
		{
			if (order != null && IsOrderActive(order))
			CancelOrder(order);
			return;
		}

		var normalizedPrice = RoundPrice(price);
		var normalizedVolume = RoundVolume(volume);

		if (normalizedVolume <= 0m || normalizedPrice <= 0m)
		{
			if (order != null && IsOrderActive(order))
			CancelOrder(order);
			return;
		}

		if (order == null)
		{
			order = side == Sides.Buy
			? BuyLimit(normalizedVolume, normalizedPrice)
			: SellLimit(normalizedVolume, normalizedPrice);
			return;
		}

		var currentPrice = order.Price ?? normalizedPrice;
		var currentVolume = order.Volume ?? normalizedVolume;

		var priceDiff = leadPrice > 0m
		? Math.Abs(currentPrice - normalizedPrice) / leadPrice
		: Math.Abs(currentPrice - normalizedPrice);

		var volumeDiff = Math.Abs(currentVolume - normalizedVolume);
		var volumeThreshold = _volumeStep > 0m ? _volumeStep / 2m : VolumeTolerance;

		if (priceDiff <= PriceToleranceRatio && volumeDiff <= volumeThreshold)
		return;

		if (IsOrderActive(order))
		{
			CancelOrder(order);
			return;
		}

		order = side == Sides.Buy
		? BuyLimit(normalizedVolume, normalizedPrice)
		: SellLimit(normalizedVolume, normalizedPrice);
	}

	private void CancelOrders(Order[] orders)
	{
		if (orders == null)
		return;

		for (var i = 0; i < orders.Length; i++)
		{
			var order = orders[i];
			if (order == null)
			continue;

			if (IsOrderActive(order))
			CancelOrder(order);

			orders[i] = null;
		}
	}

	private decimal RoundPrice(decimal price)
	{
		if (_tickSize <= 0m)
		return price;

		var steps = Math.Round(price / _tickSize, MidpointRounding.AwayFromZero);
		return steps * _tickSize;
	}

	private decimal RoundVolume(decimal volume)
	{
		var absVolume = Math.Abs(volume);
		if (absVolume <= 0m)
		return 0m;

		if (_volumeStep > 0m)
		{
			var steps = Math.Round(absVolume / _volumeStep, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
			steps = 1m;

			absVolume = steps * _volumeStep;
		}

		if (_minVolume > 0m && absVolume < _minVolume)
		absVolume = _minVolume;

		return absVolume;
	}

	private static void CleanupOrder(ref Order? order)
	{
		if (order == null)
			return;

		switch (order.State)
		{
			case OrderStates.Done:
			case OrderStates.Failed:
			case OrderStates.Canceled:
				order = null;
				break;
		}
	}

	private static bool IsOrderActive(Order? order)
	{
		return order != null && order.State is OrderStates.Active or OrderStates.Pending or OrderStates.Placed or OrderStates.Suspended;
	}

	private bool IsStrategyOrder(Order order)
	{
		if (_buyOrders != null)
		{
			for (var i = 0; i < _buyOrders.Length; i++)
			{
				if (_buyOrders[i] == order)
					return true;
			}
		}

		if (_sellOrders != null)
		{
			for (var i = 0; i < _sellOrders.Length; i++)
			{
				if (_sellOrders[i] == order)
					return true;
			}
		}

		return false;
	}
}

/// <summary>
/// Defines available sources of reference prices for the market-making grid.
/// </summary>
public enum LeadPriceSource
{
	/// <summary>
	/// Use the strategy security order book as the reference.
	/// </summary>
	OrderBook = 1,

	/// <summary>
	/// Use mark prices supplied by an auxiliary instrument.
	/// </summary>
	MarkPrice = 2,

	/// <summary>
	/// Use index prices supplied by an auxiliary instrument.
	/// </summary>
	IndexPrice = 3
}
