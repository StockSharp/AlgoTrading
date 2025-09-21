using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "N7S_AO_772012".
/// Combines multiple perceptrons of the Awesome Oscillator with a price pattern filter and multi-timeframe logic.
/// </summary>
public class N7SAo772012Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _allowLongTrades;
	private readonly StrategyParam<bool> _allowShortTrades;

	private readonly StrategyParam<decimal> _baseTakeProfitFactorLong;
	private readonly StrategyParam<decimal> _baseStopLossPointsLong;
	private readonly StrategyParam<decimal> _baseTakeProfitFactorShort;
	private readonly StrategyParam<decimal> _baseStopLossPointsShort;

	private readonly StrategyParam<int> _perceptronPeriodX;
	private readonly StrategyParam<int> _perceptronWeightX1;
	private readonly StrategyParam<int> _perceptronWeightX2;
	private readonly StrategyParam<int> _perceptronWeightX3;
	private readonly StrategyParam<int> _perceptronWeightX4;
	private readonly StrategyParam<int> _perceptronThresholdX;

	private readonly StrategyParam<int> _perceptronPeriodY;
	private readonly StrategyParam<int> _perceptronWeightY1;
	private readonly StrategyParam<int> _perceptronWeightY2;
	private readonly StrategyParam<int> _perceptronWeightY3;
	private readonly StrategyParam<int> _perceptronWeightY4;
	private readonly StrategyParam<int> _perceptronThresholdY;

	private readonly StrategyParam<int> _pricePatternPeriod;
	private readonly StrategyParam<int> _priceWeight1;
	private readonly StrategyParam<int> _priceWeight2;
	private readonly StrategyParam<int> _priceWeight3;
	private readonly StrategyParam<int> _priceWeight4;

	private readonly StrategyParam<int> _btsMode;
	private readonly StrategyParam<int> _neuroMode;

	private readonly StrategyParam<decimal> _neuroTakeProfitFactorLong;
	private readonly StrategyParam<decimal> _neuroStopLossPointsLong;
	private readonly StrategyParam<decimal> _neuroTakeProfitFactorShort;
	private readonly StrategyParam<decimal> _neuroStopLossPointsShort;

	private readonly StrategyParam<int> _neuroPeriodX;
	private readonly StrategyParam<int> _neuroWeightX1;
	private readonly StrategyParam<int> _neuroWeightX2;
	private readonly StrategyParam<int> _neuroWeightX3;
	private readonly StrategyParam<int> _neuroWeightX4;
	private readonly StrategyParam<int> _neuroThresholdX;

	private readonly StrategyParam<int> _neuroPeriodY;
	private readonly StrategyParam<int> _neuroWeightY1;
	private readonly StrategyParam<int> _neuroWeightY2;
	private readonly StrategyParam<int> _neuroWeightY3;
	private readonly StrategyParam<int> _neuroWeightY4;
	private readonly StrategyParam<int> _neuroThresholdY;

	private readonly StrategyParam<int> _neuroPeriodZ;
	private readonly StrategyParam<int> _neuroWeightZ1;
	private readonly StrategyParam<int> _neuroWeightZ2;
	private readonly StrategyParam<int> _neuroWeightZ3;
	private readonly StrategyParam<int> _neuroWeightZ4;
	private readonly StrategyParam<int> _neuroThresholdZ;

	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal Open, decimal Close)> _minuteHistory = new();
	private readonly List<decimal> _hourAoHistory = new();
	private readonly List<decimal> _h4AoHistory = new();

	private decimal _perceptronLowerX;
	private decimal _perceptronLowerY;
	private decimal _perceptronUpperX;
	private decimal _perceptronUpperY;
	private decimal _perceptronUpperZ;
	private decimal _deltaG12;

	private decimal _pointValue;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="N7SAo772012Strategy"/> class.
	/// </summary>
	public N7SAo772012Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base volume used for market orders.", "Trading");

		_allowLongTrades = Param(nameof(AllowLongTrades), true)
			.SetDisplay("Allow longs", "Enable long side trading.", "Trading");

		_allowShortTrades = Param(nameof(AllowShortTrades), true)
			.SetDisplay("Allow shorts", "Enable short side trading.", "Trading");

		_baseTakeProfitFactorLong = Param(nameof(BaseTakeProfitFactorLong), 50m)
			.SetNotNegative()
			.SetDisplay("Base TP factor (long)", "Multiplier applied to the long base stop-loss when calculating the take-profit.", "Risk");

		_baseStopLossPointsLong = Param(nameof(BaseStopLossPointsLong), 50m)
			.SetNotNegative()
			.SetDisplay("Base SL (long)", "Stop-loss distance in points for the base long signal.", "Risk");

		_baseTakeProfitFactorShort = Param(nameof(BaseTakeProfitFactorShort), 50m)
			.SetNotNegative()
			.SetDisplay("Base TP factor (short)", "Multiplier applied to the short base stop-loss when calculating the take-profit.", "Risk");

		_baseStopLossPointsShort = Param(nameof(BaseStopLossPointsShort), 50m)
			.SetNotNegative()
			.SetDisplay("Base SL (short)", "Stop-loss distance in points for the base short signal.", "Risk");

		_perceptronPeriodX = Param(nameof(PerceptronPeriodX), 10)
			.SetNotNegative()
			.SetDisplay("BTS X period", "Shift used for the first Awesome Oscillator perceptron (buy filter).", "Perceptron");

		_perceptronWeightX1 = Param(nameof(PerceptronWeightX1), 0)
			.SetDisplay("BTS X weight 1", "First weight of the base long perceptron.", "Perceptron");

		_perceptronWeightX2 = Param(nameof(PerceptronWeightX2), 0)
			.SetDisplay("BTS X weight 2", "Second weight of the base long perceptron.", "Perceptron");

		_perceptronWeightX3 = Param(nameof(PerceptronWeightX3), 0)
			.SetDisplay("BTS X weight 3", "Third weight of the base long perceptron.", "Perceptron");

		_perceptronWeightX4 = Param(nameof(PerceptronWeightX4), 0)
			.SetDisplay("BTS X weight 4", "Fourth weight of the base long perceptron.", "Perceptron");

		_perceptronThresholdX = Param(nameof(PerceptronThresholdX), 0)
			.SetNotNegative()
			.SetDisplay("BTS X threshold", "Minimum absolute value required for the base long perceptron.", "Perceptron");

		_perceptronPeriodY = Param(nameof(PerceptronPeriodY), 10)
			.SetNotNegative()
			.SetDisplay("BTS Y period", "Shift used for the base short perceptron.", "Perceptron");

		_perceptronWeightY1 = Param(nameof(PerceptronWeightY1), 0)
			.SetDisplay("BTS Y weight 1", "First weight of the base short perceptron.", "Perceptron");

		_perceptronWeightY2 = Param(nameof(PerceptronWeightY2), 0)
			.SetDisplay("BTS Y weight 2", "Second weight of the base short perceptron.", "Perceptron");

		_perceptronWeightY3 = Param(nameof(PerceptronWeightY3), 0)
			.SetDisplay("BTS Y weight 3", "Third weight of the base short perceptron.", "Perceptron");

		_perceptronWeightY4 = Param(nameof(PerceptronWeightY4), 0)
			.SetDisplay("BTS Y weight 4", "Fourth weight of the base short perceptron.", "Perceptron");

		_perceptronThresholdY = Param(nameof(PerceptronThresholdY), 0)
			.SetNotNegative()
			.SetDisplay("BTS Y threshold", "Minimum absolute value required for the base short perceptron.", "Perceptron");

		_pricePatternPeriod = Param(nameof(PricePatternPeriod), 10)
			.SetNotNegative()
			.SetDisplay("Price period", "Number of M1 candles used in the price perceptron.", "Price filter");

		_priceWeight1 = Param(nameof(PriceWeight1), 0)
			.SetDisplay("Price weight 1", "First weight of the price perceptron.", "Price filter");

		_priceWeight2 = Param(nameof(PriceWeight2), 0)
			.SetDisplay("Price weight 2", "Second weight of the price perceptron.", "Price filter");

		_priceWeight3 = Param(nameof(PriceWeight3), 0)
			.SetDisplay("Price weight 3", "Third weight of the price perceptron.", "Price filter");

		_priceWeight4 = Param(nameof(PriceWeight4), 0)
			.SetDisplay("Price weight 4", "Fourth weight of the price perceptron.", "Price filter");

		_btsMode = Param(nameof(BtsMode), 1)
			.SetDisplay("BTS mode", "When set to 0 the price perceptron filter is ignored.", "Trading");

		_neuroMode = Param(nameof(NeuroMode), 4)
			.SetDisplay("Neuro mode", "Controls how the advanced perceptrons interact before falling back to the base logic.", "Trading");

		_neuroTakeProfitFactorLong = Param(nameof(NeuroTakeProfitFactorLong), 50m)
			.SetNotNegative()
			.SetDisplay("Neuro TP factor (long)", "Multiplier for the advanced long take-profit.", "Neuro");

		_neuroStopLossPointsLong = Param(nameof(NeuroStopLossPointsLong), 50m)
			.SetNotNegative()
			.SetDisplay("Neuro SL (long)", "Stop-loss distance in points for advanced long signals.", "Neuro");

		_neuroTakeProfitFactorShort = Param(nameof(NeuroTakeProfitFactorShort), 50m)
			.SetNotNegative()
			.SetDisplay("Neuro TP factor (short)", "Multiplier for the advanced short take-profit.", "Neuro");

		_neuroStopLossPointsShort = Param(nameof(NeuroStopLossPointsShort), 50m)
			.SetNotNegative()
			.SetDisplay("Neuro SL (short)", "Stop-loss distance in points for advanced short signals.", "Neuro");

		_neuroPeriodX = Param(nameof(NeuroPeriodX), 10)
			.SetNotNegative()
			.SetDisplay("Neuro X period", "Shift used for the advanced long perceptron.", "Neuro");

		_neuroWeightX1 = Param(nameof(NeuroWeightX1), 0)
			.SetDisplay("Neuro X weight 1", "First weight of the advanced long perceptron.", "Neuro");

		_neuroWeightX2 = Param(nameof(NeuroWeightX2), 0)
			.SetDisplay("Neuro X weight 2", "Second weight of the advanced long perceptron.", "Neuro");

		_neuroWeightX3 = Param(nameof(NeuroWeightX3), 0)
			.SetDisplay("Neuro X weight 3", "Third weight of the advanced long perceptron.", "Neuro");

		_neuroWeightX4 = Param(nameof(NeuroWeightX4), 0)
			.SetDisplay("Neuro X weight 4", "Fourth weight of the advanced long perceptron.", "Neuro");

		_neuroThresholdX = Param(nameof(NeuroThresholdX), 0)
			.SetNotNegative()
			.SetDisplay("Neuro X threshold", "Minimum absolute value required for the advanced long perceptron.", "Neuro");

		_neuroPeriodY = Param(nameof(NeuroPeriodY), 10)
			.SetNotNegative()
			.SetDisplay("Neuro Y period", "Shift used for the advanced short perceptron.", "Neuro");

		_neuroWeightY1 = Param(nameof(NeuroWeightY1), 0)
			.SetDisplay("Neuro Y weight 1", "First weight of the advanced short perceptron.", "Neuro");

		_neuroWeightY2 = Param(nameof(NeuroWeightY2), 0)
			.SetDisplay("Neuro Y weight 2", "Second weight of the advanced short perceptron.", "Neuro");

		_neuroWeightY3 = Param(nameof(NeuroWeightY3), 0)
			.SetDisplay("Neuro Y weight 3", "Third weight of the advanced short perceptron.", "Neuro");

		_neuroWeightY4 = Param(nameof(NeuroWeightY4), 0)
			.SetDisplay("Neuro Y weight 4", "Fourth weight of the advanced short perceptron.", "Neuro");

		_neuroThresholdY = Param(nameof(NeuroThresholdY), 0)
			.SetNotNegative()
			.SetDisplay("Neuro Y threshold", "Minimum absolute value required for the advanced short perceptron.", "Neuro");

		_neuroPeriodZ = Param(nameof(NeuroPeriodZ), 10)
			.SetNotNegative()
			.SetDisplay("Neuro Z period", "Shift used for the gating perceptron.", "Neuro");

		_neuroWeightZ1 = Param(nameof(NeuroWeightZ1), 0)
			.SetDisplay("Neuro Z weight 1", "First weight of the gating perceptron.", "Neuro");

		_neuroWeightZ2 = Param(nameof(NeuroWeightZ2), 0)
			.SetDisplay("Neuro Z weight 2", "Second weight of the gating perceptron.", "Neuro");

		_neuroWeightZ3 = Param(nameof(NeuroWeightZ3), 0)
			.SetDisplay("Neuro Z weight 3", "Third weight of the gating perceptron.", "Neuro");

		_neuroWeightZ4 = Param(nameof(NeuroWeightZ4), 0)
			.SetDisplay("Neuro Z weight 4", "Fourth weight of the gating perceptron.", "Neuro");

		_neuroThresholdZ = Param(nameof(NeuroThresholdZ), 0)
			.SetNotNegative()
			.SetDisplay("Neuro Z threshold", "Minimum absolute value required for the gating perceptron.", "Neuro");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Primary candle type", "Timeframe used for trade execution.", "Data");
	}

