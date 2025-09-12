using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Box strategy that trades breakouts from grid lines.
/// </summary>
public class RsiBoxPseudoGridBotStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useShorts;

	private decimal _prevClose;
	private decimal _prevRsi;
	private bool _isFirst;

	private decimal? _gridTop;
	private decimal? _gridBottom;
	private decimal _bullStatusLine;
	private decimal _bearStatusLine;
	private decimal _bullBuyLine;
	private decimal _bearSellLine;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool UseShorts
	{
		get => _useShorts.Value;
		set => _useShorts.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public RsiBoxPseudoGridBotStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Length for RSI", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_useShorts = Param(nameof(UseShorts), false)
			.SetDisplay("Use Shorts", "Allow short trades", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_prevClose = 0m;
		_prevRsi = 0m;
		_isFirst = true;

		_gridTop = null;
		_gridBottom = null;
		_bullStatusLine = 0m;
		_bearStatusLine = 0m;
		_bullBuyLine = 0m;
		_bearSellLine = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var highest = new Highest { Length = RsiPeriod };
		var lowest = new Lowest { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevRsi = rsi;
			_isFirst = false;
		}

		var rsiCrossDown = _prevRsi >= Overbought && rsi < Overbought;
		var rsiCrossUp = _prevRsi <= Oversold && rsi > Oversold;

		if (rsiCrossDown)
			_gridTop = high;

		if (rsiCrossUp)
			_gridBottom = low;

		if (_gridTop is null || _gridBottom is null)
		{
			_prevClose = candle.ClosePrice;
			_prevRsi = rsi;
			return;
		}

		var gridTop = _gridTop.Value;
		var gridBottom = _gridBottom.Value;
		var gridMiddle = (gridTop + gridBottom) / 2m;
		var gridMidTop = (gridMiddle + gridTop) / 2m;
		var gridMidBottom = (gridMiddle + gridBottom) / 2m;

		var closestLine = GetClosestLine(candle.ClosePrice, gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom);

		var buyCrosses = HasCrossUp(_prevClose, candle.ClosePrice, gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom);
		var sellCrosses = HasCrossDown(_prevClose, candle.ClosePrice, gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom);

		if (buyCrosses)
			_bullStatusLine = closestLine;

		if (sellCrosses)
			_bearStatusLine = closestLine;

		_bullBuyLine = GetNextBullLine(_bullStatusLine, gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom);
		_bearSellLine = GetNextBearLine(_bearStatusLine, gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom);

		var l = CrossUp(_prevClose, candle.ClosePrice, _bullBuyLine);
		var s = CrossDown(_prevClose, candle.ClosePrice, _bearSellLine);

		if (l && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (s)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (UseShorts && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevRsi = rsi;
	}

	private static bool CrossUp(decimal prev, decimal current, decimal level)
	{
		return prev <= level && current > level;
	}

	private static bool CrossDown(decimal prev, decimal current, decimal level)
	{
		return prev >= level && current < level;
	}

	private static bool HasCrossUp(decimal prev, decimal current, params decimal[] lines)
	{
		foreach (var line in lines)
		{
			if (CrossUp(prev, current, line))
				return true;
		}
		return false;
	}

	private static bool HasCrossDown(decimal prev, decimal current, params decimal[] lines)
	{
		foreach (var line in lines)
		{
			if (CrossDown(prev, current, line))
				return true;
		}
		return false;
	}

	private static decimal GetClosestLine(decimal price, decimal gridTop, decimal gridBottom, decimal gridMiddle, decimal gridMidTop, decimal gridMidBottom)
	{
		var lines = new[] { gridTop, gridBottom, gridMiddle, gridMidTop, gridMidBottom };
		var closest = gridTop;
		var minDiff = Math.Abs(price - gridTop);

		foreach (var line in lines)
		{
			var diff = Math.Abs(price - line);
			if (diff < minDiff)
			{
				minDiff = diff;
				closest = line;
			}
		}

		return closest;
	}

	private static decimal GetNextBullLine(decimal statusLine, decimal gridTop, decimal gridBottom, decimal gridMiddle, decimal gridMidTop, decimal gridMidBottom)
	{
		if (statusLine == gridBottom)
			return gridMidBottom;
		if (statusLine == gridMidBottom)
			return gridMiddle;
		if (statusLine == gridMiddle)
			return gridMidTop;
		if (statusLine == gridMidTop)
			return gridTop;
		return statusLine;
	}

	private static decimal GetNextBearLine(decimal statusLine, decimal gridTop, decimal gridBottom, decimal gridMiddle, decimal gridMidTop, decimal gridMidBottom)
	{
		if (statusLine == gridTop)
			return gridMidTop;
		if (statusLine == gridMidTop)
			return gridMiddle;
		if (statusLine == gridMiddle)
			return gridMidBottom;
		if (statusLine == gridMidBottom)
			return gridBottom;
		return statusLine;
	}
}
