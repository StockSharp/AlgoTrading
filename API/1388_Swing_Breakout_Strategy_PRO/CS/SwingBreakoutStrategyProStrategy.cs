using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Swing breakout strategy based on recent swing highs and lows.
/// </summary>
public class SwingBreakoutStrategyProStrategy : Strategy
{
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;
	private readonly StrategyParam<bool> _showLines;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highBuffer = new();
	private readonly List<decimal> _lowBuffer = new();
	private decimal _lastSwingHigh;
	private decimal _lastSwingLow;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private decimal _longSL;
	private decimal _longTP;
	private decimal _shortSL;
	private decimal _shortTP;

	/// <summary>
	/// Left bars for pivot calculation.
	/// </summary>
	public int LeftBars { get => _leftBars.Value; set => _leftBars.Value = value; }

	/// <summary>
	/// Right bars for pivot calculation.
	/// </summary>
	public int RightBars { get => _rightBars.Value; set => _rightBars.Value = value; }

	/// <summary>
	/// Show stop-loss and target lines on chart.
	/// </summary>
	public bool ShowLines { get => _showLines.Value; set => _showLines.Value = value; }

	/// <summary>
	/// Candle type used for trading.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="SwingBreakoutStrategyProStrategy"/>.
	/// </summary>
	public SwingBreakoutStrategyProStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Left Bars", "Bars to the left of pivot", "General");

		_rightBars = Param(nameof(RightBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Right Bars", "Bars to the right of pivot", "General");

		_showLines = Param(nameof(ShowLines), true)
			.SetDisplay("Show Lines", "Draw stop-loss and target lines", "Visual");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for trading", "General");
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
		_highBuffer.Clear();
		_lowBuffer.Clear();
		_lastSwingHigh = _lastSwingLow = 0m;
		_prevHigh = _prevLow = _prevClose = 0m;
		_longSL = _longTP = _shortSL = _shortTP = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var size = LeftBars + RightBars + 1;
		_highBuffer.Add(candle.HighPrice);
		_lowBuffer.Add(candle.LowPrice);
		if (_highBuffer.Count > size)
			_highBuffer.RemoveAt(0);
		if (_lowBuffer.Count > size)
			_lowBuffer.RemoveAt(0);

		if (_highBuffer.Count == size)
		{
			var pivotIndex = size - RightBars - 1;
			var candidate = _highBuffer[pivotIndex];
			var isPivot = true;
			for (var i = 0; i < size; i++)
			{
				if (i == pivotIndex)
					continue;
				if (_highBuffer[i] >= candidate)
				{
					isPivot = false;
					break;
				}
			}
			if (isPivot)
				_lastSwingHigh = candidate;
		}

		if (_lowBuffer.Count == size)
		{
			var pivotIndex = size - RightBars - 1;
			var candidate = _lowBuffer[pivotIndex];
			var isPivot = true;
			for (var i = 0; i < size; i++)
			{
				if (i == pivotIndex)
					continue;
				if (_lowBuffer[i] <= candidate)
				{
					isPivot = false;
					break;
				}
			}
			if (isPivot)
				_lastSwingLow = candidate;
		}

		var longCondition = _prevClose > _lastSwingHigh && candle.HighPrice > _prevHigh && _lastSwingHigh != 0m && _lastSwingLow != 0m;
		var shortCondition = _prevClose < _lastSwingLow && candle.LowPrice < _prevLow && _lastSwingHigh != 0m && _lastSwingLow != 0m;

		if (longCondition && Position <= 0)
		{
			var rangeGap = Math.Abs(_lastSwingHigh - _lastSwingLow);
			_longSL = _lastSwingLow;
			_longTP = _lastSwingHigh + rangeGap;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			var rangeGap = Math.Abs(_lastSwingHigh - _lastSwingLow);
			_shortSL = _lastSwingHigh;
			_shortTP = _lastSwingLow - rangeGap;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longSL || candle.HighPrice >= _longTP)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortSL || candle.LowPrice <= _shortTP)
				BuyMarket(Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}
}
