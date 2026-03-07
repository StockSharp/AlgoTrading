using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyOpeningRangeBreakoutMaStopStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dayHigh;
	private decimal _dayLow;
	private DateTime _currentDay;
	private bool _rangeSet;
	private bool _tradedToday;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NyOpeningRangeBreakoutMaStopStrategy()
	{
		_maLength = Param(nameof(MaLength), 50).SetGreaterThanZero();
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

		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, ma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		// New day
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

		// Exit on MA cross
		if (Position > 0 && candle.ClosePrice < maValue)
		{
			SellMarket();
			_tradedToday = true;
			return;
		}

		if (Position < 0 && candle.ClosePrice > maValue)
		{
			BuyMarket();
			_tradedToday = true;
			return;
		}

		// Entry: breakout above first candle high
		if (Position <= 0 && candle.ClosePrice > _dayHigh && candle.ClosePrice > maValue)
		{
			BuyMarket();
			_tradedToday = true;
		}
		else if (Position >= 0 && candle.ClosePrice < _dayLow && candle.ClosePrice < maValue)
		{
			SellMarket();
			_tradedToday = true;
		}
	}
}