/// <summary>
/// Trading volume used for new orders.
/// </summary>
public decimal OrderVolume
{
	get => _orderVolume.Value;
	set => _orderVolume.Value = value;
}

/// <summary>
/// Enables long trades when set to <c>true</c>.
/// </summary>
public bool AllowLongTrades
{
	get => _allowLongTrades.Value;
	set => _allowLongTrades.Value = value;
}

/// <summary>
/// Enables short trades when set to <c>true</c>.
/// </summary>
public bool AllowShortTrades
{
	get => _allowShortTrades.Value;
	set => _allowShortTrades.Value = value;
}

/// <summary>
/// Multiplier applied to the long base stop-loss for the take-profit.
/// </summary>
public decimal BaseTakeProfitFactorLong
{
	get => _baseTakeProfitFactorLong.Value;
	set => _baseTakeProfitFactorLong.Value = value;
}

/// <summary>
/// Stop-loss distance in points for the base long signal.
/// </summary>
public decimal BaseStopLossPointsLong
{
	get => _baseStopLossPointsLong.Value;
	set => _baseStopLossPointsLong.Value = value;
}

/// <summary>
/// Multiplier applied to the short base stop-loss for the take-profit.
/// </summary>
public decimal BaseTakeProfitFactorShort
{
	get => _baseTakeProfitFactorShort.Value;
	set => _baseTakeProfitFactorShort.Value = value;
}

