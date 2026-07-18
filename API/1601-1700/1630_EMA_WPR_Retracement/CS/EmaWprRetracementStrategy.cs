using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R strategy with EMA trend filter and retracement gating.
/// Buys when WPR oversold and EMA trending up, sells when WPR overbought and EMA trending down.
/// </summary>
public class EmaWprRetracementStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _wprRetracement;
	private readonly StrategyParam<DataType> _candleType;

	private bool _canBuy = true;
	private bool _canSell = true;
	private decimal _prevEma;
	private bool _hasPrevEma;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public decimal WprRetracement { get => _wprRetracement.Value; set => _wprRetracement.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaWprRetracementStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "Trend");

		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Signals");

		_wprRetracement = Param(nameof(WprRetracement), 30m)
			.SetDisplay("WPR Retracement", "Retracement needed for next trade", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_canBuy = true;
		_canSell = true;
		_prevEma = 0;
		_hasPrevEma = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var wpr = new WilliamsR { Length = WprPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wpr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wpr, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaUp = _hasPrevEma && ema > _prevEma;
		var emaDown = _hasPrevEma && ema < _prevEma;

		// Retracement gating: after a buy at oversold, require WPR to retrace above threshold before next buy
		if (wpr > -100 + WprRetracement)
			_canBuy = true;
		if (wpr < -WprRetracement)
			_canSell = true;

		// Oversold buy with uptrend
		if (wpr <= -80 && _canBuy && emaUp && Position <= 0)
		{
			BuyMarket();
			_canBuy = false;
		}
		// Overbought sell with downtrend
		else if (wpr >= -20 && _canSell && emaDown && Position >= 0)
		{
			SellMarket();
			_canSell = false;
		}

		_prevEma = ema;
		_hasPrevEma = true;
	}
}
