using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger breakout strategy with multiple optional filters.
/// Enters long when price closes above upper band and all filters pass.
/// Enters short when price closes below lower band and all filters pass.
/// Places a percent based stop-loss and can force take-profit on large candles.
/// </summary>
public class BigMoverCatcherStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<bool> _useRsiThreshold;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLongMin;
	private readonly StrategyParam<decimal> _rsiShortMax;
	private readonly StrategyParam<int> _rsiSmoothLen;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<bool> _useAdxThreshold;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _adxSmoothLen;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<bool> _useAtrMinValue;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMinValue;
	private readonly StrategyParam<int> _atrSmoothLen;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaFilterLen;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<bool> _enableForceTp;
	private readonly StrategyParam<decimal> _forceTpPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _rsiSmooth;
	private ExponentialMovingAverage _adxSmooth;
	private ExponentialMovingAverage _atrSmooth;

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLongs { get => _enableLongs.Value; set => _enableLongs.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }

	/// <summary>
	/// Stop loss percent from entry price.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Enable RSI filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }

	/// <summary>
	/// Use RSI thresholds.
	/// </summary>
	public bool UseRsiThreshold { get => _useRsiThreshold.Value; set => _useRsiThreshold.Value = value; }

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Minimum RSI for long trades.
	/// </summary>
	public decimal RsiLongMin { get => _rsiLongMin.Value; set => _rsiLongMin.Value = value; }

	/// <summary>
	/// Maximum RSI for short trades.
	/// </summary>
	public decimal RsiShortMax { get => _rsiShortMax.Value; set => _rsiShortMax.Value = value; }

	/// <summary>
	/// Length for smoothing RSI by EMA.
	/// </summary>
	public int RsiSmoothLen { get => _rsiSmoothLen.Value; set => _rsiSmoothLen.Value = value; }

	/// <summary>
	/// Enable ADX filter.
	/// </summary>
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }

	/// <summary>
	/// Use ADX threshold.
	/// </summary>
	public bool UseAdxThreshold { get => _useAdxThreshold.Value; set => _useAdxThreshold.Value = value; }

	/// <summary>
	/// ADX period length.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Length for smoothing ADX by EMA.
	/// </summary>
	public int AdxSmoothLen { get => _adxSmoothLen.Value; set => _adxSmoothLen.Value = value; }

	/// <summary>
	/// Enable ATR filter.
	/// </summary>
	public bool UseAtrFilter { get => _useAtrFilter.Value; set => _useAtrFilter.Value = value; }

	/// <summary>
	/// Use minimum ATR value filter.
	/// </summary>
	public bool UseAtrMinValue { get => _useAtrMinValue.Value; set => _useAtrMinValue.Value = value; }

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Minimum ATR value.
	/// </summary>
	public decimal AtrMinValue { get => _atrMinValue.Value; set => _atrMinValue.Value = value; }

	/// <summary>
	/// Length for smoothing ATR by EMA.
	/// </summary>
	public int AtrSmoothLen { get => _atrSmoothLen.Value; set => _atrSmoothLen.Value = value; }

	/// <summary>
	/// Enable EMA trend filter.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }

	/// <summary>
	/// EMA filter length.
	/// </summary>
	public int EmaFilterLen { get => _emaFilterLen.Value; set => _emaFilterLen.Value = value; }

	/// <summary>
	/// Enable MACD filter.
	/// </summary>
	public bool UseMacdFilter { get => _useMacdFilter.Value; set => _useMacdFilter.Value = value; }

	/// <summary>
	/// Fast period for MACD.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// Slow period for MACD.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Enable force take-profit on large candle move.
	/// </summary>
	public bool EnableForceTp { get => _enableForceTp.Value; set => _enableForceTp.Value = value; }

	/// <summary>
	/// Percentage move threshold for force take-profit.
	/// </summary>
	public decimal ForceTpPercent { get => _forceTpPercent.Value; set => _forceTpPercent.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BigMoverCatcherStrategy()
	{
		_enableLongs = Param(nameof(EnableLongs), true)
		.SetDisplay("Enable Longs", "Allow long trades", "Trade Filters");
		_enableShorts = Param(nameof(EnableShorts), false)
		.SetDisplay("Enable Shorts", "Allow short trades", "Trade Filters");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Percent stop loss", "Trade Filters");
		_bollingerLength = Param(nameof(BollingerLength), 40)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Length", "Period for Bollinger Bands", "Bollinger Bands");

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
		.SetDisplay("Use RSI Filter", "Enable RSI filter", "RSI");
		_useRsiThreshold = Param(nameof(UseRsiThreshold), true)
		.SetDisplay("Use RSI Threshold", "Use RSI min/max thresholds", "RSI");
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "Period for RSI", "RSI");
		_rsiLongMin = Param(nameof(RsiLongMin), 50m)
		.SetDisplay("RSI Min Long", "Minimum RSI for long", "RSI");
		_rsiShortMax = Param(nameof(RsiShortMax), 50m)
		.SetDisplay("RSI Max Short", "Maximum RSI for short", "RSI");
		_rsiSmoothLen = Param(nameof(RsiSmoothLen), 100)
		.SetGreaterThanZero()
		.SetDisplay("RSI Smooth Length", "EMA length for RSI smoothing", "RSI");

		_useAdxFilter = Param(nameof(UseAdxFilter), false)
		.SetDisplay("Use ADX Filter", "Enable ADX filter", "ADX");
		_useAdxThreshold = Param(nameof(UseAdxThreshold), true)
		.SetDisplay("Use ADX Threshold", "Use ADX threshold", "ADX");
		_adxLength = Param(nameof(AdxLength), 28)
		.SetGreaterThanZero()
		.SetDisplay("ADX Length", "Period for ADX", "ADX");
		_adxThreshold = Param(nameof(AdxThreshold), 20.5m)
		.SetDisplay("ADX Threshold", "Minimum ADX value", "ADX");
		_adxSmoothLen = Param(nameof(AdxSmoothLen), 100)
		.SetGreaterThanZero()
		.SetDisplay("ADX Smooth Length", "EMA length for ADX smoothing", "ADX");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
		.SetDisplay("Use ATR Filter", "Enable ATR filter", "ATR");
		_useAtrMinValue = Param(nameof(UseAtrMinValue), true)
		.SetDisplay("Use ATR Min", "Use minimum ATR value", "ATR");
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "Period for ATR", "ATR");
		_atrMinValue = Param(nameof(AtrMinValue), 13.5m)
		.SetDisplay("ATR Min", "Minimum ATR value", "ATR");
		_atrSmoothLen = Param(nameof(AtrSmoothLen), 100)
		.SetGreaterThanZero()
		.SetDisplay("ATR Smooth Length", "EMA length for ATR smoothing", "ATR");

		_useEmaFilter = Param(nameof(UseEmaFilter), false)
		.SetDisplay("Use EMA Filter", "Enable EMA trend filter", "EMA");
		_emaFilterLen = Param(nameof(EmaFilterLen), 350)
		.SetGreaterThanZero()
		.SetDisplay("EMA Filter Length", "Period for EMA filter", "EMA");

		_useMacdFilter = Param(nameof(UseMacdFilter), false)
		.SetDisplay("Use MACD Filter", "Enable MACD filter", "MACD");
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast period for MACD", "MACD");
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow period for MACD", "MACD");
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal period for MACD", "MACD");

		_enableForceTp = Param(nameof(EnableForceTp), false)
		.SetDisplay("Enable Force TP", "Enable force take-profit", "Force TP");
		_forceTpPercent = Param(nameof(ForceTpPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Force TP %", "Percent move to force take-profit", "Force TP");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands { Length = BollingerLength, Width = 2m };
		var rsi = new RSI { Length = RsiLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var emaFilter = new ExponentialMovingAverage { Length = EmaFilterLen };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFast }, LongMa = { Length = MacdSlow } },
			SignalMa = { Length = MacdSignal }
		};

		_rsiSmooth = new ExponentialMovingAverage { Length = RsiSmoothLen };
		_adxSmooth = new ExponentialMovingAverage { Length = AdxSmoothLen };
		_atrSmooth = new ExponentialMovingAverage { Length = AtrSmoothLen };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(bollinger, rsi, adx, atr, emaFilter, macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, emaFilter);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue rsiValue, IIndicatorValue adxValue, IIndicatorValue atrValue, IIndicatorValue emaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
		return;

		if (!rsiValue.IsFinal || !adxValue.IsFinal || !atrValue.IsFinal || !emaValue.IsFinal || !macdValue.IsFinal)
		return;

		var rsiSmoothedValue = _rsiSmooth.Process(rsiValue);
		var adxSmoothedValue = _adxSmooth.Process(adxValue);
		var atrSmoothedValue = _atrSmooth.Process(atrValue);

		if (!rsiSmoothedValue.IsFinal || !adxSmoothedValue.IsFinal || !atrSmoothedValue.IsFinal)
		return;

		var rsi = rsiValue.GetValue<decimal>();
		var rsiSmooth = rsiSmoothedValue.GetValue<decimal>();
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var adxSmooth = adxSmoothedValue.GetValue<decimal>();
		var atrRaw = atrValue.GetValue<decimal>();
		var atrSmooth = atrSmoothedValue.GetValue<decimal>();
		var ema = emaValue.GetValue<decimal>();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Signal is not decimal signal)
		return;

		var price = candle.ClosePrice;

		var rsiOkLong = !UseRsiThreshold || rsi > RsiLongMin;
		var rsiOkShort = !UseRsiThreshold || rsi < RsiShortMax;
		var rsiFilterLong = !UseRsiFilter || (rsiOkLong && rsi > rsiSmooth);
		var rsiFilterShort = !UseRsiFilter || (rsiOkShort && rsi > rsiSmooth);

		var adxOk = !UseAdxThreshold || adxTyped.MovingAverage > AdxThreshold;
		var adxFilterPass = !UseAdxFilter || (adxOk && adxTyped.MovingAverage > adxSmooth);

		var atrOk = !UseAtrMinValue || atrRaw > AtrMinValue;
		var atrFilterPass = !UseAtrFilter || (atrOk && atrRaw > atrSmooth);

		var emaFilterPassLong = !UseEmaFilter || price > ema;
		var emaFilterPassShort = !UseEmaFilter || price < ema;

		var macdFilterLong = !UseMacdFilter || signal > 0m;
		var macdFilterShort = !UseMacdFilter || signal < 0m;

		var longCondition = EnableLongs && price > upper && macdFilterLong && adxFilterPass && rsiFilterLong && emaFilterPassLong && atrFilterPass;
		var shortCondition = EnableShorts && price < lower && macdFilterShort && adxFilterPass && rsiFilterShort && emaFilterPassShort && atrFilterPass;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && price < middle)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && price > middle)
		{
			BuyMarket(Math.Abs(Position));
		}

		var changePct = Math.Abs(candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100m;
		var forceTp = EnableForceTp && changePct > ForceTpPercent;
		if (forceTp)
		{
			if (Position > 0)
			SellMarket(Position);
			else if (Position < 0)
			BuyMarket(Math.Abs(Position));
		}
	}
}
