namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// PricerEA strategy: Bollinger Bands mean reversion with RSI filter.
/// Buys at lower band when RSI is oversold, sells at upper band when overbought.
/// </summary>
public class PricerEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _signalCooldownCandles;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public PricerEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for adaptive band distance", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 8)
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
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, rsi, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var bandDistance = atr * 2.5m;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (Position > 0 && (close >= sma || rsi >= 50))
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
		else if (Position < 0 && (close <= sma || rsi <= 50))
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (Position == 0 && _candlesSinceTrade >= SignalCooldownCandles && close <= sma - bandDistance && rsi < 30)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (Position == 0 && _candlesSinceTrade >= SignalCooldownCandles && close >= sma + bandDistance && rsi > 70)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
	}
}
