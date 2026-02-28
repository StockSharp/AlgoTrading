namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// RRS Impulse strategy.
/// Combines RSI, Stochastic and Bollinger Bands for counter-trend entries.
/// </summary>
public class RrsImpulseStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiUpperLevel { get => _rsiUpperLevel.Value; set => _rsiUpperLevel.Value = value; }
	public decimal RsiLowerLevel { get => _rsiLowerLevel.Value; set => _rsiLowerLevel.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public decimal StochasticUpperLevel { get => _stochasticUpperLevel.Value; set => _stochasticUpperLevel.Value = value; }
	public decimal StochasticLowerLevel { get => _stochasticLowerLevel.Value; set => _stochasticLowerLevel.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }

	public RrsImpulseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 65m)
			.SetDisplay("RSI Upper", "Overbought", "RSI");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 35m)
			.SetDisplay("RSI Lower", "Oversold", "RSI");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 10)
			.SetDisplay("Stochastic %K", "%K period", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D period", "Stochastic");

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 70m)
			.SetDisplay("Stochastic Upper", "Overbought", "Stochastic");

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 30m)
			.SetDisplay("Stochastic Lower", "Oversold", "Stochastic");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "BB length", "Bollinger");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "BB deviation", "Bollinger");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticKPeriod;
		stochastic.D.Length = StochasticDPeriod;
		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(rsi, stochastic, bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiVal, IIndicatorValue stochVal, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiVal.IsFinal || !stochVal.IsFinal || !bbVal.IsFinal)
			return;

		if (!rsiVal.IsFormed || !stochVal.IsFormed || !bbVal.IsFormed)
			return;

		var rsi = rsiVal.GetValue<decimal>();
		var stoch = (StochasticOscillatorValue)stochVal;
		var stochK = stoch.K ?? 50m;
		var bb = (BollingerBandsValue)bbVal;
		var bbUpper = bb.UpBand ?? candle.ClosePrice;
		var bbLower = bb.LowBand ?? candle.ClosePrice;

		var close = candle.ClosePrice;

		// Count how many indicators signal overbought/oversold
		var obSignals = 0;
		var osSignals = 0;

		if (rsi >= RsiUpperLevel) obSignals++;
		if (rsi <= RsiLowerLevel) osSignals++;

		if (stochK >= StochasticUpperLevel) obSignals++;
		if (stochK <= StochasticLowerLevel) osSignals++;

		if (close >= bbUpper) obSignals++;
		if (close <= bbLower) osSignals++;

		// Counter-trend: need at least 2 of 3 indicators confirming
		if (osSignals >= 2 && Position <= 0)
		{
			BuyMarket();
		}
		else if (obSignals >= 2 && Position >= 0)
		{
			SellMarket();
		}
		// Exit long when RSI and stoch neutralize
		else if (Position > 0 && rsi > 50m && stochK > 50m)
		{
			SellMarket();
		}
		// Exit short when RSI and stoch neutralize
		else if (Position < 0 && rsi < 50m && stochK < 50m)
		{
			BuyMarket();
		}
	}
}