/// <summary>
/// Stop-loss distance in points for the base short signal.
/// </summary>
public decimal BaseStopLossPointsShort
{
	get => _baseStopLossPointsShort.Value;
	set => _baseStopLossPointsShort.Value = value;
}

/// <summary>
/// Shift used for the base long perceptron.
/// </summary>
public int PerceptronPeriodX
{
	get => _perceptronPeriodX.Value;
	set => _perceptronPeriodX.Value = value;
}

/// <summary>
/// First weight of the base long perceptron.
/// </summary>
public int PerceptronWeightX1
{
	get => _perceptronWeightX1.Value;
	set => _perceptronWeightX1.Value = value;
}

/// <summary>
/// Second weight of the base long perceptron.
/// </summary>
public int PerceptronWeightX2
{
	get => _perceptronWeightX2.Value;
	set => _perceptronWeightX2.Value = value;
}

/// <summary>
/// Third weight of the base long perceptron.
/// </summary>
public int PerceptronWeightX3
{
	get => _perceptronWeightX3.Value;
	set => _perceptronWeightX3.Value = value;
}

/// <summary>
/// Fourth weight of the base long perceptron.
/// </summary>
public int PerceptronWeightX4
{
	get => _perceptronWeightX4.Value;
	set => _perceptronWeightX4.Value = value;
}

