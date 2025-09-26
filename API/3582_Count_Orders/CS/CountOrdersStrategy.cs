using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the MetaTrader "Count Orders" expert by keeping a live tally of open buy and sell orders.
/// It submits three sample market orders at start (two buys followed by one sell) to showcase the counters.
/// </summary>
public class CountOrdersStrategy : Strategy
{
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _waitMilliseconds;

	private readonly HashSet<Order> _activeBuyOrders = new();
	private readonly HashSet<Order> _activeSellOrders = new();

	private CancellationTokenSource? _orderCts;

	/// <summary>
	/// Magic number assigned to the sample orders (mapped to <see cref="Order.UserOrderId"/>).
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trade volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Delay between the sample orders in milliseconds.
	/// </summary>
	public int WaitMilliseconds
	{
		get => _waitMilliseconds.Value;
		set => _waitMilliseconds.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CountOrdersStrategy"/> class.
	/// </summary>
	public CountOrdersStrategy()
	{
		_magicNumber = Param(nameof(MagicNumber), 2556)
			.SetDisplay("Magic Number", "Value copied into Order.UserOrderId for sample trades", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 100)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Protective stop distance expressed in MetaTrader points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400)
			.SetNotNegative()
			.SetDisplay("Take-Profit Points", "Profit target distance expressed in MetaTrader points", "Risk Management");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume submitted by the sample trades", "General");

		_waitMilliseconds = Param(nameof(WaitMilliseconds), 2000)
			.SetNotNegative()
			.SetDisplay("Wait Time (ms)", "Delay between the three sample orders", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeBuyOrders.Clear();
		_activeSellOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var normalizedVolume = NormalizeVolume(TradeVolume);
		if (normalizedVolume <= 0m)
		{
			LogWarning("Sample trades were skipped because the normalized volume is non-positive.");
			return;
		}

		Volume = normalizedVolume;

		_orderCts = new CancellationTokenSource();
		_ = Task.Run(() => SubmitSampleOrdersAsync(normalizedVolume, _orderCts.Token));
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);

		_orderCts?.Cancel();
		_orderCts = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
			return;

		var isActive = order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;

		if (order.Side == Sides.Buy)
		{
			UpdateActiveOrders(_activeBuyOrders, order, isActive);
		}
		else if (order.Side == Sides.Sell)
		{
			UpdateActiveOrders(_activeSellOrders, order, isActive);
		}
	}

	private void UpdateActiveOrders(HashSet<Order> storage, Order order, bool isActive)
	{
		var changed = false;

		if (isActive)
		{
			changed = storage.Add(order);
		}
		else
		{
			changed = storage.Remove(order);
		}

		if (changed)
			PublishOrderSummary();
	}

	private void PublishOrderSummary()
	{
		var total = _activeBuyOrders.Count + _activeSellOrders.Count;
		LogInfo($"Orders total now: {total}. Buys: {_activeBuyOrders.Count}. Sells: {_activeSellOrders.Count}.");
	}

	private decimal NormalizeVolume(decimal requestedVolume)
	{
		var security = Security;
		if (security == null)
			return requestedVolume;

		var volume = requestedVolume;

		if (volume <= 0m)
			return 0m;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(volume / step));
			volume = steps * step;
		}

		var minVolume = security.VolumeMin ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		{
			LogWarning($"Requested volume {requestedVolume} is below the minimum {minVolume}. Using the minimum instead.");
			volume = minVolume;
		}

		var maxVolume = security.VolumeMax ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		{
			LogWarning($"Requested volume {requestedVolume} exceeds the maximum {maxVolume}. Using the maximum instead.");
			volume = maxVolume;
		}

		return volume;
	}

	private async Task SubmitSampleOrdersAsync(decimal volume, CancellationToken token)
	{
		try
		{
			await PlaceMarketOrderAsync(Sides.Buy, volume, token).ConfigureAwait(false);
			await DelayAsync(token).ConfigureAwait(false);
			await PlaceMarketOrderAsync(Sides.Buy, volume, token).ConfigureAwait(false);
			await DelayAsync(token).ConfigureAwait(false);
			await PlaceMarketOrderAsync(Sides.Sell, volume, token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// Cancellation is expected when the strategy stops.
		}
	}

	private async Task PlaceMarketOrderAsync(Sides side, decimal volume, CancellationToken token)
	{
		if (token.IsCancellationRequested || volume <= 0m)
			return;

		Order order;
		if (side == Sides.Buy)
		{
			order = BuyMarket(volume);
		}
		else
		{
			order = SellMarket(volume);
		}

		ApplyMetadata(order);
		ApplyProtections(side, volume);
	}

	private void ApplyMetadata(Order order)
	{
		if (order == null)
			return;

		order.UserOrderId = MagicNumber.ToString();
	}

	private void ApplyProtections(Sides side, decimal volume)
	{
		var price = GetReferencePrice(side);
		if (price <= 0m)
		{
			LogWarning("Protective orders were skipped because no reference price is available yet.");
			return;
		}

		var resultingPosition = side == Sides.Buy
			? Position + volume
			: Position - volume;

		if (TakeProfitPoints > 0)
			SetTakeProfit(TakeProfitPoints, price, resultingPosition);

		if (StopLossPoints > 0)
			SetStopLoss(StopLossPoints, price, resultingPosition);
	}

	private decimal GetReferencePrice(Sides side)
	{
		var security = Security;
		if (security == null)
			return 0m;

		var bid = security.BestBid?.Price ?? 0m;
		var ask = security.BestAsk?.Price ?? 0m;
		var last = security.LastTrade?.Price ?? 0m;

		if (side == Sides.Buy)
		{
			if (ask > 0m)
				return ask;
			if (last > 0m)
				return last;
			return bid;
		}

		if (bid > 0m)
			return bid;
		if (last > 0m)
			return last;
		return ask;
	}

	private async Task DelayAsync(CancellationToken token)
	{
		var delay = WaitMilliseconds;
		if (delay <= 0)
			return;

		try
		{
			await Task.Delay(delay, token).ConfigureAwait(false);
		}
		catch (TaskCanceledException)
		{
			// The delay was cancelled because the strategy stopped.
		}
	}
}
