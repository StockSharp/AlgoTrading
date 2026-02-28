using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji Arrows breakout strategy.
/// Detects doji candles (small body) and trades breakout of the doji range on the next candle.
/// Uses ATR to define what constitutes a small body.
/// </summary>
public class DojiArrowsBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _dojiThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevWasDoji;
	private bool _hasPrev;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal DojiThreshold { get => _dojiThreshold.Value; set => _dojiThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DojiArrowsBreakoutStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for doji detection", "Indicators");

		_dojiThreshold = Param(nameof(DojiThreshold), 0.3m)
			.SetDisplay("Doji Threshold", "Max body/ATR ratio for doji", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_prevWasDoji = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atr <= 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isDoji = body / atr < DojiThreshold;

		if (_hasPrev && _prevWasDoji)
		{
			// Breakout above doji high
			if (candle.ClosePrice > _prevHigh && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			// Breakout below doji low
			else if (candle.ClosePrice < _prevLow && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevWasDoji = isDoji;
		_hasPrev = true;
	}
}
