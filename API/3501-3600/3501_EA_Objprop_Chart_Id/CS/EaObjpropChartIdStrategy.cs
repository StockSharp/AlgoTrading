namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// EA Objprop Chart Id strategy: Standard Deviation breakout.
/// Buys when StdDev crosses above threshold with bullish candle, sells on bearish cross.
/// </summary>
public class EaObjpropChartIdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevStdDev;
	private decimal _prevRange;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public EaObjpropChartIdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 50)
			.SetGreaterThanZero()
			.SetDisplay("Period", "EMA period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevStdDev = 0;
		_prevRange = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevStdDev = 0;
		_prevRange = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var stdDev = new ExponentialMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		if (_hasPrev && stdDevValue > 0 && _prevRange > 0)
		{
			var expanding = range > _prevRange * 1.2m;
			var bullish = candle.ClosePrice > candle.OpenPrice;
			var bearish = candle.ClosePrice < candle.OpenPrice;

			if (expanding && bullish && candle.ClosePrice > stdDevValue && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (expanding && bearish && candle.ClosePrice < stdDevValue && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevStdDev = stdDevValue;
		_prevRange = range;
		_hasPrev = true;
	}
}
