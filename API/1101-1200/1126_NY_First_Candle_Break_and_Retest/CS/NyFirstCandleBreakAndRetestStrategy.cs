using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyFirstCandleBreakAndRetestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	private DateTime _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private bool _dayRangeSet;
	private bool _tradedToday;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public NyFirstCandleBreakAndRetestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_emaLength = Param(nameof(EmaLength), 20).SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentDay = default;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayRangeSet = false;
		_tradedToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = default;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayRangeSet = false;
		_tradedToday = false;

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, ema);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		// New day: close positions, set first candle as range
		if (day != _currentDay)
		{
			_currentDay = day;
			if (Position > 0) SellMarket();
			else if (Position < 0) BuyMarket();
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayRangeSet = true;
			_tradedToday = false;
			return;
		}

		if (!_dayRangeSet || _tradedToday)
			return;

		// Entry: breakout above first candle high with EMA confirmation
		if (Position <= 0 && candle.ClosePrice > _dayHigh && candle.ClosePrice > ema)
		{
			BuyMarket();
			_tradedToday = true;
		}
		// Entry: breakdown below first candle low with EMA confirmation
		else if (Position >= 0 && candle.ClosePrice < _dayLow && candle.ClosePrice < ema)
		{
			SellMarket();
			_tradedToday = true;
		}
	}
}
