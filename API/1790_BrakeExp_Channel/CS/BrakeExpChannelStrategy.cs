using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on exponential channel breakout signals.
/// Tracks an exponential curve from swing points and trades direction changes.
/// </summary>
public class BrakeExpChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _a;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal A { get => _a.Value; set => _a.Value = value; }
	public decimal B { get => _b.Value; set => _b.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	private bool _isLong = true;
	private bool _init;
	private decimal _max;
	private decimal _min;
	private decimal _begin;
	private decimal _prevUp;
	private decimal _prevDn;
	private int _bar;
	private decimal _entryPrice;

	public BrakeExpChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_a = Param(nameof(A), 3m)
			.SetDisplay("A", "Exponential parameter A", "Indicator");
		_b = Param(nameof(B), 1m)
			.SetDisplay("B", "Exponential parameter B", "Indicator");
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isLong = true;
		_init = false;
		_max = decimal.MinValue;
		_min = decimal.MaxValue;
		_begin = 0;
		_prevUp = 0;
		_prevDn = 0;
		_bar = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_init)
		{
			_begin = candle.LowPrice;
			_max = decimal.MinValue;
			_min = decimal.MaxValue;
			_isLong = true;
			_prevUp = 0;
			_prevDn = 0;
			_bar = 0;
			_init = true;
		}

		_max = Math.Max(_max, candle.HighPrice);
		_min = Math.Min(_min, candle.LowPrice);

		var exp = (decimal)Math.Exp((double)(_bar * (A * 0.1m))) - 1m;
		exp *= B;

		var value = _isLong ? _begin + exp : _begin - exp;

		if (_isLong && value > candle.LowPrice)
		{
			_isLong = false;
			_begin = _max;
			value = _begin;
			_bar = 0;
			_max = decimal.MinValue;
			_min = decimal.MaxValue;
		}
		else if (!_isLong && value < candle.HighPrice)
		{
			_isLong = true;
			_begin = _min;
			value = _begin;
			_bar = 0;
			_max = decimal.MinValue;
			_min = decimal.MaxValue;
		}

		decimal up = 0, dn = 0;
		if (_isLong)
			up = value;
		else
			dn = value;

		decimal buySignal = 0, sellSignal = 0;

		// Buy signal: was down, now up
		if (_prevDn > 0 && up > 0)
			buySignal = up;

		// Sell signal: was up, now down
		if (_prevUp > 0 && dn > 0)
			sellSignal = dn;

		_prevUp = up;
		_prevDn = dn;
		_bar++;

		var price = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Entry signals
		if (buySignal > 0)
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
		else if (sellSignal > 0)
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
}
