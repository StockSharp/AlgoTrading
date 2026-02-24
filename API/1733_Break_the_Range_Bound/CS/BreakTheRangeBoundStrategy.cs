using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that waits for three SMAs to stay within a small range
/// and trades when price leaves this range.
/// </summary>
public class BreakTheRangeBoundStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSma;
	private readonly StrategyParam<int> _midSma;
	private readonly StrategyParam<int> _slowSma;
	private readonly StrategyParam<decimal> _shakeThreshold;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _rangeHigh;
	private Lowest _rangeLow;

	private readonly List<decimal> _diffBuffer = new();
	private decimal _entryPrice;
	private decimal _rangeHighAtEntry;
	private decimal _rangeLowAtEntry;
	private decimal _prevHighest;
	private decimal _prevLowest;

	public int FastSma { get => _fastSma.Value; set => _fastSma.Value = value; }
	public int MidSma { get => _midSma.Value; set => _midSma.Value = value; }
	public int SlowSma { get => _slowSma.Value; set => _slowSma.Value = value; }
	public decimal ShakeThreshold { get => _shakeThreshold.Value; set => _shakeThreshold.Value = value; }
	public int RangeLength { get => _rangeLength.Value; set => _rangeLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakTheRangeBoundStrategy()
	{
		_fastSma = Param(nameof(FastSma), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast moving average period", "Parameters");

		_midSma = Param(nameof(MidSma), 30)
			.SetGreaterThanZero()
			.SetDisplay("Mid SMA", "Middle moving average period", "Parameters");

		_slowSma = Param(nameof(SlowSma), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow moving average period", "Parameters");

		_shakeThreshold = Param(nameof(ShakeThreshold), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Shake Threshold", "Max SMA spread to treat as range", "Range");

		_rangeLength = Param(nameof(RangeLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Number of candles in range", "Range");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SMA { Length = FastSma };
		var midMa = new SMA { Length = MidSma };
		var slowMa = new SMA { Length = SlowSma };

		_rangeHigh = new Highest { Length = RangeLength };
		_rangeLow = new Lowest { Length = RangeLength };
		_diffBuffer.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(new IIndicator[] { fastMa, midMa, slowMa }, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = values[0].IsEmpty ? (decimal?)null : values[0].GetValue<decimal>();
		var mid = values[1].IsEmpty ? (decimal?)null : values[1].GetValue<decimal>();
		var slow = values[2].IsEmpty ? (decimal?)null : values[2].GetValue<decimal>();

		if (fast is null || mid is null || slow is null)
			return;

		var maxSma = Math.Max(fast.Value, Math.Max(mid.Value, slow.Value));
		var minSma = Math.Min(fast.Value, Math.Min(mid.Value, slow.Value));
		var diff = maxSma - minSma;

		// Track max diff over RangeLength candles
		_diffBuffer.Add(diff);
		if (_diffBuffer.Count > RangeLength)
			_diffBuffer.RemoveAt(0);

		var highestVal = _rangeHigh.Process(candle);
		var lowestVal = _rangeLow.Process(candle);

		if (!_rangeHigh.IsFormed || !_rangeLow.IsFormed || _diffBuffer.Count < RangeLength)
			return;

		// Find max diff in buffer
		var maxDiff = decimal.MinValue;
		for (var i = 0; i < _diffBuffer.Count; i++)
		{
			if (_diffBuffer[i] > maxDiff)
				maxDiff = _diffBuffer[i];
		}

		var highest = highestVal.ToDecimal();
		var lowest = lowestVal.ToDecimal();

		if (Position == 0)
		{
			if (maxDiff < ShakeThreshold && _prevHighest > 0)
			{
				if (candle.ClosePrice > _prevHighest)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_rangeHighAtEntry = highest;
					_rangeLowAtEntry = lowest;
				}
				else if (candle.ClosePrice < _prevLowest)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_rangeHighAtEntry = highest;
					_rangeLowAtEntry = lowest;
				}
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice < _rangeLowAtEntry ||
				candle.ClosePrice - _entryPrice > 4m * (_rangeHighAtEntry - _rangeLowAtEntry))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice > _rangeHighAtEntry ||
				_entryPrice - candle.ClosePrice > 4m * (_rangeHighAtEntry - _rangeLowAtEntry))
				BuyMarket(Math.Abs(Position));
		}

		_prevHighest = highest;
		_prevLowest = lowest;
	}
}
