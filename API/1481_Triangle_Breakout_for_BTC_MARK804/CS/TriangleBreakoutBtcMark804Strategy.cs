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
/// Triangle breakout strategy for BTC.
/// Builds SMA of highs/lows as triangle bounds, trades breakouts with TP/SL.
/// </summary>
public class TriangleBreakoutBtcMark804Strategy : Strategy
{
	private readonly StrategyParam<int> _triangleLength;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _takePct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _entryPrice;

	public int TriangleLength { get => _triangleLength.Value; set => _triangleLength.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TakePct { get => _takePct.Value; set => _takePct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TriangleBreakoutBtcMark804Strategy()
	{
		_triangleLength = Param(nameof(TriangleLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Triangle Length", "Lookback for SMA lines", "General");

		_stopPct = Param(nameof(StopPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_takePct = Param(nameof(TakePct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Take %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = TriangleLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > TriangleLength)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < TriangleLength)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		// Calculate SMA of highs and lows as triangle bounds
		var upper = 0m;
		var lower = 0m;
		for (var i = 0; i < _highs.Count; i++)
		{
			upper += _highs[i];
			lower += _lows[i];
		}
		upper /= _highs.Count;
		lower /= _lows.Count;

		// Check exits first
		if (Position > 0 && _entryPrice > 0)
		{
			var stop = _entryPrice * (1m - StopPct / 100m);
			var take = _entryPrice * (1m + TakePct / 100m);
			if (candle.LowPrice <= stop || candle.HighPrice >= take)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stop = _entryPrice * (1m + StopPct / 100m);
			var take = _entryPrice * (1m - TakePct / 100m);
			if (candle.HighPrice >= stop || candle.LowPrice <= take)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Breakout detection
		if (_prevClose > 0 && _prevUpper > 0)
		{
			var breakoutUp = _prevClose <= _prevUpper && candle.ClosePrice > upper;
			var breakoutDown = _prevClose >= _prevLower && candle.ClosePrice < lower;

			if (breakoutUp && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (breakoutDown && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
