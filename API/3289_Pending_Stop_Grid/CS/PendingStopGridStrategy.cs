using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending stop grid strategy converted from the MetaTrader 4 expert advisor "new.mq4".
/// Places symmetrical ladders of buy stop and sell stop orders with incremental volume scaling.
/// </summary>
public class PendingStopGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _numberOfTrades;
	private readonly StrategyParam<decimal> _distancePips;

	private decimal _pipSize;
	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _volumeValidationFailed;
	private Order?[] _buyStopOrders = Array.Empty<Order?>();
	private Order?[] _sellStopOrders = Array.Empty<Order?>();

	/// <summary>
	/// Take profit distance in pips applied to every pending stop order.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set
		{
			_takeProfitPips.Value = value;
			EnsurePendingOrders();
		}
	}

	/// <summary>
	/// Stop loss distance in pips applied to every pending stop order.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set
		{
			_stopLossPips.Value = value;
			EnsurePendingOrders();
		}
	}

	/// <summary>
	/// Base volume for the first pending order. Subsequent orders scale by the order index.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set
		{
			_baseVolume.Value = value;
			_volumeValidationFailed = false;
			EnsurePendingOrders();
		}
	}

	/// <summary>
	/// Number of buy stop and sell stop orders to keep active simultaneously.
	/// </summary>
	public int NumberOfTrades
	{
		get => _numberOfTrades.Value;
		set
		{
			if (_numberOfTrades.Value == value)
				return;

			_numberOfTrades.Value = value;
			ResizeOrderBuffers();
			EnsurePendingOrders();
		}
	}

	/// <summary>
	/// Distance in pips between the current market price and each stop entry level.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set
		{
			_distancePips.Value = value;
			EnsurePendingOrders();
		}
	}

	/// <summary>
	/// Initializes <see cref="PendingStopGridStrategy"/>.
	/// </summary>
	public PendingStopGridStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance for each order", "Risk")
			.SetCanOptimize();

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance for each order", "Risk")
			.SetCanOptimize();

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Volume of the first pending order", "Trading")
			.SetCanOptimize();

		_numberOfTrades = Param(nameof(NumberOfTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Orders Per Side", "How many buy stop and sell stop orders to keep", "Trading")
			.SetCanOptimize();

		_distancePips = Param(nameof(DistancePips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Distance (pips)", "Offset of each pending order from the market price", "Trading")
			.SetCanOptimize();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
		_volumeValidationFailed = false;
		_buyStopOrders = Array.Empty<Order?>();
		_sellStopOrders = Array.Empty<Order?>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		ResizeOrderBuffers();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null || order.Security != Security)
			return;

		if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
		{
			ClearOrderSlot(order, _buyStopOrders);
			ClearOrderSlot(order, _sellStopOrders);
		}

		EnsurePendingOrders();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1 == null)
			return;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		EnsurePendingOrders();
	}

	private void EnsurePendingOrders()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_volumeValidationFailed)
			return;

		if (NumberOfTrades <= 0)
			return;

		if (_bestBid <= 0m || _bestAsk <= 0m)
			return;

		if (_pipSize <= 0m)
			_pipSize = GetPipSize();

		if (_pipSize <= 0m)
			return;

		if (!ValidateBaseVolume())
		{
			_volumeValidationFailed = true;
			return;
		}

		for (var index = 1; index <= NumberOfTrades; index++)
		{
			var slot = index - 1;
			var volume = RoundVolume(BaseVolume * index);

			if (volume <= 0m)
				continue;

			if (!IsOrderActive(_buyStopOrders, slot))
			{
				var entryPrice = RoundPrice(_bestAsk + DistancePips * index * _pipSize);
				if (entryPrice > 0m)
				{
					var stopLoss = StopLossPips > 0m ? RoundPrice(entryPrice - StopLossPips * _pipSize) : (decimal?)null;
					var takeProfit = TakeProfitPips > 0m ? RoundPrice(entryPrice + TakeProfitPips * _pipSize) : (decimal?)null;
					_buyStopOrders[slot] = BuyStop(volume, entryPrice, stopLoss, takeProfit);
				}
			}

			if (!IsOrderActive(_sellStopOrders, slot))
			{
				var entryPrice = RoundPrice(_bestBid - DistancePips * index * _pipSize);
				if (entryPrice > 0m)
				{
					var stopLoss = StopLossPips > 0m ? RoundPrice(entryPrice + StopLossPips * _pipSize) : (decimal?)null;
					var takeProfit = TakeProfitPips > 0m ? RoundPrice(entryPrice - TakeProfitPips * _pipSize) : (decimal?)null;
					_sellStopOrders[slot] = SellStop(volume, entryPrice, stopLoss, takeProfit);
				}
			}
		}
	}

	private bool ValidateBaseVolume()
	{
		var volumeStep = Security?.VolumeStep ?? 0.01m;
		if (volumeStep <= 0m)
			volumeStep = 0.01m;

		var minVolume = Security?.MinVolume ?? volumeStep;
		var maxVolume = Security?.MaxVolume;

		var roundedBase = Math.Round(BaseVolume / volumeStep, MidpointRounding.AwayFromZero) * volumeStep;

		if (roundedBase < minVolume)
		{
			LogWarning($"Base volume {BaseVolume} is below the minimum allowed {minVolume}.");
			return false;
		}

		if (maxVolume.HasValue && roundedBase > maxVolume.Value)
		{
			LogWarning($"Base volume {BaseVolume} exceeds the maximum allowed {maxVolume.Value}.");
			return false;
		}

		return true;
	}

	private void ResizeOrderBuffers()
	{
		var size = Math.Max(0, NumberOfTrades);
		if (_buyStopOrders.Length != size)
			Array.Resize(ref _buyStopOrders, size);

		if (_sellStopOrders.Length != size)
			Array.Resize(ref _sellStopOrders, size);
	}

	private static bool IsOrderActive(Order?[] orders, int slot)
	{
		if (orders.Length <= slot)
			return false;

		var order = orders[slot];
		if (order == null)
			return false;

		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private void ClearOrderSlot(Order order, Order?[] orders)
	{
		for (var index = 0; index < orders.Length; index++)
		{
			if (orders[index] == order)
			{
				orders[index] = null;
				break;
			}
		}
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0.01m;
		if (step <= 0m)
			step = 0.01m;

		var rounded = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = Security?.MinVolume ?? step;
		if (rounded < minVolume)
			rounded = minVolume;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume.HasValue && rounded > maxVolume.Value)
			rounded = maxVolume.Value;

		return rounded;
	}

	private decimal RoundPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals >= 3)
			return step * 10m;

		return step;
	}
}
