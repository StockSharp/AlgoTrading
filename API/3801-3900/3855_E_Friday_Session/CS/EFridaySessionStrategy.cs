using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// E-Friday Session strategy - candle body direction with EMA filter.
/// Buys when previous candle was bearish and current closes above EMA (reversal).
/// Sells when previous candle was bullish and current closes below EMA (reversal).
/// </summary>
public class EFridaySessionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EFridaySessionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA trend filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevOpen = 0m; _prevClose = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		var prevBearish = _prevClose < _prevOpen;
		var prevBullish = _prevClose > _prevOpen;

		// Previous bearish + close above EMA = buy reversal
		if (prevBearish && close > ema && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Previous bullish + close below EMA = sell reversal
		else if (prevBullish && close < ema && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = close;
	}
}
