namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// IFS Fractals strategy: Williams %R oscillator crossover.
/// Buys when WPR crosses above oversold, sells when crosses below overbought.
/// </summary>
public class IfsFractalsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;

	private decimal _prevWpr;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	public IfsFractalsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Indicators");
		_oversold = Param(nameof(Oversold), -80m)
			.SetDisplay("Oversold", "WPR oversold level", "Signals");
		_overbought = Param(nameof(Overbought), -20m)
			.SetDisplay("Overbought", "WPR overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var wpr = new WilliamsR { Length = WprPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevWpr < Oversold && wprValue >= Oversold && Position <= 0)
				BuyMarket();
			else if (_prevWpr > Overbought && wprValue <= Overbought && Position >= 0)
				SellMarket();
		}

		_prevWpr = wprValue;
		_hasPrev = true;
	}
}
