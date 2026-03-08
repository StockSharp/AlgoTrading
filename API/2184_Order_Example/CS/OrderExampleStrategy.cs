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
/// Breakout strategy based on recent highs and lows with SMA trend filter.
/// </summary>
public class OrderExampleStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrderExampleStrategy()
	{
		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Candles to calculate highs and lows", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Trend filter SMA period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new ExponentialMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_highs.Add(candle.HighPrice);
			_lows.Add(candle.LowPrice);
			if (_highs.Count > Lookback) _highs.RemoveAt(0);
			if (_lows.Count > Lookback) _lows.RemoveAt(0);
			return;
		}

		if (_highs.Count >= 3)
		{
			var highLevel = _highs.Max();
			var lowLevel = _lows.Min();

			if (candle.ClosePrice > highLevel && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < lowLevel && Position >= 0)
				SellMarket();
		}

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > Lookback) _highs.RemoveAt(0);
		if (_lows.Count > Lookback) _lows.RemoveAt(0);
	}
}
