using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Decorative strategy that plots a row of static arrows on the price chart.
/// </summary>
public class StaticArrowEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _arrowCount;
	private readonly StrategyParam<decimal> _priceOffset;
	private readonly StrategyParam<string> _arrowSymbol;

	private readonly List<(DateTimeOffset Time, decimal Price)> _arrowPoints = new();
	private IChartArea? _area;
	private bool _isInitialized;
	private TimeSpan _timeFrame;

	/// <summary>
	/// Candle series used for positioning the arrows.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of arrow markers drawn on the chart.
	/// </summary>
	public int ArrowCount
	{
		get => _arrowCount.Value;
		set => _arrowCount.Value = value;
	}

	/// <summary>
	/// Price offset added to the base candle price when drawing arrows.
	/// </summary>
	public decimal PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// Text symbol rendered for every arrow marker.
	/// </summary>
	public string ArrowSymbol
	{
		get => _arrowSymbol.Value;
		set => _arrowSymbol.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StaticArrowEaStrategy"/>.
	/// </summary>
	public StaticArrowEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_arrowCount = Param(nameof(ArrowCount), 40)
			.SetGreaterThanZero()
			.SetDisplay("Arrow Count", "Number of chart arrows", "Drawing")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 5);

		_priceOffset = Param(nameof(PriceOffset), 0m)
			.SetDisplay("Price Offset", "Offset added to the base price", "Drawing");

		_arrowSymbol = Param(nameof(ArrowSymbol), "↓")
			.SetDisplay("Arrow Symbol", "Text marker used for drawing", "Drawing");
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

		// Clean up cached chart data when the strategy is reset.
		_arrowPoints.Clear();
		_area = null;
		_isInitialized = false;
		_timeFrame = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Determine the timeframe from the selected candle type.
		if (CandleType.Arg is not TimeSpan frame)
			throw new InvalidOperationException("The candle type must provide a TimeSpan argument.");

		_timeFrame = frame;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_area = CreateChartArea();
		if (_area != null)
		{
			DrawCandles(_area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only process completed candles to avoid partial updates.
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			InitializeArrows(candle);
			_isInitialized = true;
		}

		DrawArrows();
	}

	private void InitializeArrows(ICandleMessage candle)
	{
		// Use the latest candle as the anchor for arrow placement.
		_arrowPoints.Clear();

		var baseTime = candle.OpenTime;
		var basePrice = candle.ClosePrice + PriceOffset;

		for (var i = 0; i < ArrowCount; i++)
		{
			var offset = TimeSpan.FromTicks(_timeFrame.Ticks * i);
			var time = baseTime - offset;
			_arrowPoints.Add((time, basePrice));
		}
	}

	private void DrawArrows()
	{
		if (_area == null || _arrowPoints.Count == 0)
			return;

		// Render all stored arrow markers on the chart.
		foreach (var (time, price) in _arrowPoints)
		{
			DrawText(_area, time, price, ArrowSymbol);
		}
	}
}
