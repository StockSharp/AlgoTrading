
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Side of the lead order book used to derive the quoting price.
/// </summary>
public enum LeadPriceSide
{
	Bid,
	Ask,
	Midpoint,
}

/// <summary>
/// Strategy that replicates the BITEX.ONE market maker behaviour from MetaTrader 5.
/// It continuously places symmetric limit order ladders around a reference price and
/// keeps the total inventory close to the desired position.
/// </summary>
public class BitexOneMarketMakerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxVolumePerLevel;
	private readonly StrategyParam<decimal> _priceShift;
	private readonly StrategyParam<decimal> _verticalShift;
	private readonly StrategyParam<int> _levelCount;
	private readonly StrategyParam<decimal> _desiredPosition;
	private readonly StrategyParam<string> _leadSecurityId;
	private readonly StrategyParam<LeadPriceSide> _leadPriceSide;

	private Security? _leadSecurity;
	private QuoteChange? _leadBid;
	private QuoteChange? _leadAsk;
	private QuoteChange? _localBid;
	private QuoteChange? _localAsk;
	private Order?[] _buyOrders = Array.Empty<Order?>();
	private Order?[] _sellOrders = Array.Empty<Order?>();
	private decimal _minTradableVolume;
	private decimal _volumeStep;

	/// <summary>
	/// Initialize <see cref="BitexOneMarketMakerStrategy"/>.
	/// </summary>
	public BitexOneMarketMakerStrategy()
	{
		_maxVolumePerLevel = Param(nameof(MaxVolumePerLevel), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume Per Level", "Maximum volume exposed on each quoting level.", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 5m, 0.01m);

		_priceShift = Param(nameof(PriceShift), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Price Shift", "Horizontal shift coefficient applied to the lead price for each level.", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.01m, 0.0001m);

		_verticalShift = Param(nameof(VerticalShift), 0m)
			.SetDisplay("Vertical Shift", "Constant offset applied to the lead price for both sides.", "General");

		_levelCount = Param(nameof(LevelCount), 1)
			.SetGreaterThanZero()
			.SetDisplay("Level Count", "Number of quoting levels maintained on each side.", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_desiredPosition = Param(nameof(DesiredPosition), 0m)
			.SetDisplay("Desired Position", "Inventory target that shifts the exposure balance.", "Risk");

		_leadSecurityId = Param(nameof(LeadSecurityId), string.Empty)
			.SetDisplay("Lead Security Id", "Optional security identifier that provides the reference price.", "General");

		_leadPriceSide = Param(nameof(LeadPriceSide), LeadPriceSide.Bid)
			.SetDisplay("Lead Price Side", "Order book side used as reference for quoting levels.", "General");
	}

	/// <summary>
	/// Maximum volume per level.
	/// </summary>
	public decimal MaxVolumePerLevel
	{
		get => _maxVolumePerLevel.Value;
		set => _maxVolumePerLevel.Value = value;
	}

	/// <summary>
	/// Relative horizontal shift coefficient.
	/// </summary>
	public decimal PriceShift
	{
		get => _priceShift.Value;
		set => _priceShift.Value = value;
	}

	/// <summary>
	/// Additional vertical offset applied to both sides.
	/// </summary>
	public decimal VerticalShift
	{
		get => _verticalShift.Value;
		set => _verticalShift.Value = value;
	}

	/// <summary>
	/// Number of quoting levels on each side.
	/// </summary>
	public int LevelCount
	{
		get => _levelCount.Value;
		set
		{
			_levelCount.Value = value;
			ResizeOrderBuffers();
		}
	}

	/// <summary>
	/// Inventory target expressed in strategy volume units.
	/// </summary>
	public decimal DesiredPosition
	{
		get => _desiredPosition.Value;
		set => _desiredPosition.Value = value;
	}

	/// <summary>
	/// Identifier of the security that provides the reference price.
	/// Leave empty to use the traded instrument.
	/// </summary>
	public string LeadSecurityId
	{
		get => _leadSecurityId.Value;
		set
		{
			_leadSecurityId.Value = value ?? string.Empty;
			_leadSecurity = null;
		}
	}

	/// <summary>
	/// Order book side used as price anchor.
	/// </summary>
	public LeadPriceSide LeadPriceSide
	{
		get => _leadPriceSide.Value;
		set => _leadPriceSide.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security != null)
			yield return (security, DataType.MarketDepth);

		var lead = ResolveLeadSecurity(false);
		if (lead != null && !ReferenceEquals(lead, security))
			yield return (lead, DataType.MarketDepth);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_leadSecurity = null;
		_leadBid = null;
		_leadAsk = null;
		_localBid = null;
		_localAsk = null;
		_buyOrders = Array.Empty<Order?>();
		_sellOrders = Array.Empty<Order?>();
		_minTradableVolume = 0m;
		_volumeStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not configured.");

		ResizeOrderBuffers();

		_minTradableVolume = DetermineMinimalVolume(security);
		_volumeStep = security.VolumeStep ?? 0m;

		StartProtection();

		SubscribeOrderBook()
			.Bind(ProcessLocalOrderBook)
			.Start();

		var lead = ResolveLeadSecurity(true);
		if (lead != null && !ReferenceEquals(lead, security))
		{
			SubscribeOrderBook(lead)
				.Bind(ProcessLeadOrderBook)
				.Start();
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State == OrderStates.Done 		 order.State == OrderStates.Failed 		 order.State == OrderStates.Canceled)
		{
			ClearSlot(order, _buyOrders);
			ClearSlot(order, _sellOrders);
			MaintainQuotes();
		}
	}

	private void ProcessLocalOrderBook(QuoteChangeMessage depth)
	{
		UpdateBestQuotes(depth, ref _localBid, ref _localAsk);

		if (_leadSecurity == null 		 ReferenceEquals(_leadSecurity, Security))
		{
			_leadBid = _localBid;
			_leadAsk = _localAsk;
		}

		MaintainQuotes();
	}

	private void ProcessLeadOrderBook(QuoteChangeMessage depth)
	{
		UpdateBestQuotes(depth, ref _leadBid, ref _leadAsk);
		MaintainQuotes();
	}

	private void MaintainQuotes()
	{
		CleanupFinishedOrders();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var leadPrice = GetLeadPrice();
		if (leadPrice == null 		 leadPrice <= 0m)
			return;

		var security = Security;
		if (security == null)
			return;

		var levelCount = Math.Max(0, LevelCount);
		if (levelCount == 0)
		{
			CancelAllManagedOrders();
			return;
		}

		var maxVolume = MaxVolumePerLevel;
		if (maxVolume <= 0m)
		{
			CancelAllManagedOrders();
			return;
		}

		var shiftBase = PriceShift * leadPrice.Value;
		var verticalShift = VerticalShift * leadPrice.Value;

		var desiredBuy = maxVolume * levelCount - Position + DesiredPosition;
		var desiredSell = maxVolume * levelCount + Position - DesiredPosition;

		var buyRemaining = Math.Max(0m, desiredBuy);
		var sellRemaining = Math.Max(0m, desiredSell);

		for (var level = levelCount - 1; level >= 0; level--)
		{
			var stepIndex = level + 1;
			var buyPrice = NormalizePrice(leadPrice.Value - shiftBase * stepIndex + verticalShift);
			var sellPrice = NormalizePrice(leadPrice.Value + shiftBase * stepIndex + verticalShift);

			var buyVolume = PrepareLevelVolume(ref buyRemaining, maxVolume);
			ref var buySlot = ref _buyOrders[level];
			MaintainOrder(ref buySlot, true, buyPrice, buyVolume);

			var sellVolume = PrepareLevelVolume(ref sellRemaining, maxVolume);
			ref var sellSlot = ref _sellOrders[level];
			MaintainOrder(ref sellSlot, false, sellPrice, sellVolume);
		}
	}

	private decimal PrepareLevelVolume(ref decimal remaining, decimal cap)
	{
		if (remaining <= 0m)
			return 0m;

		if (_minTradableVolume > 0m && remaining < _minTradableVolume)
			return 0m;

		var candidate = Math.Min(remaining, cap);

		if (_minTradableVolume > 0m)
			candidate = Math.Max(candidate, _minTradableVolume);

		var normalized = NormalizeVolume(candidate);
		if (normalized <= 0m)
			return 0m;

		if (normalized > remaining)
		{
			if (_minTradableVolume > 0m && remaining < _minTradableVolume * 1.5m)
			{
				return 0m;
			}

			normalized = remaining;
		}

		remaining -= normalized;
		return normalized;
	}

	private void MaintainOrder(ref Order? slot, bool isBuy, decimal price, decimal volume)
	{
		if (volume <= 0m)
		{
			CancelManagedOrder(slot);
			slot = null;
			return;
		}

		var order = slot;
		if (order == null)
		{
			slot = isBuy ? BuyLimit(volume, price) : SellLimit(volume, price);
			return;
		}

		var state = order.State;
		if (state != OrderStates.Active && state != OrderStates.Pending)
		{
			slot = isBuy ? BuyLimit(volume, price) : SellLimit(volume, price);
			return;
		}

		if (NeedReRegister(order, price) 		 NeedVolumeUpdate(order, volume))
		{
			CancelOrder(order);
			slot = null;
		}
	}

	private void CleanupFinishedOrders()
	{
		for (var i = 0; i < _buyOrders.Length; i++)
			CleanupOrder(ref _buyOrders[i]);

		for (var i = 0; i < _sellOrders.Length; i++)
			CleanupOrder(ref _sellOrders[i]);
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

	private void CancelAllManagedOrders()
	{
		for (var i = 0; i < _buyOrders.Length; i++)
		{
			CancelManagedOrder(_buyOrders[i]);
			_buyOrders[i] = null;
		}

		for (var i = 0; i < _sellOrders.Length; i++)
		{
			CancelManagedOrder(_sellOrders[i]);
			_sellOrders[i] = null;
		}
	}

	private void CancelManagedOrder(Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active 		 order.State == OrderStates.Pending)
			CancelOrder(order);
	}

	private bool NeedReRegister(Order order, decimal newPrice)
	{
		if (order.Price is not decimal existingPrice)
			return true;

		var diff = Math.Abs(existingPrice - newPrice);
		if (diff == 0m)
			return false;

		var step = Security?.PriceStep;
		if (step is null 		 step == 0m)
			return true;

		return diff >= step.Value / 2m;
	}

	private bool NeedVolumeUpdate(Order order, decimal newVolume)
	{
		if (order.Volume is not decimal current)
			return true;

		var diff = Math.Abs(current - newVolume);
		if (diff == 0m)
			return false;

		if (_volumeStep > 0m)
			return diff >= _volumeStep / 2m;

		return true;
	}

	private decimal? GetLeadPrice()
	{
		return LeadPriceSide switch
		{
			LeadPriceSide.Ask => _leadAsk?.Price,
			LeadPriceSide.Midpoint => CalculateMidpoint(),
			_ => _leadBid?.Price,
		};
	}

	private decimal? CalculateMidpoint()
	{
		if (_leadBid is { } bid && _leadAsk is { } ask)
			return (bid.Price + ask.Price) / 2m;

		return null;
	}

	private void ResizeOrderBuffers()
	{
		var size = Math.Max(0, LevelCount);
		if (_buyOrders.Length == size && _sellOrders.Length == size)
			return;

		if (_buyOrders.Length > size)
		{
			for (var i = size; i < _buyOrders.Length; i++)
				CancelManagedOrder(_buyOrders[i]);
		}

		if (_sellOrders.Length > size)
		{
			for (var i = size; i < _sellOrders.Length; i++)
				CancelManagedOrder(_sellOrders[i]);
		}

		Array.Resize(ref _buyOrders, size);
		Array.Resize(ref _sellOrders, size);
	}

	private void UpdateBestQuotes(QuoteChangeMessage depth, ref QuoteChange? bid, ref QuoteChange? ask)
	{
		var newBid = depth.GetBestBid();
		if (newBid != null)
			bid = newBid;

		var newAsk = depth.GetBestAsk();
		if (newAsk != null)
			ask = newAsk;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is { } s && s > 0m)
		{
			var steps = Math.Round(price / s, MidpointRounding.AwayFromZero);
			return steps * s;
		}

		return price;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step is { } s && s > 0m)
		{
			var steps = Math.Floor(volume / s);
			if (steps <= 0m)
				steps = 1m;
			volume = steps * s;
		}

		var min = security.MinVolume;
		if (min is { } m && m > 0m && volume < m)
			volume = m;

		return volume;
	}

	private static decimal DetermineMinimalVolume(Security security)
	{
		if (security.MinVolume is { } min && min > 0m)
			return min;

		if (security.VolumeStep is { } step && step > 0m)
			return step;

		return 0m;
	}

	private Security? ResolveLeadSecurity(bool throwOnError)
	{
		if (_leadSecurity != null)
			return _leadSecurity;

		if (string.IsNullOrWhiteSpace(LeadSecurityId))
		{
			_leadSecurity = Security;
			return _leadSecurity;
		}

		var security = this.GetSecurity(LeadSecurityId);
		if (security == null && throwOnError)
			throw new InvalidOperationException($"Lead security '{LeadSecurityId}' could not be resolved.");

		_leadSecurity = security ?? Security;
		return _leadSecurity;
	}

	private static void ClearSlot(Order order, Order?[] slots)
	{
		for (var i = 0; i < slots.Length; i++)
		{
			if (ReferenceEquals(slots[i], order))
			{
				slots[i] = null;
				break;
			}
		}
	}
}
