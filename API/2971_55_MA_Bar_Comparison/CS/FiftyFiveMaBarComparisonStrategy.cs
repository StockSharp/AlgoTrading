using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 55 MA bar comparison strategy. Compares candle body with MA direction.
/// </summary>
public class FiftyFiveMaBarComparisonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private decimal? _prevMa;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public FiftyFiveMaBarComparisonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMa = null;

		var sma = new SimpleMovingAverage { Length = MaPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal maVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevMa == null)
		{
			_prevMa = maVal;
			return;
		}

		var close = candle.ClosePrice;
		var bullishBar = candle.ClosePrice > candle.OpenPrice;
		var bearishBar = candle.ClosePrice < candle.OpenPrice;
		var maRising = maVal > _prevMa.Value;
		var maFalling = maVal < _prevMa.Value;

		_prevMa = maVal;

		// Bullish bar + rising MA + close above MA → buy
		if (bullishBar && maRising && close > maVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Bearish bar + falling MA + close below MA → sell
		else if (bearishBar && maFalling && close < maVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
