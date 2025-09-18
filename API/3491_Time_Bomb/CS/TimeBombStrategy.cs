using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Converts the "Time bomb" MetaTrader expert advisor to StockSharp.
/// </summary>
public class TimeBombStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sellPipsInTime;
	private readonly StrategyParam<int> _sellTimeToWait;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _sellStopLossPips;
	private readonly StrategyParam<decimal> _sellTakeProfitPips;

	private readonly StrategyParam<decimal> _buyPipsInTime;
	private readonly StrategyParam<int> _buyTimeToWait;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _buyStopLossPips;
	private readonly StrategyParam<decimal> _buyTakeProfitPips;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _sellReferencePrice;
	private DateTimeOffset? _sellReferenceTime;

	private decimal? _buyReferencePrice;
	private DateTimeOffset? _buyReferenceTime;

	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;

	private bool _orderInFlight;
	private bool _pendingShortEntry;
	private bool _pendingLongEntry;
	private bool _closingShortPosition;
	private bool _closingLongPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeBombStrategy"/> class.
	/// </summary>
	public TimeBombStrategy()
	{
		_sellPipsInTime = Param(nameof(SellPipsInTime), 5m)
		.SetDisplay("Sell Pips In Time", "Downward price distance (pips) to trigger a sell", "Sell")
		.SetCanOptimize(true);

		_sellTimeToWait = Param(nameof(SellTimeToWait), 10)
		.SetDisplay("Sell Time Window", "Maximum seconds allowed to reach the sell distance", "Sell")
		.SetCanOptimize(true);

		_sellVolume = Param(nameof(SellVolume), 0.01m)
		.SetDisplay("Sell Volume", "Volume used for sell orders", "Sell")
		.SetCanOptimize(true);

		_sellStopLossPips = Param(nameof(SellStopLossPips), 20m)
		.SetDisplay("Sell Stop Loss (pips)", "Protective stop size for short trades", "Sell")
		.SetCanOptimize(true);

		_sellTakeProfitPips = Param(nameof(SellTakeProfitPips), 20m)
		.SetDisplay("Sell Take Profit (pips)", "Profit target size for short trades", "Sell")
		.SetCanOptimize(true);

		_buyPipsInTime = Param(nameof(BuyPipsInTime), 5m)
		.SetDisplay("Buy Pips In Time", "Upward price distance (pips) to trigger a buy", "Buy")
		.SetCanOptimize(true);

		_buyTimeToWait = Param(nameof(BuyTimeToWait), 10)
		.SetDisplay("Buy Time Window", "Maximum seconds allowed to reach the buy distance", "Buy")
		.SetCanOptimize(true);

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
		.SetDisplay("Buy Volume", "Volume used for buy orders", "Buy")
		.SetCanOptimize(true);

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 20m)
		.SetDisplay("Buy Stop Loss (pips)", "Protective stop size for long trades", "Buy")
		.SetCanOptimize(true);

		_buyTakeProfitPips = Param(nameof(BuyTakeProfitPips), 20m)
		.SetDisplay("Buy Take Profit (pips)", "Profit target size for long trades", "Buy")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Downward distance in pips required to open a sell position.
	/// </summary>
	public decimal SellPipsInTime
	{
		get => _sellPipsInTime.Value;
		set => _sellPipsInTime.Value = value;
	}

	/// <summary>
	/// Maximum number of seconds allowed for the downward move.
	/// </summary>
	public int SellTimeToWait
	{
		get => _sellTimeToWait.Value;
		set => _sellTimeToWait.Value = value;
	}

	/// <summary>
	/// Trade volume for sell orders.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips for short positions.
	/// </summary>
	public decimal SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips for short positions.
	/// </summary>
	public decimal SellTakeProfitPips
	{
		get => _sellTakeProfitPips.Value;
		set => _sellTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Upward distance in pips required to open a buy position.
	/// </summary>
	public decimal BuyPipsInTime
	{
		get => _buyPipsInTime.Value;
		set => _buyPipsInTime.Value = value;
	}

	/// <summary>
	/// Maximum number of seconds allowed for the upward move.
	/// </summary>
	public int BuyTimeToWait
	{
		get => _buyTimeToWait.Value;
		set => _buyTimeToWait.Value = value;
	}

	/// <summary>
	/// Trade volume for buy orders.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips for long positions.
	/// </summary>
	public decimal BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips for long positions.
	/// </summary>
	public decimal BuyTakeProfitPips
	{
		get => _buyTakeProfitPips.Value;
		set => _buyTakeProfitPips.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_bestBid = null;
		_bestAsk = null;
		_sellReferencePrice = null;
		_sellReferenceTime = null;
		_buyReferencePrice = null;
		_buyReferenceTime = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_orderInFlight = false;
		_pendingShortEntry = false;
		_pendingLongEntry = false;
		_closingShortPosition = false;
		_closingLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null || priceStep == 0m)
		return 0.0001m;

		var digits = Security?.Decimals ?? 0;
		var adjust = digits is 3 or 5 ? 10m : 1m;
		return priceStep.Value * adjust;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var time = message.ServerTime != default ? message.ServerTime : CurrentTime;

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		{
			_bestBid = bid;
			ProcessSellTrigger(bid, time);
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		{
			_bestAsk = ask;
			ProcessBuyTrigger(ask, time);
		}

		ManagePosition();
	}

	private void ProcessSellTrigger(decimal bidPrice, DateTimeOffset time)
	{
		if (SellPipsInTime <= 0m || SellTimeToWait <= 0 || _pipSize <= 0m)
		{
			_sellReferencePrice = bidPrice;
			_sellReferenceTime = time;
			return;
		}

		if (_sellReferencePrice is null || _sellReferenceTime is null)
		{
			_sellReferencePrice = bidPrice;
			_sellReferenceTime = time;
			return;
		}

		var elapsed = (time - _sellReferenceTime.Value).TotalSeconds;
		if (elapsed > SellTimeToWait)
		{
			_sellReferencePrice = bidPrice;
			_sellReferenceTime = time;
			return;
		}

		var priceDiff = _sellReferencePrice.Value - bidPrice;
		var pipDiff = ConvertPriceToPips(priceDiff);

		if (pipDiff >= SellPipsInTime && bidPrice < _sellReferencePrice.Value)
		{
			_sellReferencePrice = bidPrice;
			_sellReferenceTime = time;

			TryOpenShort();
		}
	}

	private void ProcessBuyTrigger(decimal askPrice, DateTimeOffset time)
	{
		if (BuyPipsInTime <= 0m || BuyTimeToWait <= 0 || _pipSize <= 0m)
		{
			_buyReferencePrice = askPrice;
			_buyReferenceTime = time;
			return;
		}

		if (_buyReferencePrice is null || _buyReferenceTime is null)
		{
			_buyReferencePrice = askPrice;
			_buyReferenceTime = time;
			return;
		}

		var elapsed = (time - _buyReferenceTime.Value).TotalSeconds;
		if (elapsed > BuyTimeToWait)
		{
			_buyReferencePrice = askPrice;
			_buyReferenceTime = time;
			return;
		}

		var priceDiff = askPrice - _buyReferencePrice.Value;
		var pipDiff = ConvertPriceToPips(priceDiff);

		if (pipDiff >= BuyPipsInTime && askPrice > _buyReferencePrice.Value)
		{
			_buyReferencePrice = askPrice;
			_buyReferenceTime = time;

			TryOpenLong();
		}
	}

	private decimal ConvertPriceToPips(decimal priceDiff)
	{
		if (_pipSize <= 0m)
		return 0m;

		return priceDiff / _pipSize;
	}

	private void TryOpenShort()
	{
		if (_orderInFlight || Position != 0m)
		return;

		if (SellVolume <= 0m)
		return;

		_orderInFlight = true;
		_pendingShortEntry = true;
		SellMarket(SellVolume);
	}

	private void TryOpenLong()
	{
		if (_orderInFlight || Position != 0m)
		return;

		if (BuyVolume <= 0m)
		return;

		_orderInFlight = true;
		_pendingLongEntry = true;
		BuyMarket(BuyVolume);
	}

	private void ManagePosition()
	{
		if (Position > 0m)
		{
			if (_bestBid is not decimal bid)
			return;

			if (_longStopPrice is decimal stop && bid <= stop)
			{
				_orderInFlight = true;
				_closingLongPosition = true;
				SellMarket(Position);
				return;
			}

			if (_longTakeProfitPrice is decimal take && bid >= take)
			{
				_orderInFlight = true;
				_closingLongPosition = true;
				SellMarket(Position);
			}
		}
		else if (Position < 0m)
		{
			if (_bestAsk is decimal ask && _shortStopPrice is decimal stop && ask >= stop)
			{
				_orderInFlight = true;
				_closingShortPosition = true;
				BuyMarket(-Position);
				return;
			}

			if (_bestBid is decimal bid && _shortTakeProfitPrice is decimal take && bid <= take)
			{
				_orderInFlight = true;
				_closingShortPosition = true;
				BuyMarket(-Position);
			}
		}
	}

	private void UpdateShortTargets(decimal? entryPrice)
	{
		if (entryPrice is not decimal price || _pipSize <= 0m)
		{
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
			return;
		}

		_shortStopPrice = SellStopLossPips > 0m ? price + SellStopLossPips * _pipSize : null;
		_shortTakeProfitPrice = SellTakeProfitPips > 0m ? price - SellTakeProfitPips * _pipSize : null;
	}

	private void UpdateLongTargets(decimal? entryPrice)
	{
		if (entryPrice is not decimal price || _pipSize <= 0m)
		{
			_longStopPrice = null;
			_longTakeProfitPrice = null;
			return;
		}

		_longStopPrice = BuyStopLossPips > 0m ? price - BuyStopLossPips * _pipSize : null;
		_longTakeProfitPrice = BuyTakeProfitPips > 0m ? price + BuyTakeProfitPips * _pipSize : null;
	}

	private void ResetShortTargets()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void ResetLongTargets()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			UpdateLongTargets(Position.AveragePrice ?? _bestAsk);
			ResetShortTargets();
		}
		else if (Position < 0m)
		{
			UpdateShortTargets(Position.AveragePrice ?? _bestBid);
			ResetLongTargets();
		}
		else
		{
			ResetLongTargets();
			ResetShortTargets();
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_orderInFlight = false;

		if (_pendingShortEntry && trade.Order?.Direction == Sides.Sell)
		{
			_pendingShortEntry = false;
			UpdateShortTargets(Position.AveragePrice ?? trade.Trade.Price ?? _bestBid);
		}
		else if (_pendingLongEntry && trade.Order?.Direction == Sides.Buy)
		{
			_pendingLongEntry = false;
			UpdateLongTargets(Position.AveragePrice ?? trade.Trade.Price ?? _bestAsk);
		}
		else if (_closingShortPosition && trade.Order?.Direction == Sides.Buy)
		{
			_closingShortPosition = false;
			ResetShortTargets();
		}
		else if (_closingLongPosition && trade.Order?.Direction == Sides.Sell)
		{
			_closingLongPosition = false;
			ResetLongTargets();
		}
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		_orderInFlight = false;

		if (order.Direction == Sides.Sell)
		{
			_pendingShortEntry = false;
			_closingLongPosition = false;
		}
		else
		{
			_pendingLongEntry = false;
			_closingShortPosition = false;
		}
	}
}
