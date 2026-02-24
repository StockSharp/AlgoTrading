using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on previous candle direction.
/// Buys when the prior candle was bearish (open > close), sells when bullish.
/// Applies fixed take profit and stop loss.
/// </summary>
public class SheKanskigorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _entryPrice;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SheKanskigorStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Profit target", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Loss limit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		// Manage existing position
		if (Position > 0)
		{
			if (candle.ClosePrice - _entryPrice >= TakeProfit ||
				_entryPrice - candle.ClosePrice >= StopLoss)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - candle.ClosePrice >= TakeProfit ||
				candle.ClosePrice - _entryPrice >= StopLoss)
			{
				BuyMarket();
			}
		}

		// Entry based on previous candle direction
		if (Position == 0 && _prevCandle != null)
		{
			if (_prevCandle.OpenPrice > _prevCandle.ClosePrice)
			{
				// Previous candle bearish -> buy reversal
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (_prevCandle.OpenPrice < _prevCandle.ClosePrice)
			{
				// Previous candle bullish -> sell reversal
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevCandle = candle;
	}
}
