using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 "Fractured Fractals" expert advisor.
/// Places stop entries on new fractal breakouts and trails protective stops using the previous fractal levels.
/// </summary>
public class FracturedFractalsMql4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highBuffer = new();
	private readonly Queue<decimal> _lowBuffer = new();

	private decimal? _upCurrent;
	private readonly decimal?[] _upHistory = new decimal?[3];

	private decimal? _downCurrent;
	private readonly decimal?[] _downHistory = new decimal?[3];

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _shortStopOrder;

	private decimal? _bestAsk;
	private decimal? _bestBid;

	private Sides? _cycleSide;
	private decimal _cycleVolume;
	private decimal _cycleAveragePrice;
	private decimal _cyclePnl;
	private int _lossStreak;

	/// <summary>
	/// Maximum risk per trade expressed as percentage of portfolio value.
	/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	/// <summary>
	/// Factor that reduces position size after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="FracturedFractalsMql4Strategy"/> parameters.
	/// </summary>
	public FracturedFractalsMql4Strategy()
	{
		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 2m)
		.SetRange(0.0001m, 100m)
		.SetDisplay("Max Risk %", "Maximum risk per trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
		.SetRange(0m, 1000m)
		.SetDisplay("Decrease Factor", "Loss streak position size dampener", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

	_highBuffer.Clear();
	_lowBuffer.Clear();

	_upCurrent = null;
	Array.Clear(_upHistory, 0, _upHistory.Length);

	_downCurrent = null;
	Array.Clear(_downHistory, 0, _downHistory.Length);

	_buyStopOrder = null;
	_sellStopOrder = null;
	_longStopOrder = null;
	_shortStopOrder = null;

	_bestAsk = null;
	_bestBid = null;

	_cycleSide = null;
	_cycleVolume = 0m;
	_cycleAveragePrice = 0m;
	_cyclePnl = 0m;
	_lossStreak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	SubscribeLevel1();

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	// Work with closed bars only, mirroring the original EA update frequency.
	if (candle.State != CandleStates.Finished)
	return;

	CleanupInactiveOrders();

	_highBuffer.Enqueue(candle.HighPrice);
	_lowBuffer.Enqueue(candle.LowPrice);

	if (_highBuffer.Count > 5)
	_highBuffer.Dequeue();

	if (_lowBuffer.Count > 5)
	_lowBuffer.Dequeue();

	if (_highBuffer.Count < 5 || _lowBuffer.Count < 5)
	return;

	DetectUpFractal();
	DetectDownFractal();

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var spread = GetSpread();

	UpdateTrailingStops(spread);
	ValidatePendingOrders(spread);

	if (TryPlaceBuyStop(spread))
	return;

	TryPlaceSellStop(spread);
	}

	private bool DetectUpFractal()
	{
	var highs = _highBuffer.ToArray();
	var center = highs[2];

	if (center <= highs[0] || center <= highs[1] || center <= highs[3] || center <= highs[4])
	return false;

	if (_upCurrent is decimal current && AreEqual(current, center))
	return false;

	if (_upCurrent.HasValue)
	{
	_upHistory[2] = _upHistory[1];
	_upHistory[1] = _upHistory[0];
	_upHistory[0] = _upCurrent;
	}

	_upCurrent = center;
	return true;
	}

	private bool DetectDownFractal()
	{
	var lows = _lowBuffer.ToArray();
	var center = lows[2];

	if (center >= lows[0] || center >= lows[1] || center >= lows[3] || center >= lows[4])
	return false;

	if (_downCurrent is decimal current && AreEqual(current, center))
	return false;

	if (_downCurrent.HasValue)
	{
	_downHistory[2] = _downHistory[1];
	_downHistory[1] = _downHistory[0];
	_downHistory[0] = _downCurrent;
	}

	_downCurrent = center;
	return true;
	}

	private bool TryPlaceBuyStop(decimal spread)
	{
	if (Position > 0 || IsOrderActive(_buyStopOrder))
	return false;

	if (_upCurrent is not decimal up || _upHistory[0] is not decimal prevUp || _downCurrent is not decimal down)
	return false;

	if (up <= prevUp || down >= up)
	return false;

	var entry = NormalizePrice(up + spread);
	var stop = NormalizePrice(down - spread);

	if (entry <= 0m || stop <= 0m || stop >= entry)
	return false;

	var volume = CalculateOrderVolume(entry, stop, Sides.Buy);
	if (volume <= 0m)
	return false;

	var order = BuyStop(volume, entry);
	if (order == null)
	return false;

	_buyStopOrder = order;
	return true;
	}

	private void TryPlaceSellStop(decimal spread)
	{
	if (Position < 0 || IsOrderActive(_sellStopOrder))
	return;

	if (_downCurrent is not decimal down || _downHistory[0] is not decimal prevDown || _upCurrent is not decimal up)
	return;

	if (down >= prevDown || up <= down)
	return;

	var entry = NormalizePrice(down - spread);
	var stop = NormalizePrice(up + spread);

	if (entry <= 0m || stop <= 0m || stop <= entry)
	return;

	var volume = CalculateOrderVolume(entry, stop, Sides.Sell);
	if (volume <= 0m)
	return;

	var order = SellStop(volume, entry);
	if (order == null)
	return;

	_sellStopOrder = order;
	}

	private void UpdateTrailingStops(decimal spread)
	{
	if (Position > 0 && _upHistory[0] is decimal trailUp)
	{
	var newStop = NormalizePrice(trailUp - spread);
	if (newStop > 0m)
	{
	ReplaceStop(ref _longStopOrder, Sides.Sell, newStop, Position);
	}
	}
	else if (Position <= 0)
	{
	CancelStop(ref _longStopOrder);
	}

	if (Position < 0 && _downHistory[0] is decimal trailDown)
	{
	var newStop = NormalizePrice(trailDown + spread);
	if (newStop > 0m)
	{
	ReplaceStop(ref _shortStopOrder, Sides.Buy, newStop, Math.Abs(Position));
	}
	}
	else if (Position >= 0)
	{
	CancelStop(ref _shortStopOrder);
	}
	}

	private void ValidatePendingOrders(decimal spread)
	{
	if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
	{
	if (_upCurrent is not decimal up || _upHistory[0] is not decimal prev || up <= prev)
	{
	CancelOrder(_buyStopOrder);
	_buyStopOrder = null;
	}
	else if (_downCurrent is decimal down)
	{
	var stop = NormalizePrice(down - spread);
	if (stop <= 0m || stop >= _buyStopOrder.Price)
	{
	CancelOrder(_buyStopOrder);
	_buyStopOrder = null;
	}
	}
	}

	if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
	{
	if (_downCurrent is not decimal down || _downHistory[0] is not decimal prev || down >= prev)
	{
	CancelOrder(_sellStopOrder);
	_sellStopOrder = null;
	}
	else if (_upCurrent is decimal up)
	{
	var stop = NormalizePrice(up + spread);
	if (stop <= 0m || stop <= _sellStopOrder.Price)
	{
	CancelOrder(_sellStopOrder);
	_sellStopOrder = null;
	}
	}
	}
	}

	private void ReplaceStop(ref Order target, Sides side, decimal price, decimal volume)
	{
	if (volume <= 0m)
	return;

	if (target != null && target.State == OrderStates.Active)
	{
	if (Math.Abs(target.Price - price) <= (Security?.PriceStep ?? 0.00000001m))
	return;

	CancelOrder(target);
	}

	target = side == Sides.Sell
	? SellStop(volume, price)
	: BuyStop(volume, price);
	}

	private void CancelStop(ref Order order)
	{
	if (order == null)
	return;

	if (order.State == OrderStates.Active)
	CancelOrder(order);

	order = null;
	}

	private void CleanupInactiveOrders()
	{
	if (_buyStopOrder != null && _buyStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
	_buyStopOrder = null;

	if (_sellStopOrder != null && _sellStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
	_sellStopOrder = null;

	if (_longStopOrder != null && _longStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
	_longStopOrder = null;

	if (_shortStopOrder != null && _shortStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
	_shortStopOrder = null;
	}

	private bool IsOrderActive(Order order)
	{
	return order != null && order.State == OrderStates.Active;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal stopPrice, Sides side)
	{
	var security = Security;
	if (security == null)
	return 0m;

	var riskPerUnit = side == Sides.Buy ? entryPrice - stopPrice : stopPrice - entryPrice;
	if (riskPerUnit <= 0m)
	return 0m;

	var portfolioValue = Portfolio?.CurrentValue ?? 0m;
	if (portfolioValue <= 0m)
	{
	portfolioValue = Volume > 0m ? Volume * entryPrice : 0m;
	}

	var riskAmount = portfolioValue * (MaximumRiskPercent / 100m);
	if (riskAmount <= 0m)
	return 0m;

	var volume = riskAmount / riskPerUnit;

	if (DecreaseFactor > 0m && _lossStreak > 1)
	{
	volume -= volume * (_lossStreak / DecreaseFactor);
	}

	if (volume <= 0m)
	return 0m;

	var lotSize = security.LotSize ?? 1m;
	if (lotSize > 0m)
	volume /= lotSize;

	var step = security.VolumeStep ?? 0m;
	if (step > 0m)
	volume = Math.Floor(volume / step) * step;

	var minVolume = security.MinVolume ?? 0m;
	if (minVolume > 0m && volume < minVolume)
	return 0m;

	var maxVolume = security.MaxVolume;
	if (maxVolume.HasValue && volume > maxVolume.Value)
	volume = maxVolume.Value;

	return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
	var security = Security;
	return security?.ShrinkPrice(price) ?? price;
	}

	private decimal GetSpread()
	{
	var ask = _bestAsk ?? Security?.BestAskPrice ?? 0m;
	var bid = _bestBid ?? Security?.BestBidPrice ?? 0m;

	var spread = ask > 0m && bid > 0m ? ask - bid : 0m;
	if (spread <= 0m)
	{
	spread = Security?.PriceStep ?? 0.0001m;
	}

	return spread;
	}

	private bool AreEqual(decimal first, decimal second)
	{
	var step = Security?.PriceStep ?? 0.00000001m;
	return Math.Abs(first - second) <= step / 2m;
	}

	/// <inheritdoc />
	protected override void OnLevel1Received(Security security, Level1ChangeMessage message)
	{
	base.OnLevel1Received(security, message);

	if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
	_bestBid = bid;

	if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
	_bestAsk = ask;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
	base.OnOwnTradeReceived(trade);

	if (trade?.Order == null)
	return;

	var side = trade.Order.Side;
	var volume = trade.Trade.Volume;
	var price = trade.Trade.Price;

	if (_cycleSide == null)
	{
	_cycleSide = side;
	_cycleVolume = volume;
	_cycleAveragePrice = price;
	_cyclePnl = 0m;
	return;
	}

	if (side == _cycleSide)
	{
	var newVolume = _cycleVolume + volume;
	if (newVolume > 0m)
	_cycleAveragePrice = (_cycleAveragePrice * _cycleVolume + price * volume) / newVolume;

	_cycleVolume = newVolume;
	return;
	}

	var direction = _cycleSide == Sides.Buy ? 1m : -1m;
	_cyclePnl += (price - _cycleAveragePrice) * volume * direction;
	_cycleVolume -= volume;

	if (_cycleVolume > 0m)
	return;

	if (_cyclePnl < 0m)
	{
	_lossStreak++;
	}
	else
	{
	_lossStreak = 0;
	}

	_cycleSide = null;
	_cycleVolume = 0m;
	_cycleAveragePrice = 0m;
	_cyclePnl = 0m;
	}
}
