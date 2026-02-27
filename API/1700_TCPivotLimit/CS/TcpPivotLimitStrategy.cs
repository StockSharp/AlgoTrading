using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot point trading strategy based on previous period support/resistance levels.
/// Buys at support, sells at resistance, exits at opposite pivot level.
/// </summary>
public class TcpPivotLimitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	private decimal _pivot;
	private decimal _r1, _s1;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TcpPivotLimitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_currentDay = default;
		_pivot = _r1 = _s1 = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;

		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			if (_currentDay != default)
			{
				_pivot = (_dayHigh + _dayLow + _dayClose) / 3m;
				_r1 = 2m * _pivot - _dayLow;
				_s1 = 2m * _pivot - _dayHigh;
			}

			_currentDay = day;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			return;
		}

		_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
		_dayLow = Math.Min(_dayLow, candle.LowPrice);
		_dayClose = candle.ClosePrice;

		if (_pivot == 0) return;

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			// Buy at support
			if (close <= _s1)
			{
				BuyMarket();
				_entryPrice = close;
			}
			// Sell at resistance
			else if (close >= _r1)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		else if (Position > 0)
		{
			// Exit long at resistance or stop at entry - (r1 - s1)
			if (close >= _r1 || close <= _entryPrice - (_r1 - _s1))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			// Exit short at support or stop
			if (close <= _s1 || close >= _entryPrice + (_r1 - _s1))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
