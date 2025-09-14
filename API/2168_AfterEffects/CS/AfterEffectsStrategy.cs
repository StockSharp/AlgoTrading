using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
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
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _pQueue = [];
	private readonly Queue<decimal> _twoPQueue = [];
	private decimal _openP;
	private decimal _open2P;
	private decimal _stopPrice;

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Period of bars used for the signal.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Invert signal if true.
	/// </summary>
	public bool Random
	{
		get => _random.Value;
		set => _random.Value = value;
	}

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AfterEffectsStrategy()
	{
		_stopLoss = this.Param(nameof(StopLoss), 500m).SetDisplay("Stop Loss");
		_period = this.Param(nameof(Period), 3).SetDisplay("Bar Period").SetCanOptimize(true);
		_random = this.Param(nameof(Random), false).SetDisplay("Random Range");
		_volume = this.Param(nameof(Volume), 1m).SetDisplay("Volume");
		_candleType = this.Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
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
				BuyMarket(Volume);
				_stopPrice = candle.ClosePrice - StopLoss * Security.PriceStep;
			}
			else
			{
				SellMarket(Volume);
				_stopPrice = candle.ClosePrice + StopLoss * Security.PriceStep;
			}

			return;
		}

		if (Position > 0)
		{
			var profitTrigger = _stopPrice + StopLoss * 2m * Security.PriceStep;

			if (candle.ClosePrice > profitTrigger)
			{
				if (signal < 0m)
				{
					SellMarket(Volume * 2m);
					_stopPrice = candle.ClosePrice + StopLoss * Security.PriceStep;
				}
				else
				{
					_stopPrice = candle.ClosePrice - StopLoss * Security.PriceStep;
				}
			}
		}
		else if (Position < 0)
		{
			var profitTrigger = _stopPrice - StopLoss * 2m * Security.PriceStep;

			if (candle.ClosePrice < profitTrigger)
			{
				if (signal > 0m)
				{
					BuyMarket(Volume * 2m);
					_stopPrice = candle.ClosePrice - StopLoss * Security.PriceStep;
				}
				else
				{
					_stopPrice = candle.ClosePrice + StopLoss * Security.PriceStep;
				}
			}
		}
	}
}
