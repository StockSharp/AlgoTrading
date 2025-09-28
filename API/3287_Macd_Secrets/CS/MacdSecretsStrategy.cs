using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe MACD confirmation strategy inspired by the "Macd Secrets I" expert advisor.
/// </summary>
public class MacdSecretsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _monthlyCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergenceSignal _primaryMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _trendMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;
	private Momentum _trendMomentum = null!;

	private readonly Queue<decimal> _momentumDeviations = new();

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private (decimal macd, decimal signal)? _primaryMacdValue;
	private (decimal macd, decimal signal)? _trendMacdValue;
	private (decimal macd, decimal signal)? _monthlyMacdValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdSecretsStrategy"/> class.
	/// </summary>
	public MacdSecretsStrategy()
	{
	_orderVolume = Param(nameof(OrderVolume), 0.1m)
	.SetGreaterThanZero()
	.SetDisplay("Order Volume", "Position size in lots", "Trading");

	_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
	.SetNotNegative()
	.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

	_stopLossPoints = Param(nameof(StopLossPoints), 20m)
	.SetNotNegative()
	.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

	_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
	.SetGreaterThanZero()
	.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend");

	_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
	.SetGreaterThanZero()
	.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend");

	_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
	.SetGreaterThanZero()
	.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD");

	_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
	.SetGreaterThanZero()
	.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD");

	_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
	.SetGreaterThanZero()
	.SetDisplay("MACD Signal", "Signal EMA length for MACD", "MACD");

	_momentumPeriod = Param(nameof(MomentumPeriod), 14)
	.SetGreaterThanZero()
	.SetDisplay("Momentum Period", "Momentum lookback length on the trend timeframe", "Momentum");

	_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
	.SetNotNegative()
	.SetDisplay("Momentum Buy", "Minimum deviation from 100 for long trades", "Momentum");

	_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
	.SetNotNegative()
	.SetDisplay("Momentum Sell", "Minimum deviation from 100 for short trades", "Momentum");

	_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Primary TF", "Execution timeframe", "Timeframes");

	_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("Trend TF", "Higher timeframe used for confirmation", "Timeframes");

	_monthlyCandleType = Param(nameof(MonthlyCandleType), TimeSpan.FromDays(30).TimeFrame())
	.SetDisplay("Monthly TF", "Long-term confirmation timeframe", "Timeframes");
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
	get => _orderVolume.Value;
	set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
	get => _takeProfitPoints.Value;
	set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
	get => _stopLossPoints.Value;
	set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
	get => _fastMaPeriod.Value;
	set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
	get => _slowMaPeriod.Value;
	set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by MACD.
	/// </summary>
	public int MacdFastPeriod
	{
	get => _macdFastPeriod.Value;
	set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
	get => _macdSlowPeriod.Value;
	set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length used by MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
	get => _macdSignalPeriod.Value;
	set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback length on the confirmation timeframe.
	/// </summary>
	public int MomentumPeriod
	{
	get => _momentumPeriod.Value;
	set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation to allow long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
	get => _momentumBuyThreshold.Value;
	set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation to allow short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
	get => _momentumSellThreshold.Value;
	set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Execution timeframe.
	/// </summary>
	public DataType PrimaryCandleType
	{
	get => _primaryCandleType.Value;
	set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for MACD and momentum confirmation.
	/// </summary>
	public DataType TrendCandleType
	{
	get => _trendCandleType.Value;
	set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Long-term confirmation timeframe.
	/// </summary>
	public DataType MonthlyCandleType
	{
	get => _monthlyCandleType.Value;
	set => _monthlyCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, PrimaryCandleType);
	yield return (Security, TrendCandleType);
	yield return (Security, MonthlyCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
	_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
	_primaryMacd = CreateMacd();
	_trendMacd = CreateMacd();
	_monthlyMacd = CreateMacd();
	_trendMomentum = new Momentum { Length = MomentumPeriod };

	var primarySubscription = SubscribeCandles(PrimaryCandleType);
	primarySubscription
	.BindEx(_primaryMacd, _fastMa, _slowMa, ProcessPrimaryCandle)
	.Start();

	SubscribeCandles(TrendCandleType)
	.BindEx(_trendMacd, _trendMomentum, ProcessTrendCandle)
	.Start();

	SubscribeCandles(MonthlyCandleType)
	.BindEx(_monthlyMacd, ProcessMonthlyCandle)
	.Start();

	var priceStep = Security?.PriceStep;
	if (priceStep.HasValue && priceStep.Value > 0m)
	{
	Unit takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * priceStep.Value, UnitTypes.Point) : null;
	Unit stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * priceStep.Value, UnitTypes.Point) : null;

	if (takeProfit != null || stopLoss != null)
	{
	StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
	}
	}

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, primarySubscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawIndicator(area, _primaryMacd);
	DrawOwnTrades(area);
	}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
	return new MovingAverageConvergenceDivergenceSignal
	{
	Macd =
	{
	ShortMa = { Length = MacdFastPeriod },
	LongMa = { Length = MacdSlowPeriod }
	},
	SignalMa = { Length = MacdSignalPeriod }
	};
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal)
	return;

	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
	return;

	_primaryMacdValue = (macdLine, signalLine);

	_fastMaValue = fastValue.GetValue<decimal>();
	_slowMaValue = slowValue.GetValue<decimal>();

	EvaluateEntry(candle);
	}

	private void ProcessTrendCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue momentumValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal || !momentumValue.IsFinal)
	return;

	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
	return;

	_trendMacdValue = (macdLine, signalLine);

	var momentum = momentumValue.GetValue<decimal>();
	var deviation = Math.Abs(momentum - 100m);

	_momentumDeviations.Enqueue(deviation);
	while (_momentumDeviations.Count > 3)
	_momentumDeviations.Dequeue();

	EvaluateEntry(candle);
	}

	private void ProcessMonthlyCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal)
	return;

	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
	return;

	_monthlyMacdValue = (macdLine, signalLine);

	EvaluateEntry(candle);
	}

	private void EvaluateEntry(ICandleMessage candle)
	{
	if (State != StrategyState.Started)
	return;

	if (OrderVolume <= 0m)
	return;

	if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow)
	return;

	if (_primaryMacdValue is null || _trendMacdValue is null || _monthlyMacdValue is null)
	return;

	if (_momentumDeviations.Count == 0)
	return;

	if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_primaryMacd.IsFormed || !_trendMacd.IsFormed || !_monthlyMacd.IsFormed)
	return;

	if (Position != 0m)
	return;

	var (primaryMacd, primarySignal) = _primaryMacdValue.Value;
	var (trendMacd, trendSignal) = _trendMacdValue.Value;
	var (monthlyMacd, monthlySignal) = _monthlyMacdValue.Value;

	var allowBuy = fast < slow &&
	primaryMacd > primarySignal &&
	trendMacd > trendSignal &&
	monthlyMacd > monthlySignal &&
	HasMomentumSupport(MomentumBuyThreshold);

	var allowSell = primaryMacd < primarySignal &&
	trendMacd < trendSignal &&
	monthlyMacd < monthlySignal &&
	HasMomentumSupport(MomentumSellThreshold);

	if (!allowBuy && !allowSell)
	return;

	if (allowBuy)
	{
	BuyMarket(OrderVolume);
	}
	else if (allowSell)
	{
	SellMarket(OrderVolume);
	}
	}

	private bool HasMomentumSupport(decimal threshold)
	{
	foreach (var value in _momentumDeviations)
	{
	if (value >= threshold)
	return true;
	}

	return false;
	}
}

