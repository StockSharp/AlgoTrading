using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Arttrader v1.5 strategy (simplified).
/// Uses EMA slope on a higher timeframe to filter entries,
/// with candle pattern confirmation on the trading timeframe.
/// </summary>
public class ArttraderV15Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _emaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public ArttraderV15Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Trading candles", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Trend Candle Type", "Trend candles for EMA", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period on trend timeframe", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		decimal currentEma = 0;
		decimal previousEma = 0;
		bool hasCurrentEma = false;
		bool hasPreviousEma = false;

		// Subscribe to trend timeframe for EMA slope
		var trendSub = SubscribeCandles(TrendCandleType);
		trendSub
			.Bind(ema, (ICandleMessage candle, decimal emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (hasCurrentEma)
				{
					previousEma = currentEma;
					hasPreviousEma = true;
				}

				currentEma = emaVal;
				hasCurrentEma = true;
			})
			.Start();

		// Subscribe to trading timeframe for signals
		var tradeSub = SubscribeCandles(CandleType);
		tradeSub
			.Bind((ICandleMessage candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!hasPreviousEma)
					return;

				var emaSlope = currentEma - previousEma;
				var close = candle.ClosePrice;
				var open = candle.OpenPrice;

				// Long: EMA slope positive, bearish candle (close < open), close near low
				if (emaSlope > 0 && close <= open && Position <= 0)
					BuyMarket();
				// Short: EMA slope negative, bullish candle (close > open), close near high
				else if (emaSlope < 0 && close >= open && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSub);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
