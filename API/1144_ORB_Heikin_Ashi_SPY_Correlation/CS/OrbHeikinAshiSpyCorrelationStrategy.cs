using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OrbHeikinAshiSpyCorrelationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _orHigh;
	private decimal _orLow;
	private bool _tradeTakenToday;
	private bool _wasInOr;
	private DateTime _currentDay;
	private bool _orEstablished;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrbHeikinAshiSpyCorrelationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_orHigh = 0;
		_orLow = 0;
		_tradeTakenToday = false;
		_wasInOr = false;
		_currentDay = default;
		_orEstablished = false;
		_prevHaOpen = 0;
		_prevHaClose = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_orHigh = 0;
		_orLow = 0;
		_tradeTakenToday = false;
		_wasInOr = false;
		_currentDay = default;
		_orEstablished = false;
		_prevHaOpen = 0;
		_prevHaClose = 0;

		var sma = new SimpleMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!sma.IsFormed)
					return;

				var day = candle.OpenTime.Date;
				if (_currentDay != day)
				{
					_currentDay = day;
					_orHigh = 0;
					_orLow = 0;
					_tradeTakenToday = false;
					_orEstablished = false;
				}

				// Heikin Ashi
				decimal haOpen, haClose;
				if (_prevHaOpen == 0)
				{
					haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
					haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
				}
				else
				{
					haOpen = (_prevHaOpen + _prevHaClose) / 2;
					haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
				}
				_prevHaOpen = haOpen;
				_prevHaClose = haClose;

				var bullishHa = haClose > haOpen;
				var hour = candle.OpenTime.TimeOfDay.TotalHours;
				var inOr = hour < 1;

				if (inOr)
				{
					_orHigh = _orHigh > 0 ? Math.Max(_orHigh, candle.HighPrice) : candle.HighPrice;
					_orLow = _orLow > 0 ? Math.Min(_orLow, candle.LowPrice) : candle.LowPrice;
				}

				if (_wasInOr && !inOr && _orHigh > 0 && _orLow > 0 && _orHigh - _orLow > 0)
					_orEstablished = true;

				if (!_tradeTakenToday && _orEstablished && !inOr)
				{
					if (candle.ClosePrice > _orHigh && bullishHa && candle.ClosePrice > smaVal && Position <= 0)
					{
						BuyMarket();
						_tradeTakenToday = true;
					}
					else if (candle.ClosePrice < _orLow && !bullishHa && candle.ClosePrice < smaVal && Position >= 0)
					{
						SellMarket();
						_tradeTakenToday = true;
					}
				}

				_wasInOr = inOr;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
