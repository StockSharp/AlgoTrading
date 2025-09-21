using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from the MetaTrader "Stairs" expert.
/// The algorithm maintains symmetric stop orders around the latest fill and
/// closes the basket when accumulated profit exceeds configured thresholds.
/// </summary>
public class StairsStrategy : Strategy
{
	private const decimal VolumeEpsilon = 1e-6m;

	private readonly StrategyParam<int> _channelSteps;
	private readonly StrategyParam<int> _profitSteps;
	private readonly StrategyParam<int> _commonProfitSteps;
	private readonly StrategyParam<bool> _addLots;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _priceStep;
	private decimal _initialVolume;
	private decimal? _lastEntryPrice;
	private decimal _lastEntryVolume;
	private decimal _pendingCloseLongVolume;
	private decimal _pendingCloseShortVolume;
	private bool _closeAllRequested;

	/// <summary>
	/// Distance between pending stop orders expressed in price steps.
	/// </summary>
	public int ChannelSteps
	{
	get => _channelSteps.Value;
	set => _channelSteps.Value = value;
	}

	/// <summary>
	/// Profit threshold in price steps that liquidates the local basket.
	/// </summary>
	public int ProfitSteps
	{
	get => _profitSteps.Value;
	set => _profitSteps.Value = value;
	}

	/// <summary>
	/// Global profit threshold in price steps triggering a full liquidation.
	/// </summary>
	public int CommonProfitSteps
	{
	get => _commonProfitSteps.Value;
	set => _commonProfitSteps.Value = value;
	}

	/// <summary>
	/// When enabled the next pending orders increase their volume by the base lot.
	/// </summary>
	public bool AddLots
	{
	get => _addLots.Value;
	set => _addLots.Value = value;
	}

