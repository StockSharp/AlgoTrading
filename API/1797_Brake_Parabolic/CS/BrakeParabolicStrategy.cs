using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a parabolic brake indicator.
/// Opens long when trend flips to bullish and short when it turns bearish.
/// </summary>
public class BrakeParabolicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _a;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _maxPrice;
	private decimal _minPrice;
	private decimal _beginPrice;
	private bool _isLong;
	private bool _prevIsLong;
	private int _bar;
	private bool _init;
	private decimal _entryPrice;

	public decimal A { get => _a.Value; set => _a.Value = value; }
	public decimal B { get => _b.Value; set => _b.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BrakeParabolicStrategy()
	{
		_a = Param(nameof(A), 1.5m)
			.SetDisplay("A", "Curve exponent", "Indicator");
		_b = Param(nameof(B), 1.0m)
			.SetDisplay("B", "Curve speed", "Indicator");
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_maxPrice = decimal.MinValue;
		_minPrice = decimal.MaxValue;
		_beginPrice = 0;
		_isLong = true;
		_prevIsLong = true;
		_bar = 0;
		_init = false;
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
			_beginPrice = candle.LowPrice;
			_init = true;
		}

		_maxPrice = Math.Max(_maxPrice, candle.HighPrice);
		_minPrice = Math.Min(_minPrice, candle.LowPrice);

		// Calculate parabolic level
		var parab = (decimal)Math.Pow(_bar, (double)A) * B;
		var level = _isLong ? _beginPrice + parab : _beginPrice - parab;

		if (_isLong && level > candle.LowPrice)
		{
			_isLong = false;
			_beginPrice = _maxPrice;
			_bar = 0;
			_maxPrice = decimal.MinValue;
			_minPrice = decimal.MaxValue;
		}
		else if (!_isLong && level < candle.HighPrice)
		{
			_isLong = true;
			_beginPrice = _minPrice;
			_bar = 0;
			_maxPrice = decimal.MinValue;
			_minPrice = decimal.MaxValue;
		}

		var buySignal = !_prevIsLong && _isLong;
		var sellSignal = _prevIsLong && !_isLong;

		_prevIsLong = _isLong;
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

		// Entry on direction change
		if (buySignal)
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
		else if (sellSignal)
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
