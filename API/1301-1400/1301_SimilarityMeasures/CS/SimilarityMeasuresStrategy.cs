using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates a simple similarity measure: Euclidean distance between price and its SMA.
/// </summary>
public class SimilarityMeasuresStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _distanceThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _previousDistance;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public decimal DistanceThreshold { get => _distanceThreshold.Value; set => _distanceThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SimilarityMeasuresStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA used as the reference series.", "Similarity");
		_distanceThreshold = Param(nameof(DistanceThreshold), 1m)
			.SetNotNegative()
			.SetDisplay("Distance Threshold", "Euclidean distance boundary.", "Similarity");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousDistance = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var distance = Math.Abs(candle.ClosePrice - sma);

		if (_previousDistance is decimal previous)
		{
			if (previous >= DistanceThreshold && distance < DistanceThreshold && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (previous <= DistanceThreshold && distance > DistanceThreshold && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_previousDistance = distance;
	}
}
