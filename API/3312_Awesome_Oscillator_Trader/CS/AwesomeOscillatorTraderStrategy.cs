
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Awesome Oscillator Trader strategy converted from the MetaTrader AwesomeOscTrader expert advisor.
/// Implements the AO swing breakout combined with a Bollinger Bands width filter and Stochastic confirmation.
/// </summary>
public class AwesomeOscillatorTraderStrategy : Strategy
{
	private readonly StrategyParam<bool> _closeOnReversal;
	private readonly StrategyParam<ProfitCloseMode> _profitFilter;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerSigma;
	private readonly StrategyParam<decimal> _bollingerSpreadLower;
	private readonly StrategyParam<decimal> _bollingerSpreadUpper;
	private readonly StrategyParam<int> _aoFastPeriod;
	private readonly StrategyParam<int> _aoSlowPeriod;
	private readonly StrategyParam<decimal> _aoStrengthLimit;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _openHours;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _maxOpenOrders;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _aoNormalizationPeriod;
	private readonly StrategyParam<int> _requiredAoHistory;

	private BollingerBands _bollinger = null!;
	private StochasticOscillator _stochastic = null!;
	private AwesomeOscillator _awesome = null!;
	private AverageTrueRange _averageTrueRange = null!;
	private Highest _aoMaxAbs = null!;

	private decimal[] _recentUp = Array.Empty<decimal>();
	private decimal[] _recentDown = Array.Empty<decimal>();

	private decimal? _previousAo;
	private decimal? _previousStochK;
	private int _aoSamples;

	private decimal _pipSize;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// How to filter position exits based on profit when signals reverse.
	/// </summary>
	public ProfitCloseMode ProfitFilter
	{
		get => _profitFilter.Value;
		set => _profitFilter.Value = value;
	}

	/// <summary>
	/// Whether to close trades when a fresh opposite signal appears.
	/// </summary>
	public bool CloseOnReversal
	{
		get => _closeOnReversal.Value;
		set => _closeOnReversal.Value = value;
	}

	/// <summary>
	/// Base timeframe used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AoNormalizationPeriod
	{
		get => _aoNormalizationPeriod.Value;
		set => _aoNormalizationPeriod.Value = value;
	}

