using System;
using System.Collections.Generic;

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
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var avgHigh = _avgHigh.Process(highDist).ToDecimal();
		var avgHighTrue = _avgHighTrue.Process(highDist).ToDecimal();
		var avgLow = _avgLow.Process(lowDist).ToDecimal();
		var avgLowTrue = _avgLowTrue.Process(lowDist).ToDecimal();
		var maxHigh = _maxHigh.Process(highDist).ToDecimal();
		var maxLow = _maxLow.Process(lowDist).ToDecimal();

		var openTime = candle.OpenTime;
		var closeTime = candle.CloseTime;
		var middleTime = openTime + (closeTime - openTime) / 2;

		// Base triangle
		DrawLine(openTime, open, closeTime, open);
		DrawLine(openTime, open + maxHigh, openTime, open - maxLow);
		DrawLine(openTime, open - maxLow, middleTime, open);
		DrawLine(openTime, open + maxHigh, middleTime, open);
		DrawLine(openTime, open + avgHigh, middleTime, open);
		DrawLine(openTime, open + avgHighTrue, middleTime, open);
		DrawLine(openTime, open - avgLow, middleTime, open);
		DrawLine(openTime, open - avgLowTrue, middleTime, open);

		if (Extend)
		{
			DrawLine(middleTime, open, closeTime, open + maxHigh);
			DrawLine(middleTime, open, closeTime, open - maxLow);
			DrawLine(closeTime, open + maxHigh, closeTime, open - maxLow);
		}
	}
}
