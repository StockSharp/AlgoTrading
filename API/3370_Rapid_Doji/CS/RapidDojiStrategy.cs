namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Rapid Doji strategy: detects doji candles and trades the breakout direction.
/// Buys on next candle if it closes above doji high, sells if below doji low.
/// Uses ATR for volatility confirmation.
/// </summary>
public class RapidDojiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _dojiThreshold;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevWasDoji;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal DojiThreshold { get => _dojiThreshold.Value; set => _dojiThreshold.Value = value; }

	public RapidDojiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators");
		_dojiThreshold = Param(nameof(DojiThreshold), 0.15m)
			.SetDisplay("Doji Threshold", "Max body/range ratio for doji detection", "Pattern");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevWasDoji = false;
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevWasDoji && atr > 0)
		{
			var close = candle.ClosePrice;
			if (close > _prevHigh && Position <= 0)
				BuyMarket();
			else if (close < _prevLow && Position >= 0)
				SellMarket();
		}

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		_prevWasDoji = range > 0 && body <= DojiThreshold * range;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
