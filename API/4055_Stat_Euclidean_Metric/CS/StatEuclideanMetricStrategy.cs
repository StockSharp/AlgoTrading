using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD reversal strategy with moving average ratio filter.
/// Enters on MACD histogram reversals filtered by MA trend alignment.
/// </summary>
public class StatEuclideanMetricStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _trendMaLength;

	private readonly List<decimal> _macdHistory = new();

	public StatEuclideanMetricStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast EMA period for MACD.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow EMA period for MACD.", "Indicators");

		_trendMaLength = Param(nameof(TrendMaLength), 50)
			.SetDisplay("Trend MA Length", "Period for trend filter MA.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macdHistory.Clear();

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var trendMa = new SimpleMovingAverage { Length = TrendMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, trendMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trendMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdLine = fastValue - slowValue;

		_macdHistory.Add(macdLine);
		if (_macdHistory.Count > 5)
			_macdHistory.RemoveAt(0);

		if (_macdHistory.Count < 3)
			return;

		var close = candle.ClosePrice;
		var macd1 = _macdHistory[^1];
		var macd2 = _macdHistory[^2];
		var macd3 = _macdHistory[^3];

		// MACD reversal patterns
		var buyReversal = macd3 >= macd2 && macd2 < macd1; // V-shape bottom
		var sellReversal = macd3 <= macd2 && macd2 > macd1; // inverted V top

		// Exit conditions
		if (Position > 0 && sellReversal)
		{
			SellMarket();
		}
		else if (Position < 0 && buyReversal)
		{
			BuyMarket();
		}

		// Entry conditions with trend filter
		if (Position == 0)
		{
			if (buyReversal && close > trendValue)
			{
				BuyMarket();
			}
			else if (sellReversal && close < trendValue)
			{
				SellMarket();
			}
		}
	}
}
