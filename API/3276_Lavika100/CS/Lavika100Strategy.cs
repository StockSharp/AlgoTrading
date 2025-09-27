using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RAVI based multi-timeframe strategy translated from the "Lavika100" MetaTrader expert advisor.
/// </summary>
public class Lavika100Strategy : Strategy
{
	private readonly StrategyParam<DataType> _h1CandleType;
	private readonly StrategyParam<DataType> _h4CandleType;
	private readonly StrategyParam<int> _h1FastPeriod;
	private readonly StrategyParam<int> _h1SlowPeriod;
	private readonly StrategyParam<int> _h4FastPeriod;
	private readonly StrategyParam<int> _h4SlowPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;

	private SimpleMovingAverage _h1FastMa = null!;
	private SimpleMovingAverage _h1SlowMa = null!;
	private SimpleMovingAverage _h4FastMa = null!;
	private SimpleMovingAverage _h4SlowMa = null!;

	private decimal? _h4Ravi0;
	private decimal? _h4Ravi1;
	private decimal? _h4Ravi2;
	private decimal? _h4Ravi3;

	private decimal _pipSize;
	private bool _protectionConfigured;

	/// <summary>
	/// Available money management modes.
	/// </summary>
	public enum MoneyManagementMode
	{
		/// <summary>
		/// Use the configured fixed volume for every trade.
		/// </summary>
		FixedLot,