/// <summary>
/// Threshold applied to the base long perceptron value.
/// </summary>
public int PerceptronThresholdX
{
	get => _perceptronThresholdX.Value;
	set => _perceptronThresholdX.Value = value;
}

/// <summary>
/// Shift used for the base short perceptron.
/// </summary>
public int PerceptronPeriodY
{
	get => _perceptronPeriodY.Value;
	set => _perceptronPeriodY.Value = value;
}

/// <summary>
/// First weight of the base short perceptron.
/// </summary>
public int PerceptronWeightY1
{
	get => _perceptronWeightY1.Value;
	set => _perceptronWeightY1.Value = value;
}

/// <summary>
/// Second weight of the base short perceptron.
/// </summary>
public int PerceptronWeightY2
{
	get => _perceptronWeightY2.Value;
	set => _perceptronWeightY2.Value = value;
}

/// <summary>
/// Third weight of the base short perceptron.
/// </summary>
public int PerceptronWeightY3
{
	get => _perceptronWeightY3.Value;
	set => _perceptronWeightY3.Value = value;
}

/// <summary>
/// Fourth weight of the base short perceptron.
/// </summary>
public int PerceptronWeightY4
{
	get => _perceptronWeightY4.Value;
	set => _perceptronWeightY4.Value = value;
}

