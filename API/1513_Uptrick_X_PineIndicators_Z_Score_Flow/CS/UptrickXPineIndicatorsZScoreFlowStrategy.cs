namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Z-Score Flow Strategy.
/// Trades based on Z-score of price relative to moving average with RSI confirmation.
/// </summary>
public class UptrickXPineIndicatorsZScoreFlowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _zScorePeriod;
	private readonly StrategyParam<decimal> _zBuyLevel;
	private readonly StrategyParam<decimal> _zSellLevel;

	private decimal _priceSum;
	private decimal _priceSqSum;
	private int _priceCount;
	private readonly Queue<decimal> _priceQueue = new();

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int ZScorePeriod { get => _zScorePeriod.Value; set => _zScorePeriod.Value = value; }
	public decimal ZBuyLevel { get => _zBuyLevel.Value; set => _zBuyLevel.Value = value; }
	public decimal ZSellLevel { get => _zSellLevel.Value; set => _zSellLevel.Value = value; }

	public UptrickXPineIndicatorsZScoreFlowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_zScorePeriod = Param(nameof(ZScorePeriod), 50)
			.SetDisplay("Z-Score Period", "Period for z-score", "Indicators");

		_zBuyLevel = Param(nameof(ZBuyLevel), -1.5m)
			.SetDisplay("Z-Score Buy", "Buy threshold", "Strategy");

		_zSellLevel = Param(nameof(ZSellLevel), 1.5m)
			.SetDisplay("Z-Score Sell", "Sell threshold", "Strategy");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceSum = 0;
		_priceSqSum = 0;
		_priceCount = 0;
		_priceQueue.Clear();

		var rsi = new RelativeStrengthIndex { Length = 14 };
		var ema = new ExponentialMovingAverage { Length = 50 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Maintain running statistics for Z-score
		_priceQueue.Enqueue(close);
		_priceSum += close;
		_priceSqSum += close * close;
		_priceCount++;

		if (_priceCount > ZScorePeriod)
		{
			var removed = _priceQueue.Dequeue();
			_priceSum -= removed;
			_priceSqSum -= removed * removed;
			_priceCount = ZScorePeriod;
		}

		if (_priceCount < ZScorePeriod)
			return;

		var mean = _priceSum / _priceCount;
		var variance = (_priceSqSum / _priceCount) - (mean * mean);
		var stdDev = variance <= 0 ? 0m : (decimal)Math.Sqrt((double)variance);

		if (stdDev == 0)
			return;

		var zscore = (close - mean) / stdDev;

		// Trading logic
		if (zscore < ZBuyLevel && rsiValue < 40 && Position <= 0)
		{
			BuyMarket();
		}
		else if (zscore > ZSellLevel && rsiValue > 60 && Position >= 0)
		{
			SellMarket();
		}
		// Exit when z-score returns to zero
		else if (Position > 0 && zscore > 0)
		{
			SellMarket();
		}
		else if (Position < 0 && zscore < 0)
		{
			BuyMarket();
		}
	}
}
