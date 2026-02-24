using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bulls Power and Bears Power crossover.
/// Goes long when bulls overtake bears and short when bears dominate.
/// </summary>
public class BullsBearsPowerCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBulls;
	private decimal _prevBears;
	private decimal _entryPrice;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BullsBearsPowerCrossStrategy()
	{
		_length = Param(nameof(Length), 13)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Indicator length", "Indicator");
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevBulls = 0;
		_prevBears = 0;
		_entryPrice = 0;

		var bulls = new BullPower { Length = Length };
		var bears = new BearPower { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bulls, bears, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal bulls, decimal bears)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				_prevBulls = bulls;
				_prevBears = bears;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				_prevBulls = bulls;
				_prevBears = bears;
				return;
			}
		}

		// Crossover entry
		if (_prevBulls != 0 || _prevBears != 0)
		{
			var crossUp = _prevBulls <= _prevBears && bulls > bears;
			var crossDown = _prevBulls >= _prevBears && bulls < bears;

			if (crossUp && Position <= 0)
			{
				if (Position < 0)
				{
					BuyMarket();
					_entryPrice = 0;
				}
				if (Position == 0)
				{
					BuyMarket();
					_entryPrice = price;
				}
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
				{
					SellMarket();
					_entryPrice = 0;
				}
				if (Position == 0)
				{
					SellMarket();
					_entryPrice = price;
				}
			}
		}

		_prevBulls = bulls;
		_prevBears = bears;
	}
}
