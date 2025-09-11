using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Collects price displacement samples and prepares them for scatter or heatmap analysis.
/// </summary>
public class DataChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _duration;
	private readonly StrategyParam<bool> _useAtrReference;
	private readonly StrategyParam<int> _atrLength;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Duration { get => _duration.Value; set => _duration.Value = value; }
	public bool UseAtrReference { get => _useAtrReference.Value; set => _useAtrReference.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	private readonly AverageTrueRange _atr;
	private readonly Highest _highest;
	private readonly Lowest _lowest;
	private readonly Queue<decimal> _priceBuffer = new();
	private readonly Queue<decimal> _atrBuffer = new();
	private readonly Chart _chart = new();

	public DataChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_duration = Param(nameof(Duration), 10)
		.SetGreaterThanZero()
		.SetDisplay("Duration", "Number of candles to measure displacement", "Chart")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_useAtrReference = Param(nameof(UseAtrReference), true)
		.SetDisplay("Use ATR", "Measure displacement in ATR units", "Chart");

		_atrLength = Param(nameof(AtrLength), 24)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period for normalization", "Chart")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_atr = new AverageTrueRange { Length = AtrLength };
		_highest = new Highest { Length = Duration };
		_lowest = new Lowest { Length = Duration };
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		DrawCandles(area, subscription);
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var atrValue = _atr.Process(candle).ToNullableDecimal();
		var highest = _highest.Process(candle.HighPrice).ToNullableDecimal();
		var lowest = _lowest.Process(candle.LowPrice).ToNullableDecimal();

		if (atrValue is null || highest is null || lowest is null)
		return;

		_priceBuffer.Enqueue(candle.ClosePrice);
		_atrBuffer.Enqueue(atrValue.Value);

		if (_priceBuffer.Count <= Duration)
		return;

		var price = _priceBuffer.Dequeue();
		var atrRef = _atrBuffer.Dequeue();

		var positive = Math.Abs(highest.Value - price);
		var negative = Math.Abs(price - lowest.Value);

		var x = UseAtrReference ? positive / atrRef : positive * 100m / price;
		var y = UseAtrReference ? negative / atrRef : negative * 100m / price;

		_chart.AddSample(x, y);
		_chart.Draw();
	}

	private class Sample
	{
		public decimal XValue { get; }
		public decimal YValue { get; }

		public Sample(decimal x, decimal y)
		{
			XValue = x;
			YValue = y;
		}
	}

	private class Chart
	{
		public List<Sample> Samples { get; } = new();

		public void AddSample(decimal x, decimal y)
		{
			Samples.Add(new Sample(x, y));
		}

		public void Draw()
		{
			// Visualization can be added if needed.
		}
	}
}
