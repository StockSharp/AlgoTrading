using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Smart Trend Follower" MetaTrader 5 expert advisor that combines moving average signals
/// with stochastic confirmation and a martingale-style layering engine.
/// </summary>
public class SmartTrendFollowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SignalMode> _signalMode;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _layerDistancePips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private SMA? _fastSma;
	private SMA? _slowSma;
	private StochasticOscillator? _stochastic;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _pipSize;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Trading signal mode.
	/// </summary>
	public SignalMode SignalMode
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	/// <summary>
	/// Base candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial order volume expressed in lots.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume of every additional averaging order.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Distance in pips required before stacking another order in the same direction.
	/// </summary>
	public decimal LayerDistancePips
	{
		get => _layerDistancePips.Value;
		set => _layerDistancePips.Value = value;
	}

	/// <summary>
	/// Fast simple moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow simple moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %K length.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %D smoothing length.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the %K line.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips relative to the average entry price.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips relative to the average entry price.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SmartTrendFollowerStrategy"/>.
	/// </summary>
	public SmartTrendFollowerStrategy()
	{
		_signalMode = Param(nameof(SignalMode), SignalMode.CrossMa)
		.SetDisplay("Signal Mode", "Trading logic selection", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Starting order volume in lots", "Money Management");

		_multiplier = Param(nameof(Multiplier), 2m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Volume Multiplier", "Martingale multiplier applied to additional entries", "Money Management");

		_layerDistancePips = Param(nameof(LayerDistancePips), 200m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Layer Distance", "Pip distance before adding another order", "Money Management");

		_fastPeriod = Param(nameof(FastPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast moving average period", "Indicators")
		.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow moving average period", "Indicators")
		.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "%K lookback length", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Slowing", "Extra smoothing for %K", "Indicators");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Target distance in pips", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Protective distance in pips", "Risk Management");
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

	_fastSma = null;
	_slowSma = null;
	_stochastic = null;

	_longEntries.Clear();
	_shortEntries.Clear();

	_prevFast = null;
	_prevSlow = null;
	_pipSize = 0m;
	_longExitRequested = false;
	_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_fastSma = new SMA { Length = Math.Max(1, FastPeriod) };
	_slowSma = new SMA { Length = Math.Max(1, SlowPeriod) };
	_stochastic = new StochasticOscillator
	{
	Length = Math.Max(1, StochasticKPeriod),
	K = { Length = Math.Max(1, StochasticSlowing) },
	D = { Length = Math.Max(1, StochasticDPeriod) }
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_fastSma, _slowSma, _stochastic, ProcessCandle)
	.Start();

	_pipSize = CalculatePipSize();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _fastSma);
	DrawIndicator(area, _slowSma);
	DrawIndicator(area, _stochastic);
	DrawOwnTrades(area);
	}

	StartProtection();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
	base.OnNewMyTrade(trade);

	var info = trade.Trade;
	var price = info.Price;
	var volume = info.Volume;

	if (info.Side == Sides.Buy)
	{
	ReduceEntries(_shortEntries, ref volume);

	if (volume > 0m)
	{
	_longEntries.Add(new PositionEntry(price, volume));
	}
	}
	else if (info.Side == Sides.Sell)
	{
	ReduceEntries(_longEntries, ref volume);

	if (volume > 0m)
	{
	_shortEntries.Add(new PositionEntry(price, volume));
	}
	}

	if (GetTotalVolume(_longEntries) <= 0m)
	{
	_longEntries.Clear();
	_longExitRequested = false;
	}

	if (GetTotalVolume(_shortEntries) <= 0m)
	{
	_shortEntries.Clear();
	_shortExitRequested = false;
	}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue stochasticValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var fast = fastValue.ToDecimal();
	var slow = slowValue.ToDecimal();

	ManageExits(candle);

	var signal = SignalDirection.None;

	if (SignalMode == SignalMode.CrossMa)
	{
	if (_prevFast.HasValue && _prevSlow.HasValue)
	{
	var crossBuy = fast < slow && _prevSlow.Value < _prevFast.Value;
	var crossSell = fast > slow && _prevSlow.Value > _prevFast.Value;

	if (crossBuy)
	signal = SignalDirection.Buy;
	else if (crossSell)
	signal = SignalDirection.Sell;
	}
	}
	else if (_stochastic?.IsFormed == true && stochasticValue is StochasticOscillatorValue stoch && stoch.K is decimal kValue)
	{
	var bullish = candle.ClosePrice > candle.OpenPrice;
	var bearish = candle.ClosePrice < candle.OpenPrice;

	if (fast > slow && bullish && kValue <= 30m)
	signal = SignalDirection.Buy;
	else if (fast < slow && bearish && kValue >= 70m)
	signal = SignalDirection.Sell;
	}

	if (signal != SignalDirection.None && IsFormedAndOnlineAndAllowTrading())
	{
	ProcessSignal(signal, candle.ClosePrice);
	}

	_prevFast = fast;
	_prevSlow = slow;
	}

	private void ProcessSignal(SignalDirection signal, decimal referencePrice)
	{
	switch (signal)
	{
	case SignalDirection.Buy:
	{
	var shortVolume = GetTotalVolume(_shortEntries);
	if (shortVolume > 0m)
	{
	if (!_shortExitRequested)
	{
	_shortExitRequested = true;
	BuyMarket(shortVolume);
	}
	return;
	}

	var longCount = _longEntries.Count;
	var requested = CalculateRequestedVolume(longCount);
	var volume = PrepareNextVolume(requested);
	if (volume <= 0m)
	return;

	if (longCount == 0)
	{
	BuyMarket(volume);
	return;
	}

	var lowest = GetExtremePrice(_longEntries, true);
	var threshold = lowest - LayerDistancePips * (_pipSize > 0m ? _pipSize : 1m);

	if (referencePrice <= threshold)
	{
	BuyMarket(volume);
	}

	break;
	}
	case SignalDirection.Sell:
	{
	var longVolume = GetTotalVolume(_longEntries);
	if (longVolume > 0m)
	{
	if (!_longExitRequested)
	{
	_longExitRequested = true;
	SellMarket(longVolume);
	}
	return;
	}

	var shortCount = _shortEntries.Count;
	var requested = CalculateRequestedVolume(shortCount);
	var volume = PrepareNextVolume(requested);
	if (volume <= 0m)
	return;

	if (shortCount == 0)
	{
	SellMarket(volume);
	return;
	}

	var highest = GetExtremePrice(_shortEntries, false);
	var threshold = highest + LayerDistancePips * (_pipSize > 0m ? _pipSize : 1m);

	if (referencePrice >= threshold)
	{
	SellMarket(volume);
	}

	break;
	}
	}
	}

	private void ManageExits(ICandleMessage candle)
	{
	var longVolume = GetTotalVolume(_longEntries);
	if (longVolume > 0m && !_longExitRequested)
	{
	var average = GetAveragePrice(_longEntries);
	var takeProfit = TakeProfitPips > 0m ? average + TakeProfitPips * (_pipSize > 0m ? _pipSize : 1m) : (decimal?)null;
	var stopLoss = StopLossPips > 0m ? average - StopLossPips * (_pipSize > 0m ? _pipSize : 1m) : (decimal?)null;

	if (takeProfit.HasValue && candle.HighPrice >= takeProfit.Value)
	{
	_longExitRequested = true;
	SellMarket(longVolume);
	return;
	}

	if (stopLoss.HasValue && candle.LowPrice <= stopLoss.Value)
	{
	_longExitRequested = true;
	SellMarket(longVolume);
	return;
	}
	}

	var shortVolume = GetTotalVolume(_shortEntries);
	if (shortVolume > 0m && !_shortExitRequested)
	{
	var average = GetAveragePrice(_shortEntries);
	var takeProfit = TakeProfitPips > 0m ? average - TakeProfitPips * (_pipSize > 0m ? _pipSize : 1m) : (decimal?)null;
	var stopLoss = StopLossPips > 0m ? average + StopLossPips * (_pipSize > 0m ? _pipSize : 1m) : (decimal?)null;

	if (takeProfit.HasValue && candle.LowPrice <= takeProfit.Value)
	{
	_shortExitRequested = true;
	BuyMarket(shortVolume);
	return;
	}

	if (stopLoss.HasValue && candle.HighPrice >= stopLoss.Value)
	{
	_shortExitRequested = true;
	BuyMarket(shortVolume);
	}
	}
	}

	private decimal CalculateRequestedVolume(int existingCount)
	{
	if (InitialVolume <= 0m)
	return 0m;

	var result = InitialVolume;

	if (existingCount > 0 && Multiplier > 0m)
	{
	result *= (decimal)Math.Pow((double)Math.Max(Multiplier, 1m), existingCount);
	}

	return result;
	}

	private decimal PrepareNextVolume(decimal requested)
	{
	if (requested <= 0m)
	return 0m;

	var security = Security;
	if (security == null)
	return requested;

	var step = security.VolumeStep ?? 0m;
	if (step > 0m)
	{
	requested = step * Math.Round(requested / step, MidpointRounding.AwayFromZero);
	}

	var min = security.VolumeMin ?? 0m;
	if (min > 0m && requested < min)
	return 0m;

	var max = security.VolumeMax ?? decimal.MaxValue;
	if (requested > max)
	{
	requested = max;
	}

	return requested;
	}

	private void ReduceEntries(List<PositionEntry> entries, ref decimal volume)
	{
	var index = 0;
	while (volume > 0m && index < entries.Count)
	{
	var entry = entries[index];
	if (volume >= entry.Volume)
	{
	volume -= entry.Volume;
	entries.RemoveAt(index);
	}
	else
	{
	entry.Volume -= volume;
	volume = 0m;
	entries[index] = entry;
	}
	}
	}

	private static decimal GetTotalVolume(List<PositionEntry> entries)
	{
	var total = 0m;
	for (var i = 0; i < entries.Count; i++)
	total += entries[i].Volume;
	return total;
	}

	private static decimal GetAveragePrice(List<PositionEntry> entries)
	{
	var totalVolume = GetTotalVolume(entries);
	if (totalVolume <= 0m)
	return 0m;

	var weighted = 0m;
	for (var i = 0; i < entries.Count; i++)
	weighted += entries[i].Price * entries[i].Volume;

	return weighted / totalVolume;
	}

	private static decimal GetExtremePrice(List<PositionEntry> entries, bool forLong)
	{
	if (entries.Count == 0)
	return 0m;

	var extreme = entries[0].Price;
	for (var i = 1; i < entries.Count; i++)
	{
	var price = entries[i].Price;
	if (forLong)
	{
	if (price < extreme)
	extreme = price;
	}
	else if (price > extreme)
	{
	extreme = price;
	}
	}

	return extreme;
	}

	private decimal CalculatePipSize()
	{
	var security = Security;
	if (security == null)
	return 0m;

	var step = security.PriceStep ?? 0m;
	if (step <= 0m)
	return 0m;

	var decimals = security.Decimals;
	if (decimals == 3 || decimals == 5)
	return step * 10m;

	return step;
	}

	private enum SignalDirection
	{
	None,
	Buy,
	Sell
	}

	/// <summary>
	/// Signal selector for the strategy.
	/// </summary>
	public enum SignalMode
	{
	/// <summary>
	/// Use moving average crossovers in a contrarian fashion.
	/// </summary>
	CrossMa,

	/// <summary>
	/// Follow trend direction using moving averages with stochastic confirmation.
	/// </summary>
	Trend
	}

	private sealed class PositionEntry
	{
	public PositionEntry(decimal price, decimal volume)
	{
	Price = price;
	Volume = volume;
	}

	public decimal Price { get; }

	public decimal Volume { get; set; }
	}
}