/// <summary>
/// Threshold applied to the base short perceptron value.
/// </summary>
public int PerceptronThresholdY
{
	get => _perceptronThresholdY.Value;
	set => _perceptronThresholdY.Value = value;
}

/// <summary>
/// Number of minute candles used in the price perceptron.
/// </summary>
public int PricePatternPeriod
{
	get => _pricePatternPeriod.Value;
	set => _pricePatternPeriod.Value = value;
}

/// <summary>
/// First weight of the price perceptron.
/// </summary>
public int PriceWeight1
{
	get => _priceWeight1.Value;
	set => _priceWeight1.Value = value;
}

/// <summary>
/// Second weight of the price perceptron.
/// </summary>
public int PriceWeight2
{
	get => _priceWeight2.Value;
	set => _priceWeight2.Value = value;
}

/// <summary>
/// Third weight of the price perceptron.
/// </summary>
public int PriceWeight3
{
	get => _priceWeight3.Value;
	set => _priceWeight3.Value = value;
}

/// <summary>
/// Fourth weight of the price perceptron.
/// </summary>
public int PriceWeight4
{
	get => _priceWeight4.Value;
	set => _priceWeight4.Value = value;
}

/// <summary>
/// Controls whether the price perceptron gate is enforced.
/// </summary>
public int BtsMode
{
	get => _btsMode.Value;
	set => _btsMode.Value = value;
}

/// <summary>
/// Controls how advanced perceptrons override the base logic.
/// </summary>
public int NeuroMode
{
	get => _neuroMode.Value;
	set => _neuroMode.Value = value;
}

/// <summary>
/// Multiplier for the advanced long take-profit.
/// </summary>
public decimal NeuroTakeProfitFactorLong
{
	get => _neuroTakeProfitFactorLong.Value;
	set => _neuroTakeProfitFactorLong.Value = value;
}

/// <summary>
/// Stop-loss distance in points for advanced long signals.
/// </summary>
public decimal NeuroStopLossPointsLong
{
	get => _neuroStopLossPointsLong.Value;
	set => _neuroStopLossPointsLong.Value = value;
}

/// <summary>
/// Multiplier for the advanced short take-profit.
/// </summary>
public decimal NeuroTakeProfitFactorShort
{
	get => _neuroTakeProfitFactorShort.Value;
	set => _neuroTakeProfitFactorShort.Value = value;
}

/// <summary>
/// Stop-loss distance in points for advanced short signals.
/// </summary>
public decimal NeuroStopLossPointsShort
{
	get => _neuroStopLossPointsShort.Value;
	set => _neuroStopLossPointsShort.Value = value;
}

/// <summary>
/// Shift used for the advanced long perceptron.
/// </summary>
public int NeuroPeriodX
{
	get => _neuroPeriodX.Value;
	set => _neuroPeriodX.Value = value;
}

/// <summary>
/// First weight of the advanced long perceptron.
/// </summary>
public int NeuroWeightX1
{
	get => _neuroWeightX1.Value;
	set => _neuroWeightX1.Value = value;
}

/// <summary>
/// Second weight of the advanced long perceptron.
/// </summary>
public int NeuroWeightX2
{
	get => _neuroWeightX2.Value;
	set => _neuroWeightX2.Value = value;
}

/// <summary>
/// Third weight of the advanced long perceptron.
/// </summary>
public int NeuroWeightX3
{
	get => _neuroWeightX3.Value;
	set => _neuroWeightX3.Value = value;
}

/// <summary>
/// Fourth weight of the advanced long perceptron.
/// </summary>
public int NeuroWeightX4
{
	get => _neuroWeightX4.Value;
	set => _neuroWeightX4.Value = value;
}

/// <summary>
/// Threshold applied to the advanced long perceptron value.
/// </summary>
public int NeuroThresholdX
{
	get => _neuroThresholdX.Value;
	set => _neuroThresholdX.Value = value;
}

/// <summary>
/// Shift used for the advanced short perceptron.
/// </summary>
public int NeuroPeriodY
{
	get => _neuroPeriodY.Value;
	set => _neuroPeriodY.Value = value;
}

/// <summary>
/// First weight of the advanced short perceptron.
/// </summary>
public int NeuroWeightY1
{
	get => _neuroWeightY1.Value;
	set => _neuroWeightY1.Value = value;
}

/// <summary>
/// Second weight of the advanced short perceptron.
/// </summary>
public int NeuroWeightY2
{
	get => _neuroWeightY2.Value;
	set => _neuroWeightY2.Value = value;
}

