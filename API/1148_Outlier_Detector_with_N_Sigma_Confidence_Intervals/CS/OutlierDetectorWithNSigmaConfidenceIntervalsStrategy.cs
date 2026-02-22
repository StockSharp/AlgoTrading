using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OutlierDetectorWithNSigmaConfidenceIntervalsStrategy : Strategy
{
	private readonly StrategyParam<int> _sampleSize;
	private readonly StrategyParam<decimal> _firstLimit;
	private readonly StrategyParam<decimal> _secondLimit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousClose;
	private readonly List<decimal> _diffs = new();

	public int SampleSize { get => _sampleSize.Value; set => _sampleSize.Value = value; }
	public decimal FirstLimit { get => _firstLimit.Value; set => _firstLimit.Value = value; }
	public decimal SecondLimit { get => _secondLimit.Value; set => _secondLimit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OutlierDetectorWithNSigmaConfidenceIntervalsStrategy()
	{
		_sampleSize = Param(nameof(SampleSize), 50).SetGreaterThanZero();
		_firstLimit = Param(nameof(FirstLimit), 1.0m);
		_secondLimit = Param(nameof(SecondLimit), 2.0m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousClose = 0;
		_diffs.Clear();

		var sma = new SimpleMovingAverage { Length = SampleSize };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		if (_previousClose == 0m)
		{
			_previousClose = close;
			return;
		}

		var dif = close - _previousClose;
		_diffs.Add(dif);
		_previousClose = close;

		if (_diffs.Count < SampleSize)
			return;

		// Calculate std manually
		var recent = _diffs.Skip(_diffs.Count - SampleSize).Take(SampleSize).ToList();
		var mean = recent.Average();
		var variance = recent.Sum(d => (d - mean) * (d - mean)) / recent.Count;
		var std = (decimal)Math.Sqrt((double)variance);

		if (std == 0) return;

		var z = dif / std;

		if (z > SecondLimit && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}
		else if (z < -SecondLimit && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (Math.Abs(z) < FirstLimit && Position != 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			else BuyMarket(Math.Abs(Position));
		}

		if (_diffs.Count > SampleSize * 3)
			_diffs.RemoveRange(0, _diffs.Count - SampleSize * 3);
	}
}
