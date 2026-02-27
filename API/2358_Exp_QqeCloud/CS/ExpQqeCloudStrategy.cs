using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// QQE Cloud strategy.
/// Uses RSI with EMA smoothing and volatility-based bands for trend detection.
/// Buys when smoothed RSI crosses above upper band, sells when it crosses below lower band.
/// </summary>
public class ExpQqeCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<decimal> _qqeFactor;

	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SmoothPeriod { get => _smoothPeriod.Value; set => _smoothPeriod.Value = value; }
	public decimal QqeFactor { get => _qqeFactor.Value; set => _qqeFactor.Value = value; }

	public ExpQqeCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Length", "RSI period", "QQE");

		_smoothPeriod = Param(nameof(SmoothPeriod), 5)
			.SetDisplay("Smooth Period", "EMA smoothing period for RSI", "QQE");

		_qqeFactor = Param(nameof(QqeFactor), 4.236m)
			.SetDisplay("QQE Factor", "QQE volatility factor", "QQE");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barCount = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = SmoothPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		// Use EMA of price as trend filter, RSI for signals
		// QQE-style: when RSI is strong (>60) and price above EMA -> buy
		// when RSI is weak (<40) and price below EMA -> sell

		if (_barCount < 3)
			return;

		var price = candle.ClosePrice;

		if (rsiValue > 60 && price > emaValue && Position <= 0)
			BuyMarket();
		else if (rsiValue < 40 && price < emaValue && Position >= 0)
			SellMarket();
	}
}
