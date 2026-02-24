using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that alternates buy and sell orders on each candle close.
/// Each position is protected with stop-loss and take-profit via manual tracking.
/// </summary>
public class TimerTradeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isBuyNext = true;
	private decimal _entryPrice;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimerTradeStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isBuyNext = true;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Check SL/TP for existing position
		if (Position > 0)
		{
			if (candle.LowPrice <= _entryPrice - StopLoss || candle.HighPrice >= _entryPrice + TakeProfit)
			{
				SellMarket();
				_isBuyNext = !_isBuyNext;
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _entryPrice + StopLoss || candle.LowPrice <= _entryPrice - TakeProfit)
			{
				BuyMarket();
				_isBuyNext = !_isBuyNext;
				return;
			}
		}

		// Open new position when flat
		if (Position == 0)
		{
			if (_isBuyNext)
				BuyMarket();
			else
				SellMarket();

			_entryPrice = price;
			_isBuyNext = !_isBuyNext;
		}
	}
}
