using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedged martingale strategy that scales positions on moving average crossovers.
/// </summary>
public class NewMartinStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longPositions = new();
	private readonly List<PositionEntry> _shortPositions = new();

	private SmoothedMovingAverage _slowMa;
	private SmoothedMovingAverage _fastMa;

	private decimal? _slowPrev1;
	private decimal? _slowPrev2;
	private decimal? _fastPrev1;
	private decimal? _fastPrev2;

	private decimal _currentVolume;
	private decimal _pipSize;
	private decimal _startBalance;
	private decimal _peakBalance;
	private DateTimeOffset? _lastCrossTime;
	private bool _positionsInitialized;

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial hedge volume per side.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Period of the slow smoothed moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Period of the fast smoothed moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Maximum equity drawdown percentage before all positions are liquidated.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the martingale additions and base volume growth.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NewMartinStrategy"/>.
	/// </summary>
	public NewMartinStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 200m, 10m);

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Volume per hedge side", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_slowPeriod = Param(nameof(SlowPeriod), 20)
		.SetGreaterThan(1)
		.SetDisplay("Slow MA", "Slow smoothed MA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);

		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetGreaterThan(1)
		.SetDisplay("Fast MA", "Fast smoothed MA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_lossPercent = Param(nameof(LossPercent), 12m)
		.SetGreaterThanZero()
		.SetDisplay("Equity DD %", "Maximum drawdown before reset", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 1m);

		_multiplier = Param(nameof(Multiplier), 1.6m)
		.SetGreaterThan(1m)
		.SetDisplay("Multiplier", "Martingale growth factor", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1.1m, 3m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for calculations", "General");
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

	_longPositions.Clear();
	_shortPositions.Clear();
	_slowPrev1 = null;
	_slowPrev2 = null;
	_fastPrev1 = null;
	_fastPrev2 = null;
	_currentVolume = 0m;
	_pipSize = 0m;
	_startBalance = 0m;
	_peakBalance = 0m;
	_lastCrossTime = null;
	_positionsInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (SlowPeriod <= FastPeriod)
	throw new InvalidOperationException("Slow period must be greater than fast period.");

	_currentVolume = AdjustVolume(InitialVolume);

	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	{
		_pipSize = 1m;
	}
	else
	{
		_pipSize = step;
		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			_pipSize = step * 10m;
	}

	_slowMa = new SmoothedMovingAverage { Length = SlowPeriod };
	_fastMa = new SmoothedMovingAverage { Length = FastPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_slowMa, _fastMa, ProcessCandle)
	.Start();

	_startBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	_peakBalance = _startBalance;
	}

	private void ProcessCandle(ICandleMessage candle, decimal slow, decimal fast)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!_slowMa.IsFormed || !_fastMa.IsFormed)
	{
	UpdateAverageHistory(slow, fast);
	return;
	}

	UpdateAccountMetrics();

	if (ShouldCloseAllPositions())
	{
	CloseAllPositions();
	_positionsInitialized = false;
	}

	if (!_positionsInitialized)
	{
	InitializeHedge(candle.ClosePrice);
	}

	var tpTriggered = CheckTakeProfits(candle);

	if (tpTriggered)
	{
	CloseExtremePositions(candle.ClosePrice);
	}

	if (_longPositions.Count == 0)
	OpenPosition(Sides.Buy, _currentVolume, candle.ClosePrice);

	if (_shortPositions.Count == 0)
	OpenPosition(Sides.Sell, _currentVolume, candle.ClosePrice);

	HandleCrossing(candle, slow, fast);

	UpdateAverageHistory(slow, fast);
	}

	private void InitializeHedge(decimal price)
	{
	if (_currentVolume <= 0m)
	return;

	// Start with symmetric hedge on both sides.
	OpenPosition(Sides.Buy, _currentVolume, price);
	OpenPosition(Sides.Sell, _currentVolume, price);
	_positionsInitialized = _longPositions.Count > 0 && _shortPositions.Count > 0;
	}

	private void UpdateAccountMetrics()
	{
	var equity = Portfolio?.CurrentValue ?? 0m;

	if (equity > _peakBalance)
	_peakBalance = equity;

	if (_startBalance > 0m && equity >= _startBalance * Multiplier)
	{
	_startBalance = equity;

	var newVolume = AdjustVolume(_currentVolume * Multiplier);
	if (newVolume > 0m)
	_currentVolume = newVolume;
	}
	}

	private bool ShouldCloseAllPositions()
	{
	if (_peakBalance <= 0m)
	return false;

	var equity = Portfolio?.CurrentValue ?? 0m;
	if (equity <= 0m)
	return false;

	var drawdown = (_peakBalance - equity) / _peakBalance * 100m;
	return drawdown >= LossPercent;
	}

	private bool CheckTakeProfits(ICandleMessage candle)
	{
	var triggered = false;
	var offset = TakeProfitPips * _pipSize;
	if (offset <= 0m)
	return false;

	foreach (var entry in _longPositions.ToArray())
	{
	if (candle.HighPrice >= entry.TakeProfit)
	{
	CloseEntry(entry);
	triggered = true;
	}
	}

	foreach (var entry in _shortPositions.ToArray())
	{
	if (candle.LowPrice <= entry.TakeProfit)
	{
	CloseEntry(entry);
	triggered = true;
	}
	}

	return triggered;
	}

	private void CloseExtremePositions(decimal price)
	{
	var (lossEntry, lossValue, profitEntry, profitValue) = GetExtremePositions(price);

	if (lossEntry is not null && lossValue < 0m)
	CloseEntry(lossEntry);

	if (profitEntry is not null && profitEntry != lossEntry)
	CloseEntry(profitEntry);
	}

	private void HandleCrossing(ICandleMessage candle, decimal slow, decimal fast)
	{
	if (!_slowPrev2.HasValue || !_slowPrev1.HasValue || !_fastPrev2.HasValue || !_fastPrev1.HasValue)
	return;

	var crossDetected = (_slowPrev2.Value > _fastPrev2.Value && _slowPrev1.Value < _fastPrev1.Value)
	|| (_slowPrev2.Value < _fastPrev2.Value && _slowPrev1.Value > _fastPrev1.Value);

	if (!crossDetected)
	return;

	if (_lastCrossTime == candle.OpenTime)
	return;

	_lastCrossTime = candle.OpenTime;

	var (lossEntry, _, profitEntry, _) = GetExtremePositions(candle.ClosePrice);
	if (lossEntry is null)
	return;

	var volume = AdjustVolume(lossEntry.Volume * Multiplier);
	if (volume <= 0m)
	return;

	// Average down on the weakest side.
	OpenPosition(lossEntry.Side, volume, candle.ClosePrice);

	if (profitEntry is not null && profitEntry != lossEntry)
	{
	// Lock in profit on the strongest position after the new hedge.
	CloseEntry(profitEntry);
	}
	}

	private void UpdateAverageHistory(decimal slow, decimal fast)
	{
	_slowPrev2 = _slowPrev1;
	_slowPrev1 = slow;
	_fastPrev2 = _fastPrev1;
	_fastPrev1 = fast;
	}

	private void OpenPosition(Sides side, decimal requestedVolume, decimal price)
	{
	var volume = AdjustVolume(requestedVolume);
	if (volume <= 0m)
	return;

	var offset = TakeProfitPips * _pipSize;
	if (offset <= 0m)
	return;

	var takeProfit = side == Sides.Buy ? price + offset : price - offset;
	var entry = new PositionEntry(side, volume, price, takeProfit);

	if (side == Sides.Buy)
	{
	_longPositions.Add(entry);
	BuyMarket(volume);
	}
	else
	{
	_shortPositions.Add(entry);
	SellMarket(volume);
	}
	}

	private void CloseAllPositions()
	{
		foreach (var entry in _longPositions.ToArray())
			CloseEntry(entry);

		foreach (var entry in _shortPositions.ToArray())
			CloseEntry(entry);
	}

	private void CloseEntry(PositionEntry entry)
	{
	if (entry.Side == Sides.Buy)
	{
	SellMarket(entry.Volume);
	_longPositions.Remove(entry);
	}
	else
	{
	BuyMarket(entry.Volume);
	_shortPositions.Remove(entry);
	}
	}

	private (PositionEntry? lossEntry, decimal lossValue, PositionEntry? profitEntry, decimal profitValue) GetExtremePositions(decimal price)
	{
	PositionEntry? lossEntry = null;
	PositionEntry? profitEntry = null;
	var lossValue = 0m;
	var profitValue = 0m;

	foreach (var entry in _longPositions)
	{
	var pnl = (price - entry.EntryPrice) * entry.Volume;
	if (lossEntry is null || pnl < lossValue)
	{
	lossEntry = entry;
	lossValue = pnl;
	}

	if (profitEntry is null || pnl > profitValue)
	{
	profitEntry = entry;
	profitValue = pnl;
	}
	}

	foreach (var entry in _shortPositions)
	{
	var pnl = (entry.EntryPrice - price) * entry.Volume;
	if (lossEntry is null || pnl < lossValue)
	{
	lossEntry = entry;
	lossValue = pnl;
	}

	if (profitEntry is null || pnl > profitValue)
	{
	profitEntry = entry;
	profitValue = pnl;
	}
	}

	return (lossEntry, lossValue, profitEntry, profitValue);
	}

	private decimal AdjustVolume(decimal volume)
	{
	var security = Security;
	if (security is null)
	return volume;

	var step = security.StepVolume ?? 0m;
	if (step > 0m)
	{
	var steps = decimal.Floor(volume / step);
	volume = steps * step;
	}

	var min = security.MinVolume ?? 0m;
	if (min > 0m && volume < min)
	return 0m;

	var max = security.MaxVolume ?? 0m;
	if (max > 0m && volume > max)
	volume = max;

	return volume;
	}

	private sealed record PositionEntry(Sides Side, decimal Volume, decimal EntryPrice, decimal TakeProfit);
}
