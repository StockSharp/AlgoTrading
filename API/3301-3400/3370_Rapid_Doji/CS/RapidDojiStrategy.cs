namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Rapid Doji strategy: detects doji candles and trades the breakout direction.
/// Buys on next candle if it closes above doji high, sells if below doji low.
/// Uses ATR for volatility confirmation.
/// </summary>
public class RapidDojiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _dojiThreshold;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _prevWasDoji;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal DojiThreshold { get => _dojiThreshold.Value; set => _dojiThreshold.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public RapidDojiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators");
		_dojiThreshold = Param(nameof(DojiThreshold), 0.15m)
			.SetDisplay("Doji Threshold", "Max body/range ratio for doji detection", "Pattern");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between breakouts", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevWasDoji = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevWasDoji = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_prevWasDoji && atr > 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			var close = candle.ClosePrice;
			if (close > _prevHigh + atr * 0.2m && Position <= 0)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (close < _prevLow - atr * 0.2m && Position >= 0)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		_prevWasDoji = range > 0 && body <= DojiThreshold * range;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