	public int RequiredAoHistory
	{
		get => _requiredAoHistory.Value;
		set => _requiredAoHistory.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AwesomeOscillatorTraderStrategy"/>.
	/// </summary>
	public AwesomeOscillatorTraderStrategy()
	{
		_closeOnReversal = Param(nameof(CloseOnReversal), true)
			.SetDisplay("Close On Reversal", "Close trades when the opposite AO setup appears", "Risk");

		_profitFilter = Param(nameof(ProfitFilter), ProfitCloseMode.OnlyProfitable)
			.SetDisplay("Profit Filter", "Filter which positions can be closed by signals", "Risk");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period used for the Bollinger Bands filter", "Filters");

		_bollingerSigma = Param(nameof(BollingerSigma), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Sigma", "Standard deviation multiplier of the Bollinger Bands", "Filters");

		_bollingerSpreadLower = Param(nameof(BollingerSpreadLower), 24m)
			.SetGreaterThanZero()
			.SetDisplay("Band Spread Min (pips)", "Minimum Bollinger band width in pips", "Filters");

		_bollingerSpreadUpper = Param(nameof(BollingerSpreadUpper), 230m)
			.SetGreaterThanZero()
			.SetDisplay("Band Spread Max (pips)", "Maximum Bollinger band width in pips", "Filters");

		_aoFastPeriod = Param(nameof(AoFastPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("AO Fast Period", "Fast period of the Awesome Oscillator", "Awesome Oscillator");

		_aoSlowPeriod = Param(nameof(AoSlowPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("AO Slow Period", "Slow period of the Awesome Oscillator", "Awesome Oscillator");

		_aoStrengthLimit = Param(nameof(AoStrengthLimit), 0m)
			.SetDisplay("AO Strength", "Minimum normalized AO magnitude to confirm entries", "Awesome Oscillator");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Number of bars for the Stochastic %K", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Number of bars for the Stochastic %D", "Stochastic");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 1)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Slowing", "Additional smoothing applied to %K", "Stochastic");

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 12m)
			.SetDisplay("Stoch Lower", "Lower Stochastic threshold for longs", "Stochastic");

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 21m)
			.SetDisplay("Stoch Upper", "Upper Stochastic threshold for shorts", "Stochastic");

		_entryHour = Param(nameof(EntryHour), 16)
			.SetDisplay("Entry Hour", "Hour when the trading window opens (0-23)", "Schedule");

		_openHours = Param(nameof(OpenHours), 13)
			.SetDisplay("Open Hours", "Duration of the trading window in hours", "Schedule");

		_riskPercent = Param(nameof(RiskPercent), 0.5m)
			.SetDisplay("Risk Percent", "Risk percentage used for position sizing", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 4.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier used to derive stop distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of ATR used for dynamic stops", "Risk");

		_maxOpenOrders = Param(nameof(MaxOpenOrders), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Open Trades", "Maximum number of simultaneous positions", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");

		_aoNormalizationPeriod = Param(nameof(AoNormalizationPeriod), 100)
			.SetGreaterOrEqual(1)
			.SetDisplay("AO Normalization Period", "Period for normalizing AO strength", "Awesome Oscillator");

		_requiredAoHistory = Param(nameof(RequiredAoHistory), 5)
			.SetGreaterOrEqual(5)
			.SetDisplay("AO History", "Number of past AO samples to keep", "Awesome Oscillator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_bollinger = null!;
	_stochastic = null!;
	_awesome = null!;
	_averageTrueRange = null!;
	_aoMaxAbs = null!;

	_recentUp = new decimal[RequiredAoHistory];
	_recentDown = new decimal[RequiredAoHistory];

	_previousAo = null;
	_previousStochK = null;
	_aoSamples = 0;

	_pipSize = 0m;
	ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_pipSize = CalculatePipSize();

	_bollinger = new BollingerBands
	{
	Length = _bollingerPeriod.Value,
	Width = _bollingerSigma.Value
	};

	_stochastic = new StochasticOscillator
	{
	KPeriod = _stochasticKPeriod.Value,
	DPeriod = _stochasticDPeriod.Value,
	Slowing = _stochasticSlowing.Value
	};

	_awesome = new AwesomeOscillator
	{
	ShortPeriod = _aoFastPeriod.Value,
	LongPeriod = _aoSlowPeriod.Value
	};

	_averageTrueRange = new AverageTrueRange
	{
	Length = _atrPeriod.Value
	};

	_aoMaxAbs = new Highest
	{
	Length = AoNormalizationPeriod
	};

	var subscription = SubscribeCandles(CandleType);

	subscription
	.BindEx(_bollinger, _stochastic, _awesome, _averageTrueRange, ProcessIndicators)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _bollinger);
	DrawIndicator(area, _stochastic);
	DrawIndicator(area, _awesome);
	}

	StartProtection();
	}

	private void ProcessIndicators(
	ICandleMessage candle,
	IIndicatorValue bollingerValue,
	IIndicatorValue stochasticValue,
	IIndicatorValue awesomeValue,
	IIndicatorValue atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (_pipSize <= 0m)
	return;

	var bollinger = (BollingerBandsValue)bollingerValue;
	if (bollinger.UpBand is not decimal upperBand ||
	bollinger.LowBand is not decimal lowerBand ||
	bollinger.MovingAverage is not decimal middleBand)
	{
	return;
	}

	if (!stochasticValue.IsFinal ||
	!awesomeValue.IsFinal ||
	!atrValue.IsFinal)
	{
	return;
	}

	if (stochasticValue is not StochasticOscillatorValue stoch)
	return;

	if (!awesomeValue.TryGetValue(out decimal aoRaw))
	return;

	var atr = atrValue.ToDecimal();

	var previousK = _previousStochK;
	var normalized = UpdateAwesomeHistory(candle, aoRaw);
	UpdateStochasticHistory(stoch);

	UpdateTrailingStop(candle);
	CheckStopTakeTargets(candle);

	if (Position == 0)
	ResetProtection();

	var spreadPips = (upperBand - lowerBand) / _pipSize;
	if (spreadPips < _bollingerSpreadLower.Value || spreadPips > _bollingerSpreadUpper.Value)
	return;

	if (!normalized.HasValue)
	return;

	if (previousK is null)
	return;

	var buySignal = CheckBuySignal(stoch.K, previousK, normalized.Value);
	var sellSignal = CheckSellSignal(stoch.K, previousK, normalized.Value);

	ManageExistingPosition(candle, buySignal, sellSignal);

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (Position != 0 || _maxOpenOrders.Value <= 0)
	return;

	if (buySignal && IsHourAllowed(candle.OpenTime))
	{
	EnterLong(candle, atr);
	}
	else if (sellSignal && IsHourAllowed(candle.OpenTime))
	{
	EnterShort(candle, atr);
	}
	}

	private void UpdateStochasticHistory(StochasticOscillatorValue stoch)
	{
	_previousStochK = stoch.K;
	}

	private decimal? UpdateAwesomeHistory(ICandleMessage candle, decimal aoRaw)
	{
	var absValue = Math.Abs(aoRaw);
	var maxValue = _aoMaxAbs.Process(new DecimalIndicatorValue(_aoMaxAbs, absValue, candle.OpenTime));
	if (!maxValue.IsFormed)
	{
	_previousAo = aoRaw;
	return null;
	}

	var divisor = maxValue.ToDecimal();
	if (divisor == 0m)
	{
	_previousAo = aoRaw;
	return null;
	}

	var normalized = aoRaw / divisor;

	var previous = _previousAo;
	var isUp = previous is null || aoRaw > previous.Value ? true : aoRaw < previous.Value ? false : normalized >= 0m;

	for (var i = RequiredAoHistory - 1; i > 0; i--)
	{
	_recentUp[i] = _recentUp[i - 1];
	_recentDown[i] = _recentDown[i - 1];
	}

	_recentUp[0] = isUp ? normalized : 0m;
	_recentDown[0] = isUp ? 0m : normalized;

	_previousAo = aoRaw;
	_aoSamples = Math.Min(_aoSamples + 1, RequiredAoHistory);

	return normalized;
	}

	private bool CheckBuySignal(decimal? stochCurrent, decimal? stochPrevious, decimal normalizedAo)
	{
	if (_aoSamples < RequiredAoHistory || stochCurrent is null || stochPrevious is null)
	return false;

	if (_recentDown[4] >= 0m || _recentDown[3] >= 0m || _recentDown[2] >= 0m)
	return false;

	if (!(_recentDown[1] < _recentDown[2]))
	return false;

	if (!(_recentUp[0] > _recentDown[1]))
	return false;

	if (!(_recentUp[0] < 0m))
	return false;

	if (stochCurrent.Value <= _stochasticLowerLevel.Value)
	return false;

	if (Math.Abs(_recentUp[0]) <= _aoStrengthLimit.Value)
	return false;

	return true;
	}

	private bool CheckSellSignal(decimal? stochCurrent, decimal? stochPrevious, decimal normalizedAo)
	{
	if (_aoSamples < RequiredAoHistory || stochCurrent is null || stochPrevious is null)
	return false;

	if (_recentUp[4] <= 0m || _recentUp[3] <= 0m || _recentUp[2] <= 0m)
	return false;

	if (!(_recentUp[1] > _recentUp[2]))
	return false;

	if (!(_recentDown[0] < _recentUp[1]))
	return false;

	if (!(_recentDown[0] > 0m))
	return false;

	if (stochCurrent.Value >= _stochasticUpperLevel.Value)
	return false;

	if (Math.Abs(_recentDown[0]) <= _aoStrengthLimit.Value)
	return false;

	return true;
	}

	private void ManageExistingPosition(ICandleMessage candle, bool buySignal, bool sellSignal)
	{
	if (Position == 0 || !CloseOnReversal)
	return;

	var profit = CalculateFloatingProfit(candle);
	if (Position > 0)
	{
	var shouldClose = (sellSignal || _recentDown[0] != 0m) && CanCloseWithProfit(profit);
	if (shouldClose)
	{
	SellMarket(Position);
	ResetProtection();
	}
	}
	else if (Position < 0)
	{
	var shouldClose = (buySignal || _recentUp[0] != 0m) && CanCloseWithProfit(profit);
	if (shouldClose)
	{
	BuyMarket(-Position);
	ResetProtection();
	}
	}
	}

	private void EnterLong(ICandleMessage candle, decimal atr)
	{
	var stopPips = CalculateDynamicStopPips(atr);
	if (stopPips <= 0m)
	return;

	var volume = GetOrderVolume(stopPips);
	if (volume <= 0m)
	return;

	BuyMarket(volume);

	var entryPrice = candle.ClosePrice;
	var distance = stopPips * _pipSize;
	_longStopPrice = entryPrice - distance;
	_longTakePrice = entryPrice + distance;
	}

	private void EnterShort(ICandleMessage candle, decimal atr)
	{
	var stopPips = CalculateDynamicStopPips(atr);
	if (stopPips <= 0m)
	return;

	var volume = GetOrderVolume(stopPips);
	if (volume <= 0m)
	return;

	SellMarket(volume);

	var entryPrice = candle.ClosePrice;
	var distance = stopPips * _pipSize;
	_shortStopPrice = entryPrice + distance;
	_shortTakePrice = entryPrice - distance;
	}

	private decimal CalculateDynamicStopPips(decimal atr)
	{
	var distance = atr * _atrMultiplier.Value;
	var stopPips = distance / _pipSize;
	if (stopPips <= 0m)
	return 0m;

	return Math.Max(1m, Math.Floor(stopPips));
	}

	private decimal GetOrderVolume(decimal stopPips)
	{
	var baseVolume = Volume > 0 ? Volume : 1m;

	if (Security?.StepVolume is null || Security.MinVolume is null || Security.MaxVolume is null || Security.PriceStep is null || Security.StepPrice is null || Portfolio is null)
	return baseVolume;

	var stepVolume = Security.StepVolume.Value;
	var minVolume = Security.MinVolume.Value;
	var maxVolume = Security.MaxVolume.Value;
	var priceStep = Security.PriceStep.Value;
	var stepPrice = Security.StepPrice.Value;

	if (priceStep <= 0m || stepPrice <= 0m)
	return baseVolume;

	var pipValue = stepPrice * (_pipSize / priceStep);
	if (pipValue <= 0m)
	return baseVolume;

	var riskMoney = Portfolio.CurrentValue * (_riskPercent.Value / 100m);
	if (riskMoney <= 0m)
	return baseVolume;

	var volume = riskMoney / (stopPips * pipValue);
	if (volume <= 0m)
	return baseVolume;

	var steps = Math.Max(1m, Math.Round(volume / stepVolume, MidpointRounding.ToZero));
	var normalized = steps * stepVolume;
	normalized = Math.Clamp(normalized, minVolume, maxVolume);

	return normalized;
	}

	private bool IsHourAllowed(DateTimeOffset time)
	{
	var hour = time.Hour;
	var start = _entryHour.Value;
	var duration = _openHours.Value;
	var closeHour = (start + duration) % 24;
	var wraps = start + duration > 23;

	if (wraps)
	return !(hour < start && hour > closeHour);

	return !(hour < start || hour > closeHour);
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
	if (_trailingStopPips.Value <= 0m || Position == 0)
	return;

	if (PositionPrice is not decimal entryPrice)
	return;

	var distance = _trailingStopPips.Value * _pipSize;

	if (Position > 0)
	{
	if (candle.ClosePrice - entryPrice <= distance)
	return;

	var candidate = candle.ClosePrice - distance;
	if (_longStopPrice is null || candidate > _longStopPrice.Value)
	_longStopPrice = candidate;
	}
	else
	{
	if (entryPrice - candle.ClosePrice <= distance)
	return;

	var candidate = candle.ClosePrice + distance;
	if (_shortStopPrice is null || candidate < _shortStopPrice.Value)
	_shortStopPrice = candidate;
	}
	}

	private void CheckStopTakeTargets(ICandleMessage candle)
	{
	if (Position > 0)
	{
	if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
	{
	SellMarket(Position);
	ResetProtection();
	return;
	}

	if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
	{
	SellMarket(Position);
	ResetProtection();
	}
	}
	else if (Position < 0)
	{
	if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
	{
	BuyMarket(-Position);
	ResetProtection();
	return;
	}

	if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
	{
	BuyMarket(-Position);
	ResetProtection();
	}
	}
	}

	private void ResetProtection()
	{
	_longStopPrice = null;
	_longTakePrice = null;
	_shortStopPrice = null;
	_shortTakePrice = null;
	}

	private decimal CalculateFloatingProfit(ICandleMessage candle)
	{
	if (Position == 0 || PositionPrice is null)
	return 0m;

	var entry = PositionPrice.Value;
	var move = candle.ClosePrice - entry;
	return move * Position;
	}

	private bool CanCloseWithProfit(decimal profit)
	{
	return ProfitFilter switch
	{
	ProfitCloseMode.Any => true,
	ProfitCloseMode.OnlyProfitable => profit >= 0m,
	ProfitCloseMode.OnlyLosing => profit <= 0m,
	_ => true,
	};
	}

	private decimal CalculatePipSize()
	{
	if (Security?.PriceStep is not decimal step)
	return 0m;

	var digits = Security.Decimals;
	var adjust = digits is 3 or 5 ? 10m : 1m;
	return step * adjust;
	}

	/// <summary>
	/// Profit filter options used when closing trades on reversals.
	/// </summary>
	public enum ProfitCloseMode
	{
	/// <summary>
	/// Close trades regardless of their current profit.
	/// </summary>
	Any = 0,

	/// <summary>
	/// Close only profitable trades.
	/// </summary>
	OnlyProfitable = 1,

	/// <summary>
	/// Close only losing trades.
	/// </summary>
	OnlyLosing = 2
	}
}
