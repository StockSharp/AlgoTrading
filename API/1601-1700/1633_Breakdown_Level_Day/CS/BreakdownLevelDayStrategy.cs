using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day range breakout strategy. Tracks the day's high/low during accumulation period,
/// then enters on breakout above high or below low.
/// </summary>
public class BreakdownLevelDayStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rangeHigh;
	private decimal _rangeLow;
	private int _barCount;
	private bool _rangeEstablished;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakdownLevelDayStrategy()
	{
		_lookback = Param(nameof(Lookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars to establish range", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_rangeHigh = 0;
		_rangeLow = decimal.MaxValue;
		_barCount = 0;
		_rangeEstablished = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rangeEstablished)
		{
			if (candle.HighPrice > _rangeHigh)
				_rangeHigh = candle.HighPrice;
			if (candle.LowPrice < _rangeLow)
				_rangeLow = candle.LowPrice;

			_barCount++;

			if (_barCount >= Lookback)
				_rangeEstablished = true;

			return;
		}

		var price = candle.ClosePrice;

		// Breakout above range high
		if (price > _rangeHigh && Position <= 0)
		{
			BuyMarket();
			// Reset range for next setup
			_rangeHigh = candle.HighPrice;
			_rangeLow = candle.LowPrice;
			_barCount = 1;
			_rangeEstablished = false;
		}
		// Breakdown below range low
		else if (price < _rangeLow && Position >= 0)
		{
			SellMarket();
			_rangeHigh = candle.HighPrice;
			_rangeLow = candle.LowPrice;
			_barCount = 1;
			_rangeEstablished = false;
		}
		else
		{
			// Update range
			if (candle.HighPrice > _rangeHigh)
				_rangeHigh = candle.HighPrice;
			if (candle.LowPrice < _rangeLow)
				_rangeLow = candle.LowPrice;
		}
	}
}