/// <summary>
/// Third weight of the advanced short perceptron.
/// </summary>
public int NeuroWeightY3
{
	get => _neuroWeightY3.Value;
	set => _neuroWeightY3.Value = value;
}

/// <summary>
/// Fourth weight of the advanced short perceptron.
/// </summary>
public int NeuroWeightY4
{
	get => _neuroWeightY4.Value;
	set => _neuroWeightY4.Value = value;
}

/// <summary>
/// Threshold applied to the advanced short perceptron value.
/// </summary>
public int NeuroThresholdY
{
	get => _neuroThresholdY.Value;
	set => _neuroThresholdY.Value = value;
}

/// <summary>
/// Shift used for the gating perceptron.
/// </summary>
public int NeuroPeriodZ
{
	get => _neuroPeriodZ.Value;
	set => _neuroPeriodZ.Value = value;
}

/// <summary>
/// First weight of the gating perceptron.
/// </summary>
public int NeuroWeightZ1
{
	get => _neuroWeightZ1.Value;
	set => _neuroWeightZ1.Value = value;
}

/// <summary>
/// Second weight of the gating perceptron.
/// </summary>
public int NeuroWeightZ2
{
	get => _neuroWeightZ2.Value;
	set => _neuroWeightZ2.Value = value;
}

/// <summary>
/// Third weight of the gating perceptron.
/// </summary>
public int NeuroWeightZ3
{
	get => _neuroWeightZ3.Value;
	set => _neuroWeightZ3.Value = value;
}

/// <summary>
/// Fourth weight of the gating perceptron.
/// </summary>
public int NeuroWeightZ4
{
	get => _neuroWeightZ4.Value;
	set => _neuroWeightZ4.Value = value;
}

/// <summary>
/// Threshold applied to the gating perceptron value.
/// </summary>
public int NeuroThresholdZ
{
	get => _neuroThresholdZ.Value;
	set => _neuroThresholdZ.Value = value;
}

/// <summary>
/// Primary candle type used for signal evaluation.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	Volume = OrderVolume;

	_pointValue = Security?.PriceStep ?? 1m;
	if (_pointValue <= 0m)
	_pointValue = 1m;

	_minuteHistory.Clear();
	_hourAoHistory.Clear();
	_h4AoHistory.Clear();

	_perceptronLowerX = 0m;
	_perceptronLowerY = 0m;
	_perceptronUpperX = 0m;
	_perceptronUpperY = 0m;
	_perceptronUpperZ = 0m;
	_deltaG12 = 0m;
	_stopPrice = null;
	_takeProfitPrice = null;

	var minuteSubscription = SubscribeCandles(CandleType);
	minuteSubscription
		.Bind(ProcessMinuteCandle)
		.Start();

	var hourSubscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
	var hourAo = new AwesomeOscillator();
	hourSubscription
		.Bind(hourAo, ProcessHourAo)
		.Start();

	var h4Subscription = SubscribeCandles(TimeSpan.FromHours(4).TimeFrame());
	var h4Ao = new AwesomeOscillator();
	h4Subscription
		.Bind(h4Ao, ProcessH4Ao)
		.Start();
}

private void ProcessMinuteCandle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (Volume != OrderVolume)
	Volume = OrderVolume;

	UpdateMinuteHistory(candle);

	if (ManagePosition(candle))
	return;

	var time = candle.CloseTime ?? candle.OpenTime;
	if (!IsTradingTime(time))
	return;

	var signal = EvaluateSignal();
	switch (signal.Direction)
	{
		case SignalDirection.Long:
			TryEnterLong(candle.ClosePrice, signal.StopDistance, signal.TakeDistance);
			break;
		case SignalDirection.Short:
			TryEnterShort(candle.ClosePrice, signal.StopDistance, signal.TakeDistance);
			break;
	}
}

private void ProcessHourAo(ICandleMessage candle, decimal aoValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	AddHourValue(aoValue);
	UpdatePerceptrons();
}

private void ProcessH4Ao(ICandleMessage candle, decimal aoValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	_h4AoHistory.Insert(0, aoValue);
	if (_h4AoHistory.Count > 10)
	_h4AoHistory.RemoveAt(_h4AoHistory.Count - 1);

	if (_h4AoHistory.Count > 2)
	_deltaG12 = _h4AoHistory[1] - _h4AoHistory[2];
}

