using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pattern-learning strategy that emulates the "The Enchantress" virtual order scoring model.
/// </summary>
public class TheEnchantressStrategy : Strategy
{
	private const int PatternLength = 7;

	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _useRiskMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _virtualStopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _virtualTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<char> _patternWindow = new();
	private readonly List<VirtualOrder> _virtualBuyOrders = new();
	private readonly List<VirtualOrder> _virtualSellOrders = new();
	private readonly Dictionary<string, PatternStats> _patternStats = new();

	private DateTimeOffset? _lastTradeCandleTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="TheEnchantressStrategy"/> class.
	/// </summary>
	public TheEnchantressStrategy()
	{
		_lotSize = Param(nameof(LotSize), 0.01m)
			.SetNotNegative()
			.SetDisplay("Lot Size")
			.SetCanOptimize(true);

		_useRiskMoneyManagement = Param(nameof(UseRiskMoneyManagement), true)
			.SetDisplay("Use Risk Money Management");

		_riskPercent = Param(nameof(RiskPercent), 15m)
			.SetNotNegative()
			.SetDisplay("Risk Percent")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 60m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)")
			.SetCanOptimize(true);

		_virtualStopLoss = Param(nameof(VirtualStopLoss), 55m)
			.SetNotNegative()
			.SetDisplay("Virtual Stop Loss (pips)")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 19m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)")
			.SetCanOptimize(true);

		_virtualTakeProfit = Param(nameof(VirtualTakeProfit), 25m)
			.SetNotNegative()
			.SetDisplay("Virtual Take Profit (pips)")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type");
	}

	/// <summary>
	/// Fixed lot size used when risk-based sizing is disabled.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Enable risk-based position sizing.
	/// </summary>
	public bool UseRiskMoneyManagement
	{
		get => _useRiskMoneyManagement.Value;
		set => _useRiskMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of equity used when <see cref="UseRiskMoneyManagement"/> is true.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Real stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for the virtual scoring layer in pips.
	/// </summary>
	public decimal VirtualStopLoss
	{
		get => _virtualStopLoss.Value;
		set => _virtualStopLoss.Value = value;
	}

	/// <summary>
	/// Real take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the virtual scoring layer in pips.
	/// </summary>
	public decimal VirtualTakeProfit
	{
		get => _virtualTakeProfit.Value;
		set => _virtualTakeProfit.Value = value;
	}

	/// <summary>
	/// Main candle series used by the strategy.
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		CloseVirtualOrders(candle);

		if (candle.OpenTime.DayOfWeek == DayOfWeek.Friday)
		return;

		if (_patternWindow.Count >= PatternLength)
		{
		var currentPattern = new string(_patternWindow.ToArray());
		TryExecuteSignals(candle, currentPattern);
		}

		AppendPattern(candle);
	}

	private void AppendPattern(ICandleMessage candle)
	{
	var digit = GetPatternDigit(candle);

	if (_patternWindow.Count == PatternLength)
	_patternWindow.RemoveAt(0);

	_patternWindow.Add(digit);

	if (_patternWindow.Count < PatternLength)
	return;

	var pattern = new string(_patternWindow.ToArray());
	EnsurePatternExists(pattern);

	var entryPrice = candle.ClosePrice;

	var buyStop = VirtualStopLoss > 0m ? entryPrice - GetPriceDifference(VirtualStopLoss) : 0m;
	var buyTake = VirtualTakeProfit > 0m ? entryPrice + GetPriceDifference(VirtualTakeProfit) : 0m;

	_virtualBuyOrders.Add(new VirtualOrder(pattern, entryPrice, buyStop, buyTake, true));

	var sellStop = VirtualStopLoss > 0m ? entryPrice + GetPriceDifference(VirtualStopLoss) : 0m;
	var sellTake = VirtualTakeProfit > 0m ? entryPrice - GetPriceDifference(VirtualTakeProfit) : 0m;

	_virtualSellOrders.Add(new VirtualOrder(pattern, entryPrice, sellStop, sellTake, false));
	}

	private void TryExecuteSignals(ICandleMessage candle, string currentPattern)
	{
	var bullishLeaders = GetTopPatterns(stats => stats.BullishScore);
	var bearishLeaders = GetTopPatterns(stats => stats.BearishScore);

	var canBuy = bullishLeaders.Contains(currentPattern);
	var canSell = bearishLeaders.Contains(currentPattern);

	if (canBuy)
	ExecuteEntry(candle, true);

	if (canSell)
	ExecuteEntry(candle, false);
	}

	private void ExecuteEntry(ICandleMessage candle, bool isBuy)
	{
	if (Security is null)
	return;

	if (_lastTradeCandleTime == candle.OpenTime)
	return;

	var volume = CalculateOrderVolume();
	if (volume <= 0m)
	return;

	if (isBuy)
	{
	BuyMarket(volume);
	}
	else
	{
	SellMarket(volume);
	}

	_lastTradeCandleTime = candle.OpenTime;

	var takeDistance = GetPriceDifference(TakeProfit);
	var stopDistance = GetPriceDifference(StopLoss);

	if (takeDistance > 0m)
	SetTakeProfit(takeDistance, candle.ClosePrice, Position);

	if (stopDistance > 0m)
	SetStopLoss(stopDistance, candle.ClosePrice, Position);
	}

	private void CloseVirtualOrders(ICandleMessage candle)
	{
	for (var i = _virtualBuyOrders.Count - 1; i >= 0; i--)
	{
	var order = _virtualBuyOrders[i];

	var closed = false;

	if (order.TakeProfit > 0m && candle.HighPrice >= order.TakeProfit)
	{
	UpdatePatternStats(order.Pattern, true, true);
	closed = true;
	}
	else if (order.StopLoss > 0m && candle.LowPrice <= order.StopLoss)
	{
	UpdatePatternStats(order.Pattern, true, false);
	closed = true;
	}

	if (closed)
	_virtualBuyOrders.RemoveAt(i);
	}

	for (var i = _virtualSellOrders.Count - 1; i >= 0; i--)
	{
	var order = _virtualSellOrders[i];

	var closed = false;

	if (order.TakeProfit > 0m && candle.LowPrice <= order.TakeProfit)
	{
	UpdatePatternStats(order.Pattern, false, true);
	closed = true;
	}
	else if (order.StopLoss > 0m && candle.HighPrice >= order.StopLoss)
	{
	UpdatePatternStats(order.Pattern, false, false);
	closed = true;
	}

	if (closed)
	_virtualSellOrders.RemoveAt(i);
	}
	}

	private HashSet<string> GetTopPatterns(Func<PatternStats, int> selector)
	{
	return _patternStats
	.Where(pair => selector(pair.Value) >= 1)
	.OrderByDescending(pair => selector(pair.Value))
	.ThenBy(pair => pair.Key, StringComparer.Ordinal)
	.Take(10)
	.Select(pair => pair.Key)
	.ToHashSet(StringComparer.Ordinal);
	}

	private void UpdatePatternStats(string pattern, bool isBullish, bool success)
	{
	if (!_patternStats.TryGetValue(pattern, out var stats))
	{
	stats = new PatternStats();
	_patternStats[pattern] = stats;
	}

	var delta = success ? 1 : -3;

	if (isBullish)
	{
	stats.BullishScore += delta;
	}
	else
	{
	stats.BearishScore += delta;
	}
	}

	private void EnsurePatternExists(string pattern)
	{
	if (_patternStats.ContainsKey(pattern))
	return;

	_patternStats.Add(pattern, new PatternStats());
	}

	private char GetPatternDigit(ICandleMessage candle)
	{
	var isBearish = candle.OpenPrice >= candle.ClosePrice;
	var high = candle.HighPrice;
	var low = candle.LowPrice;

	if (high <= 0m)
	return isBearish ? '0' : '5';

	var ratio = Math.Abs(100m - (low * 100m / high));

	if (isBearish)
	{
	if (ratio > 0m && ratio <= 0.04m)
	return '0';

	if (ratio > 0.04m && ratio <= 0.15m)
	return '1';

	if (ratio > 0.15m && ratio <= 0.25m)
	return '2';

	if (ratio > 0.25m && ratio <= 0.40m)
	return '3';

	return '4';
	}

	if (ratio > 0m && ratio <= 0.04m)
	return '5';

	if (ratio > 0.04m && ratio <= 0.15m)
	return '6';

	if (ratio > 0.15m && ratio <= 0.25m)
	return '7';

	if (ratio > 0.25m && ratio <= 0.40m)
	return '8';

	return '9';
	}

	private decimal CalculateOrderVolume()
	{
	var volume = LotSize;

	if (UseRiskMoneyManagement && RiskPercent > 0m)
	{
	var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

	if (portfolioValue > 0m)
	{
	var riskVolume = portfolioValue / 100000m * RiskPercent;
	if (riskVolume > 0m)
	volume = riskVolume;
	}
	}

	var security = Security;
	if (security is not null)
	{
	var step = security.VolumeStep ?? 0m;
	if (step > 0m)
	volume = Math.Round(volume / step) * step;

	var minVolume = security.MinVolume ?? 0m;
	if (minVolume > 0m && volume < minVolume)
	volume = minVolume;

	var maxVolume = security.MaxVolume ?? 0m;
	if (maxVolume > 0m && volume > maxVolume)
	volume = maxVolume;
	}

	return volume;
	}

	private decimal GetPriceDifference(decimal pips)
	{
	if (pips <= 0m)
	return 0m;

	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	step = 0.0001m;

	return pips * step;
	}

	private sealed class VirtualOrder
	{
	public VirtualOrder(string pattern, decimal entryPrice, decimal stopLoss, decimal takeProfit, bool isBuy)
	{
	Pattern = pattern;
	EntryPrice = entryPrice;
	StopLoss = stopLoss;
	TakeProfit = takeProfit;
	IsBuy = isBuy;
	}

	public string Pattern { get; }

	public decimal EntryPrice { get; }

	public decimal StopLoss { get; }

	public decimal TakeProfit { get; }

	public bool IsBuy { get; }
	}

	private sealed class PatternStats
	{
	public int BullishScore { get; set; }

	public int BearishScore { get; set; }
	}
}
