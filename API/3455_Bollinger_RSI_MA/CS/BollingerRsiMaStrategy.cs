namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Bollinger + RSI + MA strategy.
/// Buys when price at lower BB and RSI oversold, sells at upper BB and RSI overbought.
/// </summary>
public class BollingerRsiMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bandPercent;
	private readonly StrategyParam<int> _signalCooldownCandles;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BandPercent { get => _bandPercent.Value; set => _bandPercent.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public BollingerRsiMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		_bandPercent = Param(nameof(BandPercent), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Band Percent", "MA percentage band width", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
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
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ma = new SimpleMovingAverage { Length = BbPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var close = candle.ClosePrice;
		var upper = maValue * (1 + BandPercent);
		var lower = maValue * (1 - BandPercent);

		// Mean reversion: buy at lower band, sell at upper band
		if (close < lower && rsiValue < 35 && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (close > upper && rsiValue > 65 && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
	}
}
