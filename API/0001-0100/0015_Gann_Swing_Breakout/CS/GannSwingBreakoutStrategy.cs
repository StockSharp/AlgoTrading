using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Gann Swing Breakout technique.
/// A swing high is a candle whose high exceeds the highs of SwingLookback candles on each side, a swing low the reverse.
/// A close above the latest swing high while above the SMA opens a long position, a close below the latest swing low
/// while below the SMA a short one. A position stays open until the opposing swing is breached.
/// </summary>
public class GannSwingBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal High, decimal Low)> _window = [];
	private decimal? _swingHigh;
	private decimal? _swingLow;

	/// <summary>
	/// Number of bars to identify swing points.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initialize the Gann Swing Breakout strategy.
	/// </summary>
	public GannSwingBreakoutStrategy()
	{
		_swingLookback = Param(nameof(SwingLookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Lookback period for swing high/low", "Trading parameters")
			.SetOptimize(20, 60, 10);

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for trend filter MA", "Indicators")
			.SetOptimize(40, 80, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_window.Clear();
		_swingHigh = null;
		_swingLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_window.Clear();
		_swingHigh = null;
		_swingLow = null;

		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSwings(candle);

		if (!maValue.IsFormed || _swingHigh is not decimal swingHigh || _swingLow is not decimal swingLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = maValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > swingHigh && close > ma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (close < swingLow && close < ma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && close < swingLow)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close > swingHigh)
		{
			BuyMarket(-Position);
		}
	}

	private void UpdateSwings(ICandleMessage candle)
	{
		// A pivot is confirmed once SwingLookback candles have closed after it.
		_window.Add((candle.HighPrice, candle.LowPrice));

		var size = 2 * SwingLookback + 1;

		if (_window.Count > size)
			_window.RemoveAt(0);

		if (_window.Count < size)
			return;

		var pivot = _window[SwingLookback];
		var others = _window.Where((_, index) => index != SwingLookback).ToArray();

		if (others.All(c => pivot.High > c.High))
			_swingHigh = pivot.High;

		if (others.All(c => pivot.Low < c.Low))
			_swingLow = pivot.Low;
	}
}
