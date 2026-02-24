using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy trading previous session high/low with trailing stop.
/// </summary>
public class Breakout04Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingDist;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private DateTime _currentDate;
	private decimal _entryPrice;
	private decimal _trailingStop;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingDist { get => _trailingDist.Value; set => _trailingDist.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Breakout04Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_trailingDist = Param(nameof(TrailingDist), 200m)
			.SetDisplay("Trailing Dist", "Trailing stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_sessionHigh = 0;
		_sessionLow = decimal.MaxValue;
		_currentDate = default;
		_entryPrice = 0;
		_trailingStop = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var date = candle.OpenTime.Date;

		// Track daily high/low for previous day reference
		if (date != _currentDate)
		{
			if (_currentDate != default)
			{
				_prevHigh = _sessionHigh;
				_prevLow = _sessionLow;
			}
			_currentDate = date;
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
		}
		else
		{
			if (candle.HighPrice > _sessionHigh)
				_sessionHigh = candle.HighPrice;
			if (candle.LowPrice < _sessionLow)
				_sessionLow = candle.LowPrice;
		}

		if (_prevHigh == 0)
			return;

		// Manage exits with trailing stop
		if (Position > 0)
		{
			// Update trailing stop
			var newTrail = price - TrailingDist;
			if (newTrail > _trailingStop)
				_trailingStop = newTrail;

			if (price - _entryPrice >= TakeProfit || price <= _trailingStop || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			var newTrail = price + TrailingDist;
			if (newTrail < _trailingStop || _trailingStop == 0)
				_trailingStop = newTrail;

			if (_entryPrice - price >= TakeProfit || price >= _trailingStop || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Entry on breakout
		if (Position == 0)
		{
			if (price > _prevHigh)
			{
				BuyMarket();
				_entryPrice = price;
				_trailingStop = price - TrailingDist;
			}
			else if (price < _prevLow)
			{
				SellMarket();
				_entryPrice = price;
				_trailingStop = price + TrailingDist;
			}
		}
	}
}
