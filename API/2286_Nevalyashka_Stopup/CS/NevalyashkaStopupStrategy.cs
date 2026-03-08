using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating martingale strategy.
/// Opens opposite direction after each trade and increases
/// stop loss and take profit distances after losses.
/// </summary>
public class NevalyashkaStopupStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _martingaleCoeff;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _currentStopLoss;
	private decimal _currentTakeProfit;
	private bool _nextIsBuy;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal MartingaleCoeff { get => _martingaleCoeff.Value; set => _martingaleCoeff.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NevalyashkaStopupStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "General");

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "General");

		_martingaleCoeff = Param(nameof(MartingaleCoeff), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Coeff", "Multiplier applied after loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_currentStopLoss = 0;
		_currentTakeProfit = 0;
		_nextIsBuy = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentStopLoss = StopLoss;
		_currentTakeProfit = TakeProfit;
		_nextIsBuy = true;
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

		var closePrice = candle.ClosePrice;

		// Open first position
		if (Position == 0)
		{
			if (_nextIsBuy)
				BuyMarket();
			else
				SellMarket();

			_entryPrice = closePrice;
			return;
		}

		// Check SL/TP for long
		if (Position > 0)
		{
			if (candle.LowPrice <= _entryPrice - _currentStopLoss)
			{
				SellMarket();
				OnTradeClosed(false);
			}
			else if (candle.HighPrice >= _entryPrice + _currentTakeProfit)
			{
				SellMarket();
				OnTradeClosed(true);
			}
		}
		// Check SL/TP for short
		else if (Position < 0)
		{
			if (candle.HighPrice >= _entryPrice + _currentStopLoss)
			{
				BuyMarket();
				OnTradeClosed(false);
			}
			else if (candle.LowPrice <= _entryPrice - _currentTakeProfit)
			{
				BuyMarket();
				OnTradeClosed(true);
			}
		}
	}

	private void OnTradeClosed(bool wasProfit)
	{
		if (wasProfit)
		{
			_currentStopLoss = StopLoss;
			_currentTakeProfit = TakeProfit;
		}
		else
		{
			_currentStopLoss *= MartingaleCoeff;
			_currentTakeProfit *= MartingaleCoeff;
		}

		_nextIsBuy = !_nextIsBuy;
	}
}
