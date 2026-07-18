using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy entering above yesterday's high with trailing stop and EMA filter.
/// Tracks daily highs and enters when price breaks above the previous day's high.
/// </summary>
public class YesterdaysHighStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gap;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _currentHigh;
	private DateTime _sessionDate;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailHighest;
	private bool _trailActive;

	public decimal Gap { get => _gap.Value; set => _gap.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailOffset { get => _trailOffset.Value; set => _trailOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public YesterdaysHighStrategy()
	{
		_gap = Param(nameof(Gap), 0.5m)
			.SetDisplay("Gap%", "Entry gap percent above prev high", "Entry");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss", "Stop-loss percent", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take-profit", "Take-profit percent", "Risk");

		_trailOffset = Param(nameof(TrailOffset), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Offset", "Trailing stop offset percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_currentHigh = 0;
		_sessionDate = default;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
		_trailHighest = 0;
		_trailActive = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = 20 };

		_prevHigh = 0;
		_currentHigh = 0;
		_sessionDate = default;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
		_trailHighest = 0;
		_trailActive = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track daily highs
		var date = candle.OpenTime.Date;
		if (_sessionDate == default)
		{
			_sessionDate = date;
			_currentHigh = candle.HighPrice;
		}
		else if (date > _sessionDate)
		{
			_prevHigh = _currentHigh;
			_currentHigh = candle.HighPrice;
			_sessionDate = date;
		}
		else
		{
			if (candle.HighPrice > _currentHigh)
				_currentHigh = candle.HighPrice;
		}

		var price = candle.ClosePrice;

		// Exit management
		if (Position > 0 && _entryPrice > 0)
		{
			if (price > _trailHighest)
				_trailHighest = price;

			// Trailing stop activation
			if (!_trailActive && price >= _entryPrice * (1 + TrailOffset / 100m))
			{
				_trailActive = true;
			}

			if (_trailActive)
			{
				var trailStop = _trailHighest * (1 - TrailOffset / 100m);
				if (price <= trailStop)
				{
					SellMarket();
					_entryPrice = 0;
					return;
				}
			}

			// Fixed SL/TP
			if (price <= _stopPrice || price >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}

		// Entry: price breaks above yesterday's high
		if (Position == 0 && _prevHigh > 0)
		{
			var breakoutLevel = _prevHigh * (1 + Gap / 100m);

			if (price > breakoutLevel && price > emaVal)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = _entryPrice * (1 - StopLoss / 100m);
				_takePrice = _entryPrice * (1 + TakeProfit / 100m);
				_trailHighest = price;
				_trailActive = false;
			}
		}
	}
}
