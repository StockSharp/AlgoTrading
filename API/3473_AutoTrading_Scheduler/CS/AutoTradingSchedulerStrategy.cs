namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// AutoTrading Scheduler strategy: Momentum indicator crossover.
/// Buys when Momentum crosses above 100, sells when crosses below 100.
/// </summary>
public class AutoTradingSchedulerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevMom;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public decimal MomentumLevel { get => _momentumLevel.Value; set => _momentumLevel.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public AutoTradingSchedulerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_momentumPeriod = Param(nameof(MomentumPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");
		_momentumLevel = Param(nameof(MomentumLevel), 101m)
			.SetDisplay("Momentum Level", "Momentum threshold", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMom = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var momentum = new Momentum { Length = MomentumPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevMom < MomentumLevel && momValue >= MomentumLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevMom > 200m - MomentumLevel && momValue <= 200m - MomentumLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevMom = momValue;
		_hasPrev = true;
	}
}
