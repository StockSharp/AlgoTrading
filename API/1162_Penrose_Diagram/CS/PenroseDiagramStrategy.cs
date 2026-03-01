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
/// Draws a simplified Penrose diagram based on breakout distances.
/// </summary>
public class PenroseDiagramStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _extend;

	private SimpleMovingAverage _avgHigh = null!;
	private SimpleMovingAverage _avgHighTrue = null!;
	private SimpleMovingAverage _avgLow = null!;
	private SimpleMovingAverage _avgLowTrue = null!;
	private Highest _maxHigh = null!;
	private Highest _maxLow = null!;

	/// <summary>
	/// Period for averages.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
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
	/// Whether to draw extended triangles.
	/// </summary>
	public bool Extend
	{
		get => _extend.Value;
		set => _extend.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PenroseDiagramStrategy"/>.
	/// </summary>
	public PenroseDiagramStrategy()
	{
		_period = Param(nameof(Period), 48)
			.SetGreaterThanZero()
			.SetDisplay("Range Period", "Number of periods for averages", "General")
			
			.SetOptimize(10, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candle timeframe", "General");

		_extend = Param(nameof(Extend), true)
			.SetDisplay("Extend Diagram", "Draw additional triangles", "Drawing");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_avgHigh = new SimpleMovingAverage { Length = Period };
		_avgHighTrue = new SimpleMovingAverage { Length = Period };
		_avgLow = new SimpleMovingAverage { Length = Period };
		_avgLowTrue = new SimpleMovingAverage { Length = Period };
		_maxHigh = new Highest { Length = Period };
		_maxLow = new Highest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var highDist = Math.Max(0m, high - open);
		var lowDist = Math.Max(0m, open - low);

		var avgHigh = _avgHigh.Process(new DecimalIndicatorValue(_avgHigh, highDist, candle.OpenTime)).ToDecimal();
		var avgHighTrue = _avgHighTrue.Process(new DecimalIndicatorValue(_avgHighTrue, highDist, candle.OpenTime)).ToDecimal();
		var avgLow = _avgLow.Process(new DecimalIndicatorValue(_avgLow, lowDist, candle.OpenTime)).ToDecimal();
		var avgLowTrue = _avgLowTrue.Process(new DecimalIndicatorValue(_avgLowTrue, lowDist, candle.OpenTime)).ToDecimal();
		var maxHigh = _maxHigh.Process(new DecimalIndicatorValue(_maxHigh, highDist, candle.OpenTime)).ToDecimal();
		var maxLow = _maxLow.Process(new DecimalIndicatorValue(_maxLow, lowDist, candle.OpenTime)).ToDecimal();

		var openTime = candle.OpenTime;
		var closeTime = candle.CloseTime;
		var middleTime = openTime + (closeTime - openTime) / 2;

		// Base triangle

		if (Extend)
		{
		}
	}
}
