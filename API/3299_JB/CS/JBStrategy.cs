using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JB strategy converted from MQL4 implementation.
/// Combines Bollinger Bands, 100-period SMA, and Force Index signals.
/// Uses a martingale-style volume multiplier after losing sequences
/// and closes positions when the average unrealized profit per contract reaches a target.
/// </summary>
public class JBStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _forcePeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lossMultiplier;
	private readonly StrategyParam<decimal> _averageProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentVolume;
	private decimal _lastRealizedPnL;
	private decimal? _previousClose;

	/// <summary>
	/// Initializes a new instance of <see cref="JBStrategy"/>.
	/// </summary>
	public JBStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Length of the smoothing moving average", "Trend Filters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_forcePeriod = Param(nameof(ForcePeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Force Index Period", "Length of the Force Index indicator", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(13, 150, 13);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of candles used by Bollinger Bands", "Volatility")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Volatility")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial order volume before multipliers", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);

		_lossMultiplier = Param(nameof(LossMultiplier), 1.55m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Multiplier", "Multiplier applied to volume after a losing cycle", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2.0m, 0.1m);

		_averageProfitTarget = Param(nameof(AverageProfitTarget), 2.8m)
			.SetDisplay("Average Profit Target", "Average unrealized profit per contract required to close all positions", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for signal calculation", "General");
	}

	/// <summary>
	/// Period of the simple moving average filter.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Period length of the Force Index indicator.
	/// </summary>
	public int ForcePeriod
	{
		get => _forcePeriod.Value;
		set => _forcePeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Base order volume before martingale adjustments.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to order volume after a losing cycle.
	/// </summary>
	public decimal LossMultiplier
	{
		get => _lossMultiplier.Value;
		set => _lossMultiplier.Value = value;
	}

	/// <summary>
	/// Average profit per contract required to close open positions.
	/// </summary>
	public decimal AverageProfitTarget
	{
		get => _averageProfitTarget.Value;
		set => _averageProfitTarget.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	_currentVolume = 0m;
	_lastRealizedPnL = 0m;
	_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_currentVolume = BaseVolume;
	_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

	var sma = new SimpleMovingAverage
	{
	Length = SmaPeriod
	};

	var forceIndex = new ForceIndex
	{
	Length = ForcePeriod
	};

	var bollinger = new BollingerBands
	{
	Length = BollingerPeriod,
	Width = BollingerDeviation
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(sma, forceIndex, bollinger, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal forceValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var closePrice = candle.ClosePrice;

	if (_previousClose is null)
	{
	_previousClose = closePrice;
	return;
	}

	TryCloseByAverageProfit();

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousClose = closePrice;
	return;
	}

	var previousClose = _previousClose.Value;

	var buySignal = previousClose <= lowerBand
	&& smaValue < previousClose
	&& forceValue > 0m;

	var sellSignal = previousClose >= upperBand
	&& smaValue > previousClose
	&& forceValue < 0m;

	if (buySignal)
	{
	PlaceOrder(Sides.Buy, lowerBand, upperBand, smaValue, forceValue);
	}
	else if (sellSignal)
	{
	PlaceOrder(Sides.Sell, lowerBand, upperBand, smaValue, forceValue);
	}

	_previousClose = closePrice;
	}

	private void PlaceOrder(Sides direction, decimal lowerBand, decimal upperBand, decimal smaValue, decimal forceValue)
	{
	var volume = GetOrderVolume();
	if (volume <= 0m)
	return;

	if (direction == Sides.Buy)
	{
	LogInfo($"Buy signal. PrevClose <= LowerBand ({_previousClose} <= {lowerBand}), SMA={smaValue}, Force={forceValue}, Volume={volume}");
	BuyMarket(volume: volume);
	}
	else
	{
	LogInfo($"Sell signal. PrevClose >= UpperBand ({_previousClose} >= {upperBand}), SMA={smaValue}, Force={forceValue}, Volume={volume}");
	SellMarket(volume: volume);
	}
	}

	private decimal GetOrderVolume()
	{
	var volume = _currentVolume;
	if (volume <= 0m)
	return 0m;

	var security = Security;
	if (security?.VolumeStep is decimal step && step > 0m)
	{
	volume = Math.Max(step, Math.Round(volume / step, MidpointRounding.AwayFromZero) * step);
	}

	var minVolume = security?.VolumeMin;
	if (minVolume is decimal min && volume < min)
	volume = min;

	var maxVolume = security?.VolumeMax;
	if (maxVolume is decimal max && max > 0m && volume > max)
	volume = max;

	return volume;
	}

	private void TryCloseByAverageProfit()
	{
	if (Position == 0m)
	return;

	var totalVolume = Math.Abs(Position);
	if (totalVolume <= 0m)
	return;

	var unrealized = PnLManager?.UnrealizedPnL ?? 0m;
	var averageProfit = unrealized / totalVolume;

	if (averageProfit >= AverageProfitTarget)
	{
	LogInfo($"Average profit target reached. Unrealized={unrealized}, Volume={totalVolume}, Average={averageProfit}. Closing position.");
	ClosePosition();
	}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
	base.OnPositionChanged(delta);

	if (Position != 0m)
	return;

	var realized = PnLManager?.RealizedPnL ?? _lastRealizedPnL;
	var cyclePnL = realized - _lastRealizedPnL;

	if (cyclePnL < 0m)
	{
	_currentVolume = Math.Max(BaseVolume, _currentVolume * LossMultiplier);
	LogInfo($"Loss detected. CyclePnL={cyclePnL}. Increasing volume to {_currentVolume}.");
	}
	else if (cyclePnL > 0m)
	{
	_currentVolume = BaseVolume;
	LogInfo($"Profit detected. CyclePnL={cyclePnL}. Resetting volume to {BaseVolume}.");
	}

	_lastRealizedPnL = realized;
	}
}
