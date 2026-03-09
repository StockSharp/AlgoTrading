namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Stochastic Accelerator strategy: Rate of Change crossover.
/// Buys when ROC crosses above zero, sells when ROC crosses below zero.
/// </summary>
public class StochasticAcceleratorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _rocLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevRoc;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal RocLevel { get => _rocLevel.Value; set => _rocLevel.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public StochasticAcceleratorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 12)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ROC period", "Indicators");
		_rocLevel = Param(nameof(RocLevel), 0.2m)
			.SetDisplay("ROC Level", "ROC threshold for crossover", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRoc = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevRoc = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var roc = new RateOfChange { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(roc, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevRoc <= -RocLevel && rocValue > -RocLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevRoc >= RocLevel && rocValue < RocLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevRoc = rocValue;
		_hasPrev = true;
	}
}
