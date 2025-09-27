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
/// Chart button style strategy that tracks a movable price zone.
/// </summary>
public class ChartButtonTestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pricePadding;
	private readonly StrategyParam<TimeSpan> _selectionLength;
	private readonly StrategyParam<bool> _lockTime;

	private decimal _centerPrice;
	private decimal _topPrice;
	private decimal _bottomPrice;
	private DateTimeOffset _startTime;
	private DateTimeOffset _endTime;
	private bool _zoneInitialized;
	private bool _isInsideZone;

	/// <summary>
	/// Initializes a new instance of <see cref="ChartButtonTestStrategy"/>.
	/// </summary>
	public ChartButtonTestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of data to analyse", "General")
			.SetCanOptimize(true);

		_pricePadding = Param(nameof(PricePadding), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Price Padding", "Distance from the centre price to the zone boundaries", "Parameters")
			.SetCanOptimize(true);

		_selectionLength = Param(nameof(SelectionLength), TimeSpan.FromHours(1))
			.SetDisplay("Selection Length", "Duration covered by the virtual button", "Parameters")
			.SetCanOptimize(true);

		_lockTime = Param(nameof(LockTime), false)
			.SetDisplay("Lock Time", "Keep the time window fixed when price updates", "Parameters");
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Padding applied above and below the centre price.
	/// </summary>
	public decimal PricePadding
	{
		get => _pricePadding.Value;
		set
		{
			_pricePadding.Value = value;

			if (_zoneInitialized)
			{
				UpdatePriceWindow(_centerPrice, true);
			}
		}
	}

	/// <summary>
	/// Time span covered by the virtual button.
	/// </summary>
	public TimeSpan SelectionLength
	{
		get => _selectionLength.Value;
		set
		{
			_selectionLength.Value = value;

			if (_zoneInitialized)
			{
				UpdateTimeWindow(_startTime, true);
			}
		}
	}

	/// <summary>
	/// When true, the time window is not moved with the latest candle.
	/// </summary>
	public bool LockTime
	{
		get => _lockTime.Value;
		set => _lockTime.Value = value;
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

		_zoneInitialized = false;
		_isInsideZone = false;
		_centerPrice = 0m;
		_topPrice = 0m;
		_bottomPrice = 0m;
		_startTime = default;
		_endTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;
		var openTime = candle.OpenTime;

		if (!_zoneInitialized)
		{
			InitializeZone(closePrice, openTime);
			return;
		}

		if (!LockTime)
		{
			UpdateTimeWindow(openTime, false);
		}

		UpdatePriceWindow(closePrice, false);

		var isInside = closePrice >= _bottomPrice && closePrice <= _topPrice;

		if (isInside == _isInsideZone)
			return;

		_isInsideZone = isInside;

		if (isInside)
		{
			LogInfo($"Price {closePrice:F4} entered the tracked zone between {_bottomPrice:F4} and {_topPrice:F4}.");
		}
		else
		{
			LogInfo($"Price {closePrice:F4} left the tracked zone between {_bottomPrice:F4} and {_topPrice:F4}.");
		}
	}

	private void InitializeZone(decimal price, DateTimeOffset openTime)
	{
		_zoneInitialized = true;
		_centerPrice = price;
		_topPrice = price + PricePadding;
		_bottomPrice = price - PricePadding;
		_startTime = openTime;
		_endTime = openTime + SelectionLength;

		LogInfo($"Virtual button created at price {price:F4} with padding {PricePadding:F4}. Time window {_startTime:u} - {_endTime:u}.");
	}

	private void UpdateTimeWindow(DateTimeOffset openTime, bool forceLog)
	{
		_startTime = openTime;
		_endTime = openTime + SelectionLength;

		if (forceLog)
		{
			LogInfo($"Time window adjusted to {_startTime:u} - {_endTime:u}.");
		}
	}

	private void UpdatePriceWindow(decimal price, bool forceLog)
	{
		var priceChanged = price != _centerPrice;
		var paddingChanged = PricePadding != (_topPrice - _centerPrice);

		if (!priceChanged && !paddingChanged && !forceLog)
			return;

		_centerPrice = price;
		_topPrice = price + PricePadding;
		_bottomPrice = price - PricePadding;

		var action = priceChanged ? "moved" : "resized";
		LogInfo($"Virtual button {action} to price {_centerPrice:F4}. Range [{_bottomPrice:F4}; {_topPrice:F4}].");
	}
}

