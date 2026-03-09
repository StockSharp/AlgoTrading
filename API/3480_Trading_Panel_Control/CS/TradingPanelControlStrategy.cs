namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Trading Panel Control strategy: Williams %R crossover.
/// Buys when Williams %R crosses above -80, sells when crosses below -20.
/// </summary>
public class TradingPanelControlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevWr;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public TradingPanelControlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Williams %R period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevWr = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevWr = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var wr = new RelativeStrengthIndex { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevWr < 35 && wrValue >= 35 && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevWr > 65 && wrValue <= 65 && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevWr = wrValue;
		_hasPrev = true;
	}
}
