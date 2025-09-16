using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBJ_LABEL_Example translation (11072).
/// Draws a single text label on the chart.
/// </summary>
public class ObjLabelExampleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _labelText;
	private readonly StrategyParam<decimal> _priceOffset;

	private IChartArea? _area;
	private bool _labelDrawn;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Text displayed on the chart.
	/// </summary>
	public string LabelText
	{
		get => _labelText.Value;
		set => _labelText.Value = value;
	}

	/// <summary>
	/// Vertical offset from candle close price.
	/// </summary>
	public decimal PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ObjLabelExampleStrategy"/>.
	/// </summary>
	public ObjLabelExampleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_labelText = Param(nameof(LabelText), "Simple text")
			.SetDisplay("Label text", "Text shown on chart", "Visualization")
			.SetCanOptimize(false);

		_priceOffset = Param(nameof(PriceOffset), 0m)
			.SetDisplay("Price offset", "Shift label vertically", "Visualization");
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_area = CreateChartArea();
		if (_area != null)
			DrawCandles(_area, subscription);
	}

	// Process finished candles to place the label.
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _labelDrawn || _area == null)
			return;

		var price = candle.ClosePrice + PriceOffset;

		DrawText(_area, candle.OpenTime, price, LabelText);

		_labelDrawn = true;
	}
}
