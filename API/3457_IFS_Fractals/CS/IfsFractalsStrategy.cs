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
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevWpr;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public IfsFractalsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Indicators");
		_oversold = Param(nameof(Oversold), -85m)
			.SetDisplay("Oversold", "WPR oversold level", "Signals");
		_overbought = Param(nameof(Overbought), -15m)
			.SetDisplay("Overbought", "WPR overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevWpr = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevWpr = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var wpr = new WilliamsR { Length = WprPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevWpr < Oversold && wprValue >= Oversold && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevWpr > Overbought && wprValue <= Overbought && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevWpr = wprValue;
		_hasPrev = true;
	}
}
