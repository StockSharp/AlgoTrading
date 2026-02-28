using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// True Scalper strategy - RSI overbought/oversold reversal.
/// Buys when RSI drops below oversold level.
/// Sells when RSI rises above overbought level.
/// </summary>
public class TrueScalperProfitLockBreakEvenStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrueScalperProfitLockBreakEvenStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought", "Levels");
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold", "Levels");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev) { _prevRsi = rsi; _hasPrev = true; return; }

		if (_prevRsi >= Oversold && rsi < Oversold && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevRsi <= Overbought && rsi > Overbought && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		_prevRsi = rsi;
	}
}
