using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OutlierDetectorWithNSigmaConfidenceIntervalsStrategy : Strategy
{
	private readonly StrategyParam<int> _sampleSize;
	private readonly StrategyParam<decimal> _nSigma;
	private readonly StrategyParam<DataType> _candleType;

	public int SampleSize { get => _sampleSize.Value; set => _sampleSize.Value = value; }
	public decimal NSigma { get => _nSigma.Value; set => _nSigma.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OutlierDetectorWithNSigmaConfidenceIntervalsStrategy()
	{
		_sampleSize = Param(nameof(SampleSize), 50).SetGreaterThanZero();
		_nSigma = Param(nameof(NSigma), 2.0m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SampleSize };
		var stdDev = new StandardDeviation { Length = SampleSize };

		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, (candle, avg, std) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!sma.IsFormed || !stdDev.IsFormed)
					return;

				if (std <= 0 || candle.OpenTime - lastSignal < cooldown)
					return;

				var upper = avg + NSigma * std;
				var lower = avg - NSigma * std;

				if (candle.ClosePrice > upper && Position <= 0)
				{
					BuyMarket();
					lastSignal = candle.OpenTime;
				}
				else if (candle.ClosePrice < lower && Position >= 0)
				{
					SellMarket();
					lastSignal = candle.OpenTime;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
