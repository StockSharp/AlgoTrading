using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.MatchingEngine;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedged martingale grid using real server-side conditional stop orders for the initial OCO pair.
/// </summary>
public class MartiniMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private decimal _lastExecutionPrice;
	private decimal _lastLegVolume;
	private int _orderCount;
	private bool _martingaleOrderPending;
	private bool _closingCycle;
	private decimal _cyclePnlBase;

	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal ProfitClose { get => _profitClose.Value; set => _profitClose.Value = value; }
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartiniMartingaleStrategy()
	{
		_step = Param(nameof(Step), 10m).SetGreaterThanZero();
		_profitClose = Param(nameof(ProfitClose), 10m).SetGreaterThanZero();
		_initialVolume = Param(nameof(InitialVolume), 0.1m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetCycleState();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetCycleState();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0m)
		{
			if (!_closingCycle && _buyStopOrder is null && _sellStopOrder is null)
				PlaceInitialStops(candle.ClosePrice);

			return;
		}

		if (!_closingCycle && PnL - _cyclePnlBase >= ProfitClose)
		{
			_closingCycle = true;
			CancelInitialStops();
			CloseNetPosition();
			return;
		}

		if (_closingCycle || _martingaleOrderPending || _lastLegVolume <= 0m || _orderCount <= 0)
			return;

		var adverseDistance = Step * _orderCount;

		if (Position > 0m && candle.LowPrice <= _lastExecutionPrice - adverseDistance)
			PlaceMartingale(Sides.Sell, _lastLegVolume * 2m);
		else if (Position < 0m && candle.HighPrice >= _lastExecutionPrice + adverseDistance)
			PlaceMartingale(Sides.Buy, _lastLegVolume * 2m);
	}

	private void PlaceInitialStops(decimal center)
	{
		_cyclePnlBase = PnL;

		_buyStopOrder = CreateStopOrder(Sides.Buy, center + Step, InitialVolume, "Martini initial buy stop");
		_sellStopOrder = CreateStopOrder(Sides.Sell, center - Step, InitialVolume, "Martini initial sell stop");

		RegisterOrder(_buyStopOrder);
		RegisterOrder(_sellStopOrder);
	}

	private Order CreateStopOrder(Sides side, decimal activationPrice, decimal volume, string comment)
		=> new()
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Conditional,
			Condition = new StopOrderCondition { ActivationPrice = activationPrice },
			Side = side,
			Volume = volume,
			Comment = comment,
		};

	private void PlaceMartingale(Sides side, decimal volume)
	{
		volume = NormalizeVolume(volume);
		if (volume <= 0m)
			return;

		_martingaleOrderPending = true;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order is null || trade.Trade is null)
			return;

		if (_closingCycle)
		{
			if (Position == 0m)
			{
				_closingCycle = false;
				ResetCycleState(keepPnlBase: false);
			}
			return;
		}

		if (trade.Order.Type == OrderTypes.Conditional)
		{
			if (trade.Order.Side == Sides.Buy)
				CancelIfActive(_sellStopOrder);
			else
				CancelIfActive(_buyStopOrder);

			_buyStopOrder = null;
			_sellStopOrder = null;
		}

		_lastExecutionPrice = trade.Trade.Price;
		_lastLegVolume = trade.Trade.Volume;
		_orderCount++;
		_martingaleOrderPending = false;
	}

	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order is null || order.State != OrderStates.Done)
			return;

		if (_buyStopOrder is not null && order.TransactionId == _buyStopOrder.TransactionId && order.Balance == order.Volume)
			_buyStopOrder = null;

		if (_sellStopOrder is not null && order.TransactionId == _sellStopOrder.TransactionId && order.Balance == order.Volume)
			_sellStopOrder = null;
	}

	private void CancelInitialStops()
	{
		CancelIfActive(_buyStopOrder);
		CancelIfActive(_sellStopOrder);
		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	private void CancelIfActive(Order order)
	{
		if (order is { State: OrderStates.Active })
			CancelOrder(order);
	}

	private void CloseNetPosition()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;
		return volume;
	}

	private void ResetCycleState(bool keepPnlBase = false)
	{
		_buyStopOrder = null;
		_sellStopOrder = null;
		_lastExecutionPrice = 0m;
		_lastLegVolume = 0m;
		_orderCount = 0;
		_martingaleOrderPending = false;
		_closingCycle = false;
		if (!keepPnlBase)
			_cyclePnlBase = PnL;
	}
}
