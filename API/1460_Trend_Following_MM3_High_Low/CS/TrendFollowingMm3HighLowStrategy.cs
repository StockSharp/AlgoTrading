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
/// Trend Following strategy using SMA on highs and lows.
/// Enters long when close is above SMA of highs, exits when close drops below SMA of lows.
/// Enters short when close is below SMA of lows, exits when close goes above SMA of highs.
/// </summary>
public class TrendFollowingMm3HighLowStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendFollowingMm3HighLowStrategy()
	{
		_length = Param(nameof(Length), 3)
			.SetDisplay("SMA Length", "Period for moving averages", "Parameters")
			.SetOptimize(2, 20, 1);

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
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Length };

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

		if (_highs.Count > Length)
			_highs.RemoveAt(0);
		if (_lows.Count > Length)
			_lows.RemoveAt(0);

		if (_highs.Count < Length)
			return;

		var highMa = 0m;
		var lowMa = 0m;
		for (var i = 0; i < Length; i++)
		{
			highMa += _highs[i];
			lowMa += _lows[i];
		}
		highMa /= Length;
		lowMa /= Length;

		// Entry: close above SMA of highs
		if (candle.ClosePrice > highMa && Position <= 0)
			BuyMarket();
		// Exit long / entry short: close below SMA of lows
		else if (candle.ClosePrice < lowMa && Position >= 0)
			SellMarket();
	}
}
