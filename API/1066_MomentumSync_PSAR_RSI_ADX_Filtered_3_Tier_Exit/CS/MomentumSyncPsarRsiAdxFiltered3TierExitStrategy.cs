using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on Parabolic SAR bullish flip filtered by RSI and ADX.
/// Closes the position three bars after a Parabolic SAR bearish flip.
/// </summary>
public class MomentumSyncPsarRsiAdxFiltered3TierExitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _psarAbovePrev1;
	private bool _psarAbovePrev2;
	private int _barsSinceBearishFlip = -1;

	/// <summary>
	/// Parabolic SAR start acceleration factor.
	/// </summary>
	public decimal SarStart
	{
	get => _sarStart.Value;
	set => _sarStart.Value = value;
	}

	/// <summary>
	/// Parabolic SAR increment step.
	/// </summary>
	public decimal SarIncrement
	{
	get => _sarIncrement.Value;
	set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMax
	{
	get => _sarMax.Value;
	set => _sarMax.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
	get => _adxPeriod.Value;
	set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum RSI value to allow entry.
	/// </summary>
	public decimal RsiThreshold
	{
	get => _rsiThreshold.Value;
	set => _rsiThreshold.Value = value;
	}

	/// <summary>
	/// Minimum ADX value to allow entry.
	/// </summary>
	public decimal AdxThreshold
	{
	get => _adxThreshold.Value;
	set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MomentumSyncPsarRsiAdxFiltered3TierExitStrategy()
	{
	_sarStart =
		Param(nameof(SarStart), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Start",
				"Initial acceleration factor for Parabolic SAR",
				"Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.05m, 0.01m);

	_sarIncrement =
		Param(nameof(SarIncrement), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Increment", "Increment for Parabolic SAR",
				"Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.05m, 0.01m);

	_sarMax =
		Param(nameof(SarMax), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Max",
				"Maximum acceleration factor for Parabolic SAR",
				"Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.3m, 0.1m);

	_rsiPeriod = Param(nameof(RsiPeriod), 14)
			 .SetGreaterThanZero()
			 .SetDisplay("RSI Period", "Period for RSI indicator",
					 "Indicators")
			 .SetCanOptimize(true)
			 .SetOptimize(7, 21, 7);

	_adxPeriod = Param(nameof(AdxPeriod), 14)
			 .SetGreaterThanZero()
			 .SetDisplay("ADX Period", "Period for ADX indicator",
					 "Indicators")
			 .SetCanOptimize(true)
			 .SetOptimize(7, 21, 7);

	_rsiThreshold =
		Param(nameof(RsiThreshold), 40m)
		.SetNotNegative()
		.SetDisplay("RSI Threshold", "Minimum RSI for entry", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(30m, 50m, 5m);

	_adxThreshold =
		Param(nameof(AdxThreshold), 18m)
		.SetNotNegative()
		.SetDisplay("ADX Threshold", "Minimum ADX for entry", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(15m, 30m, 5m);

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var psar = new ParabolicSar { Acceleration = SarStart,
					  AccelerationStep = SarIncrement,
					  AccelerationMax = SarMax };

	var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
	var adx = new AverageDirectionalIndex { Length = AdxPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription.BindEx(psar, rsi, adx, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, psar);

		var indicatorsArea = CreateChartArea();
		if (indicatorsArea != null)
		{
		DrawIndicator(indicatorsArea, rsi);
		DrawIndicator(indicatorsArea, adx);
		}

		DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue psarValue,
				   IIndicatorValue rsiValue,
				   IIndicatorValue adxValue)
	{
	if (candle.State != CandleStates.Finished)
		return;

	if (!psarValue.IsFinal || !rsiValue.IsFinal || !adxValue.IsFinal)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	var psar = psarValue.GetValue<decimal>();
	var rsi = rsiValue.GetValue<decimal>();
	var adx = ((AverageDirectionalIndexValue)adxValue).MovingAverage;

	var psarBullishFlip =
		psar < candle.ClosePrice && _psarAbovePrev1 && _psarAbovePrev2;
	var psarBearishFlip =
		psar > candle.ClosePrice && !_psarAbovePrev1 && !_psarAbovePrev2;
	var rsiAdxOk = rsi > RsiThreshold && adx > AdxThreshold;

	if (Position == 0 && psarBullishFlip && rsiAdxOk)
	{
		BuyMarket();
	}

	if (Position > 0)
	{
		if (psarBearishFlip && _barsSinceBearishFlip < 0)
		_barsSinceBearishFlip = 0;
		else if (_barsSinceBearishFlip >= 0)
		_barsSinceBearishFlip++;

		if (_barsSinceBearishFlip == 3)
		{
		SellMarket(Math.Abs(Position));
		_barsSinceBearishFlip = -1;
		}
	}
	else
	{
		_barsSinceBearishFlip = -1;
	}

	_psarAbovePrev2 = _psarAbovePrev1;
	_psarAbovePrev1 = psar > candle.ClosePrice;
	}
}
