using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that opens positions at fixed price intervals.
/// </summary>
public class AeronRobotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _gap;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastTradePrice;
	private decimal _entryPrice;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal Gap { get => _gap.Value; set => _gap.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AeronRobotStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Profit target", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss", "General");

		_gap = Param(nameof(Gap), 200m)
			.SetDisplay("Grid Gap", "Distance between grid levels", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Manage exits
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_lastTradePrice = price;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_lastTradePrice = price;
				return;
			}
		}

		// Grid entries
		if (Position == 0)
		{
			if (_lastTradePrice == 0)
			{
				// First trade
				BuyMarket();
				_entryPrice = price;
				_lastTradePrice = price;
			}
			else if (price <= _lastTradePrice - Gap)
			{
				// Buy on dip
				BuyMarket();
				_entryPrice = price;
				_lastTradePrice = price;
			}
			else if (price >= _lastTradePrice + Gap)
			{
				// Sell on rise
				SellMarket();
				_entryPrice = price;
				_lastTradePrice = price;
			}
		}
	}
}
