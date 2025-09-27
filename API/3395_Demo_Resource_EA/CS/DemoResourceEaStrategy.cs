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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demo_resource_EA translation (293).
/// Draws a currency icon on the chart using text to emulate bitmap labels.
/// </summary>
public class DemoResourceEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _priceOffset;
	private readonly StrategyParam<TimeSpan> _timeOffset;
	private readonly StrategyParam<bool> _showDollarIcon;

	private IChartArea? _area;
	private bool _labelDrawn;
	private bool _isDollarDisplayed;

	/// <summary>
	/// Candle type that drives label placement.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Vertical distance from candle close to the icon.
	/// </summary>
	public decimal PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// Horizontal offset applied to the candle time for positioning.
	/// </summary>
	public TimeSpan TimeOffset
	{
		get => _timeOffset.Value;
		set => _timeOffset.Value = value;
	}

	/// <summary>
	/// Displays a dollar icon instead of the euro icon when true.
	/// </summary>
	public bool ShowDollarIcon
	{
		get => _showDollarIcon.Value;
		set => _showDollarIcon.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DemoResourceEaStrategy"/> class.
	/// </summary>
	public DemoResourceEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle series", "Visualization");

		_priceOffset = Param(nameof(PriceOffset), 0m)
			.SetDisplay("Price Offset", "Shift icon vertically", "Visualization");

		_timeOffset = Param(nameof(TimeOffset), TimeSpan.Zero)
			.SetDisplay("Time Offset", "Shift icon horizontally", "Visualization");

		_showDollarIcon = Param(nameof(ShowDollarIcon), false)
			.SetDisplay("Use Dollar Icon", "Switch between euro and dollar icons", "Visualization");
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

		_area = null;
		_labelDrawn = false;
		_isDollarDisplayed = ShowDollarIcon;
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
		{
			DrawCandles(_area, subscription);
		}
	}

	// Render the appropriate icon when a candle finishes.
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _area == null)
			return;

		if (!_labelDrawn)
		{
			DrawLabel(candle);
			_labelDrawn = true;
			return;
		}

		if (_isDollarDisplayed != ShowDollarIcon)
		{
			DrawLabel(candle);
		}
	}

	// Draws the icon text with configured offsets.
	private void DrawLabel(ICandleMessage candle)
	{
		var targetTime = candle.OpenTime + TimeOffset;
		var price = candle.ClosePrice + PriceOffset;
		var icon = ShowDollarIcon ? "$" : "€";

		DrawText(_area!, targetTime, price, icon);

		_isDollarDisplayed = ShowDollarIcon;
	}
}

