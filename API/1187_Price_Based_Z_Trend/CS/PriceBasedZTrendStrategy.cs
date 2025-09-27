using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Z-score of price relative to EMA.
/// </summary>
public class PriceBasedZTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _priceDeviationLength;
	private readonly StrategyParam<int> _priceAverageLength;
	private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevZScore;

	public int PriceDeviationLength { get => _priceDeviationLength.Value; set => _priceDeviationLength.Value = value; }
	public int PriceAverageLength { get => _priceAverageLength.Value; set => _priceAverageLength.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PriceBasedZTrendStrategy()
	{
		_priceDeviationLength = Param(nameof(PriceDeviationLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Standard Deviation Length", "Length for std deviation", "Parameters");

		_priceAverageLength = Param(nameof(PriceAverageLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Average Length", "Length for EMA", "Parameters");

		_threshold = Param(nameof(Threshold), 1m)
			.SetDisplay("Threshold", "Z-score threshold", "Parameters");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trading Direction", "Allowed position direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevZScore = 0m;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = PriceAverageLength };
		var stdDev = new StandardDeviation { Length = PriceDeviationLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdDevValue == 0m)
			return;

var zScore = (candle.ClosePrice - emaValue) / stdDevValue;

var crossOver = _prevZScore <= Threshold && zScore > Threshold;
var crossUnder = _prevZScore >= -Threshold && zScore < -Threshold;

var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;

if (crossOver)
{
if (allowLong && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (allowShort && Position < 0)
{
BuyMarket(Math.Abs(Position));
}
}
else if (crossUnder)
{
if (allowShort && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
else if (allowLong && Position > 0)
{
SellMarket(Math.Abs(Position));
}
}

		_prevZScore = zScore;
	}
}
