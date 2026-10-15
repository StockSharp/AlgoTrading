using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dow Theory Trend strategy.
/// A pivot high is a candle whose high is above the highs of the PivotLookback candles on each side, a pivot low mirrors it with
/// lows; a pivot is confirmed PivotLookback candles after it formed. When the last pivot high is above the one before it and the
/// last pivot low is above the one before it, the trend is up and the strategy goes long; lower highs and lower lows go short.
/// The opposite signal reverses the position.
/// </summary>
public class DowTheoryTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLookback;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private decimal? _lastPivotHigh;
	private decimal? _prevPivotHigh;
	private decimal? _lastPivotLow;
	private decimal? _prevPivotLow;

	/// <summary>
	/// Candles on each side of a pivot.
	/// </summary>
	public int PivotLookback
	{
		get => _pivotLookback.Value;
		set => _pivotLookback.Value = value;
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
	public DowTheoryTrendStrategy()
	{
		_pivotLookback = Param(nameof(PivotLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Lookback", "Candles on each side of a pivot", "Pivots");

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
		_highs.Clear();
		_lows.Clear();
		_lastPivotHigh = null;
		_prevPivotHigh = null;
		_lastPivotLow = null;
		_prevPivotLow = null;
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

		var lookback = PivotLookback;
		var size = lookback * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > size)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < size)
			return;

		var centerHigh = _highs[lookback];
		var centerLow = _lows[lookback];
		var isPivotHigh = true;
		var isPivotLow = true;

		for (var i = 0; i < size; i++)
		{
			if (i == lookback)
				continue;

			if (_highs[i] >= centerHigh)
				isPivotHigh = false;

			if (_lows[i] <= centerLow)
				isPivotLow = false;
		}

		if (isPivotHigh)
		{
			_prevPivotHigh = _lastPivotHigh;
			_lastPivotHigh = centerHigh;
		}

		if (isPivotLow)
		{
			_prevPivotLow = _lastPivotLow;
			_lastPivotLow = centerLow;
		}

		if (_lastPivotHigh is not decimal lastHigh || _prevPivotHigh is not decimal prevHigh
			|| _lastPivotLow is not decimal lastLow || _prevPivotLow is not decimal prevLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upTrend = lastHigh > prevHigh && lastLow > prevLow;
		var downTrend = lastHigh < prevHigh && lastLow < prevLow;

		if (upTrend && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (downTrend && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
