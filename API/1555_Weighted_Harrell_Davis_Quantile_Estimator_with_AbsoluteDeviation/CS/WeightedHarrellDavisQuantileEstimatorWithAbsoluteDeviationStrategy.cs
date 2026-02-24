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
/// Quantile estimator strategy with deviation bands.
/// Uses SMA + StdDev as Bollinger-like bands for mean reversion.
/// Buys when price drops below the lower band, sells when above upper band.
/// </summary>
public class WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _devMult;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal DevMult { get => _devMult.Value; set => _devMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy()
	{
		_length = Param(nameof(Length), 39)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback period", "General");

		_devMult = Param(nameof(DevMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Mult", "Band multiplier", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Length };
		var stdDev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		var upper = smaVal + DevMult * stdVal;
		var lower = smaVal - DevMult * stdVal;

		if (candle.ClosePrice > upper && Position >= 0)
			SellMarket();
		else if (candle.ClosePrice < lower && Position <= 0)
			BuyMarket();
	}
}
