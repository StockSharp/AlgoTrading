using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Automatic trendlines strategy.
/// A pivot high is a high above the LeftBars candles before it and the RightBars candles after it, and a pivot low is the
/// mirror. The resistance line connects the last two pivot highs and the support line the last two pivot lows, both extended
/// to the current candle. A close crossing above resistance goes long and a close crossing below support goes short,
/// reversing an opposite position.
/// </summary>
public class AutomaticTrendlinesStrategy : Strategy
{
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal high, decimal low)> _window = [];
	private int _barIndex;
	private (int index, decimal price)? _lastHigh;
	private (int index, decimal price)? _prevHigh;
	private (int index, decimal price)? _lastLow;
	private (int index, decimal price)? _prevLow;
	private decimal? _prevClose;
	private decimal? _prevResistance;
	private decimal? _prevSupport;

	/// <summary>
	/// Candles before a pivot it must exceed.
	/// </summary>
	public int LeftBars
	{
		get => _leftBars.Value;
		set => _leftBars.Value = value;
	}

	/// <summary>
	/// Candles after a pivot it must exceed.
	/// </summary>
	public int RightBars
	{
		get => _rightBars.Value;
		set => _rightBars.Value = value;
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
	public AutomaticTrendlinesStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 100)
			.SetGreaterThanZero()
			.SetDisplay("Left Bars", "Candles before a pivot it must exceed", "Pivots");

		_rightBars = Param(nameof(RightBars), 15)
			.SetGreaterThanZero()
			.SetDisplay("Right Bars", "Candles after a pivot it must exceed", "Pivots");

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
		_barIndex = -1;
		_lastHigh = null;
		_prevHigh = null;
		_lastLow = null;
		_prevLow = null;
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

		_barIndex++;
		_window.Add((candle.HighPrice, candle.LowPrice));

		var size = LeftBars + RightBars + 1;
		if (_window.Count > size)
			_window.RemoveAt(0);

		if (_window.Count == size)
			DetectPivots();

		var close = candle.ClosePrice;
		var resistance = LineValue(_prevHigh, _lastHigh);
		var support = LineValue(_prevLow, _lastLow);

		var prevClose = _prevClose;
		var prevResistance = _prevResistance;
		var prevSupport = _prevSupport;
		_prevClose = close;
		_prevResistance = resistance;
		_prevSupport = support;

		if (prevClose is not decimal pc)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = resistance is decimal res && prevResistance is decimal prevRes && pc <= prevRes && close > res;
		var crossDown = support is decimal sup && prevSupport is decimal prevSup && pc >= prevSup && close < sup;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void DetectPivots()
	{
		var (pivotHigh, pivotLow) = _window[LeftBars];
		var isHigh = true;
		var isLow = true;

		for (var i = 0; i < _window.Count; i++)
		{
			if (i == LeftBars)
				continue;

			if (_window[i].high >= pivotHigh)
				isHigh = false;

			if (_window[i].low <= pivotLow)
				isLow = false;
		}

		var pivotIndex = _barIndex - RightBars;

		if (isHigh)
		{
			_prevHigh = _lastHigh;
			_lastHigh = (pivotIndex, pivotHigh);
		}

		if (isLow)
		{
			_prevLow = _lastLow;
			_lastLow = (pivotIndex, pivotLow);
		}
	}

	private decimal? LineValue((int index, decimal price)? first, (int index, decimal price)? second)
	{
		if (first is not (int i1, decimal p1) || second is not (int i2, decimal p2) || i2 == i1)
			return null;

		var slope = (p2 - p1) / (i2 - i1);
		return p2 + slope * (_barIndex - i2);
	}
}
