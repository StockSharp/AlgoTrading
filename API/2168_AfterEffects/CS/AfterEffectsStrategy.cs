using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on aftereffects in price series.
/// It evaluates a custom signal from historical opens and the current close.
/// </summary>
public class AfterEffectsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<bool> _random;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _pQueue = new();
	private readonly Queue<decimal> _twoPQueue = new();
	private decimal _openP;
	private decimal _open2P;
	private decimal _stopPrice;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public bool Random { get => _random.Value; set => _random.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AfterEffectsStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop Loss distance", "General");
		_period = Param(nameof(Period), 3)
			.SetDisplay("Bar Period", "Period of bars for signal", "General");
		_random = Param(nameof(Random), false)
			.SetDisplay("Random Range", "Invert signal", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pQueue.Clear();
		_twoPQueue.Clear();
		_openP = 0m;
		_open2P = 0m;
		_stopPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_pQueue.Enqueue(candle.OpenPrice);

		if (_pQueue.Count > Period)
		{
			_openP = _pQueue.Dequeue();
			_twoPQueue.Enqueue(_openP);

			if (_twoPQueue.Count > Period)
				_open2P = _twoPQueue.Dequeue();
		}

		if (_twoPQueue.Count < Period)
			return;

		var signal = candle.ClosePrice - 2m * _openP + _open2P;

		if (Random)
			signal = -signal;

		if (Position == 0)
		{
			if (signal > 0m)
			{
				BuyMarket();
				_stopPrice = candle.ClosePrice - StopLoss;
			}
			else
			{
				SellMarket();
				_stopPrice = candle.ClosePrice + StopLoss;
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice <= _stopPrice)
			{
				if (signal < 0m)
				{
					// Reverse to short
					SellMarket();
					SellMarket();
					_stopPrice = candle.ClosePrice + StopLoss;
				}
				else
				{
					// Just exit
					SellMarket();
				}
			}
			else
			{
				_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - StopLoss);
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _stopPrice)
			{
				if (signal > 0m)
				{
					// Reverse to long
					BuyMarket();
					BuyMarket();
					_stopPrice = candle.ClosePrice - StopLoss;
				}
				else
				{
					// Just exit
					BuyMarket();
				}
			}
			else
			{
				_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + StopLoss);
			}
		}
	}
}
