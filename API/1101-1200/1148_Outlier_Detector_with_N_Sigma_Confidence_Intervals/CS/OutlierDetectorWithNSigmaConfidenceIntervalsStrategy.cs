using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Outlier detector with N-sigma confidence intervals.
/// The close-to-close price change is turned into a z-score against the mean and standard deviation of the last SampleSize changes.
/// A z-score above SecondLimit goes short and one below -SecondLimit goes long, reversing an opposite position.
/// The position is closed once the absolute z-score falls back below FirstLimit.
/// </summary>
public class OutlierDetectorWithNSigmaConfidenceIntervalsStrategy : Strategy
{
	private readonly StrategyParam<int> _sampleSize;
	private readonly StrategyParam<decimal> _firstLimit;
	private readonly StrategyParam<decimal> _secondLimit;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _mean;
	private StandardDeviation _stdDev;
	private decimal? _prevClose;

	/// <summary>
	/// Number of price changes in the sample.
	/// </summary>
	public int SampleSize
	{
		get => _sampleSize.Value;
		set => _sampleSize.Value = value;
	}

	/// <summary>
	/// Z-score below which the position is closed.
	/// </summary>
	public decimal FirstLimit
	{
		get => _firstLimit.Value;
		set => _firstLimit.Value = value;
	}

	/// <summary>
	/// Z-score beyond which a move counts as an outlier.
	/// </summary>
	public decimal SecondLimit
	{
		get => _secondLimit.Value;
		set => _secondLimit.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OutlierDetectorWithNSigmaConfidenceIntervalsStrategy()
	{
		_sampleSize = Param(nameof(SampleSize), 30)
			.SetGreaterThanZero()
			.SetDisplay("Sample Size", "Number of price changes in the sample", "Indicators");

		_firstLimit = Param(nameof(FirstLimit), 2m)
			.SetGreaterThanZero()
			.SetDisplay("First Limit", "Z-score below which the position is closed", "Signals");

		_secondLimit = Param(nameof(SecondLimit), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Second Limit", "Z-score beyond which a move counts as an outlier", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_mean = new SimpleMovingAverage { Length = SampleSize };
		_stdDev = new StandardDeviation { Length = SampleSize };

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

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal prev)
			return;

		var change = candle.ClosePrice - prev;
		var meanValue = _mean.Process(new DecimalIndicatorValue(_mean, change, candle.OpenTime) { IsFinal = true });
		var stdValue = _stdDev.Process(new DecimalIndicatorValue(_stdDev, change, candle.OpenTime) { IsFinal = true });

		if (!_mean.IsFormed || !_stdDev.IsFormed)
			return;

		var std = stdValue.GetValue<decimal>();
		if (std <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var z = (change - meanValue.GetValue<decimal>()) / std;

		if (z > SecondLimit && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (z < -SecondLimit && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position != 0 && Math.Abs(z) < FirstLimit)
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(-Position);
		}
	}
}
