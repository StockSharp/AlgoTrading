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
/// Yeong Relative Rotation Graph strategy.
/// Uses normalized relative strength (price vs SMA) and momentum
/// to classify market into quadrants and trade accordingly.
/// </summary>
public class YeongRrgStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _rsRatioHistory = new();
	private readonly List<decimal> _rmRatioHistory = new();
	private decimal _prevRsRatio;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public YeongRrgStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period for calculations", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsRatioHistory.Clear();
		_rmRatioHistory.Clear();
		_prevRsRatio = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Length };

		_rsRatioHistory.Clear();
		_rmRatioHistory.Clear();
		_prevRsRatio = 0;

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

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (smaVal <= 0)
			return;

		// RS ratio: price relative to its SMA (like relative strength vs benchmark)
		var rsRatio = (candle.ClosePrice / smaVal) * 100m;

		_rsRatioHistory.Add(rsRatio);
		if (_rsRatioHistory.Count > Length * 3)
			_rsRatioHistory.RemoveAt(0);

		// RM ratio: momentum of RS ratio
		decimal rmRatio;
		if (_prevRsRatio > 0)
			rmRatio = rsRatio - _prevRsRatio;
		else
			rmRatio = 0;

		_prevRsRatio = rsRatio;

		_rmRatioHistory.Add(rmRatio);
		if (_rmRatioHistory.Count > Length * 3)
			_rmRatioHistory.RemoveAt(0);

		if (_rsRatioHistory.Count < Length || _rmRatioHistory.Count < Length)
			return;

		// Normalize RS ratio
		var rsMean = _rsRatioHistory.Skip(_rsRatioHistory.Count - Length).Average();
		var rsStd = StdDev(_rsRatioHistory.Skip(_rsRatioHistory.Count - Length));
		if (rsStd == 0) rsStd = 1;

		// Normalize RM ratio
		var rmMean = _rmRatioHistory.Skip(_rmRatioHistory.Count - Length).Average();
		var rmStd = StdDev(_rmRatioHistory.Skip(_rmRatioHistory.Count - Length));
		if (rmStd == 0) rmStd = 1;

		var jdkRs = 100m + ((rsRatio - rsMean) / rsStd);
		var jdkRm = 100m + ((rmRatio - rmMean) / rmStd);

		// Quadrant classification
		// Green: RS > 100 && RM > 100 (leading)
		// Red: RS < 100 && RM < 100 (lagging)
		var buySignal = jdkRs > 100m && jdkRm > 100m;
		var sellSignal = jdkRs < 100m && jdkRm < 100m;

		if (buySignal && Position <= 0)
		{
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket();
		}
	}

	private static decimal StdDev(IEnumerable<decimal> values)
	{
		var list = values.ToList();
		if (list.Count < 2) return 0;
		var mean = list.Average();
		var sumSq = list.Sum(v => (v - mean) * (v - mean));
		return (decimal)Math.Sqrt((double)(sumSq / list.Count));
	}
}
