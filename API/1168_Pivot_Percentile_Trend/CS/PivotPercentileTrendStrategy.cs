using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot Percentile Trend Strategy.
/// Uses percentile based trend strength and SuperTrend filter.
/// </summary>
public class PivotPercentileTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _percentileLength;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendFactor;
private readonly StrategyParam<Sides?> _direction;
	
	private SuperTrend _supertrend;
	private int[] _lengths = [];
	private List<Queue<decimal>> _highQueues = [];
	private List<Queue<decimal>> _lowQueues = [];
	private readonly Queue<decimal> _high144 = new();
	private readonly Queue<decimal> _low144 = new();
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Base length for percentile calculations.
	/// </summary>
	public int PercentileLength
	{
		get => _percentileLength.Value;
		set => _percentileLength.Value = value;
	}
	
	/// <summary>
	/// SuperTrend ATR length.
	/// </summary>
	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}
	
	/// <summary>
	/// SuperTrend multiplier.
	/// </summary>
	public decimal SupertrendFactor
	{
		get => _supertrendFactor.Value;
		set => _supertrendFactor.Value = value;
	}
	
	/// <summary>
	/// Allowed trading direction.
	/// </summary>
public Sides? Direction
{
get => _direction.Value;
set => _direction.Value = value;
}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public PivotPercentileTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
	
		_percentileLength = Param(nameof(PercentileLength), 10)
	.SetGreaterThanZero()
	.SetDisplay("Percentile Length", "Base length for percentile arrays", "Percentile");
	
		_supertrendLength = Param(nameof(SupertrendLength), 20)
	.SetGreaterThanZero()
	.SetDisplay("SuperTrend Length", "ATR period for SuperTrend", "SuperTrend");
	
		_supertrendFactor = Param(nameof(SupertrendFactor), 16m)
	.SetGreaterThanZero()
	.SetDisplay("SuperTrend Factor", "Multiplier for SuperTrend", "SuperTrend");
	
_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trading Direction", "Allowed trade direction", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();
	
		_highQueues.Clear();
		_lowQueues.Clear();
		_high144.Clear();
		_low144.Clear();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
		_supertrend = new SuperTrend { Length = SupertrendLength, Multiplier = SupertrendFactor };
	
		_lengths = new[]
	{
	PercentileLength,
	PercentileLength * 2,
	PercentileLength * 3,
	PercentileLength * 4,
	PercentileLength * 5,
	PercentileLength * 6,
	PercentileLength * 7,
	};
	
		_highQueues = new List<Queue<decimal>>(_lengths.Length);
		_lowQueues = new List<Queue<decimal>>(_lengths.Length);
		for (var i = 0; i < _lengths.Length; i++)
	{
		_highQueues.Add(new Queue<decimal>());
		_lowQueues.Add(new Queue<decimal>());
	}
	
		var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_supertrend, ProcessCandle)
	.Start();
	
		var area = CreateChartArea();
		if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _supertrend);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
	
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
	
		_high144.Enqueue(candle.HighPrice);
		if (_high144.Count > 144)
		_high144.Dequeue();
	
		_low144.Enqueue(candle.LowPrice);
		if (_low144.Count > 144)
		_low144.Dequeue();
	
		for (var i = 0; i < _lengths.Length; i++)
	{
		var len = _lengths[i];
		var hq = _highQueues[i];
		var lq = _lowQueues[i];
	
	hq.Enqueue(candle.HighPrice);
		if (hq.Count > len)
	hq.Dequeue();
	
	lq.Enqueue(candle.LowPrice);
		if (lq.Count > len)
	lq.Dequeue();
	}
	
		if (_high144.Count < 144 || _low144.Count < 144)
		return;
	
		for (var i = 0; i < _lengths.Length; i++)
	{
		if (_highQueues[i].Count < _lengths[i] || _lowQueues[i].Count < _lengths[i])
		return;
	}
	
		var highestHigh = GetPercentile(_high144, 0.75m);
		var lowestLow = GetPercentile(_low144, 0.25m);
	
		var countBull = 0;
		var countBear = 0;
		var weakBull = 0;
		var weakBear = 0;
	
		for (var i = 0; i < _lengths.Length; i++)
	{
		var highPerc = GetPercentile(_highQueues[i], 0.75m);
		var lowPerc = GetPercentile(_lowQueues[i], 0.25m);
	
		var trendBullHigh = highPerc > highestHigh;
		var trendBullLow = lowPerc > highestHigh;
		var trendBearHigh = highPerc < lowestLow;
		var trendBearLow = lowPerc < lowestLow;
		var wBull = lowPerc < highestHigh && lowPerc > lowestLow;
		var wBear = highPerc > lowestLow && highPerc < highestHigh;
	
		if (trendBullHigh)
	countBull++;
		if (trendBullLow)
	countBull++;
		if (trendBearHigh)
	countBear++;
		if (trendBearLow)
	countBear++;
		if (wBull)
	weakBull++;
		if (wBear)
	weakBear++;
	}
	
		var bullStrength = countBull + 0.5m * weakBull - 0.5m * weakBear - countBear;
		var bearStrength = countBear + 0.5m * weakBear - 0.5m * weakBull - countBull;
		var trendValue = bullStrength - bearStrength;
	
		var st = (SuperTrendIndicatorValue)stValue;
		var priceAbove = candle.ClosePrice > st.Value;
		var priceBelow = candle.ClosePrice < st.Value;
	
		var enterLong = trendValue > 0 && priceAbove;
		var enterShort = trendValue < 0 && priceBelow;
	
var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;

if (allowLong && enterLong && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (allowShort && enterShort && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

if (allowLong && enterShort && Position > 0)
SellMarket(Math.Abs(Position));
else if (allowShort && enterLong && Position < 0)
BuyMarket(Math.Abs(Position));
	}
	
	private static decimal GetPercentile(Queue<decimal> values, decimal percentile)
	{
		var arr = values.ToArray();
	Array.Sort(arr);
		var pos = (arr.Length - 1) * percentile;
		var idx = (int)pos;
		var frac = pos - idx;
		var lower = arr[idx];
		var upper = arr[Math.Min(idx + 1, arr.Length - 1)];
		return lower + (upper - lower) * frac;
	}
	}
