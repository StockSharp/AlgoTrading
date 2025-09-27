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

using System.Globalization;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Logs chart resolution metrics similar to the original MQL diagnostic script.
/// The strategy does not trade and only prints structural information about the sampled candles.
/// </summary>
public class ChartParametersDiagnosticsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _visibleBars;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private int _digits;
	private decimal _quotePoint;
	private int _totalBars;
	private int _horizontalResolution;

	/// <summary>
	/// Type of candles to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars that emulate the visible width of the chart.
	/// </summary>
	public int VisibleBars
	{
		get => _visibleBars.Value;
		set => _visibleBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChartParametersDiagnosticsStrategy"/> class.
	/// </summary>
	public ChartParametersDiagnosticsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that provides candle data for diagnostics.", "General");

		_visibleBars = Param(nameof(VisibleBars), 100)
			.SetGreaterThanZero()
			.SetDisplay("Visible Bars", "Approximate number of candles that the chart can display at once.", "Chart Layout")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);
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

		_digits = 0;
		_quotePoint = 0m;
		_totalBars = 0;
		_horizontalResolution = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_horizontalResolution = VisibleBars;
		_digits = (int)(Security?.Decimals ?? 0);
		_quotePoint = CalculateQuotePoint();

		LogInfo(Security != null ? $"Security: {Security.Id}" : "Security is not set.");
		LogInfo($"Digits: {_digits}");
		LogInfo($"Quote point: {FormatPrice(_quotePoint)}");
		LogInfo($"Horizontal resolution (bars): {_horizontalResolution}");

		_highest = new Highest
		{
			Length = VisibleBars,
			CandlePrice = CandlePrice.High
		};

		_lowest = new Lowest
		{
			Length = VisibleBars,
			CandlePrice = CandlePrice.Low
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_totalBars++;

		var visibleBars = Math.Min(_horizontalResolution, _totalBars);
		var firstVisible = Math.Max(0, _totalBars - visibleBars);
		var lastVisible = Math.Max(0, _totalBars - 1);
		var shiftBars = _horizontalResolution - visibleBars;

		var priceMax = _highest.IsFormed ? highestValue : Math.Max(candle.HighPrice, highestValue);
		var priceMin = _lowest.IsFormed ? lowestValue : Math.Min(candle.LowPrice, lowestValue);
		var priceRange = priceMax - priceMin;

		var verticalResolution = _quotePoint > 0 ? priceRange / _quotePoint : 0m;

		LogInfo(string.Empty);
		LogInfo($"Horizontal resolution: {_horizontalResolution}");
		LogInfo($"Vertical resolution (points): {verticalResolution}");
		LogInfo("Summary:");
		LogInfo(string.Empty);
		LogInfo($"Total bars processed: {_totalBars}");
		LogInfo($"Visible bars: {visibleBars}");
		LogInfo($"Last visible bar index: {lastVisible}");
		LogInfo($"First visible bar index: {firstVisible}");
		LogInfo($"Shift bars: {shiftBars}");
		LogInfo("Horizontal:");
		LogInfo(string.Empty);
		LogInfo($"Price range: {FormatPrice(priceRange)}");
		LogInfo($"Minimum price: {FormatPrice(priceMin)}");
		LogInfo($"Maximum price: {FormatPrice(priceMax)}");
		LogInfo("Vertical:");
		LogInfo(string.Empty);
		LogInfo("Chart parameters updated.");
	}

	private decimal CalculateQuotePoint()
	{
		if (Security?.PriceStep is decimal step && step > 0)
			return step;

		var digits = Security?.Decimals;

		if (digits is null || digits <= 0)
			return 1m;

		var value = 1m;

		for (var i = 0; i < digits.Value; i++)
		{
			value /= 10m;
		}

		return value;
	}

	private string FormatPrice(decimal value)
	{
		if (_digits <= 0)
			return value.ToString(CultureInfo.InvariantCulture);

		return value.ToString($"F{_digits}", CultureInfo.InvariantCulture);
	}
}
