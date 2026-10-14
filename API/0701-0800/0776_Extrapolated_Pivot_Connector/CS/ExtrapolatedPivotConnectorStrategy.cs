using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Extrapolated Pivot Connector strategy.
/// Pivot highs and lows are bars that stay the extreme for PivotLength bars on each side. The resistance line connects the pivot
/// high HighStart pivots back with the one HighEnd pivots back (0 is the latest) and is extrapolated to the current bar; the
/// support line does the same with LowStart and LowEnd pivot lows. A close crossing above resistance goes long and a close
/// crossing below support goes short, reversing an opposite position.
/// </summary>
public class ExtrapolatedPivotConnectorStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _highStart;
	private readonly StrategyParam<int> _highEnd;
	private readonly StrategyParam<int> _lowStart;
	private readonly StrategyParam<int> _lowEnd;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal high, decimal low)> _window = [];
	private readonly List<(int bar, decimal price)> _pivotHighs = [];
	private readonly List<(int bar, decimal price)> _pivotLows = [];
	private int _barIndex;
	private decimal? _prevClose;
	private decimal? _prevResistance;
	private decimal? _prevSupport;

	/// <summary>
	/// Bars on each side that a pivot must exceed.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Pivot high where the resistance line starts, counted back from the latest (0).
	/// </summary>
	public int HighStart
	{
		get => _highStart.Value;
		set => _highStart.Value = value;
	}

	/// <summary>
	/// Pivot high where the resistance line ends, counted back from the latest (0).
	/// </summary>
	public int HighEnd
	{
		get => _highEnd.Value;
		set => _highEnd.Value = value;
	}

	/// <summary>
	/// Pivot low where the support line starts, counted back from the latest (0).
	/// </summary>
	public int LowStart
	{
		get => _lowStart.Value;
		set => _lowStart.Value = value;
	}

	/// <summary>
	/// Pivot low where the support line ends, counted back from the latest (0).
	/// </summary>
	public int LowEnd
	{
		get => _lowEnd.Value;
		set => _lowEnd.Value = value;
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
	/// Constructor.
	/// </summary>
	public ExtrapolatedPivotConnectorStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars on each side that a pivot must exceed", "Pivots");

		_highStart = Param(nameof(HighStart), 1)
			.SetNotNegative()
			.SetDisplay("High Start", "Pivot high where the resistance line starts", "Pivots");

		_highEnd = Param(nameof(HighEnd), 0)
			.SetNotNegative()
			.SetDisplay("High End", "Pivot high where the resistance line ends", "Pivots");

		_lowStart = Param(nameof(LowStart), 1)
			.SetNotNegative()
			.SetDisplay("Low Start", "Pivot low where the support line starts", "Pivots");

		_lowEnd = Param(nameof(LowEnd), 0)
			.SetNotNegative()
			.SetDisplay("Low End", "Pivot low where the support line ends", "Pivots");

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
		ResetState();
	}

	private void ResetState()
	{
		_window.Clear();
		_pivotHighs.Clear();
		_pivotLows.Clear();
		_barIndex = 0;
		_prevClose = null;
		_prevResistance = null;
		_prevSupport = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bar = _barIndex++;

		UpdatePivots(candle, bar);

		var resistance = LineValue(_pivotHighs, HighStart, HighEnd, bar);
		var support = LineValue(_pivotLows, LowStart, LowEnd, bar);

		var prevClose = _prevClose;
		var prevResistance = _prevResistance;
		var prevSupport = _prevSupport;

		_prevClose = candle.ClosePrice;
		_prevResistance = resistance;
		_prevSupport = support;

		if (prevClose is not decimal lastClose)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var longSignal = resistance is decimal res && prevResistance is decimal lastRes && lastClose <= lastRes && close > res;
		var shortSignal = support is decimal sup && prevSupport is decimal lastSup && lastClose >= lastSup && close < sup;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void UpdatePivots(ICandleMessage candle, int bar)
	{
		var length = PivotLength;
		var size = length * 2 + 1;

		_window.Add((candle.HighPrice, candle.LowPrice));
		if (_window.Count > size)
			_window.RemoveAt(0);

		if (_window.Count < size)
			return;

		// The candidate is the middle bar: it is confirmed only after PivotLength newer bars.
		var (centerHigh, centerLow) = _window[length];
		var isHigh = true;
		var isLow = true;

		for (var i = 0; i < size && (isHigh || isLow); i++)
		{
			if (i == length)
				continue;

			var (high, low) = _window[i];

			if (i < length ? high >= centerHigh : high > centerHigh)
				isHigh = false;

			if (i < length ? low <= centerLow : low < centerLow)
				isLow = false;
		}

		var pivotBar = bar - length;
		var keep = Math.Max(Math.Max(HighStart, HighEnd), Math.Max(LowStart, LowEnd)) + 1;

		if (isHigh)
			AddPivot(_pivotHighs, pivotBar, centerHigh, keep);

		if (isLow)
			AddPivot(_pivotLows, pivotBar, centerLow, keep);
	}

	private static void AddPivot(List<(int bar, decimal price)> pivots, int bar, decimal price, int keep)
	{
		pivots.Insert(0, (bar, price));
		if (pivots.Count > keep)
			pivots.RemoveAt(pivots.Count - 1);
	}

	private static decimal? LineValue(List<(int bar, decimal price)> pivots, int start, int end, int bar)
	{
		if (start >= pivots.Count || end >= pivots.Count)
			return null;

		var (startBar, startPrice) = pivots[start];
		var (endBar, endPrice) = pivots[end];

		if (startBar == endBar)
			return null;

		var slope = (endPrice - startPrice) / (endBar - startBar);
		return endPrice + slope * (bar - endBar);
	}
}