		/// <summary>
		/// Scale the volume so that the configured risk percent is lost when the stop-loss is hit.
		/// </summary>
		RiskPercent
	}

/// <summary>
/// Initializes a new instance of <see cref="Lavika100Strategy"/>.
/// </summary>
public Lavika100Strategy()
{
	_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("H1 Candle Type", "Intraday timeframe used for the RAVI trigger.", "General");

	_h4CandleType = Param(nameof(H4CandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("H4 Candle Type", "Higher timeframe feeding the trend filter.", "General");

	_h1FastPeriod = Param(nameof(H1FastPeriod), 2)
	.SetRange(1, 200)
	.SetDisplay("H1 Fast Period", "Fast moving average length for the H1 RAVI calculation.", "Indicators");

	_h1SlowPeriod = Param(nameof(H1SlowPeriod), 8)
	.SetRange(1, 400)
	.SetDisplay("H1 Slow Period", "Slow moving average length for the H1 RAVI calculation.", "Indicators");

	_h4FastPeriod = Param(nameof(H4FastPeriod), 1)
	.SetRange(1, 200)
	.SetDisplay("H4 Fast Period", "Fast moving average length for the H4 RAVI calculation.", "Indicators");

	_h4SlowPeriod = Param(nameof(H4SlowPeriod), 8)
	.SetRange(1, 400)
	.SetDisplay("H4 Slow Period", "Slow moving average length for the H4 RAVI calculation.", "Indicators");

	_stopLossPoints = Param(nameof(StopLossPoints), 500m)
	.SetRange(0m, 10000m)
	.SetDisplay("Stop Loss (pips)", "Distance between entry and stop in points (pip like units).", "Risk");

	_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
	.SetRange(0m, 10000m)
	.SetDisplay("Take Profit (pips)", "Target distance expressed in the same pip units.", "Risk");

	_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
	.SetRange(0m, 10000m)
	.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after a position becomes profitable.", "Risk");

	_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
	.SetRange(0m, 10000m)
	.SetDisplay("Trailing Step (pips)", "Minimum increment for trailing stop updates.", "Risk");

	_fixedVolume = Param(nameof(FixedVolume), 0.1m)
	.SetRange(0.0001m, 1000m)
	.SetDisplay("Fixed Volume", "Lot size used when money management mode is FixedLot.", "Trading");

	_riskPercent = Param(nameof(RiskPercent), 3m)
	.SetRange(0m, 100m)
	.SetDisplay("Risk Percent", "Percent of portfolio value to risk per trade when RiskPercent mode is selected.", "Trading");

	_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.RiskPercent)
	.SetDisplay("Money Mode", "Choose between fixed lots or percent risk sizing.", "Trading");

	_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
	.SetDisplay("Only One Position", "Allow only a single open position at any time.", "Behaviour");

	_reverseSignals = Param(nameof(ReverseSignals), true)
	.SetDisplay("Reverse Signals", "Flip long and short actions, mirroring the original EA setting.", "Behaviour");

	_closeOpposite = Param(nameof(CloseOpposite), true)
	.SetDisplay("Close Opposite", "Close an opposite position before opening a new trade.", "Behaviour");
}

/// <summary>
/// Candle type used for the main (H1) RAVI calculation.
/// </summary>
public DataType H1CandleType
{
	get => _h1CandleType.Value;
	set => _h1CandleType.Value = value;
}

/// <summary>
/// Candle type used for the higher timeframe trend filter (H4).
/// </summary>
public DataType H4CandleType
{
	get => _h4CandleType.Value;
	set => _h4CandleType.Value = value;
}

/// <summary>
/// Fast moving average length for the H1 calculation.
/// </summary>
public int H1FastPeriod
{
	get => _h1FastPeriod.Value;
	set => _h1FastPeriod.Value = value;
}

/// <summary>
/// Slow moving average length for the H1 calculation.
/// </summary>
public int H1SlowPeriod
{
	get => _h1SlowPeriod.Value;
	set => _h1SlowPeriod.Value = value;
}

/// <summary>
/// Fast moving average length for the H4 calculation.
/// </summary>
public int H4FastPeriod
{
	get => _h4FastPeriod.Value;
	set => _h4FastPeriod.Value = value;
}

/// <summary>
/// Slow moving average length for the H4 calculation.
/// </summary>
public int H4SlowPeriod
{
	get => _h4SlowPeriod.Value;
	set => _h4SlowPeriod.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pip based points.
/// </summary>
public decimal StopLossPoints
{
	get => _stopLossPoints.Value;
	set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pip based points.
/// </summary>
public decimal TakeProfitPoints
{
	get => _takeProfitPoints.Value;
	set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Trailing stop distance expressed in pip based points.
/// </summary>
public decimal TrailingStopPoints
{
	get => _trailingStopPoints.Value;
	set => _trailingStopPoints.Value = value;
}

/// <summary>
/// Trailing stop minimum step expressed in pip based points.
/// </summary>
public decimal TrailingStepPoints
{
	get => _trailingStepPoints.Value;
	set => _trailingStepPoints.Value = value;
}

/// <summary>
/// Fixed lot size used when <see cref="MoneyMode"/> is <see cref="MoneyManagementMode.FixedLot"/>.
/// </summary>
public decimal FixedVolume
{
	get => _fixedVolume.Value;
	set => _fixedVolume.Value = value;
}

/// <summary>
/// Risk percentage applied when <see cref="MoneyMode"/> is <see cref="MoneyManagementMode.RiskPercent"/>.
/// </summary>
public decimal RiskPercent
{
	get => _riskPercent.Value;
	set => _riskPercent.Value = value;
}

/// <summary>
/// Selects the position sizing approach.
/// </summary>
public MoneyManagementMode MoneyMode
{
	get => _moneyMode.Value;
	set => _moneyMode.Value = value;
}

/// <summary>
/// Determines whether only a single position can stay open.
/// </summary>
public bool OnlyOnePosition
{
	get => _onlyOnePosition.Value;
	set => _onlyOnePosition.Value = value;
}

/// <summary>
/// Flips the buy/sell actions when enabled.
/// </summary>
public bool ReverseSignals
{
	get => _reverseSignals.Value;
	set => _reverseSignals.Value = value;
}

/// <summary>
/// When true an opposite position is closed before opening a new one.
/// </summary>
public bool CloseOpposite
{
	get => _closeOpposite.Value;
	set => _closeOpposite.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, H1CandleType), (Security, H4CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_h4Ravi0 = null;
	_h4Ravi1 = null;
	_h4Ravi2 = null;
	_h4Ravi3 = null;
	_protectionConfigured = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	if (TrailingStopPoints > 0m && TrailingStepPoints <= 0m)
	throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

	_h1FastMa = new SimpleMovingAverage { Length = H1FastPeriod };
	_h1SlowMa = new SimpleMovingAverage { Length = H1SlowPeriod };
	_h4FastMa = new SimpleMovingAverage { Length = H4FastPeriod };
	_h4SlowMa = new SimpleMovingAverage { Length = H4SlowPeriod };

	_pipSize = CalculatePipSize();

	var h1Subscription = SubscribeCandles(H1CandleType);
	h1Subscription
	.Bind(ProcessH1Candle)
	.Start();

	var h4Subscription = SubscribeCandles(H4CandleType);
	h4Subscription
	.Bind(ProcessH4Candle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, h1Subscription);
		DrawIndicator(area, _h1FastMa);
		DrawIndicator(area, _h1SlowMa);
		DrawOwnTrades(area);
	}

ConfigureProtection();
}

private void ProcessH4Candle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	var fastValue = _h4FastMa.Process(candle.OpenPrice, candle.OpenTime, true);
	var slowValue = _h4SlowMa.Process(candle.OpenPrice, candle.OpenTime, true);

	if (!_h4FastMa.IsFormed || !_h4SlowMa.IsFormed)
	return;

	var slow = slowValue.ToDecimal();
	if (slow == 0m)
	return;

	var fast = fastValue.ToDecimal();
	var ravi = (fast - slow) / slow * 100m;

	// Maintain the last four RAVI values.
	_h4Ravi3 = _h4Ravi2;
	_h4Ravi2 = _h4Ravi1;
	_h4Ravi1 = _h4Ravi0;
	_h4Ravi0 = ravi;
}

private void ProcessH1Candle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	var fastValue = _h1FastMa.Process(candle.OpenPrice, candle.OpenTime, true);
	var slowValue = _h1SlowMa.Process(candle.OpenPrice, candle.OpenTime, true);

	if (!_h1FastMa.IsFormed || !_h1SlowMa.IsFormed)
	return;

	if (_h4Ravi0 is null || _h4Ravi1 is null || _h4Ravi2 is null || _h4Ravi3 is null)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var slow = slowValue.ToDecimal();
	if (slow == 0m)
	return;

	var fast = fastValue.ToDecimal();
	var h1Ravi = (fast - slow) / slow * 100m;

	var buySignal = false;
	var sellSignal = false;

	if (h1Ravi > 0m)
	{
		if (_h4Ravi0 > _h4Ravi1 && _h4Ravi1 < _h4Ravi2 && _h4Ravi2 < _h4Ravi3)
		buySignal = true;

		if (_h4Ravi0 < _h4Ravi1 && _h4Ravi1 > _h4Ravi2 && _h4Ravi2 > _h4Ravi3)
		sellSignal = true;
	}

if (buySignal)
{
	if (!ReverseSignals)
	TryEnterLong(candle);
	else
	TryEnterShort(candle);
}

if (sellSignal)
{
	if (!ReverseSignals)
	TryEnterShort(candle);
	else
	TryEnterLong(candle);
}
}

private void TryEnterLong(ICandleMessage candle)
{
	if (CloseOpposite && Position < 0m)
	ClosePosition();

	if (OnlyOnePosition && Position != 0m)
	return;

	var volume = CalculateVolume();
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	LogInfo($"Long entry at {candle.ClosePrice} with volume {volume}.");
}

private void TryEnterShort(ICandleMessage candle)
{
	if (CloseOpposite && Position > 0m)
	ClosePosition();

	if (OnlyOnePosition && Position != 0m)
	return;

	var volume = CalculateVolume();
	if (volume <= 0m)
	return;

	SellMarket(volume);
	LogInfo($"Short entry at {candle.ClosePrice} with volume {volume}.");
}

private decimal CalculateVolume()
{
	var volume = FixedVolume;

	if (MoneyMode == MoneyManagementMode.RiskPercent)
	{
		if (Portfolio is null)
		return 0m;

		var portfolioValue = Portfolio.CurrentValue;
		if (portfolioValue <= 0m)
		return 0m;

		var stopDistance = StopLossPoints * _pipSize;
		if (stopDistance <= 0m)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var riskAmount = portfolioValue * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return 0m;

		var valuePerUnit = stepPrice / priceStep;
		if (valuePerUnit <= 0m)
		return 0m;

		volume = riskAmount / (stopDistance * valuePerUnit);
	}

return AlignVolume(volume);
}

private decimal AlignVolume(decimal volume)
{
	var step = Security?.VolumeStep ?? 0m;
	var min = Security?.VolumeMin ?? 0m;
	var max = Security?.VolumeMax ?? 0m;

	if (step > 0m)
	volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

	if (min > 0m && volume < min)
	volume = min;

	if (max > 0m && volume > max)
	volume = max;

	return volume;
}

private void ConfigureProtection()
{
	if (_protectionConfigured)
	return;

	var hasStop = StopLossPoints > 0m;
	var hasTake = TakeProfitPoints > 0m;
	var hasTrailing = TrailingStopPoints > 0m;

	if (!hasStop && !hasTake && !hasTrailing)
	return;

	var stopLoss = hasStop ? new Unit(StopLossPoints * _pipSize, UnitTypes.Price) : null;
	var takeProfit = hasTake ? new Unit(TakeProfitPoints * _pipSize, UnitTypes.Price) : null;
	Unit trailingStop = null;
	Unit trailingStep = null;

	if (hasTrailing)
	{
		trailingStop = new Unit(TrailingStopPoints * _pipSize, UnitTypes.Price);
		if (TrailingStepPoints > 0m)
		trailingStep = new Unit(TrailingStepPoints * _pipSize, UnitTypes.Price);
	}

		StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, isTrailingStop: hasTrailing, trailingStop: trailingStop, trailingStopStep: trailingStep);
		_protectionConfigured = true;
}

private decimal CalculatePipSize()
{
	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	return 1m;

	var decimals = Security?.Decimals;
	if (decimals == 3 || decimals == 5)
	return step * 10m;

	return step;
}
}
