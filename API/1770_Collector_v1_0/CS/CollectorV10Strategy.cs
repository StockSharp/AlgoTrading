using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that opens trades when price crosses dynamic levels.
/// </summary>
public class CollectorV10Strategy : Strategy
{
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _buyLevel;
	private decimal _sellLevel;
	private decimal _entryPrice;
	private int _tradeCount;

	public decimal Distance { get => _distance.Value; set => _distance.Value = value; }
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CollectorV10Strategy()
	{
		_distance = Param(nameof(Distance), 200m)
			.SetDisplay("Distance", "Grid distance", "Grid");

		_maxTrades = Param(nameof(MaxTrades), 200)
			.SetDisplay("Max Trades", "Maximum trades", "Grid");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("TP", "Take profit", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("SL", "Stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_buyLevel = 0;
		_sellLevel = 0;
		_entryPrice = 0;
		_tradeCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Initialize levels on first candle
		if (_buyLevel == 0 && _sellLevel == 0)
		{
			SetLevels(price);
			return;
		}

		// Manage exits
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				SetLevels(price);
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				SetLevels(price);
				return;
			}
		}

		// Grid entries when flat
		if (Position == 0 && _tradeCount < MaxTrades)
		{
			if (price >= _buyLevel)
			{
				BuyMarket();
				_entryPrice = price;
				_tradeCount++;
				SetLevels(price);
			}
			else if (price <= _sellLevel)
			{
				SellMarket();
				_entryPrice = price;
				_tradeCount++;
				SetLevels(price);
			}
		}
	}

	private void SetLevels(decimal price)
	{
		var half = Distance / 2m;
		_buyLevel = price + half;
		_sellLevel = price - half;
	}
}
