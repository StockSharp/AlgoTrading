using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average strategy with smoothing offset.
/// </summary>
public class SmoothingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _smoothing;
	private readonly StrategyParam<DataType> _candleType;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmoothingAverageStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetDisplay("MA Period", "Moving average period", "MA")
			.SetCanOptimize();
		_smoothing = Param(nameof(Smoothing), 1400m)
			.SetDisplay("Smoothing", "Price offset from moving average", "General")
			.SetCanOptimize();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// create moving average indicator
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		// subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var price = candle.ClosePrice;

		// open new position if flat
		if (Position == 0)
		{
			if (price < maValue + Smoothing)
				SellMarket();
			else if (price > maValue - Smoothing)
				BuyMarket();
		}
		// close position when price crosses offset
		else if (Position < 0 && price > maValue + Smoothing)
			BuyMarket();
		else if (Position > 0 && price < maValue - Smoothing)
			SellMarket();
	}
}
