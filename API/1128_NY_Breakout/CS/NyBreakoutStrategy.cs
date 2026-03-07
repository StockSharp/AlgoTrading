using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dayHigh;
	private decimal _dayLow;
	private DateTime _currentDay;
	private bool _rangeSet;
	private bool _tradedToday;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NyBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_dayHigh = 0m;
		_dayLow = 0m;
		_currentDay = default;
		_rangeSet = false;
		_tradedToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_dayHigh = 0m;
		_dayLow = 0m;
		_currentDay = default;
		_rangeSet = false;
		_tradedToday = false;

		var sma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		// New day: close positions, capture first candle range
		if (day != _currentDay)
		{
			_currentDay = day;
			if (Position > 0) SellMarket();
			else if (Position < 0) BuyMarket();
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_rangeSet = true;
			_tradedToday = false;
			return;
		}

		if (!_rangeSet || _tradedToday)
			return;

		var range = _dayHigh - _dayLow;
		if (range <= 0)
			return;

		// Entry: breakout above range high
		if (Position <= 0 && candle.ClosePrice > _dayHigh)
		{
			BuyMarket();
			_tradedToday = true;
		}
		else if (Position >= 0 && candle.ClosePrice < _dayLow)
		{
			SellMarket();
			_tradedToday = true;
		}
	}
}
