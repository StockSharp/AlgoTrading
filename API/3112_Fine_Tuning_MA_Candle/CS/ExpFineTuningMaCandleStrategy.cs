using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Fine Tuning MA Candle indicator from MetaTrader.
/// Computes a weighted MA candle inline and trades on color transitions.
/// Color 0 = bearish, 1 = neutral, 2 = bullish.
/// </summary>
public class ExpFineTuningMaCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _rank;

	private readonly List<decimal> _opens = new();
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private int? _prevColor;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Rank { get => _rank.Value; set => _rank.Value = value; }

	public ExpFineTuningMaCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Weighted MA lookback", "Indicator");

		_rank = Param(nameof(Rank), 2m)
			.SetDisplay("Rank", "Weight curvature", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_opens.Clear();
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_prevColor = null;

		var sma = new SimpleMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_opens.Add(candle.OpenPrice);
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		var len = Length;
		if (_closes.Count < len)
			return;

		// trim to keep only what we need
		while (_opens.Count > len + 5)
		{
			_opens.RemoveAt(0);
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
			_closes.RemoveAt(0);
		}

		// compute weighted MA for OHLC
		var maOpen = ComputeWeightedMA(_opens, len);
		var maHigh = ComputeWeightedMA(_highs, len);
		var maLow = ComputeWeightedMA(_lows, len);
		var maClose = ComputeWeightedMA(_closes, len);

		// determine color: bullish if maClose > maOpen, bearish otherwise
		int color;
		if (maClose > maOpen)
			color = 2; // bullish
		else if (maClose < maOpen)
			color = 0; // bearish
		else
			color = 1; // neutral

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		var prev = _prevColor.Value;
		_prevColor = color;

		// Trade on color transitions
		if (prev != 2 && color == 2 && Position <= 0)
		{
			// Turn bullish
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (prev != 0 && color == 0 && Position >= 0)
		{
			// Turn bearish
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}

	private decimal ComputeWeightedMA(List<decimal> data, int length)
	{
		var count = data.Count;
		if (count < length) return data[count - 1];

		var rank = Rank;
		var sum = 0m;
		var weightSum = 0m;

		for (var i = 0; i < length; i++)
		{
			var idx = count - 1 - i;
			// weight decreases with distance, curvature controlled by rank
			var w = (decimal)Math.Pow((double)(length - i), (double)rank);
			sum += data[idx] * w;
			weightSum += w;
		}

		return weightSum > 0 ? sum / weightSum : data[count - 1];
	}
}
