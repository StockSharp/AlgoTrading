using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DLM v1 Grid strategy - EMA with RSI direction filter.
/// Buys when EMA is rising and RSI is above 50.
/// Sells when EMA is falling and RSI is below 50.
/// </summary>
public class Dlmv1GridStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Dlmv1GridStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetDisplay("EMA Period", "EMA lookback", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevEma = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevEma = ema;
			_hasPrev = true;
			return;
		}

		// EMA rising + RSI above 50 = buy
		if (ema > _prevEma && rsi > 50 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// EMA falling + RSI below 50 = sell
		else if (ema < _prevEma && rsi < 50 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevEma = ema;
	}
}
