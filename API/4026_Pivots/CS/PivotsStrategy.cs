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
/// Strategy that calculates classic floor pivot levels from daily candles and trades
/// breakouts around the central pivot. Goes long on close above pivot, short on close below.
/// Uses S2/R2 as stop/target levels.
/// </summary>
public class PivotsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pivotLevel;
	private decimal _r1, _r2, _s1, _s2;
	private decimal? _previousClose;
	private decimal? _entryPrice;
	private bool _pivotReady;

	private readonly List<decimal> _dailyHighs = new();
	private readonly List<decimal> _dailyLows = new();
	private readonly List<decimal> _dailyCloses = new();
	private DateTime _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	public PivotsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal generation", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_pivotLevel = 0m;
		_r1 = _r2 = _s1 = _s2 = 0m;
		_previousClose = null;
		_entryPrice = null;
		_pivotReady = false;
		_dailyHighs.Clear();
		_dailyLows.Clear();
		_dailyCloses.Clear();
		_currentDay = DateTime.MinValue;
		_dayHigh = 0m;
		_dayLow = decimal.MaxValue;
		_dayClose = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleDay = candle.OpenTime.Date;

		// Track daily OHLC
		if (candleDay != _currentDay)
		{
			if (_currentDay != DateTime.MinValue && _dayHigh > 0m)
			{
				// Previous day completed, calculate pivots
				var high = _dayHigh;
				var low = _dayLow;
				var close = _dayClose;

				_pivotLevel = (high + low + close) / 3m;
				_r1 = 2m * _pivotLevel - low;
				_s1 = 2m * _pivotLevel - high;
				_r2 = _pivotLevel + (high - low);
				_s2 = _pivotLevel - (high - low);
				_pivotReady = true;
			}

			_currentDay = candleDay;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
		}
		else
		{
			if (candle.HighPrice > _dayHigh) _dayHigh = candle.HighPrice;
			if (candle.LowPrice < _dayLow) _dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
		}

		if (!_pivotReady)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (_previousClose is null)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		// Manage open positions
		if (Position > 0)
		{
			// Exit long at R2 (take profit) or S1 (stop loss)
			if (candle.HighPrice >= _r2 || candle.LowPrice <= _s1)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			// Exit short at S2 (take profit) or R1 (stop loss)
			if (candle.LowPrice <= _s2 || candle.HighPrice >= _r1)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
			}
		}

		// Entry signals based on pivot cross
		if (Position == 0)
		{
			var crossAbovePivot = _previousClose.Value <= _pivotLevel && candle.ClosePrice > _pivotLevel;
			var crossBelowPivot = _previousClose.Value >= _pivotLevel && candle.ClosePrice < _pivotLevel;

			if (crossAbovePivot)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (crossBelowPivot)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_previousClose = candle.ClosePrice;
	}
}