private void UpdateMinuteHistory(ICandleMessage candle)
{
	_minuteHistory.Insert(0, (candle.OpenPrice, candle.ClosePrice));

	var period = PricePatternPeriod;
	var limit = Math.Max(period * 4 + 5, 50);
	if (_minuteHistory.Count > limit)
	_minuteHistory.RemoveAt(_minuteHistory.Count - 1);
}

private void AddHourValue(decimal value)
{
	_hourAoHistory.Insert(0, value);

	var maxPeriod = GetMaxHourPeriod();
	var limit = Math.Max(maxPeriod * 3 + 5, 20);
	if (_hourAoHistory.Count > limit)
	_hourAoHistory.RemoveAt(_hourAoHistory.Count - 1);
}

private int GetMaxHourPeriod()
{
	var max = Math.Max(Math.Max(PerceptronPeriodX, PerceptronPeriodY), PricePatternPeriod);
	max = Math.Max(max, Math.Max(Math.Max(NeuroPeriodX, NeuroPeriodY), NeuroPeriodZ));
	return Math.Max(max, 1);
}

private void UpdatePerceptrons()
{
	_perceptronLowerX = ComputePerceptron(PerceptronWeightX1, PerceptronWeightX2, PerceptronWeightX3, PerceptronWeightX4, PerceptronPeriodX, PerceptronThresholdX);
	_perceptronLowerY = ComputePerceptron(PerceptronWeightY1, PerceptronWeightY2, PerceptronWeightY3, PerceptronWeightY4, PerceptronPeriodY, PerceptronThresholdY);
	_perceptronUpperX = ComputePerceptron(NeuroWeightX1, NeuroWeightX2, NeuroWeightX3, NeuroWeightX4, NeuroPeriodX, NeuroThresholdX);
	_perceptronUpperY = ComputePerceptron(NeuroWeightY1, NeuroWeightY2, NeuroWeightY3, NeuroWeightY4, NeuroPeriodY, NeuroThresholdY);
	_perceptronUpperZ = ComputePerceptron(NeuroWeightZ1, NeuroWeightZ2, NeuroWeightZ3, NeuroWeightZ4, NeuroPeriodZ, NeuroThresholdZ);
}

private decimal ComputePerceptron(int weight1, int weight2, int weight3, int weight4, int period, int threshold)
{
	if (period <= 0)
	return 0m;

	var required = period * 3;
	if (_hourAoHistory.Count <= required)
	return 0m;

	var baseValue = _hourAoHistory[0];
	if (baseValue == 0m)
	return 0m;

	var idx1 = period;
	var idx2 = period * 2;
	var idx3 = period * 3;

	if (idx3 >= _hourAoHistory.Count)
	return 0m;

	var value1 = _hourAoHistory[idx1];
	var value2 = _hourAoHistory[idx2];
	var value3 = _hourAoHistory[idx3];

	var q1 = weight1 - 50m;
	var q2 = weight2 - 50m;
	var q3 = weight3 - 50m;
	var q4 = weight4 - 50m;

	var result = q1 + (q2 * value1 + q3 * value2 + q4 * value3) / baseValue;

	if (Math.Abs(result) > threshold)
	return result;

	return 0m;
}

private SignalResult EvaluateSignal()
{
	var priceSignal = ComputePricePerceptron();
	return EvaluateVsr(priceSignal);
}

private decimal ComputePricePerceptron()
{
	var period = PricePatternPeriod;
	if (period <= 0)
	return 0m;

	var required = period * 4;
	if (_minuteHistory.Count <= required)
	return 0m;

	var close = _minuteHistory[0].Close;
	var open1 = _minuteHistory[period].Open;
	var open2 = _minuteHistory[period * 2].Open;
	var open3 = _minuteHistory[period * 3].Open;
	var open4 = _minuteHistory[period * 4].Open;

	var w1 = PriceWeight1 - 50m;
	var w2 = PriceWeight2 - 50m;
	var w3 = PriceWeight3 - 50m;
	var w4 = PriceWeight4 - 50m;

	return w1 * (close - open1) +
	w2 * (open1 - open2) +
	w3 * (open2 - open3) +
	w4 * (open3 - open4);
}

