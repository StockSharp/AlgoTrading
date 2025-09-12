using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi indicator swing strategy.
/// </summary>
public class MultiIndicatorSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _usePsar;
	private readonly StrategyParam<bool> _useSuperTrend;
	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<bool> _useLiquidityDelta;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private readonly StrategyParam<decimal> _psarStart;
	private readonly StrategyParam<decimal> _psarIncrement;
	private readonly StrategyParam<decimal> _psarMaximum;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _deltaLength;
	private readonly StrategyParam<int> _deltaSmooth;
	private readonly StrategyParam<decimal> _deltaThreshold;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private ParabolicSar _psar;
	private SuperTrend _superTrend;
	private AverageDirectionalIndex _adx;
	private SimpleMovingAverage _deltaSma;
	private SimpleMovingAverage _volumeSma;
	private ExponentialMovingAverage _deltaEma;

	private decimal _deltaValue;
	private bool _deltaIsFormed;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool UsePsar { get => _usePsar.Value; set => _usePsar.Value = value; }
	public bool UseSuperTrend { get => _useSuperTrend.Value; set => _useSuperTrend.Value = value; }
	public bool UseAdx { get => _useAdx.Value; set => _useAdx.Value = value; }
	public bool UseLiquidityDelta { get => _useLiquidityDelta.Value; set => _useLiquidityDelta.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	public decimal PsarStart { get => _psarStart.Value; set => _psarStart.Value = value; }
	public decimal PsarIncrement { get => _psarIncrement.Value; set => _psarIncrement.Value = value; }
	public decimal PsarMaximum { get => _psarMaximum.Value; set => _psarMaximum.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int DeltaLength { get => _deltaLength.Value; set => _deltaLength.Value = value; }
	public int DeltaSmooth { get => _deltaSmooth.Value; set => _deltaSmooth.Value = value; }
	public decimal DeltaThreshold { get => _deltaThreshold.Value; set => _deltaThreshold.Value = value; }
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public MultiIndicatorSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(2).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_usePsar = Param(nameof(UsePsar), false)
			.SetDisplay("Use PSAR", "Enable Parabolic SAR", "Switches");
		_useSuperTrend = Param(nameof(UseSuperTrend), true)
			.SetDisplay("Use SuperTrend", "Enable SuperTrend", "Switches");
		_useAdx = Param(nameof(UseAdx), true)
			.SetDisplay("Use ADX", "Enable Average Directional Index", "Switches");
		_useLiquidityDelta = Param(nameof(UseLiquidityDelta), true)
			.SetDisplay("Use Delta", "Enable volume delta filter", "Switches");
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long positions", "Trading");
		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short positions", "Trading");

		_psarStart = Param(nameof(PsarStart), 0.02m)
			.SetDisplay("PSAR Start", "Initial acceleration factor", "PSAR");
		_psarIncrement = Param(nameof(PsarIncrement), 0.02m)
			.SetDisplay("PSAR Increment", "Acceleration step", "PSAR");
		_psarMaximum = Param(nameof(PsarMaximum), 0.2m)
			.SetDisplay("PSAR Max", "Maximum acceleration factor", "PSAR");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length for SuperTrend", "SuperTrend");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR multiplier", "SuperTrend");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Period for ADX", "ADX");
		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Trend strength threshold", "ADX");

		_deltaLength = Param(nameof(DeltaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Delta Length", "Length for volume delta average", "Delta");
		_deltaSmooth = Param(nameof(DeltaSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Delta Smoothing", "EMA period for delta", "Delta");
		_deltaThreshold = Param(nameof(DeltaThreshold), 0.5m)
			.SetDisplay("Delta Threshold", "Signal threshold", "Delta");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");
		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_deltaValue = 0m;
		_deltaIsFormed = false;
		_longStop = _longTake = _shortStop = _shortTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_psar = new ParabolicSar
		{
		Acceleration = PsarStart,
		AccelerationStep = PsarIncrement,
		AccelerationMax = PsarMaximum
		};

		_superTrend = new SuperTrend
		{
		Length = AtrPeriod,
		Multiplier = AtrMultiplier
		};

		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_deltaSma = new SimpleMovingAverage { Length = DeltaLength };
		_volumeSma = new SimpleMovingAverage { Length = DeltaLength };
		_deltaEma = new ExponentialMovingAverage { Length = DeltaSmooth };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_psar, _superTrend, _adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _psar);
		DrawIndicator(area, _superTrend);
		DrawIndicator(area, _adx);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue psarValue, IIndicatorValue stValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// update volume delta
		var range = candle.HighPrice - candle.LowPrice + 0.000001m;
		var bidVolume = candle.ClosePrice < candle.OpenPrice
		? candle.TotalVolume
		: candle.TotalVolume * (candle.HighPrice - candle.ClosePrice) / range;
		var askVolume = candle.ClosePrice > candle.OpenPrice
		? candle.TotalVolume
		: candle.TotalVolume * (candle.ClosePrice - candle.LowPrice) / range;
		var deltaRaw = bidVolume - askVolume;

		var deltaAvg = _deltaSma.Process(candle, deltaRaw);
		var volAvg = _volumeSma.Process(candle, candle.TotalVolume);

		if (deltaAvg is DecimalIndicatorValue { IsFinal: true, Value: var avg } &&
		volAvg is DecimalIndicatorValue { IsFinal: true, Value: var vAvg } && vAvg != 0m)
		{
		var normalized = avg / vAvg;
		var smooth = _deltaEma.Process(candle, normalized);
		if (smooth is DecimalIndicatorValue { IsFinal: true, Value: var s })
		{
		_deltaValue = s;
		_deltaIsFormed = true;
		}
		}

		if (psarValue is not DecimalIndicatorValue { IsFinal: true, Value: var psar })
		return;

		if (stValue is not SuperTrendIndicatorValue st || !stValue.IsFinal)
		return;

		if (adxValue is not AverageDirectionalIndexValue adx || !adxValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var psarBuy = !UsePsar || candle.ClosePrice > psar;
		var psarSell = !UsePsar || candle.ClosePrice < psar;
		var stBuy = !UseSuperTrend || st.IsUpTrend;
		var stSell = !UseSuperTrend || st.IsDownTrend;
		var adxOk = !UseAdx || adx.Adx > AdxThreshold;
		var deltaBuy = !UseLiquidityDelta || (_deltaIsFormed && _deltaValue > DeltaThreshold);
		var deltaSell = !UseLiquidityDelta || (_deltaIsFormed && _deltaValue < -DeltaThreshold);

		var buySignal = psarBuy && stBuy && adxOk && deltaBuy;
		var sellSignal = psarSell && stSell && adxOk && deltaSell;

		if (EnableLong && buySignal && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		if (UseStopLoss)
		_longStop = candle.ClosePrice * (1 - StopLossPercent / 100m);
		if (UseTakeProfit)
		_longTake = candle.ClosePrice * (1 + TakeProfitPercent / 100m);
		}
		else if (EnableShort && sellSignal && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		if (UseStopLoss)
		_shortStop = candle.ClosePrice * (1 + StopLossPercent / 100m);
		if (UseTakeProfit)
		_shortTake = candle.ClosePrice * (1 - TakeProfitPercent / 100m);
		}

		if (Position > 0)
		{
		if (UseStopLoss && candle.LowPrice <= _longStop)
		SellMarket(Math.Abs(Position));
		else if (UseTakeProfit && candle.HighPrice >= _longTake)
		SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
		if (UseStopLoss && candle.HighPrice >= _shortStop)
		BuyMarket(Math.Abs(Position));
		else if (UseTakeProfit && candle.LowPrice <= _shortTake)
		BuyMarket(Math.Abs(Position));
		}
	}
}
