using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging-like strategy converted from the MetaTrader 4 "10pips" expert advisor.
/// It maintains both long and short exposure with fixed take profit, stop loss and trailing stop levels.
/// </summary>
public class Fortrader10PipsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitBuy;
	private readonly StrategyParam<decimal> _stopLossBuy;
	private readonly StrategyParam<decimal> _trailingStopBuy;
	private readonly StrategyParam<decimal> _takeProfitSell;
	private readonly StrategyParam<decimal> _stopLossSell;
	private readonly StrategyParam<decimal> _trailingStopSell;
	private readonly StrategyParam<decimal> _volume;

	private Order? _longStopOrder;
	private Order? _longTakeProfitOrder;
	private decimal _longEntryPrice;
	private bool _hasLongPosition;

	private Order? _shortStopOrder;
	private Order? _shortTakeProfitOrder;
	private decimal _shortEntryPrice;
	private bool _hasShortPosition;

	private decimal _priceStep;
	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Buy-side take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitBuy
	{
		get => _takeProfitBuy.Value;
		set => _takeProfitBuy.Value = value;
	}

	/// <summary>
	/// Buy-side stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossBuy
	{
		get => _stopLossBuy.Value;
		set => _stopLossBuy.Value = value;
	}

	/// <summary>
	/// Buy-side trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopBuy
	{
		get => _trailingStopBuy.Value;
		set => _trailingStopBuy.Value = value;
	}

	/// <summary>
	/// Sell-side take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitSell
	{
		get => _takeProfitSell.Value;
		set => _takeProfitSell.Value = value;
	}

	/// <summary>
	/// Sell-side stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossSell
	{
		get => _stopLossSell.Value;
		set => _stopLossSell.Value = value;
	}

	/// <summary>
	/// Sell-side trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopSell
	{
		get => _trailingStopSell.Value;
		set => _trailingStopSell.Value = value;
	}

	/// <summary>
	/// Order volume expressed in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fortrader10PipsStrategy"/> class.
	/// </summary>
	public Fortrader10PipsStrategy()
	{
		_takeProfitBuy = Param(nameof(TakeProfitBuy), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit Buy", "Take profit distance for long positions (points)", "Risk")
			.SetCanOptimize(true);

		_stopLossBuy = Param(nameof(StopLossBuy), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Buy", "Stop loss distance for long positions (points)", "Risk")
			.SetCanOptimize(true);

		_trailingStopBuy = Param(nameof(TrailingStopBuy), 50m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Buy", "Trailing stop distance for long positions (points)", "Risk");

		_takeProfitSell = Param(nameof(TakeProfitSell), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit Sell", "Take profit distance for short positions (points)", "Risk")
			.SetCanOptimize(true);

		_stopLossSell = Param(nameof(StopLossSell), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Sell", "Stop loss distance for short positions (points)", "Risk")
			.SetCanOptimize(true);

		_trailingStopSell = Param(nameof(TrailingStopSell), 50m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Sell", "Trailing stop distance for short positions (points)", "Risk");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_longEntryPrice = 0m;
		_hasLongPosition = false;

		_shortStopOrder = null;
		_shortTakeProfitOrder = null;
		_shortEntryPrice = 0m;
		_hasShortPosition = false;

		_priceStep = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();

		// Immediately open both directions to replicate the original hedging behavior.
		BuyMarket(Volume);
		SellMarket(Volume);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		UpdateTrailingStops();
	}

	private void UpdateTrailingStops()
	{
		if (_priceStep <= 0m)
			return;

		if (_hasLongPosition && TrailingStopBuy > 0m && _bestBid > 0m && IsOrderActive(_longStopOrder))
		{
			var trailingDistance = TrailingStopBuy * _priceStep;
			var desiredStop = _bestBid - trailingDistance;

			if (_bestBid - _longEntryPrice > trailingDistance && NeedReRegister(_longStopOrder!, desiredStop))
			{
				CancelOrder(_longStopOrder!);
				_longStopOrder = SellStop(Volume, NormalizePrice(desiredStop));
			}
		}

		if (_hasShortPosition && TrailingStopSell > 0m && _bestAsk > 0m && IsOrderActive(_shortStopOrder))
		{
			var trailingDistance = TrailingStopSell * _priceStep;
			var desiredStop = _bestAsk + trailingDistance;

			if (_shortEntryPrice - _bestAsk > trailingDistance && NeedReRegister(_shortStopOrder!, desiredStop))
			{
				CancelOrder(_shortStopOrder!);
				_shortStopOrder = BuyStop(Volume, NormalizePrice(desiredStop));
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == _longStopOrder || trade.Order == _longTakeProfitOrder)
		{
			// Long position was closed, immediately re-establish another long leg.
			_hasLongPosition = false;
			_longStopOrder = null;
			_longTakeProfitOrder = null;
			BuyMarket(Volume);
			return;
		}

		if (trade.Order == _shortStopOrder || trade.Order == _shortTakeProfitOrder)
		{
			// Short position was closed, immediately re-establish another short leg.
			_hasShortPosition = false;
			_shortStopOrder = null;
			_shortTakeProfitOrder = null;
			SellMarket(Volume);
			return;
		}

		if (trade.Order.Direction == Sides.Buy)
		{
			// Capture the latest entry price and create protective orders for the long leg.
			_hasLongPosition = true;
			_longEntryPrice = trade.Trade.Price;

			_longStopOrder = StopLossBuy > 0m
				? SellStop(Volume, NormalizePrice(_longEntryPrice - StopLossBuy * _priceStep))
				: null;

			_longTakeProfitOrder = TakeProfitBuy > 0m
				? SellLimit(Volume, NormalizePrice(_longEntryPrice + TakeProfitBuy * _priceStep))
				: null;

			return;
		}

		if (trade.Order.Direction == Sides.Sell)
		{
			// Capture the latest entry price and create protective orders for the short leg.
			_hasShortPosition = true;
			_shortEntryPrice = trade.Trade.Price;

			_shortStopOrder = StopLossSell > 0m
				? BuyStop(Volume, NormalizePrice(_shortEntryPrice + StopLossSell * _priceStep))
				: null;

			_shortTakeProfitOrder = TakeProfitSell > 0m
				? BuyLimit(Volume, NormalizePrice(_shortEntryPrice - TakeProfitSell * _priceStep))
				: null;
		}
	}

	private static bool IsOrderActive(Order? order)
	{
		return order is not null && order.State is not OrderStates.Done and not OrderStates.Failed and not OrderStates.Canceled;
	}

	private bool NeedReRegister(Order order, decimal newPrice)
	{
		if (order.Price is not decimal currentPrice)
			return true;

		var diff = Math.Abs(currentPrice - newPrice);
		if (diff == 0m)
			return false;

		return _priceStep <= 0m || diff >= _priceStep / 2m;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		return Math.Round(price / _priceStep) * _priceStep;
	}
}
