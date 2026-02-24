using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversals With Pin Bars strategy: Pin bar reversal + EMA trend.
/// Detects pin bars (long wick relative to body) and trades reversals.
/// </summary>
public class ReversalsWithPinBarsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _wickRatio;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public decimal WickRatio
	{
		get => _wickRatio.Value;
		set => _wickRatio.Value = value;
	}

	public ReversalsWithPinBarsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_wickRatio = Param(nameof(WickRatio), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Wick Ratio", "Lower/upper wick must be N times the body", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body <= 0)
			body = 0.01m;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		// Bullish pin bar: long lower wick, price near EMA support
		if (lowerWick >= body * WickRatio && candle.LowPrice <= ema && Position <= 0)
		{
			BuyMarket();
		}
		// Bearish pin bar: long upper wick, price near EMA resistance
		else if (upperWick >= body * WickRatio && candle.HighPrice >= ema && Position >= 0)
		{
			SellMarket();
		}
	}
}