private SignalResult EvaluateVsr(decimal priceSignal)
{
	switch (NeuroMode)
	{
		case 4:
			if (_perceptronUpperZ > 0m)
			{
			if (_perceptronUpperX > 0m)
			return CreateSignal(SignalDirection.Long, NeuroStopLossPointsLong, NeuroTakeProfitFactorLong * NeuroStopLossPointsLong);
		}
	else
	{
		if (_perceptronUpperY > 0m)
		return CreateSignal(SignalDirection.Short, NeuroStopLossPointsShort, NeuroTakeProfitFactorShort * NeuroStopLossPointsShort);
	}
return EvaluateBts(priceSignal);
case 3:
	if (_perceptronUpperY > 0m)
	return CreateSignal(SignalDirection.Short, NeuroStopLossPointsShort, NeuroTakeProfitFactorShort * NeuroStopLossPointsShort);
	return EvaluateBts(priceSignal);
case 2:
	if (_perceptronUpperX > 0m)
	return CreateSignal(SignalDirection.Long, NeuroStopLossPointsLong, NeuroTakeProfitFactorLong * NeuroStopLossPointsLong);
	return EvaluateBts(priceSignal);
default:
	return EvaluateBts(priceSignal);
}
}

private SignalResult EvaluateBts(decimal priceSignal)
{
	var allowLongCheck = priceSignal > 0m || BtsMode == 0;
	var allowShortCheck = priceSignal < 0m || BtsMode == 0;

	if (allowLongCheck && _perceptronLowerX > 0m && _deltaG12 > 0m)
	{
		var stop = BaseStopLossPointsLong;
		var take = BaseTakeProfitFactorLong * stop;
		return CreateSignal(SignalDirection.Long, stop, take);
	}

if (allowShortCheck && _perceptronLowerY > 0m && _deltaG12 < 0m)
{
	var stop = BaseStopLossPointsShort;
	var take = BaseTakeProfitFactorShort * stop;
	return CreateSignal(SignalDirection.Short, stop, take);
}

return SignalResult.None;
}

private SignalResult CreateSignal(SignalDirection direction, decimal stopPoints, decimal takePoints)
{
	if (direction == SignalDirection.None)
	return SignalResult.None;

	var stopDistance = stopPoints > 0m ? stopPoints * _pointValue : 0m;
	var takeDistance = takePoints > 0m ? takePoints * _pointValue : 0m;

	return new SignalResult(direction, stopDistance, takeDistance);
}

private bool ManagePosition(ICandleMessage candle)
{
	if (Position == 0m)
	{
		ResetProtection();
		return false;
	}

if (Position > 0m)
{
	if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
	{
		ClosePosition();
		ResetProtection();
		return true;
	}

if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
{
	ClosePosition();
	ResetProtection();
	return true;
}
}
else if (Position < 0m)
{
	if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
	{
		ClosePosition();
		ResetProtection();
		return true;
	}

if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
{
	ClosePosition();
	ResetProtection();
	return true;
}
}

return false;
}

private void TryEnterLong(decimal price, decimal stopDistance, decimal takeDistance)
{
	if (!AllowLongTrades)
	return;

	if (Position < 0m)
	{
		ClosePosition();
		return;
	}

if (Position > 0m)
return;

_stopPrice = stopDistance > 0m ? price - stopDistance : null;
_takeProfitPrice = takeDistance > 0m ? price + takeDistance : null;

BuyMarket();
}

private void TryEnterShort(decimal price, decimal stopDistance, decimal takeDistance)
{
	if (!AllowShortTrades)
	return;

	if (Position > 0m)
	{
		ClosePosition();
		return;
	}

if (Position < 0m)
return;

_stopPrice = stopDistance > 0m ? price + stopDistance : null;
_takeProfitPrice = takeDistance > 0m ? price - takeDistance : null;

SellMarket();
}

private void ResetProtection()
{
	_stopPrice = null;
	_takeProfitPrice = null;
}

private static bool IsTradingTime(DateTimeOffset time)
{
	var local = time.LocalDateTime;
	switch (local.DayOfWeek)
	{
		case DayOfWeek.Monday:
			return local.Hour >= 2;
		case DayOfWeek.Friday:
			return local.Hour < 18;
		default:
			return true;
	}
}

private readonly struct SignalResult
{
	public static readonly SignalResult None = new(SignalDirection.None, 0m, 0m);

	public SignalResult(SignalDirection direction, decimal stopDistance, decimal takeDistance)
	{
		Direction = direction;
		StopDistance = stopDistance;
		TakeDistance = takeDistance;
	}

public SignalDirection Direction { get; }
public decimal StopDistance { get; }
public decimal TakeDistance { get; }
}

private enum SignalDirection
{
	None,
	Long,
	Short
}
}
