using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging strategy opening simultaneous long and short positions with fixed take profit,
/// stop loss and trailing stop.
/// </summary>
public class TenPipsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitBuy;
	private readonly StrategyParam<decimal> _stopLossBuy;
	private readonly StrategyParam<decimal> _trailingStopBuy;
	private readonly StrategyParam<decimal> _takeProfitSell;
	private readonly StrategyParam<decimal> _stopLossSell;
	private readonly StrategyParam<decimal> _trailingStopSell;
	private readonly StrategyParam<decimal> _volume;

	private Order _longStop, _longTake;
	private decimal _longEntryPrice;
	private bool _hasLong;

	private Order _shortStop, _shortTake;
	private decimal _shortEntryPrice;
	private bool _hasShort;

	public decimal TakeProfitBuy { get => _takeProfitBuy.Value; set => _takeProfitBuy.Value = value; }
	public decimal StopLossBuy { get => _stopLossBuy.Value; set => _stopLossBuy.Value = value; }
	public decimal TrailingStopBuy { get => _trailingStopBuy.Value; set => _trailingStopBuy.Value = value; }
	public decimal TakeProfitSell { get => _takeProfitSell.Value; set => _takeProfitSell.Value = value; }
	public decimal StopLossSell { get => _stopLossSell.Value; set => _stopLossSell.Value = value; }
	public decimal TrailingStopSell { get => _trailingStopSell.Value; set => _trailingStopSell.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TenPipsStrategy()
	{
		_takeProfitBuy = Param(nameof(TakeProfitBuy), 10m).SetDisplay("Take Profit Buy");
		_stopLossBuy = Param(nameof(StopLossBuy), 50m).SetDisplay("Stop Loss Buy");
		_trailingStopBuy = Param(nameof(TrailingStopBuy), 50m).SetDisplay("Trailing Stop Buy");
		_takeProfitSell = Param(nameof(TakeProfitSell), 10m).SetDisplay("Take Profit Sell");
		_stopLossSell = Param(nameof(StopLossSell), 50m).SetDisplay("Stop Loss Sell");
		_trailingStopSell = Param(nameof(TrailingStopSell), 50m).SetDisplay("Trailing Stop Sell");
		_volume = Param(nameof(Volume), 1m).SetDisplay("Volume");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var trades = SubscribeTrades();
		trades.Bind(ProcessTrade).Start();

		BuyMarket(Volume);
		SellMarket(Volume);
	}

	private void ProcessTrade(Trade trade)
	{
		if (_hasLong && TrailingStopBuy > 0 && _longStop != null)
		{
			var newStop = trade.Price - TrailingStopBuy;
			if (newStop > _longStop.Price)
			{
				CancelOrder(_longStop);
				_longStop = SellStop(Volume, newStop);
			}
		}

		if (_hasShort && TrailingStopSell > 0 && _shortStop != null)
		{
			var newStop = trade.Price + TrailingStopSell;
			if (newStop < _shortStop.Price)
			{
				CancelOrder(_shortStop);
				_shortStop = BuyStop(Volume, newStop);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == _longStop || trade.Order == _longTake)
		{
			_hasLong = false;
			_longStop = null;
			_longTake = null;
			BuyMarket(Volume);
			return;
		}

		if (trade.Order == _shortStop || trade.Order == _shortTake)
		{
			_hasShort = false;
			_shortStop = null;
			_shortTake = null;
			SellMarket(Volume);
			return;
		}

		if (trade.Order.Direction == Sides.Buy)
		{
			_hasLong = true;
			_longEntryPrice = trade.Trade.Price;
			_longStop = SellStop(Volume, _longEntryPrice - StopLossBuy);
			_longTake = SellLimit(Volume, _longEntryPrice + TakeProfitBuy);
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			_hasShort = true;
			_shortEntryPrice = trade.Trade.Price;
			_shortStop = BuyStop(Volume, _shortEntryPrice + StopLossSell);
			_shortTake = BuyLimit(Volume, _shortEntryPrice - TakeProfitSell);
		}
	}
}
