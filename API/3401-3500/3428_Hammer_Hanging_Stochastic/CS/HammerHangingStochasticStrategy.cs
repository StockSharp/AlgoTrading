namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Hammer/Hanging Man + Stochastic strategy.
/// Buys on hammer in oversold, sells on hanging man in overbought.
/// </summary>
public class HammerHangingStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	public HammerHangingStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic K period", "Indicators");
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Stochastic oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Stochastic overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0 || body <= 0) return;

		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		// Hammer: small body at top, long lower shadow
		var isHammer = lowerShadow > body * 2 && upperShadow < body;

		// Hanging Man: small body at bottom, long upper shadow
		var isHangingMan = upperShadow > body * 2 && lowerShadow < body;

		if (isHammer && kValue < Oversold && Position <= 0)
			BuyMarket();
		else if (isHangingMan && kValue > Overbought && Position >= 0)
			SellMarket();
	}
}
