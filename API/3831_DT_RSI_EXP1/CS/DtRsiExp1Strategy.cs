using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DT RSI EXP1 strategy - RSI overbought/oversold with EMA trend filter.
/// Buys when RSI crosses above oversold level while above EMA trend.
/// Sells when RSI crosses below overbought level while below EMA trend.
/// </summary>
public class DtRsiExp1Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DtRsiExp1Strategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevRsi = rsi;
			_hasPrev = true;
			return;
		}

		// RSI crosses above oversold + bullish trend
		if (_prevRsi <= Oversold && rsi > Oversold && close > ema && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// RSI crosses below overbought + bearish trend
		else if (_prevRsi >= Overbought && rsi < Overbought && close < ema && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevRsi = rsi;
	}
}
