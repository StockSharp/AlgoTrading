using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Probability of ATR Index indicator.
/// It trades when probability crosses its long-term average.
/// </summary>
public class ProbabilityOfAtrIndexStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrDistance;
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _smaHigh;
	private SimpleMovingAverage _smaLow;
	private StandardDeviation _sdHigh;
	private StandardDeviation _sdLow;
	private SimpleMovingAverage _probSma;

	private bool _prevIsAbove;

	/// <summary>
	/// ATR distance multiplier.
	/// </summary>
	public decimal AtrDistance
	{
		get => _atrDistance.Value;
		set => _atrDistance.Value = value;
	}

	/// <summary>
	/// Number of bars for calculations.
	/// </summary>
	public int Bars
	{
		get => _bars.Value;
		set => _bars.Value = value;
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
	/// Initialize the Probability of ATR Index strategy.
	/// </summary>
	public ProbabilityOfAtrIndexStrategy()
	{
		_atrDistance = Param(nameof(AtrDistance), 1.5m)
			.SetDisplay("ATR Distance", "ATR distance multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_bars = Param(nameof(Bars), 8)
			.SetDisplay("Bars", "Number of bars for calculations", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var length = Math.Max(8, Bars);

		_atr = new AverageTrueRange { Length = length };
		_smaHigh = new SimpleMovingAverage { Length = length };
		_smaLow = new SimpleMovingAverage { Length = length };
		_sdHigh = new StandardDeviation { Length = length };
		_sdLow = new StandardDeviation { Length = length };
		_probSma = new SimpleMovingAverage { Length = 1000 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr!.Process(candle).GetValue<decimal>();

		var highMean = _smaHigh!.Process(new DecimalIndicatorValue(_smaHigh, candle.HighPrice)).GetValue<decimal>();
		var lowMean = _smaLow!.Process(new DecimalIndicatorValue(_smaLow, candle.LowPrice)).GetValue<decimal>();
		var highStd = _sdHigh!.Process(new DecimalIndicatorValue(_sdHigh, candle.HighPrice)).GetValue<decimal>();
		var lowStd = _sdLow!.Process(new DecimalIndicatorValue(_sdLow, candle.LowPrice)).GetValue<decimal>();

		var meanDiff = highMean - lowMean;
		var variance = (highStd * highStd + lowStd * lowStd) / 2m + (meanDiff * meanDiff) / 4m;
		if (variance <= 0m)
			return;

		var a = (decimal)Math.Sqrt((double)variance);
		if (a == 0m || Bars == 0)
			return;

		var d = (AtrDistance * atrValue) / (Bars * a);

		const decimal a1 = 0.278393m;
		const decimal a2 = 0.230389m;
		const decimal a3 = 0.000972m;
		const decimal a4 = 0.078108m;

		var z = d / (decimal)Math.Sqrt(2);
		var z2 = z * z;
		var z3 = z2 * z;
		var z4 = z2 * z2;

		var de = 1m + a1 * z + a2 * z2 + a3 * z3 + a4 * z4;
		var den = de * de * de * de;
		var fx = 0.5m * (1m - 1m / den);

		var probability = 100m * (0.5m - fx);

		var avg = _probSma!.Process(new DecimalIndicatorValue(_probSma, probability, candle.ServerTime)).GetValue<decimal>();

		var isAbove = probability > avg;

		if (isAbove && !_prevIsAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (!isAbove && _prevIsAbove && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevIsAbove = isAbove;
	}
}
