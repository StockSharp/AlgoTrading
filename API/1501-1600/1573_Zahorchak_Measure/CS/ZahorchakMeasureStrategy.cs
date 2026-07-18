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
/// Breadth score based on moving averages.
/// Buys when score turns positive and sells when negative.
/// Uses short, medium, and long SMAs to compute a composite score.
/// </summary>
public class ZahorchakMeasureStrategy : Strategy
{
	private readonly StrategyParam<decimal> _points;
	private readonly StrategyParam<int> _emaSmoothing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevScore;
	private bool _hasPrev;
	private decimal _emaMeasure;

	public decimal Points { get => _points.Value; set => _points.Value = value; }
	public int EmaSmoothing { get => _emaSmoothing.Value; set => _emaSmoothing.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZahorchakMeasureStrategy()
	{
		_points = Param(nameof(Points), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Point Value", "Score per condition", "Scoring");

		_emaSmoothing = Param(nameof(EmaSmoothing), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Smoothing", "Smoothing length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevScore = 0;
		_hasPrev = false;
		_emaMeasure = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var smaShort = new SimpleMovingAverage { Length = 25 };
		var smaMedium = new SimpleMovingAverage { Length = 75 };
		var smaLong = new SimpleMovingAverage { Length = 200 };

		_prevScore = 0;
		_hasPrev = false;
		_emaMeasure = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(smaShort, smaMedium, smaLong, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, smaShort);
			DrawIndicator(area, smaMedium);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortVal, decimal mediumVal, decimal longVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute breadth score
		var score = 0m;
		score += candle.ClosePrice > shortVal ? Points : -Points;
		score += candle.ClosePrice > mediumVal ? Points : -Points;
		score += candle.ClosePrice > longVal ? Points : -Points;
		score += shortVal > mediumVal ? Points : -Points;
		score += mediumVal > longVal ? Points : -Points;
		score += shortVal > longVal ? Points : -Points;

		var maxScore = Points * 6m;
		var normalized = maxScore != 0 ? 10m * score / maxScore : 0;

		// EMA smoothing
		if (!_hasPrev)
		{
			_emaMeasure = normalized;
			_prevScore = normalized;
			_hasPrev = true;
			return;
		}

		var k = 2m / (EmaSmoothing + 1);
		_emaMeasure = normalized * k + _emaMeasure * (1 - k);

		var measure = _emaMeasure;

		// Trade on zero-line cross
		if (_prevScore <= 0 && measure > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevScore >= 0 && measure < 0 && Position >= 0)
		{
			SellMarket();
		}

		_prevScore = measure;
	}
}