	/// <summary>
	/// Base volume used for the very first pending orders.
	/// </summary>
	public decimal BaseVolume
	{
	get => _baseVolume.Value;
	set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for trade management.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="StairsStrategy"/> parameters.
	/// </summary>
	public StairsStrategy()
	{
	_channelSteps = Param(nameof(ChannelSteps), 1000)
	.SetGreaterThanZero()
	.SetDisplay("Channel (points)", "Distance between symmetric stop orders", "Grid");

	_profitSteps = Param(nameof(ProfitSteps), 1500)
	.SetGreaterThanZero()
	.SetDisplay("Profit Threshold", "Points required to close the local basket", "Risk");

	_commonProfitSteps = Param(nameof(CommonProfitSteps), 1000)
	.SetGreaterThanZero()
	.SetDisplay("Common Profit", "Global profit target across all directions", "Risk");

	_addLots = Param(nameof(AddLots), true)
	.SetDisplay("Add Lots", "Increase stop order volume after each fill", "Position Sizing");

	_baseVolume = Param(nameof(BaseVolume), 0.1m)
	.SetGreaterThanZero()
	.SetDisplay("Base Volume", "Initial volume submitted to the grid", "Position Sizing");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Primary timeframe used to supervise the grid", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_longEntries.Clear();
	_shortEntries.Clear();
	_priceStep = 0m;
	_initialVolume = 0m;
	_lastEntryPrice = null;
	_lastEntryVolume = 0m;
	_pendingCloseLongVolume = 0m;
	_pendingCloseShortVolume = 0m;
	_closeAllRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_priceStep = Security.PriceStep ?? 0m;
	if (_priceStep <= 0m)
	_priceStep = 1m;

	_initialVolume = NormalizeVolume(BaseVolume);

	SubscribeCandles(CandleType)
	.Bind(ProcessCandle)
	.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var bestBid = Security.BestBid?.Price ?? candle.ClosePrice;
	var bestAsk = Security.BestAsk?.Price ?? candle.ClosePrice;

	if (_closeAllRequested)
	{
		CancelStopOrders();

		if (CloseAllPositions())
		{
			_closeAllRequested = false;
		}

		return;
	}

	UpdatePendingOrders(bestBid, bestAsk);

	var symbolProfit = CalculateProfit(bestBid, bestAsk);
	if (symbolProfit > ProfitSteps)
	{
	_closeAllRequested = true;
	}

	var combinedProfit = CalculateCombinedProfit(bestBid, bestAsk);
	if (combinedProfit > CommonProfitSteps)
	{
	_closeAllRequested = true;
	}

	if (_closeAllRequested)
	{
		CancelStopOrders();

		if (CloseAllPositions())
		{
			_closeAllRequested = false;
		}
	}
	}

	private void UpdatePendingOrders(decimal bestBid, decimal bestAsk)
	{
	var stopOrders = GetActiveStopOrders().ToArray();

	if (_longEntries.Count == 0 && _shortEntries.Count == 0)
	{
	if (!IsStartConfigurationValid(stopOrders))
	{
	CancelOrders(stopOrders);
	PlaceInitialGrid(bestAsk);
	}

	return;
	}

	if (stopOrders.Length >= 2)
	return;

	CancelOrders(stopOrders);

	if (_lastEntryPrice is not decimal lastPrice || _lastEntryVolume <= 0m)
	return;

	var halfChannel = ChannelSteps / 2m;
	if (halfChannel <= 0m)
	return;

	var distanceFromLast = Math.Abs(lastPrice - bestBid) / _priceStep;
	if (distanceFromLast >= halfChannel)
	return;

	var nextVolume = _lastEntryVolume;
	if (AddLots && _initialVolume > 0m)
	{
	nextVolume = NormalizeVolume(nextVolume + _initialVolume);
	}

	PlaceFollowUpGrid(lastPrice, nextVolume);
	}

	private void PlaceInitialGrid(decimal referenceAsk)
	{
	var halfChannel = ChannelSteps / 2m;
	if (halfChannel <= 0m || _priceStep <= 0m)
	return;

	var volume = _initialVolume > 0m ? _initialVolume : NormalizeVolume(BaseVolume);
	if (volume <= 0m)
	return;

	_initialVolume = volume;

	var distance = halfChannel * _priceStep;
	var buyPrice = NormalizePrice(referenceAsk + distance);
	var sellPrice = NormalizePrice(referenceAsk - distance);

	if (buyPrice <= 0m || sellPrice <= 0m)
	return;

	BuyStop(volume, buyPrice);
	SellStop(volume, sellPrice);
	}

	private void PlaceFollowUpGrid(decimal lastPrice, decimal volume)
	{
	if (ChannelSteps <= 0 || _priceStep <= 0m)
	return;

	var normalizedVolume = NormalizeVolume(volume);
	if (normalizedVolume <= 0m)
	return;

	var offset = ChannelSteps * _priceStep;
	var buyPrice = NormalizePrice(lastPrice + offset);
	var sellPrice = NormalizePrice(lastPrice - offset);

	if (buyPrice <= 0m || sellPrice <= 0m)
	return;

	BuyStop(normalizedVolume, buyPrice);
	SellStop(normalizedVolume, sellPrice);
	}

	private IEnumerable<Order> GetActiveStopOrders()
	{
	foreach (var order in Orders)
	{
	if (order.State == OrderStates.Active && order.Type == OrderTypes.Stop)
	yield return order;
	}
	}

	private void CancelOrders(IEnumerable<Order> orders)
	{
	foreach (var order in orders)
	{
	if (order.State.IsActive())
	CancelOrder(order);
	}
	}

	private bool IsStartConfigurationValid(IReadOnlyCollection<Order> stopOrders)
	{
	if (stopOrders.Count < 2)
	return false;

	var buyOrder = stopOrders.FirstOrDefault(o => o.Direction == Sides.Buy);
	var sellOrder = stopOrders.FirstOrDefault(o => o.Direction == Sides.Sell);

	if (buyOrder?.Price is not decimal buyPrice || sellOrder?.Price is not decimal sellPrice)
	return false;

	var distanceSteps = Math.Abs(buyPrice - sellPrice) / _priceStep;
	var minDistance = ChannelSteps * 0.5m;
	var maxDistance = ChannelSteps * 1.5m;

	return distanceSteps > minDistance && distanceSteps < maxDistance;
	}

	private decimal CalculateProfit(decimal bestBid, decimal bestAsk)
	{
	if (_priceStep <= 0m)
	return 0m;

	decimal profit = 0m;

	foreach (var entry in _longEntries)
	{
	profit += (bestBid - entry.Price) / _priceStep;
	}

	foreach (var entry in _shortEntries)
	{
	profit += (entry.Price - bestAsk) / _priceStep;
	}

	return profit;
	}

	private decimal CalculateCombinedProfit(decimal bestBid, decimal bestAsk)
	{
	return CalculateProfit(bestBid, bestAsk);
	}

	private bool CloseAllPositions()
	{
	var hasLongs = _longEntries.Count > 0;
	var hasShorts = _shortEntries.Count > 0;

	if (hasLongs && _pendingCloseLongVolume <= VolumeEpsilon)
	{
	var longVolume = NormalizeVolume(_longEntries.Sum(e => e.Volume));
	if (longVolume > VolumeEpsilon)
	{
	SellMarket(longVolume);
	_pendingCloseLongVolume = longVolume;
	}
	}

	if (hasShorts && _pendingCloseShortVolume <= VolumeEpsilon)
	{
	var shortVolume = NormalizeVolume(_shortEntries.Sum(e => e.Volume));
	if (shortVolume > VolumeEpsilon)
	{
	BuyMarket(shortVolume);
	_pendingCloseShortVolume = shortVolume;
	}
	}

	var closingLongs = hasLongs || _pendingCloseLongVolume > VolumeEpsilon;
	var closingShorts = hasShorts || _pendingCloseShortVolume > VolumeEpsilon;

	return !closingLongs && !closingShorts;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
	base.OnOwnTradeReceived(trade);

	if (trade.Order == null)
	return;

	var direction = trade.Order.Side;
	var price = trade.Trade.Price;
	var volume = trade.Trade.Volume;

	if (direction == Sides.Buy)
	{
	if (_pendingCloseShortVolume > VolumeEpsilon)
	{
	ReduceEntries(_shortEntries, volume);
	_pendingCloseShortVolume = Math.Max(0m, _pendingCloseShortVolume - volume);
	}
	else
	{
	_lastEntryPrice = price;
	_lastEntryVolume = volume;
	_longEntries.Add(new PositionEntry(volume, price));
	}
	}
	else if (direction == Sides.Sell)
	{
	if (_pendingCloseLongVolume > VolumeEpsilon)
	{
	ReduceEntries(_longEntries, volume);
	_pendingCloseLongVolume = Math.Max(0m, _pendingCloseLongVolume - volume);
	}
	else
	{
	_lastEntryPrice = price;
	_lastEntryVolume = volume;
	_shortEntries.Add(new PositionEntry(volume, price));
	}
	}

	if (_longEntries.Count == 0 && _shortEntries.Count == 0)
	{
	_lastEntryPrice = null;
	_lastEntryVolume = 0m;
	}
	}

	private static void ReduceEntries(List<PositionEntry> entries, decimal volume)
	{
	var remaining = volume;

	for (var i = 0; i < entries.Count && remaining > VolumeEpsilon;)
	{
	var entry = entries[i];

	if (entry.Volume <= remaining + VolumeEpsilon)
	{
	remaining -= entry.Volume;
	entries.RemoveAt(i);
	}
	else
	{
	entries[i] = entry with { Volume = entry.Volume - remaining };
	remaining = 0m;
	}
	}
	}

	private void CancelStopOrders()
	{
	CancelOrders(GetActiveStopOrders());
	}

	private sealed record PositionEntry(decimal Volume, decimal Price);
}
