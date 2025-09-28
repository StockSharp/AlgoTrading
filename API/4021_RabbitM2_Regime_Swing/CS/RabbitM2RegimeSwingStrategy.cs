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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Rabbit M2" MetaTrader expert advisor by Peter Byrom.
/// The strategy combines hourly EMA regime detection with Williams %R momentum
/// and Donchian channel exits on the primary timeframe.
/// </summary>
public class RabbitM2RegimeSwingStrategy : Strategy
{
	private static readonly DataType TrendCandleType = TimeSpan.FromHours(1).TimeFrame();

	private readonly StrategyParam<int> _cciSellLevel;
	private readonly StrategyParam<int> _cciBuyLevel;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _bigWinTarget;
	private readonly StrategyParam<decimal> _volumeIncrement;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _baseVolume;
	private decimal _profitThreshold;
	private decimal _lastRealizedPnL;
	private decimal? _previousWpr;
	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private bool _longRegimeEnabled;
	private bool _shortRegimeEnabled;
	private decimal _stopDistance;
	private decimal _takeDistance;
	private decimal _activeStop;
	private decimal _activeTake;

	/// <summary>
/// Initializes a new instance of the <see cref="RabbitM2RegimeSwingStrategy"/> class.
/// </summary>
public RabbitM2RegimeSwingStrategy()
	{
		_cciSellLevel = Param(nameof(CciSellLevel), 101)
			.SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
			.SetCanOptimize(true);

		_cciBuyLevel = Param(nameof(CciBuyLevel), 99)
			.SetDisplay("CCI Buy Level", "CCI threshold confirming a long signal", "CCI")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Lookback window for the Commodity Channel Index", "CCI")
			.SetCanOptimize(true);

		_donchianPeriod = Param(nameof(DonchianPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Length of the Donchian channel used for exits", "Donchian")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of base-volume units that can be open", "Risk")
			.SetCanOptimize(true);

		_bigWinTarget = Param(nameof(BigWinTarget), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Big Win Target", "Profit needed before the volume increases", "Money Management")
			.SetCanOptimize(true);

		_volumeIncrement = Param(nameof(VolumeIncrement), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Increment", "How much to add to the base volume after a big win", "Money Management")
			.SetCanOptimize(true);

		_wprPeriod = Param(nameof(WprPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Length of the Williams %R oscillator", "Momentum")
			.SetCanOptimize(true);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA period on the hourly trend feed", "Trend")
			.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Slow EMA period on the hourly trend feed", "Trend")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance from entry price to the take profit", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Distance from entry price to the stop loss", "Risk")
			.SetCanOptimize(true);

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting base order size before scaling", "Money Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe for Williams %R, CCI and Donchian calculations", "General");
	}

	/// <summary>
	/// Minimum CCI value required to confirm a short setup.
	/// </summary>
	public int CciSellLevel
	{
		get => _cciSellLevel.Value;
		set => _cciSellLevel.Value = value;
	}

	/// <summary>
	/// Maximum CCI value required to confirm a long setup.
	/// </summary>
	public int CciBuyLevel
	{
		get => _cciBuyLevel.Value;
		set => _cciBuyLevel.Value = value;
	}

	/// <summary>
	/// Lookback used for the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Donchian channel period that drives breakout exits.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of base-volume multiples allowed in the net position.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Profit threshold that triggers a volume increase.
	/// </summary>
	public decimal BigWinTarget
	{
		get => _bigWinTarget.Value;
		set => _bigWinTarget.Value = value;
	}

	/// <summary>
	/// Volume increment added after a qualifying profit.
	/// </summary>
	public decimal VolumeIncrement
	{
		get => _volumeIncrement.Value;
		set => _volumeIncrement.Value = value;
	}

	/// <summary>
	/// Williams %R calculation period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period on the hourly trend feed.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period on the hourly trend feed.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Starting base volume used for each entry.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for Williams %R, CCI and Donchian calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	if (Security != null)
	{
	yield return (Security, CandleType);
	yield return (Security, TrendCandleType);
	}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_baseVolume = 0m;
	_profitThreshold = 0m;
	_lastRealizedPnL = 0m;
	_previousWpr = null;
	_previousUpperBand = null;
	_previousLowerBand = null;
	_longRegimeEnabled = false;
	_shortRegimeEnabled = false;
	_stopDistance = 0m;
	_takeDistance = 0m;
	_activeStop = 0m;
	_activeTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_baseVolume = InitialVolume;
	_profitThreshold = BigWinTarget;
	_lastRealizedPnL = PnL;
	_previousWpr = null;
	_previousUpperBand = null;
	_previousLowerBand = null;
	_longRegimeEnabled = false;
	_shortRegimeEnabled = false;
	_activeStop = 0m;
	_activeTake = 0m;

	_stopDistance = CalculatePriceOffset(StopLossPoints);
	_takeDistance = CalculatePriceOffset(TakeProfitPoints);

	NormalizeBaseVolume();

	var wpr = new WilliamsR { Length = WprPeriod };
	var cci = new CommodityChannelIndex { Length = CciPeriod };
	var donchian = new DonchianChannels { Length = DonchianPeriod };

	var emaFast = new ExponentialMovingAverage { Length = FastEmaPeriod };
	var emaSlow = new ExponentialMovingAverage { Length = SlowEmaPeriod };

	// The hourly subscription controls the trading regime and closes opposite positions when a cross happens.
	var trendSubscription = SubscribeCandles(TrendCandleType);
	trendSubscription
	.Bind(emaFast, emaSlow, ProcessTrend)
	.Start();

	// The primary subscription provides momentum signals and breakout exits.
	var primarySubscription = SubscribeCandles(CandleType);
	primarySubscription
	.BindEx(wpr, cci, donchian, ProcessPrimaryCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, primarySubscription);
	DrawIndicator(area, emaFast);
	DrawIndicator(area, emaSlow);
	DrawOwnTrades(area);
	}
	}

	private void ProcessTrend(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Fast EMA below slow EMA activates the short regime and forces longs to exit.
	if (fastEma < slowEma)
	{
	_shortRegimeEnabled = true;
	_longRegimeEnabled = false;
	CloseLongPosition("Hourly trend turned bearish");
	}
	// Fast EMA above slow EMA activates the long regime and forces shorts to exit.
	else if (fastEma > slowEma)
	{
	_longRegimeEnabled = true;
	_shortRegimeEnabled = false;
	CloseShortPosition("Hourly trend turned bullish");
	}
	}

	private void ProcessPrimaryCandle(
	ICandleMessage candle,
	IIndicatorValue wprValue,
	IIndicatorValue cciValue,
	IIndicatorValue donchianValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!wprValue.IsFinal || !cciValue.IsFinal || !donchianValue.IsFinal)
	return;

	var donchian = (DonchianChannelsValue)donchianValue;
	if (donchian.UpperBand is not decimal upperBand || donchian.LowerBand is not decimal lowerBand)
	return;

	// Always evaluate exit conditions before looking for new signals.
	HandleActivePosition(candle);

	var currentWpr = wprValue.ToDecimal();
	if (currentWpr == 0m)
	currentWpr = -1m;

	var previousWpr = _previousWpr;
	var currentCci = cciValue.ToDecimal();

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousWpr = currentWpr;
	_previousUpperBand = upperBand;
	_previousLowerBand = lowerBand;
	return;
	}

	if (previousWpr is null)
	{
	_previousWpr = currentWpr;
	_previousUpperBand = upperBand;
	_previousLowerBand = lowerBand;
	return;
	}

	var wprLag = previousWpr.Value;
	if (wprLag == 0m)
	wprLag = -1m;

	if (_shortRegimeEnabled)
	TryOpenShort(candle, currentWpr, wprLag, currentCci);

	if (_longRegimeEnabled)
	TryOpenLong(candle, currentWpr, wprLag, currentCci);

	_previousWpr = currentWpr;
	_previousUpperBand = upperBand;
	_previousLowerBand = lowerBand;
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
	if (Position > 0m)
	{
	// Long positions exit on take profit, stop loss or a Donchian breakout against the trade.
	if (_takeDistance > 0m && _activeTake > 0m && candle.HighPrice >= _activeTake)
	{
	CloseLongPosition("Take profit reached");
	}
	else if (_stopDistance > 0m && _activeStop > 0m && candle.LowPrice <= _activeStop)
	{
	CloseLongPosition("Stop loss reached");
	}
	else if (_previousLowerBand is decimal previousLower && candle.ClosePrice < previousLower)
	{
	CloseLongPosition("Closed below previous Donchian low");
	}
	}
	else if (Position < 0m)
	{
	// Short positions exit using mirrored conditions.
	if (_takeDistance > 0m && _activeTake > 0m && candle.LowPrice <= _activeTake)
	{
	CloseShortPosition("Take profit reached");
	}
	else if (_stopDistance > 0m && _activeStop > 0m && candle.HighPrice >= _activeStop)
	{
	CloseShortPosition("Stop loss reached");
	}
	else if (_previousUpperBand is decimal previousUpper && candle.ClosePrice > previousUpper)
	{
	CloseShortPosition("Closed above previous Donchian high");
	}
	}
	}

	private void TryOpenShort(ICandleMessage candle, decimal currentWpr, decimal previousWpr, decimal currentCci)
	{
	if (!(currentWpr < -20m && previousWpr > -20m && previousWpr < 0m && currentCci > CciSellLevel))
	return;

	if (_baseVolume <= 0m)
	return;

	// Net short exposure cannot exceed MaxTrades multiples of the base volume.
	var netVolume = Math.Abs(Position);
	var maxVolume = _baseVolume * MaxTrades;
	if (maxVolume <= 0m || netVolume >= maxVolume)
	return;

	var volume = Math.Min(_baseVolume, maxVolume - netVolume);
	volume = AlignVolume(volume);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_activeStop = _stopDistance > 0m ? candle.ClosePrice + _stopDistance : 0m;
	_activeTake = _takeDistance > 0m ? candle.ClosePrice - _takeDistance : 0m;
	}

	private void TryOpenLong(ICandleMessage candle, decimal currentWpr, decimal previousWpr, decimal currentCci)
	{
	if (!(currentWpr > -80m && previousWpr < -80m && previousWpr < 0m && currentCci < CciBuyLevel))
	return;

	if (_baseVolume <= 0m)
	return;

	var netVolume = Math.Abs(Position);
	var maxVolume = _baseVolume * MaxTrades;
	if (maxVolume <= 0m || netVolume >= maxVolume)
	return;

	var volume = Math.Min(_baseVolume, maxVolume - netVolume);
	volume = AlignVolume(volume);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	_activeStop = _stopDistance > 0m ? candle.ClosePrice - _stopDistance : 0m;
	_activeTake = _takeDistance > 0m ? candle.ClosePrice + _takeDistance : 0m;
	}

	private void CloseLongPosition(string reason)
	{
	var volume = Math.Abs(Position);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_activeStop = 0m;
	_activeTake = 0m;
	LogInfo($"Closing long position: {reason}.");
	}

	private void CloseShortPosition(string reason)
	{
	var volume = Math.Abs(Position);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	_activeStop = 0m;
	_activeTake = 0m;
	LogInfo($"Closing short position: {reason}.");
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
	base.OnOwnTradeReceived(trade);

	var realizedChange = PnL - _lastRealizedPnL;
	_lastRealizedPnL = PnL;

	// Increase the base volume after trades with sufficiently large realized profits.
	if (realizedChange > _profitThreshold && VolumeIncrement > 0m)
	{
	_baseVolume += VolumeIncrement;
	NormalizeBaseVolume();

	if (_profitThreshold > 0m)
	_profitThreshold *= 2m;
	}

	if (Math.Abs(Position) == 0m)
	{
	_activeStop = 0m;
	_activeTake = 0m;
	}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
	base.OnPositionReceived(position);

	if (Position == 0m)
	{
	_activeStop = 0m;
	_activeTake = 0m;
	}
	}

	private void NormalizeBaseVolume()
	{
	if (_baseVolume <= 0m)
	{
	Volume = 0m;
	return;
	}

	_baseVolume = AlignVolume(_baseVolume);
	Volume = _baseVolume;
	}

	private decimal AlignVolume(decimal volume)
	{
	if (Security == null || volume <= 0m)
	return volume;

	var step = Security.VolumeStep;
	if (step.HasValue && step.Value > 0m)
	{
	var steps = Math.Floor(volume / step.Value);
	volume = steps > 0m ? steps * step.Value : step.Value;
	}

	var min = Security.VolumeMin;
	if (min.HasValue && min.Value > 0m && volume < min.Value)
	volume = min.Value;

	var max = Security.VolumeMax;
	if (max.HasValue && max.Value > 0m && volume > max.Value)
	volume = max.Value;

	return volume;
	}

	private decimal CalculatePriceOffset(int points)
	{
	if (points <= 0)
	return 0m;

	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	step = 0.0001m;

	var decimals = Security?.Decimals;
	if (decimals == 3 || decimals == 5)
	step *= 10m;

	return points * step;
	}
}
