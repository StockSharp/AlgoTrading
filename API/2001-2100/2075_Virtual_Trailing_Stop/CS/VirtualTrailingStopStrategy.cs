using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Virtual risk manager. Never opens a position. Tracks externally executed
/// position average price and closes it if a finished candle reaches the
/// fixed stop, target, or a monotonically tightening virtual trailing stop.
/// All configured distances are security price steps.
/// </summary>
public class VirtualTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss, _takeProfit, _trailingStop, _trailingStart, _trailingStep;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _entryPrice, _bestPrice, _virtualStop;
	private decimal _trackedPosition;
	private Order _pending;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VirtualTrailingStopStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative()
			.SetDisplay("Stop Loss", "Fixed distance from filled entry in price steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 0m).SetNotNegative()
			.SetDisplay("Take Profit", "Fixed target distance in price steps", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 30m).SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing distance in price steps", "Risk");
		_trailingStart = Param(nameof(TrailingStart), 10m).SetNotNegative()
			.SetDisplay("Trailing Start", "Required favorable movement in price steps", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 1m).SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum improvement before tightening", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Processed finished candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetTracking()
	{
		_entryPrice = _bestPrice = _virtualStop = null;
		_trackedPosition = 0m;
		_pending = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetTracking();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetTracking();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	protected override void OnNewPosition(Position position)
	{
		base.OnNewPosition(position);
		ObservePosition(position);
	}

	protected override void OnPositionChanged(Position position)
	{
		base.OnPositionChanged(position);
		ObservePosition(position);
	}

	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);
		ObservePosition(position);
	}

	private void ObservePosition(Position position)
	{
		if (position.Security != Security || position.Portfolio != Portfolio)
			return;

		var volume = position.CurrentValue ?? 0m;
		if (volume == 0m)
		{
			_entryPrice = _bestPrice = _virtualStop = null;
			_trackedPosition = 0m;
			return;
		}
		if (position.AveragePrice is not decimal price || price <= 0m)
			return;
		if (_entryPrice == price && _trackedPosition * volume > 0m)
		{
			_trackedPosition = volume;
			return;
		}
		_entryPrice = _bestPrice = price;
		_trackedPosition = volume;
		_virtualStop = null;
	}

	private void Tighten(bool isLong, decimal candidate)
		=> _virtualStop = _virtualStop is decimal old
			? isLong ? Math.Max(old, candidate) : Math.Min(old, candidate)
			: candidate;

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || Position == 0m || _entryPrice is not decimal entry ||
			!IsFormedAndOnlineAndAllowTrading() ||
			(_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		var isLong = Position > 0m;
		var price = candle.ClosePrice;
		var step = Security.PriceStep is decimal positive && positive > 0m ? positive : 1m;
		var best = _bestPrice ?? entry;
		best = isLong ? Math.Max(best, price) : Math.Min(best, price);
		_bestPrice = best;

		if (TrailingStop > 0m)
		{
			var profit = isLong ? best - entry : entry - best;
			if (profit >= TrailingStart * step)
			{
				var candidate = isLong ? best - TrailingStop * step : best + TrailingStop * step;
				if (_virtualStop is not decimal existing || 
					(isLong ? candidate >= existing + TrailingStep * step
						: candidate <= existing - TrailingStep * step))
					Tighten(isLong, candidate);
			}
		}

		var fixedStop = StopLoss > 0m && (isLong ? price <= entry - StopLoss * step
			: price >= entry + StopLoss * step);
		var target = TakeProfit > 0m && (isLong ? price >= entry + TakeProfit * step
			: price <= entry - TakeProfit * step);
		var trailing = _virtualStop is decimal stop && (isLong ? price <= stop : price >= stop);
		if (fixedStop || target || trailing)
			_pending = isLong ? SellMarket(Math.Abs(Position)) : BuyMarket(Math.Abs(Position));
	}
}
