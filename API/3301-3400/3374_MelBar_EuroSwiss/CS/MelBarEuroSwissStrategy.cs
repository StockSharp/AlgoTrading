namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MelBar EuroSwiss strategy: Bollinger Bands breakout with RSI confirmation.
/// Buys when close breaks below lower band with oversold RSI.
/// Sells when close breaks above upper band with overbought RSI.
/// </summary>
public class MelBarEuroSwissStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _signalCooldownCandles;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public MelBarEuroSwissStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_bbPeriod = Param(nameof(BbPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candlesSinceTrade = SignalCooldownCandles;
		var sma = new SimpleMovingAverage { Length = BbPeriod };
		var atr = new AverageTrueRange { Length = BbPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, atr, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal atr, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var close = candle.ClosePrice;
		var bandDistance = atr * 4m;

		if (_candlesSinceTrade >= SignalCooldownCandles && close <= sma - bandDistance && rsi < 25 && Position <= 0)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (_candlesSinceTrade >= SignalCooldownCandles && close >= sma + bandDistance && rsi > 75 && Position >= 0)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
	}
}
