using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Fractured Fractals" MetaTrader strategy using high-level StockSharp API.
/// Places stop orders on newly confirmed fractals and trails the stop with the opposite fractal.
/// </summary>
public class FracturedFractalsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _expirationHours;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highBuffer = new();
	private readonly Queue<decimal> _lowBuffer = new();

	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private decimal? _upYoungest;
	private decimal? _upMiddle;
	private decimal? _upOld;
	private decimal? _downYoungest;
	private decimal? _downMiddle;
	private decimal? _downOld;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _shortStopOrder;
	private DateTimeOffset? _buyStopExpiry;
	private DateTimeOffset? _sellStopExpiry;

	private Sides? _openSide;
	private decimal _openVolume;
	private decimal _averageEntryPrice;
	private decimal _pendingTradePnl;
	private int _consecutiveLosses;
	private DateTimeOffset? _lastProfitTime;

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
	/// Pending order lifetime in hours.
	/// </summary>
	public int ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	/// <summary>
	/// Candle type used for fractal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FracturedFractalsStrategy"/> with default parameters.
	/// </summary>
	public FracturedFractalsStrategy()
	{
		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 2m)
		.SetRange(0.0001m, 100m)
		.SetDisplay("Max Risk %", "Maximum risk per trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 10m)
		.SetRange(0m, 1000m)
		.SetDisplay("Decrease Factor", "Loss streak position size dampener", "Risk");

		_expirationHours = Param(nameof(ExpirationHours), 1)
		.SetRange(0, 240)
		.SetDisplay("Expiration", "Pending order lifetime (hours)", "General");

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

		_lastUpFractal = null;
		_lastDownFractal = null;
		_upYoungest = null;
		_upMiddle = null;
		_upOld = null;
		_downYoungest = null;
		_downMiddle = null;
		_downOld = null;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_shortStopOrder = null;
		_buyStopExpiry = null;
		_sellStopExpiry = null;

		_openSide = null;
		_openVolume = 0m;
		_averageEntryPrice = 0m;
		_pendingTradePnl = 0m;
		_consecutiveLosses = 0;
		_lastProfitTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Main candle handler translated from the MT5 tick routine.
		if (candle.State != CandleStates.Finished)
		// Only act on completed bars to mimic MT5 new-bar trigger.
		return;

		// Clean up references to entry and stop orders that changed their state.
		CleanupInactiveOrders();

		_highBuffer.Enqueue(candle.HighPrice);
		_lowBuffer.Enqueue(candle.LowPrice);

		if (_highBuffer.Count > 5)
		_highBuffer.Dequeue();
		if (_lowBuffer.Count > 5)
		_lowBuffer.Dequeue();
		// Maintain a rolling five-bar window required for fractal confirmation.

		if (_highBuffer.Count < 5 || _lowBuffer.Count < 5)
		return;

		DetectFractals();
		// Refresh protective stops with the latest opposite fractal level.
		UpdateTrailingStops();
		// Cancel pending entries if the structure or time invalidates them.
		ValidatePendingOrders(candle.CloseTime);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (TryPlaceBuyStop(candle.CloseTime))
		return;

		TryPlaceSellStop(candle.CloseTime);
	}

	private void DetectFractals()
	{
		// Evaluate the five-bar pattern that defines Williams fractals.
		var highs = _highBuffer.ToArray();
		var lows = _lowBuffer.ToArray();

		decimal? upFractal = null;
		decimal? downFractal = null;

		if (highs[2] > highs[0] && highs[2] > highs[1] && highs[2] > highs[3] && highs[2] > highs[4])
		upFractal = highs[2];

		if (lows[2] < lows[0] && lows[2] < lows[1] && lows[2] < lows[3] && lows[2] < lows[4])
		downFractal = lows[2];

		if (upFractal is decimal up && !AreEqual(_lastUpFractal, up))
		{
			_lastUpFractal = up;
			_upOld = _upMiddle;
			_upMiddle = _upYoungest;
			_upYoungest = up;
		}

		if (downFractal is decimal down && !AreEqual(_lastDownFractal, down))
		{
			_lastDownFractal = down;
			_downOld = _downMiddle;
			_downMiddle = _downYoungest;
			_downYoungest = down;
		}
	}

	private void UpdateTrailingStops()
	{
		// Rebuild protective stop orders using the newest confirmed opposite fractal.
		if (Position > 0 && _downYoungest is decimal longStopPrice)
		{
			var shouldUpdate = _longStopOrder == null ||
			(_longStopOrder.State == OrderStates.Active && longStopPrice > _longStopOrder.Price) ||
			(_longStopOrder.State != OrderStates.Active);

			if (shouldUpdate)
			{
				ReplaceStopOrder(ref _longStopOrder, Sides.Sell, longStopPrice, Math.Abs(Position));
			}
		}
		else if (Position <= 0 && _longStopOrder != null)
		{
			CancelStop(ref _longStopOrder);
		}

		if (Position < 0 && _upYoungest is decimal shortStopPrice)
		{
			var shouldUpdate = _shortStopOrder == null ||
			(_shortStopOrder.State == OrderStates.Active && shortStopPrice < _shortStopOrder.Price) ||
			(_shortStopOrder.State != OrderStates.Active);

			if (shouldUpdate)
			{
				ReplaceStopOrder(ref _shortStopOrder, Sides.Buy, shortStopPrice, Math.Abs(Position));
			}
		}
		else if (Position >= 0 && _shortStopOrder != null)
		{
			CancelStop(ref _shortStopOrder);
		}
	}

	private void ValidatePendingOrders(DateTimeOffset currentTime)
	{
		// Remove stale pending orders when price structure changes or expiration is reached.
		if (IsOrderActive(_buyStopOrder) && _upYoungest is decimal latestUp)
		{
			if (latestUp < _buyStopOrder.Price && !AreEqual(latestUp, _buyStopOrder.Price))
			{
				CancelOrder(_buyStopOrder);
				_buyStopOrder = null;
				_buyStopExpiry = null;
			}
		}

		if (IsOrderActive(_sellStopOrder) && _downYoungest is decimal latestDown)
		{
			if (latestDown > _sellStopOrder.Price && !AreEqual(latestDown, _sellStopOrder.Price))
			{
				CancelOrder(_sellStopOrder);
				_sellStopOrder = null;
				_sellStopExpiry = null;
			}
		}

		if (IsOrderActive(_buyStopOrder) && _buyStopExpiry is DateTimeOffset buyExpiry && currentTime >= buyExpiry)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
			_buyStopExpiry = null;
		}

		if (IsOrderActive(_sellStopOrder) && _sellStopExpiry is DateTimeOffset sellExpiry && currentTime >= sellExpiry)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
			_sellStopExpiry = null;
		}

		if (Position != 0)
		{
			if (IsOrderActive(_buyStopOrder))
			CancelOrder(_buyStopOrder);
			if (IsOrderActive(_sellStopOrder))
			CancelOrder(_sellStopOrder);

			_buyStopOrder = null;
			_sellStopOrder = null;
			_buyStopExpiry = null;
			_sellStopExpiry = null;
		}
	}

	private bool TryPlaceBuyStop(DateTimeOffset time)
	{
		// Deploy a buy stop once the latest upper fractal breaks above the previous one.
		if (Position > 0 || IsOrderActive(_buyStopOrder))
		return false;

		if (_upYoungest is not decimal up || _upMiddle is not decimal middle || _downYoungest is not decimal stop)
		return false;

		if (up <= middle || stop >= up)
		return false;

		var volume = CalculateOrderVolume(up, stop, Sides.Buy);
		if (volume <= 0m)
		return false;

		var order = BuyStop(volume, up);
		if (order == null)
		return false;

		_buyStopOrder = order;
		_buyStopExpiry = ExpirationHours > 0 ? time + TimeSpan.FromHours(ExpirationHours) : null;
		return true;
	}

	private void TryPlaceSellStop(DateTimeOffset time)
	{
		// Deploy a sell stop once the latest lower fractal confirms a breakdown.
		if (Position < 0 || IsOrderActive(_sellStopOrder))
		return;

		if (_downYoungest is not decimal down || _downMiddle is not decimal middle || _upYoungest is not decimal stop)
		return;

		if (down >= middle || stop <= down)
		return;

		var volume = CalculateOrderVolume(down, stop, Sides.Sell);
		if (volume <= 0m)
		return;

		var order = SellStop(volume, down);
		if (order == null)
		return;

		_sellStopOrder = order;
		_sellStopExpiry = ExpirationHours > 0 ? time + TimeSpan.FromHours(ExpirationHours) : null;
	}

	private void ReplaceStopOrder(ref Order target, Sides side, decimal price, decimal volume)
	{
		// Cancel the previous protective order and register an updated one.
		if (volume <= 0m)
		return;

		if (target != null && target.State == OrderStates.Active)
		CancelOrder(target);

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
		{
			_buyStopOrder = null;
			_buyStopExpiry = null;
		}

		if (_sellStopOrder != null && _sellStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		{
			_sellStopOrder = null;
			_sellStopExpiry = null;
		}

		if (_longStopOrder != null && _longStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_longStopOrder = null;

		if (_shortStopOrder != null && _shortStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		_shortStopOrder = null;
	}

	private bool IsOrderActive(Order order)
	{
		return order != null && order.State == OrderStates.Active;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal stopPrice, Sides direction)
	{
		// Risk-based position sizing similar to the original MT5 implementation.
		var security = Security;
		if (security == null)
		return 0m;

		var riskPerUnit = direction == Sides.Buy ? entryPrice - stopPrice : stopPrice - entryPrice;
		if (riskPerUnit <= 0m)
		return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
		portfolioValue = Volume > 0m ? Volume * entryPrice : 0m;

		var riskAmount = portfolioValue * (MaximumRiskPercent / 100m);
		if (riskAmount <= 0m)
		return 0m;

		var volume = riskAmount / riskPerUnit;

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		volume -= volume * (_consecutiveLosses / DecreaseFactor);

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

	private bool AreEqual(decimal? first, decimal second)
	{
		if (first is not decimal value)
		return false;

		var step = Security?.PriceStep ?? 0.00000001m;
		return Math.Abs(value - second) <= step / 2m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelStop(ref _longStopOrder);
			CancelStop(ref _shortStopOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		// Track fills to determine whether the latest cycle ended in profit or loss.
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
		return;

		var tradeSide = trade.Order.Side;
		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;
		var tradeTime = trade.Trade.Time;

		if (_openSide == null)
		{
			_openSide = tradeSide;
			_averageEntryPrice = tradePrice;
			_openVolume = tradeVolume;
			_pendingTradePnl = 0m;
			return;
		}

		if (tradeSide == _openSide)
		{
			var newVolume = _openVolume + tradeVolume;
			if (newVolume > 0m)
			_averageEntryPrice = (_averageEntryPrice * _openVolume + tradePrice * tradeVolume) / newVolume;
			_openVolume = newVolume;
			return;
		}

		var direction = _openSide == Sides.Buy ? 1m : -1m;
		_pendingTradePnl += (tradePrice - _averageEntryPrice) * tradeVolume * direction;
		_openVolume -= tradeVolume;

		if (_openVolume <= 0m)
		{
			if (_pendingTradePnl > 0m)
			{
				_consecutiveLosses = 0;
				_lastProfitTime = tradeTime;
			}
			else if (_pendingTradePnl < 0m)
			{
				_consecutiveLosses++;
			}
			else
			{
				_consecutiveLosses = 0;
			}

			_openSide = null;
			_openVolume = 0m;
			_averageEntryPrice = 0m;
			_pendingTradePnl = 0m;
		}
	}
}
