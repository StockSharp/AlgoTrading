using System;
using System.Collections.Generic;

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

	private Highest _diffHighest = null!;
	private Highest _rangeHigh = null!;
	private Lowest _rangeLow = null!;

	private decimal _entryPrice;
	private decimal _rangeHighAtEntry;
	private decimal _rangeLowAtEntry;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastSma
	{
		get => _fastSma.Value;
		set => _fastSma.Value = value;
	}

	/// <summary>
	/// Mid SMA period.
	/// </summary>
	public int MidSma
	{
		get => _midSma.Value;
		set => _midSma.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowSma
	{
		get => _slowSma.Value;
		set => _slowSma.Value = value;
	}

	/// <summary>
	/// Maximum difference between SMAs during range period.
	/// </summary>
	public decimal ShakeThreshold
	{
		get => _shakeThreshold.Value;
		set => _shakeThreshold.Value = value;
	}

	/// <summary>
	/// Number of candles to analyse for range detection.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
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
	/// Initializes parameters.
	/// </summary>
	public BreakTheRangeBoundStrategy()
	{
		_fastSma = Param(nameof(FastSma), 38)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_midSma = Param(nameof(MidSma), 140)
			.SetGreaterThanZero()
			.SetDisplay("Mid SMA", "Middle moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_slowSma = Param(nameof(SlowSma), 210)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 10);

		_shakeThreshold = Param(nameof(ShakeThreshold), 250m)
			.SetGreaterThanZero()
			.SetDisplay("Shake Threshold", "Max SMA spread to treat as range", "Range")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_rangeLength = Param(nameof(RangeLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Number of candles in range", "Range")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
		_rangeHighAtEntry = 0m;
		_rangeLowAtEntry = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SimpleMovingAverage { Length = FastSma };
		var midMa = new SimpleMovingAverage { Length = MidSma };
		var slowMa = new SimpleMovingAverage { Length = SlowSma };

		_diffHighest = new Highest { Length = RangeLength };
		_rangeHigh = new Highest { Length = RangeLength };
		_rangeLow = new Lowest { Length = RangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, midMa, slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, midMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal mid, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxSma = Math.Max(fast, Math.Max(mid, slow));
		var minSma = Math.Min(fast, Math.Min(mid, slow));
		var diff = maxSma - minSma;

		var maxDiff = _diffHighest.Process(diff).ToDecimal();
		var highest = _rangeHigh.Process(candle).ToDecimal();
		var lowest = _rangeLow.Process(candle).ToDecimal();

		if (!_diffHighest.IsFormed || !_rangeHigh.IsFormed || !_rangeLow.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (maxDiff < ShakeThreshold)
			{
				if (candle.ClosePrice > highest)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_rangeHighAtEntry = highest;
					_rangeLowAtEntry = lowest;
				}
				else if (candle.ClosePrice < lowest)
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
	}
}
