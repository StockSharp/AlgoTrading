using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alexav SpeedUp M1 Body strategy.
/// Trades based on large candle body breakouts.
/// Buys after a large bullish candle, sells after a large bearish candle.
/// Uses ATR to define "large" body threshold.
/// </summary>
public class AlexavSpeedUpM1BodyBreakStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _bodyMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal BodyMultiplier { get => _bodyMultiplier.Value; set => _bodyMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AlexavSpeedUpM1BodyBreakStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for body threshold", "Indicators");

		_bodyMultiplier = Param(nameof(BodyMultiplier), 1.0m)
			.SetDisplay("Body Multiplier", "Body must exceed ATR * multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = candle.ClosePrice - candle.OpenPrice;
		var absBody = Math.Abs(body);
		var threshold = atrValue * BodyMultiplier;

		if (absBody < threshold)
			return;

		// Large bullish candle - buy
		if (body > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Large bearish candle - sell
		else if (body < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
