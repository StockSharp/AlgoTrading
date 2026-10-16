using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternates between long and short entries after actual fills, with a
/// fill-anchored stop and a target increased after a realized losing trade.
/// Entry scheduling uses finished candles; protective exits use fresh bid/ask quotes.
/// </summary>
public class SuperTakeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _martinFactor;
	private readonly StrategyParam<DataType> _candleType;

	private Order _entryOrder;
	private Order _exitOrder;
	private Order _pendingOrder;
	private bool _submittingEntry;
	private bool _submittingExit;
	private bool _entryLong;
	private decimal _entryVolume;
	private decimal _entryValue;
	private decimal _remainingVolume;
	private decimal _realizedPnl;
	private decimal _currentTakeProfit;
	private decimal _lastTakeProfitDistance;
	private bool? _lastClosedWasBuy;
	private bool _lastTradeWasLoss;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal MartinFactor { get => _martinFactor.Value; set => _martinFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SuperTakeStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 10m).SetGreaterThanZero()
			.SetDisplay("Take Profit", "Base price-distance target", "Risk");
		_stopLoss = Param(nameof(StopLoss), 15m).SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Price-distance stop", "Risk");
		_martinFactor = Param(nameof(MartinFactor), 1.8m).SetGreaterThanZero()
			.SetDisplay("Martingale Factor", "Take-profit distance multiplier after loss", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		// The registration event can precede a synchronous fill, in which case
		// storing only BuyMarket/SellMarket's return value would miss the trade.
		OrderRegistering += order =>
		{
			if (_submittingEntry) _entryOrder = order;
			else if (_submittingExit) _exitOrder = order;
			_pendingOrder = order;
		};
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private bool HasPendingOrder => _pendingOrder is not null &&
		_pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetLocalState();
	}

	private void ResetLocalState()
	{
		_entryOrder = _exitOrder = _pendingOrder = null;
		_submittingEntry = _submittingExit = false;
		_entryLong = true;
		_entryVolume = _entryValue = _remainingVolume = _realizedPnl = 0m;
		_currentTakeProfit = _lastTakeProfitDistance = 0m;
		_lastClosedWasBuy = null;
		_lastTradeWasLoss = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetLocalState();
		_lastTakeProfitDistance = TakeProfit;

		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || Position != 0m ||
			_remainingVolume != 0m || HasPendingOrder || !IsFormedAndOnlineAndAllowTrading())
			return;

		_entryLong = _lastClosedWasBuy is not true;
		_currentTakeProfit = _lastTradeWasLoss ? _lastTakeProfitDistance * MartinFactor : TakeProfit;
		_entryVolume = _entryValue = _realizedPnl = 0m;
		_exitOrder = null;

		_submittingEntry = true;
		try
		{
			_entryOrder = _entryLong ? BuyMarket(Volume) : SellMarket(Volume);
			_pendingOrder = _entryOrder;
		}
		finally
		{
			_submittingEntry = false;
		}
	}

	/// <summary>Recognize executions only; a submitted order never creates a hypothetical fill.</summary>
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Order is null || trade.Trade is null)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		if (trade.Order == _entryOrder)
		{
			_entryVolume += volume;
			_entryValue += volume * price;
			_remainingVolume += volume;
		}
		else if (trade.Order == _exitOrder && _entryVolume > 0m)
		{
			var entry = _entryValue / _entryVolume;
			_realizedPnl += (_entryLong ? price - entry : entry - price) * volume;
			_remainingVolume = Math.Max(0m, _remainingVolume - volume);
			if (_remainingVolume == 0m)
			{
				_lastClosedWasBuy = _entryLong;
				_lastTradeWasLoss = _realizedPnl < 0m;
				_lastTakeProfitDistance = _currentTakeProfit;
			}
		}
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _remainingVolume <= 0m || _entryVolume <= 0m ||
			HasPendingOrder || !IsFormedAndOnlineAndAllowTrading())
			return;

		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;

		var entry = _entryValue / _entryVolume;
		var profit = Position > 0m ? price - entry : entry - price;
		if (profit < _currentTakeProfit && profit > -StopLoss)
			return;

		_submittingExit = true;
		try
		{
			_exitOrder = Position > 0m ? SellMarket(Math.Abs(Position)) : BuyMarket(Math.Abs(Position));
			_pendingOrder = _exitOrder;
		}
		finally
		{
			_submittingExit = false;
		}
	}
}
