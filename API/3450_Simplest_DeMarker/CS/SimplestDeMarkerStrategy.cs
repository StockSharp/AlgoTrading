namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simplest DeMarker strategy: DeMarker oscillator crossover.
/// Buys when DeMarker crosses above oversold, sells when crosses below overbought.
/// </summary>
public class SimplestDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevValue;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int DemarkerPeriod { get => _demarkerPeriod.Value; set => _demarkerPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public SimplestDeMarkerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "DeMarker period", "Indicators");
		_oversold = Param(nameof(Oversold), 0.2m)
			.SetDisplay("Oversold", "DeMarker oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 0.8m)
			.SetDisplay("Overbought", "DeMarker overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevValue = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevValue = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var demarker = new DeMarker { Length = DemarkerPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(demarker, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal demarkerValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevValue < Oversold && demarkerValue >= Oversold && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevValue > Overbought && demarkerValue <= Overbought && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevValue = demarkerValue;
		_hasPrev = true;
	}
}
