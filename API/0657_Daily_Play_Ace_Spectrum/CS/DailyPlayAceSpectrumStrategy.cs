using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that draws lines connecting past open prices similar to the TradingView "Day Play Ace Spectrum" study.
/// </summary>
public class DailyPlayAceSpectrumStrategy : Strategy
{
	private readonly StrategyParam<int> _linesCount;
	private readonly StrategyParam<int> _intervalA;
	private readonly StrategyParam<int> _intervalB;
	private readonly StrategyParam<decimal> _scale;
	private readonly StrategyParam<decimal> _thicknessOffset;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly List<ICandleMessage> _candles = new();
	
	/// <summary>
	/// Number of lines to draw.
	/// </summary>
	public int LinesCount
	{
		get => _linesCount.Value;
		set => _linesCount.Value = value;
	}
	
	/// <summary>
	/// Multiplier for the first bar index.
	/// </summary>
	public int IntervalA
	{
		get => _intervalA.Value;
		set => _intervalA.Value = value;
	}
	
	/// <summary>
	/// Multiplier for the second bar index.
	/// </summary>
	public int IntervalB
	{
		get => _intervalB.Value;
		set => _intervalB.Value = value;
	}
	
	/// <summary>
	/// Scaling factor for line thickness.
	/// </summary>
	public decimal Scale
	{
		get => _scale.Value;
		set => _scale.Value = value;
	}
	
	/// <summary>
	/// Offset added to line thickness.
	/// </summary>
	public decimal ThicknessOffset
	{
		get => _thicknessOffset.Value;
		set => _thicknessOffset.Value = value;
	}
	
	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public DailyPlayAceSpectrumStrategy()
	{
		_linesCount = Param(nameof(LinesCount), 120)
		.SetGreaterThanZero()
		.SetDisplay("Lines Count", "Number of lines to draw", "General");
		
		_intervalA = Param(nameof(IntervalA), 18)
		.SetGreaterThanZero()
		.SetDisplay("Interval A", "Multiplier for first bar index", "General");
		
		_intervalB = Param(nameof(IntervalB), 9)
		.SetGreaterThanZero()
		.SetDisplay("Interval B", "Multiplier for second bar index", "General");
		
		_scale = Param(nameof(Scale), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Scale", "Scaling factor for line thickness", "General");
		
		_thicknessOffset = Param(nameof(ThicknessOffset), 4m)
		.SetDisplay("Thickness Offset", "Offset added to line thickness", "General");
		
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
		_candles.Clear();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		DrawCandles(area, subscription);
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_candles.Add(candle);
		
		var maxIndex = LinesCount * Math.Max(IntervalA, IntervalB);
		if (_candles.Count <= maxIndex)
		return;
		
		for (var i = 1; i <= LinesCount; i++)
		{
			var indexA = i * IntervalA;
			var indexB = i * IntervalB;
			
			if (_candles.Count <= indexA || _candles.Count <= indexB)
			continue;
			
			var candleA = _candles[_candles.Count - 1 - indexA];
			var candleB = _candles[_candles.Count - 1 - indexB];
			
			var width = (decimal)i / (LinesCount / (Scale == 0 ? 1m : Scale)) + ThicknessOffset;
			
			DrawLine(candleA.OpenTime, candleA.OpenPrice, candleB.OpenTime, candleB.OpenPrice, thickness: (int)Math.Round(width));
		}
	}
}

