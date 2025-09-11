using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and DMI difference thresholds.
/// </summary>
public class MonthlyPerformanceTableStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _longDifference;
	private readonly StrategyParam<decimal> _shortDifference;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal LongDifference { get => _longDifference.Value; set => _longDifference.Value = value; }
	public decimal ShortDifference { get => _shortDifference.Value; set => _shortDifference.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MonthlyPerformanceTableStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for DMI/ADX", "General");

		_longDifference = Param(nameof(LongDifference), 10m)
			.SetDisplay("Long Difference", "Minimum diff for longs", "General");

		_shortDifference = Param(nameof(ShortDifference), 10m)
			.SetDisplay("Short Difference", "Minimum diff for shorts", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var dmi = new DirectionalIndex { Length = Length };
		var adx = new AverageDirectionalIndex { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dmi, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (dmiValue is not DirectionalIndexValue dmiData ||
			adxValue is not AverageDirectionalIndexValue adxData)
			return;

		if (dmiData.Plus is not decimal diPlus ||
			dmiData.Minus is not decimal diMinus ||
			adxData.MovingAverage is not decimal adx)
			return;

		var diff2 = Math.Abs(diPlus - adx);
		var diff3 = Math.Abs(diMinus - adx);

		var buyCond = diff2 >= LongDifference && diff3 >= LongDifference && adx < diPlus && adx > diMinus;
		var sellCond = diff2 >= ShortDifference && diff3 >= ShortDifference && adx > diPlus && adx < diMinus;

		if (buyCond && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellCond && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

