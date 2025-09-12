using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that detects price outliers using N-sigma confidence intervals.
/// Trades against extreme price changes expecting mean reversion.
/// </summary>
public class OutlierDetectorWithNSigmaConfidenceIntervalsStrategy : Strategy
{
	private readonly StrategyParam<int> _sampleSize;
	private readonly StrategyParam<decimal> _firstLimit;
	private readonly StrategyParam<decimal> _secondLimit;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev;
	private decimal _previousClose;

	/// <summary>
	/// Sample size for standard deviation calculation.
	/// </summary>
	public int SampleSize
	{
		get => _sampleSize.Value;
		set => _sampleSize.Value = value;
	}

	/// <summary>
	/// First z-score limit for exiting positions.
	/// </summary>
	public decimal FirstLimit
	{
		get => _firstLimit.Value;
		set => _firstLimit.Value = value;
	}

	/// <summary>
	/// Second z-score limit for entering positions.
	/// </summary>
	public decimal SecondLimit
	{
		get => _secondLimit.Value;
		set => _secondLimit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OutlierDetectorWithNSigmaConfidenceIntervalsStrategy()
	{
		_sampleSize = Param(nameof(SampleSize), 30)
			.SetGreaterThanZero()
			.SetDisplay("Sample Size", "Number of periods for standard deviation", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_firstLimit = Param(nameof(FirstLimit), 2m)
			.SetGreaterThanZero()
			.SetDisplay("First Limit", "Z-score exit threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_secondLimit = Param(nameof(SecondLimit), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Second Limit", "Z-score entry threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(2m, 5m, 0.5m);

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

		_stdDev = null;
		_previousClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stdDev = new() { Length = SampleSize };

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_previousClose == 0m)
		{
			_previousClose = close;
			return;
		}

		var dif = close - _previousClose;
		var std = _stdDev.Process(dif).ToDecimal();
		_previousClose = close;

		if (!_stdDev.IsFormed || std == 0)
			return;

		var z = dif / std;
		LogInfo($"Dif: {dif:F4}, Std: {std:F4}, Z: {z:F4}");

		if (z > SecondLimit)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (z < -SecondLimit)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Math.Abs(z) < FirstLimit && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
		}
	}
}
