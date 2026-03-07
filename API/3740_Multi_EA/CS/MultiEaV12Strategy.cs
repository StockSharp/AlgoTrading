namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simplified port of the MetaTrader expert advisor "MultiStrategyEA v1.2".
/// Combines multiple oscillators (RSI, Stochastic, MACD, Bollinger, ADX)
/// and requires a configurable number of bullish or bearish confirmations before entering a trade.
/// </summary>
public class MultiEaV12Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _requiredConfirmations;

	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;

	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochUpper;
	private readonly StrategyParam<decimal> _stochLower;

	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;

	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxTrendLevel;

	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RequiredConfirmations { get => _requiredConfirmations.Value; set => _requiredConfirmations.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiUpper { get => _rsiUpper.Value; set => _rsiUpper.Value = value; }
	public decimal RsiLower { get => _rsiLower.Value; set => _rsiLower.Value = value; }
	public int StochKPeriod { get => _stochKPeriod.Value; set => _stochKPeriod.Value = value; }
	public int StochDPeriod { get => _stochDPeriod.Value; set => _stochDPeriod.Value = value; }
	public decimal StochUpper { get => _stochUpper.Value; set => _stochUpper.Value = value; }
	public decimal StochLower { get => _stochLower.Value; set => _stochLower.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxTrendLevel { get => _adxTrendLevel.Value; set => _adxTrendLevel.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	public MultiEaV12Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_requiredConfirmations = Param(nameof(RequiredConfirmations), 3)
			.SetDisplay("Required Confirmations", "Number of modules required for entry", "Consensus");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "RSI");

		_rsiUpper = Param(nameof(RsiUpper), 65m)
			.SetDisplay("RSI Upper", "Overbought level", "RSI");

		_rsiLower = Param(nameof(RsiLower), 35m)
			.SetDisplay("RSI Lower", "Oversold level", "RSI");

		_stochKPeriod = Param(nameof(StochKPeriod), 10)
			.SetDisplay("Stochastic %K", "%K period", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D period", "Stochastic");

		_stochUpper = Param(nameof(StochUpper), 70m)
			.SetDisplay("Stoch Upper", "Overbought", "Stochastic");

		_stochLower = Param(nameof(StochLower), 30m)
			.SetDisplay("Stoch Lower", "Oversold", "Stochastic");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "BB length", "Bollinger");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "BB width", "Bollinger");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX length", "ADX");

		_adxTrendLevel = Param(nameof(AdxTrendLevel), 20m)
			.SetDisplay("ADX Trend Level", "Min ADX for trend", "ADX");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetDisplay("MACD Fast", "Fast EMA period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetDisplay("MACD Slow", "Slow EMA period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetDisplay("MACD Signal", "Signal line period", "MACD");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(StockSharp.BusinessEntities.Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochKPeriod;
		stochastic.D.Length = StochDPeriod;

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = MacdFast;
		macd.Macd.LongMa.Length = MacdSlow;
		macd.SignalMa.Length = MacdSignal;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(rsi, stochastic, bollinger, adx, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiVal, IIndicatorValue stochVal, IIndicatorValue bbVal, IIndicatorValue adxVal, IIndicatorValue macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiVal.IsFinal || !stochVal.IsFinal || !bbVal.IsFinal || !adxVal.IsFinal || !macdVal.IsFinal)
			return;

		if (!rsiVal.IsFormed || !stochVal.IsFormed || !bbVal.IsFormed || !adxVal.IsFormed || !macdVal.IsFormed)
			return;

		var rsi = rsiVal.GetValue<decimal>();
		var stoch = (StochasticOscillatorValue)stochVal;
		var stochK = stoch.K ?? 50m;
		var bb = (BollingerBandsValue)bbVal;
		var bbUpper = bb.UpBand ?? candle.ClosePrice;
		var bbLower = bb.LowBand ?? candle.ClosePrice;
		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		var adxMain = adxTyped.MovingAverage ?? 0m;
		var adxPlus = adxTyped.Dx.Plus ?? 0m;
		var adxMinus = adxTyped.Dx.Minus ?? 0m;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		var macdLine = macdTyped.Macd ?? 0m;
		var macdSignalLine = macdTyped.Signal ?? 0m;

		var close = candle.ClosePrice;

		// Count bullish and bearish signals from each module
		var bullish = 0;
		var bearish = 0;

		// Module 1: RSI
		if (rsi < RsiLower) bullish++;
		else if (rsi > RsiUpper) bearish++;

		// Module 2: Stochastic
		if (stochK < StochLower) bullish++;
		else if (stochK > StochUpper) bearish++;

		// Module 3: Bollinger Bands
		if (close <= bbLower) bullish++;
		else if (close >= bbUpper) bearish++;

		// Module 4: ADX directional
		if (adxMain >= AdxTrendLevel)
		{
			if (adxPlus > adxMinus) bullish++;
			else if (adxMinus > adxPlus) bearish++;
		}

		// Module 5: MACD
		if (macdLine > macdSignalLine && macdLine > 0) bullish++;
		else if (macdLine < macdSignalLine && macdLine < 0) bearish++;

		var minConfirmations = RequiredConfirmations;

		// Enter on consensus
		if (bullish >= minConfirmations && bearish == 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (bearish >= minConfirmations && bullish == 0 && Position >= 0)
		{
			SellMarket();
		}
		// Exit when consensus breaks
		else if (Position > 0 && bearish >= 2)
		{
			SellMarket();
		}
		else if (Position < 0 && bullish >= 2)
		{
			BuyMarket();
		}
	}
}
