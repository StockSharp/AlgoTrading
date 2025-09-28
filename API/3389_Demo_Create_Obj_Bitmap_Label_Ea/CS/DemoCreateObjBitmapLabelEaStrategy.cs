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

using StockSharp.Algo;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demo_Create_OBJ_BITMAP_LABEL_EA translation (289).
/// Simulates a bitmap label button by alternating between two text values on the chart.
/// </summary>
public class DemoCreateObjBitmapLabelEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _pressedText;
	private readonly StrategyParam<string> _releasedText;
	private readonly StrategyParam<decimal> _priceOffset;
	private readonly StrategyParam<int> _switchInterval;

	private IChartArea _chartArea;
	private int _processedCandles;
	private bool _isPressed;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Text displayed when the virtual button is considered pressed.
	/// </summary>
	public string PressedText
	{
		get => _pressedText.Value;
		set => _pressedText.Value = value;
	}

	/// <summary>
	/// Text displayed when the virtual button is considered released.
	/// </summary>
	public string ReleasedText
	{
		get => _releasedText.Value;
		set => _releasedText.Value = value;
	}

	/// <summary>
	/// Vertical offset from candle close price for text placement.
	/// </summary>
	public decimal PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// Number of finished candles before the label toggles state.
	/// </summary>
	public int SwitchInterval
	{
		get => _switchInterval.Value;
		set => _switchInterval.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DemoCreateObjBitmapLabelEaStrategy"/>.
	/// </summary>
	public DemoCreateObjBitmapLabelEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_pressedText = Param(nameof(PressedText), "€")
			.SetDisplay("Pressed text", "Text shown when button is pressed", "Visualization")
			.SetCanOptimize(false);

		_releasedText = Param(nameof(ReleasedText), "$")
			.SetDisplay("Released text", "Text shown when button is released", "Visualization")
			.SetCanOptimize(false);

		_priceOffset = Param(nameof(PriceOffset), 0m)
			.SetDisplay("Price offset", "Shift label vertically", "Visualization");

		_switchInterval = Param(nameof(SwitchInterval), 1)
			.SetDisplay("Switch interval", "Finished candles between state changes", "Visualization")
			.SetCanOptimize(false);
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

		_processedCandles = 0;
		_isPressed = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_chartArea = CreateChartArea();
		if (_chartArea != null)
		{
			DrawCandles(_chartArea, subscription);
		}
	}

	// Toggle the virtual button after a defined number of finished candles.
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _chartArea == null)
			return;

		_processedCandles++;

		if (SwitchInterval <= 0 || _processedCandles % SwitchInterval != 0)
			return;

		var text = _isPressed ? ReleasedText : PressedText;
		var price = candle.ClosePrice + PriceOffset;

		// Draw the current state using the same mechanics as bitmap labels.
		DrawText(_chartArea, candle.OpenTime, price, text);

		_isPressed = !_isPressed;
	}
}

