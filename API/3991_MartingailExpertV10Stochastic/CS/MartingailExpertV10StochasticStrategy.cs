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
/// Conversion of the "MartingailExpert v1.0 Stochastic" MetaTrader expert advisor.
/// Implements stochastic based entries with martingale averaging and cluster take profits.
/// </summary>
public class MartingailExpertV10StochasticStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<int> _stepMode;
	private readonly StrategyParam<decimal> _profitFactorPoints;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;

	private decimal _pointSize;
	private decimal? _prevK;
	private decimal? _prevD;

	private decimal _buyLastPrice;
	private decimal _buyLastVolume;
	private decimal _buyTotalVolume;
	private decimal _buyWeightedSum;
	private int _buyOrderCount;
	private decimal _buyTakeProfit;

	private decimal _sellLastPrice;
	private decimal _sellLastVolume;
	private decimal _sellTotalVolume;
	private decimal _sellWeightedSum;
	private int _sellOrderCount;
	private decimal _sellTakeProfit;

	/// <summary>
	/// Distance in points that price has to travel against the latest entry before adding.
	/// </summary>
	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Step mode from the original EA: 0 - fixed, 1 - fixed plus two extra points per filled order.
	/// </summary>
	public int StepMode
	{
		get => _stepMode.Value;
		set => _stepMode.Value = value;
	}

	/// <summary>
	/// Profit target in points applied to every open order.
	/// </summary>
	public decimal ProfitFactorPoints
	{
		get => _profitFactorPoints.Value;
		set => _profitFactorPoints.Value = value;
	}

	/// <summary>
	/// Martingale multiplier for the next averaging order.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Initial long position volume.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Initial short position volume.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Stochastic %K lookback period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing length.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing length applied to %K line.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Minimum stochastic level that confirms long setups.
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Maximum stochastic level that confirms short setups.
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MartingailExpertV10StochasticStrategy"/>.
	/// </summary>
	public MartingailExpertV10StochasticStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 25m)
		.SetGreaterThanZero()
		.SetDisplay("Step", "Price step in points before averaging", "Martingale");

		_stepMode = Param(nameof(StepMode), 0)
		.SetDisplay("Step Mode", "0 - fixed step, 1 - step plus two points per order", "Martingale");

		_profitFactorPoints = Param(nameof(ProfitFactorPoints), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Factor", "Points multiplied by order count for take profit", "Martingale");

		_multiplier = Param(nameof(Multiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Multiplier", "Martingale multiplier for averaging", "Martingale");

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Volume", "Initial buy order volume", "Trading");

		_sellVolume = Param(nameof(SellVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Volume", "Initial sell order volume", "Trading");

		_kPeriod = Param(nameof(KPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Stochastic %K lookback", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Stochastic %D smoothing", "Indicators");

		_slowing = Param(nameof(Slowing), 20)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Additional smoothing for %K", "Indicators");

		_zoneBuy = Param(nameof(ZoneBuy), 50m)
		.SetDisplay("Zone Buy", "%D lower bound to allow buys", "Indicators");

		_zoneSell = Param(nameof(ZoneSell), 50m)
		.SetDisplay("Zone Sell", "%D upper bound to allow sells", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for processing", "General");
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

	_pointSize = 0m;
	_prevK = null;
	_prevD = null;

	ResetLongState();
	ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_pointSize = CalculatePointSize();

	_stochastic = new StochasticOscillator
	{
	Length = KPeriod,
	K = { Length = Slowing },
	D = { Length = DPeriod }
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_stochastic, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _stochastic);
	DrawOwnTrades(area);
	}

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (stochasticValue is not StochasticOscillatorValue stoch)
	return;

	if (stoch.K is not decimal currentK || stoch.D is not decimal currentD)
	return;

	if (!_stochastic.IsFormed)
	{
	_prevK = currentK;
	_prevD = currentD;
	return;
	}

	var tradingAllowed = IsFormedAndOnlineAndAllowTrading();

	ManageClusters(candle, tradingAllowed);

	if (!tradingAllowed)
	{
	_prevK = currentK;
	_prevD = currentD;
	return;
	}

	if (Position == 0m && _prevK is decimal prevK && _prevD is decimal prevD)
	{
	if (prevK > prevD && prevD > ZoneBuy)
	{
	OpenLong(candle.ClosePrice);
	}
	else if (prevK < prevD && prevD < ZoneSell)
	{
	OpenShort(candle.ClosePrice);
	}
	}

	_prevK = currentK;
	_prevD = currentD;
	}

	private void ManageClusters(ICandleMessage candle, bool tradingAllowed)
	{
	if (Position > 0m && _buyOrderCount > 0)
	{
	HandleLongCluster(candle, tradingAllowed);
	}
	else if (Position < 0m && _sellOrderCount > 0)
	{
	HandleShortCluster(candle, tradingAllowed);
	}
	else if (Position == 0m)
	{
	if (_buyOrderCount > 0 || _sellOrderCount > 0)
	{
	ResetLongState();
	ResetShortState();
	}
	}
	}

	private void HandleLongCluster(ICandleMessage candle, bool tradingAllowed)
	{
	if (!tradingAllowed || _pointSize <= 0m)
	return;

	var currentCount = Math.Max(1, _buyOrderCount);
	var stepPoints = StepMode == 0
	? StepPoints
	: StepPoints + Math.Max(0m, currentCount * 2m - 2m);
	var addTrigger = _buyLastPrice - stepPoints * _pointSize;

	// Try to add another long order when the bar trades below the trigger.
	if (_buyLastVolume > 0m && candle.LowPrice <= addTrigger)
	{
	var desiredVolume = _buyLastVolume * Multiplier;
	var nextVolume = PrepareNextVolume(desiredVolume, _buyTotalVolume);
	if (nextVolume > 0m)
	{
	var executionPrice = Math.Min(addTrigger, candle.LowPrice);
	BuyMarket(nextVolume);

	_buyLastVolume = nextVolume;
	_buyLastPrice = executionPrice;
	_buyTotalVolume += nextVolume;
	_buyWeightedSum += executionPrice * nextVolume;
	_buyOrderCount++;
	_buyTakeProfit = _buyLastPrice + ProfitFactorPoints * _pointSize * _buyOrderCount;
	}
	}

// Exit the entire long cluster once the shared take profit is reached.
if (_buyTakeProfit > 0m && candle.HighPrice >= _buyTakeProfit)
{
var estimatedPnL = (_buyTakeProfit - GetAveragePrice(true)) * _buyTotalVolume;
if (estimatedPnL > 0m)
{
SellMarket(Math.Abs(Position));
ResetLongState();
}
}
	}

	private void HandleShortCluster(ICandleMessage candle, bool tradingAllowed)
	{
	if (!tradingAllowed || _pointSize <= 0m)
	return;

	var currentCount = Math.Max(1, _sellOrderCount);
	var stepPoints = StepMode == 0
	? StepPoints
	: StepPoints + Math.Max(0m, currentCount * 2m - 2m);
	var addTrigger = _sellLastPrice + stepPoints * _pointSize;

	// Try to add another short order when the bar trades above the trigger.
	if (_sellLastVolume > 0m && candle.HighPrice >= addTrigger)
	{
	var desiredVolume = _sellLastVolume * Multiplier;
	var nextVolume = PrepareNextVolume(desiredVolume, _sellTotalVolume);
	if (nextVolume > 0m)
	{
	var executionPrice = Math.Max(addTrigger, candle.HighPrice);
	SellMarket(nextVolume);

	_sellLastVolume = nextVolume;
	_sellLastPrice = executionPrice;
	_sellTotalVolume += nextVolume;
	_sellWeightedSum += executionPrice * nextVolume;
	_sellOrderCount++;
	_sellTakeProfit = _sellLastPrice - ProfitFactorPoints * _pointSize * _sellOrderCount;
	}
	}

	// Exit the entire short cluster once the shared take profit is reached.
	if (_sellTakeProfit > 0m && candle.LowPrice <= _sellTakeProfit)
	{
	var estimatedPnL = (GetAveragePrice(false) - _sellTakeProfit) * _sellTotalVolume;
	if (estimatedPnL > 0m)
	{
	BuyMarket(Math.Abs(Position));
	ResetShortState();
	}
	}
	}

	private void OpenLong(decimal price)
	{
	var volume = PrepareNextVolume(BuyVolume, 0m);
	if (volume <= 0m)
	return;

	BuyMarket(volume);

	_buyLastPrice = price;
	_buyLastVolume = volume;
	_buyTotalVolume = volume;
	_buyWeightedSum = price * volume;
	_buyOrderCount = 1;
	_buyTakeProfit = price + ProfitFactorPoints * _pointSize;

	ResetShortState();
	}

	private void OpenShort(decimal price)
	{
	var volume = PrepareNextVolume(SellVolume, 0m);
	if (volume <= 0m)
	return;

	SellMarket(volume);

	_sellLastPrice = price;
	_sellLastVolume = volume;
	_sellTotalVolume = volume;
	_sellWeightedSum = price * volume;
	_sellOrderCount = 1;
	_sellTakeProfit = price - ProfitFactorPoints * _pointSize;

	ResetLongState();
	}

	private void ResetLongState()
	{
	_buyLastPrice = 0m;
	_buyLastVolume = 0m;
	_buyTotalVolume = 0m;
	_buyWeightedSum = 0m;
	_buyOrderCount = 0;
	_buyTakeProfit = 0m;
	}

	private void ResetShortState()
	{
	_sellLastPrice = 0m;
	_sellLastVolume = 0m;
	_sellTotalVolume = 0m;
	_sellWeightedSum = 0m;
	_sellOrderCount = 0;
	_sellTakeProfit = 0m;
	}

	private decimal PrepareNextVolume(decimal requestedVolume, decimal currentTotal)
	{
	if (requestedVolume <= 0m)
	return 0m;

	var normalized = NormalizeVolume(requestedVolume);
	if (normalized <= 0m)
	return 0m;

	var maxVolume = GetMaxVolumeLimit();
	if (maxVolume < decimal.MaxValue)
	{
	var remaining = maxVolume - currentTotal;
	if (remaining <= 0m)
	return 0m;

	if (normalized > remaining)
	normalized = NormalizeVolume(remaining);
	}

	return normalized;
	}

	private decimal NormalizeVolume(decimal volume)
	{
	var security = Security;
	if (security == null)
	return volume;

	var step = security.VolumeStep ?? 0m;
	if (step > 0m)
	volume = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);

	var min = security.VolumeMin ?? 0m;
	if (min > 0m && volume < min)
	return 0m;

	var max = security.VolumeMax ?? decimal.MaxValue;
	if (volume > max)
	volume = max;

	return volume;
	}

	private decimal GetMaxVolumeLimit()
	{
	var security = Security;
	if (security?.VolumeMax is decimal max && max > 0m)
	return max;

	return decimal.MaxValue;
	}

	private decimal GetAveragePrice(bool isLong)
	{
	var total = isLong ? _buyTotalVolume : _sellTotalVolume;
	if (total <= 0m)
	return 0m;

	var sum = isLong ? _buyWeightedSum : _sellWeightedSum;
	return sum / total;
	}

	private decimal CalculatePointSize()
	{
	var security = Security;
	if (security == null)
	return 0m;

	var step = security.PriceStep ?? 0m;
	if (step <= 0m)
	return 0m;

	if (step == 0.00001m || step == 0.001m)
	return step * 10m;

	return step;
	}
}

