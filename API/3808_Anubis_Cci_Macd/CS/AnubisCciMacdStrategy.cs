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
/// Port of the MetaTrader "Anubis" expert advisor.
/// Trades when a 4-hour CCI filter aligns with a bearish or bullish MACD crossover on the 15-minute chart.
/// Applies adaptive position sizing, dynamic breakeven, and multi-condition exits.
/// </summary>
public class AnubisCciMacdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _breakevenPoints;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _lossFactor;
	private readonly StrategyParam<int> _maxShortTrades;
	private readonly StrategyParam<int> _maxLongTrades;
	private readonly StrategyParam<decimal> _closeAtrMultiplier;
	private readonly StrategyParam<decimal> _profitThresholdPoints;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _priceFilterPoints;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private CommodityChannelIndex _cci = null!;
	private StandardDeviation _stdDev = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _cciValue;
	private decimal? _stdDevValue;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prev2Macd;
	private decimal? _prev2Signal;
	private decimal? _prevAtr;

	private decimal? _prevCandleOpen;
	private decimal? _prevCandleClose;

	private DateTimeOffset? _lastLongBarTime;
	private DateTimeOffset? _lastShortBarTime;
	private decimal _lastLongPrice;
	private decimal _lastShortPrice;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAvgPrice;
	private decimal _shortAvgPrice;
	private bool _longBreakevenActivated;
	private bool _shortBreakevenActivated;
	private bool _lastTradeWasLoss;

	private decimal? _atrValue;

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal VolumeValue
	{
	get => _volume.Value;
	set => _volume.Value = value;
	}

	/// <summary>
	/// Threshold for the 4-hour CCI filter.
	/// </summary>
	public decimal CciThreshold
	{
	get => _cciThreshold.Value;
	set => _cciThreshold.Value = value;
	}

	/// <summary>
	/// Period for the 4-hour CCI indicator.
	/// </summary>
	public int CciPeriod
	{
	get => _cciPeriod.Value;
	set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
	get => _stopLossPoints.Value;
	set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Breakeven trigger distance in points.
	/// </summary>
	public int BreakevenPoints
	{
	get => _breakevenPoints.Value;
	set => _breakevenPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
	get => _macdFastPeriod.Value;
	set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
	get => _macdSlowPeriod.Value;
	set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
	get => _macdSignalPeriod.Value;
	set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Volume reduction factor after a losing trade.
	/// </summary>
	public decimal LossFactor
	{
	get => _lossFactor.Value;
	set => _lossFactor.Value = value;
	}

	/// <summary>
	/// Maximum number of concurrent short trades.
	/// </summary>
	public int MaxShortTrades
	{
	get => _maxShortTrades.Value;
	set => _maxShortTrades.Value = value;
	}

	/// <summary>
	/// Maximum number of concurrent long trades.
	/// </summary>
	public int MaxLongTrades
	{
	get => _maxLongTrades.Value;
	set => _maxLongTrades.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR-based early exits.
	/// </summary>
	public decimal CloseAtrMultiplier
	{
	get => _closeAtrMultiplier.Value;
	set => _closeAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Additional profit threshold in points for MACD exits.
	/// </summary>
	public decimal ProfitThresholdPoints
	{
	get => _profitThresholdPoints.Value;
	set => _profitThresholdPoints.Value = value;
	}

	/// <summary>
	/// Multiplier for the 4-hour standard deviation take-profit.
	/// </summary>
	public decimal StdDevMultiplier
	{
	get => _stdDevMultiplier.Value;
	set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum price change in points between consecutive entries on the same side.
	/// </summary>
	public decimal PriceFilterPoints
	{
	get => _priceFilterPoints.Value;
	set => _priceFilterPoints.Value = value;
	}

	/// <summary>
	/// Primary candle type used for MACD and ATR calculations.
	/// </summary>
	public DataType SignalCandleType
	{
	get => _signalCandleType.Value;
	set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used for CCI and standard deviation.
	/// </summary>
	public DataType TrendCandleType
	{
	get => _trendCandleType.Value;
	set => _trendCandleType.Value = value;
	}

	/// <summary>
/// Initializes <see cref="AnubisCciMacdStrategy"/>.
/// </summary>
public AnubisCciMacdStrategy()
	{
	_volume = Param(nameof(VolumeValue), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Volume", "Base order volume", "Trading")
	.SetCanOptimize(true)
	.SetOptimize(0.1m, 5m, 0.1m);

	_cciThreshold = Param(nameof(CciThreshold), 80m)
	.SetGreaterThanZero()
	.SetDisplay("CCI Threshold", "Absolute threshold for 4H CCI filter", "Filters")
	.SetCanOptimize(true)
	.SetOptimize(40m, 200m, 10m);

	_cciPeriod = Param(nameof(CciPeriod), 11)
	.SetGreaterThanZero()
	.SetDisplay("CCI Period", "Period for 4H CCI", "Filters")
	.SetCanOptimize(true)
	.SetOptimize(5, 40, 1);

	_stopLossPoints = Param(nameof(StopLossPoints), 100)
	.SetGreaterThanZero()
	.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(20, 300, 10);

	_breakevenPoints = Param(nameof(BreakevenPoints), 65)
	.SetGreaterThanZero()
	.SetDisplay("Breakeven", "Profit in points to move stop to entry", "Risk");

	_macdFastPeriod = Param(nameof(MacdFastPeriod), 20)
	.SetGreaterThanZero()
	.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
	.SetCanOptimize(true)
	.SetOptimize(5, 40, 1);

	_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 50)
	.SetGreaterThanZero()
	.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
	.SetCanOptimize(true)
	.SetOptimize(10, 120, 5);

	_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 2)
	.SetGreaterThanZero()
	.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD")
	.SetCanOptimize(true)
	.SetOptimize(1, 10, 1);

	_lossFactor = Param(nameof(LossFactor), 0.6m)
	.SetGreaterThanZero()
	.SetDisplay("Loss Factor", "Volume multiplier after a losing trade", "Risk");

	_maxShortTrades = Param(nameof(MaxShortTrades), 2)
	.SetGreaterThanZero()
	.SetDisplay("Max Shorts", "Maximum concurrent short entries", "Trading");

	_maxLongTrades = Param(nameof(MaxLongTrades), 2)
	.SetGreaterThanZero()
	.SetDisplay("Max Longs", "Maximum concurrent long entries", "Trading");

	_closeAtrMultiplier = Param(nameof(CloseAtrMultiplier), 2m)
	.SetGreaterThanZero()
	.SetDisplay("ATR Close", "ATR multiplier for aggressive exits", "Exits");

	_profitThresholdPoints = Param(nameof(ProfitThresholdPoints), 28m)
	.SetGreaterThanZero()
	.SetDisplay("Profit Threshold", "Extra profit in points before MACD exit", "Exits");

	_stdDevMultiplier = Param(nameof(StdDevMultiplier), 2.9m)
	.SetGreaterThanZero()
	.SetDisplay("StdDev Take", "Standard deviation multiplier for take-profit", "Exits");

	_priceFilterPoints = Param(nameof(PriceFilterPoints), 20m)
	.SetGreaterThanZero()
	.SetDisplay("Price Filter", "Minimum distance between consecutive entries", "Filters");

	_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Signal Candle", "Primary timeframe for MACD", "Data");

	_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Trend Candle", "Higher timeframe for filters", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, SignalCandleType);

	if (!Equals(SignalCandleType, TrendCandleType))
	{
	yield return (Security, TrendCandleType);
	}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_cciValue = null;
	_stdDevValue = null;
	_prevMacd = null;
	_prevSignal = null;
	_prev2Macd = null;
	_prev2Signal = null;
	_prevAtr = null;
	_prevCandleOpen = null;
	_prevCandleClose = null;
	_lastLongBarTime = null;
	_lastShortBarTime = null;
	_lastLongPrice = 0m;
	_lastShortPrice = 0m;
	_longVolume = 0m;
	_shortVolume = 0m;
	_longAvgPrice = 0m;
	_shortAvgPrice = 0m;
	_longBreakevenActivated = false;
	_shortBreakevenActivated = false;
	_lastTradeWasLoss = false;
	_atrValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	// Enable the built-in protection module once before submitting orders.
	StartProtection();

	_cci = new CommodityChannelIndex
	{
	Length = CciPeriod
	};

	_stdDev = new StandardDeviation
	{
	Length = 30
	};

	_macd = new MovingAverageConvergenceDivergenceSignal
	{
	ShortPeriod = MacdFastPeriod,
	LongPeriod = MacdSlowPeriod,
	SignalPeriod = MacdSignalPeriod
	};

	_atr = new AverageTrueRange
	{
	Length = 12
	};

	// Subscribe to the main timeframe for MACD and ATR updates.
	var signalSubscription = SubscribeCandles(SignalCandleType);
	signalSubscription.Bind(UpdateAtr);
	signalSubscription.BindEx(_macd, ProcessSignalCandle);
	signalSubscription.Start();

	// Subscribe to the higher timeframe for CCI and standard deviation.
	var trendSubscription = SubscribeCandles(TrendCandleType);
	trendSubscription.Bind(_cci, _stdDev, ProcessTrendCandle);
	trendSubscription.Start();

	Volume = VolumeValue;
	}

	private void ProcessTrendCandle(ICandleMessage candle, decimal cciValue, decimal stdDevValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Cache higher timeframe values for the next entry and exit decisions.
	_cciValue = cciValue;
	_stdDevValue = stdDevValue;
	}

	private void UpdateAtr(ICandleMessage candle, decimal atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Store ATR for ATR-driven exit rules.
	_atrValue = atrValue;
	}

	private void ProcessSignalCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	// Skip processing until the MACD value is fully formed.
	if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdSignal)
	return;

	if (macdSignal.Macd is not decimal macd || macdSignal.Signal is not decimal signal)
	return;

	// Manage any open positions before evaluating new entries.
	ApplyExitRules(candle);

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	ShiftMacdHistory(macd, signal);
	StorePreviousCandle(candle);
	_prevAtr = _atrValue;
	return;
	}

	var cciValue = _cciValue;
	var prevMacd = _prevMacd;
	var prevSignal = _prevSignal;
	var prev2Macd = _prev2Macd;
	var prev2Signal = _prev2Signal;

	if (cciValue is decimal cci && prevMacd is decimal macd1 && prevSignal is decimal signal1 &&
	prev2Macd is decimal macd2 && prev2Signal is decimal signal2)
	{
			// Evaluate entry rules only when the historical MACD and CCI buffers are available.
		TryEnterPositions(candle, cci, macd1, signal1, macd2, signal2);
	}

	ShiftMacdHistory(macd, signal);
	StorePreviousCandle(candle);
	_prevAtr = _atrValue;
	}

	private void TryEnterPositions(ICandleMessage candle, decimal cci, decimal macd1, decimal signal1, decimal macd2, decimal signal2)
	{
	var point = GetPointSize();

	var shortSignal = cci > CciThreshold && macd2 >= signal2 && macd1 < signal1 && macd1 > 0m;
	var longSignal = cci < -CciThreshold && macd2 <= signal2 && macd1 > signal1 && macd1 < 0m;

	if (shortSignal && Position <= 0)
	{
	TryEnterShort(candle, point);
	}
	else if (longSignal && Position >= 0)
	{
	TryEnterLong(candle, point);
	}
	}

	private void TryEnterLong(ICandleMessage candle, decimal point)
	{
	// Respect the configured cap on the number of stacked long entries.
	if (_longVolume >= MaxLongTrades * VolumeValue)
	return;

	// Avoid repeating a trade inside the same bar.
	if (_lastLongBarTime == candle.OpenTime)
	return;

	// Enforce the minimum price distance filter inherited from the original EA.
	if (Math.Abs(candle.ClosePrice - _lastLongPrice) <= PriceFilterPoints * point)
	return;

	var volume = CalculateTradeVolume();
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	_lastLongBarTime = candle.OpenTime;
	_lastLongPrice = candle.ClosePrice;
	_longBreakevenActivated = false;
	}

	private void TryEnterShort(ICandleMessage candle, decimal point)
	{
	// Respect the configured cap on the number of stacked short entries.
	if (_shortVolume >= MaxShortTrades * VolumeValue)
	return;

	// Avoid repeating a trade inside the same bar.
	if (_lastShortBarTime == candle.OpenTime)
	return;

	// Enforce the minimum price distance filter inherited from the original EA.
	if (Math.Abs(candle.ClosePrice - _lastShortPrice) <= PriceFilterPoints * point)
	return;

	var volume = CalculateTradeVolume();
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_lastShortBarTime = candle.OpenTime;
	_lastShortPrice = candle.ClosePrice;
	_shortBreakevenActivated = false;
	}

	private void ApplyExitRules(ICandleMessage candle)
	{
		// Exit logic mirrors the original expert advisor with virtual stops and multiple triggers.
	var point = GetPointSize();
	var stopDistance = StopLossPoints * point;
	var breakevenDistance = BreakevenPoints * point;
	var profitThreshold = ProfitThresholdPoints * point;
	var atr = _prevAtr ?? _atrValue;
	var std = _stdDevValue;

	if (_longVolume > 0m)
	{
		// Long side management: breakeven, stop-loss, standard deviation and MACD exits.
	if (!_longBreakevenActivated && candle.ClosePrice - _longAvgPrice >= breakevenDistance)
	{
	_longBreakevenActivated = true;
	}

	if (_longBreakevenActivated && candle.ClosePrice <= _longAvgPrice)
	{
	ClosePosition();
	return;
	}

	if (candle.LowPrice <= _longAvgPrice - stopDistance)
	{
	ClosePosition();
	return;
	}

	if (std is decimal stdValue && candle.ClosePrice >= _longAvgPrice + StdDevMultiplier * stdValue)
	{
	ClosePosition();
	return;
	}

	if (atr is decimal atrValue && _prevCandleOpen is decimal prevOpen && _prevCandleClose is decimal prevClose)
	{
	var range = prevClose - prevOpen;
	var macdCondition = _prevMacd is decimal macd1 && _prev2Macd is decimal macd2 && macd1 < macd2;

	if ((range > CloseAtrMultiplier * atrValue) ||
	(macdCondition && candle.ClosePrice - _longAvgPrice > profitThreshold))
	{
	ClosePosition();
	return;
	}
	}
	}

	if (_shortVolume > 0m)
	{
		// Short side management mirrors the long logic with inverted comparisons.
	if (!_shortBreakevenActivated && _shortAvgPrice - candle.ClosePrice >= breakevenDistance)
	{
	_shortBreakevenActivated = true;
	}

	if (_shortBreakevenActivated && candle.ClosePrice >= _shortAvgPrice)
	{
	ClosePosition();
	return;
	}

	if (candle.HighPrice >= _shortAvgPrice + stopDistance)
	{
	ClosePosition();
	return;
	}

	if (std is decimal stdValue && candle.ClosePrice <= _shortAvgPrice - StdDevMultiplier * stdValue)
	{
	ClosePosition();
	return;
	}

	if (atr is decimal atrValue && _prevCandleOpen is decimal prevOpen && _prevCandleClose is decimal prevClose)
	{
	var range = prevOpen - prevClose;
	var macdCondition = _prevMacd is decimal macd1 && _prev2Macd is decimal macd2 && macd1 > macd2;

	if ((range > CloseAtrMultiplier * atrValue) ||
	(macdCondition && _shortAvgPrice - candle.ClosePrice > profitThreshold))
	{
	ClosePosition();
	return;
	}
	}
	}
	}

	private void StorePreviousCandle(ICandleMessage candle)
	{
	_prevCandleOpen = candle.OpenPrice;
	_prevCandleClose = candle.ClosePrice;
	}

	private void ShiftMacdHistory(decimal macd, decimal signal)
	{
	_prev2Macd = _prevMacd;
	_prev2Signal = _prevSignal;
	_prevMacd = macd;
	_prevSignal = signal;
	}

	private decimal CalculateTradeVolume()
	{
		// Start from the configured base volume.
	var volume = VolumeValue;

	var capital = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	if (capital > 22000m)
	{
	volume *= 3.2m;
	}
	else if (capital > 14000m)
	{
	volume *= 2m;
	}

	if (_lastTradeWasLoss)
	{
	volume *= LossFactor;
	}

	return volume;
	}

	private decimal GetPointSize()
	{
		// Translate point-based distances using the instrument metadata.
	var step = Security?.PriceStep;
	if (step == null || step == 0m)
	return 1m;

	return step.Value;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
	base.OnNewMyTrade(trade);

	var t = trade.Trade;
	if (t.Security != Security)
	return;

	var price = t.Price;
	var volume = t.Volume;
	if (volume <= 0m)
	return;

	if (t.Side == Sides.Buy)
	{
	ProcessBuyTrade(price, volume);
	}
	else if (t.Side == Sides.Sell)
	{
	ProcessSellTrade(price, volume);
	}
	}

	private void ProcessBuyTrade(decimal price, decimal volume)
	{
		// Buying either covers existing shorts or builds a new long position.
	if (_shortVolume > 0m)
	{
		// Short side management mirrors the long logic with inverted comparisons.
	var closed = Math.Min(volume, _shortVolume);
	var pnl = (_shortAvgPrice - price) * closed;

	_shortVolume -= closed;
	if (_shortVolume == 0m)
	{
	_shortAvgPrice = 0m;
	_shortBreakevenActivated = false;
	}

	if (closed > 0m)
	{
	_lastTradeWasLoss = pnl < 0m;
	}

	var remaining = volume - closed;
	if (remaining > 0m)
	{
	_longAvgPrice = (_longAvgPrice * _longVolume + price * remaining) / (_longVolume + remaining);
	_longVolume += remaining;
	}
	}
	else
	{
	_longAvgPrice = (_longAvgPrice * _longVolume + price * volume) / (_longVolume + volume);
	_longVolume += volume;
	}
	}

	private void ProcessSellTrade(decimal price, decimal volume)
	{
		// Selling either reduces long exposure or adds a fresh short position.
	if (_longVolume > 0m)
	{
		// Long side management: breakeven, stop-loss, standard deviation and MACD exits.
	var closed = Math.Min(volume, _longVolume);
	var pnl = (price - _longAvgPrice) * closed;

	_longVolume -= closed;
	if (_longVolume == 0m)
	{
	_longAvgPrice = 0m;
	_longBreakevenActivated = false;
	}

	if (closed > 0m)
	{
	_lastTradeWasLoss = pnl < 0m;
	}

	var remaining = volume - closed;
	if (remaining > 0m)
	{
	_shortAvgPrice = (_shortAvgPrice * _shortVolume + price * remaining) / (_shortVolume + remaining);
	_shortVolume += remaining;
	}
	}
	else
	{
	_shortAvgPrice = (_shortAvgPrice * _shortVolume + price * volume) / (_shortVolume + volume);
	_shortVolume += volume;
	}
	}
}

