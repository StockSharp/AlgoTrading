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
/// XAU/USD strategy using Bollinger Bands breakout with trend strength filter.
/// Uses SMA + StdDev as Bollinger proxy. Trades band breakouts when trend is strong.
/// </summary>
public class XauUsdAdxBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _bollPeriod;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();

	public int BollingerPeriod { get => _bollPeriod.Value; set => _bollPeriod.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XauUsdAdxBollingerStrategy()
	{
		_bollPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "SMA/StdDev period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Band multiplier", "Indicators");

		_trendLength = Param(nameof(TrendLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Directional movement lookback", "Indicators");

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
		_closes.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = BollingerPeriod };
		var stdDev = new StandardDeviation { Length = BollingerPeriod };

		_closes.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		while (_closes.Count > TrendLength + 1)
			_closes.RemoveAt(0);

		if (stdVal <= 0 || _closes.Count < TrendLength)
			return;

		var upper = smaVal + BbWidth * stdVal;
		var lower = smaVal - BbWidth * stdVal;

		// Simple trend strength: absolute price change over period / average range
		var priceChange = Math.Abs(candle.ClosePrice - _closes[0]);
		var avgChange = stdVal * 2; // approximate
		var trendStrength = avgChange > 0 ? priceChange / avgChange : 0;

		// Only trade when trend is reasonably strong
		if (trendStrength > 0.5m)
		{
			if (candle.ClosePrice > upper && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < lower && Position >= 0)
				SellMarket();
		}

		// Mean reversion exit
		if (Position > 0 && candle.ClosePrice < smaVal)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > smaVal)
			BuyMarket();
	}
}
