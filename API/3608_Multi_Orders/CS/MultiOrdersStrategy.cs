using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the MetaTrader MultiOrders expert that can batch-submit multiple orders
/// and automatically reacts to tight spreads using best bid/ask updates.
/// </summary>
public class MultiOrdersStrategy : Strategy
{
	private readonly StrategyParam<int> _buyOrdersCount;
	private readonly StrategyParam<int> _sellOrdersCount;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<decimal> _baseVolume;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private bool _buyBatchRequested;
	private bool _sellBatchRequested;

	/// <summary>
	/// Initializes <see cref="MultiOrdersStrategy"/>.
	/// </summary>
	public MultiOrdersStrategy()
	{
		_buyOrdersCount = Param(nameof(BuyOrdersCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Buy orders", "Number of market buy orders submitted by a manual batch trigger.", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_sellOrdersCount = Param(nameof(SellOrdersCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sell orders", "Number of market sell orders submitted by a manual batch trigger.", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_riskPercentage = Param(nameof(RiskPercentage), 1m)
			.SetDisplay("Risk percentage", "Portfolio percentage used to derive the default trade volume.", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss (points)", "Distance from entry for the protective stop, expressed in price steps.", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (points)", "Target distance from entry expressed in price steps.", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(50, 800, 50);

		_slippagePoints = Param(nameof(SlippagePoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slippage (points)", "Maximum spread (in price steps) tolerated before automatic entries can trigger.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base volume", "Fallback order size used when portfolio information is unavailable.", "Risk management");
	}

	/// <summary>
	/// Number of market buy orders sent when <see cref="TriggerBuyBatch"/> is called.
	/// </summary>
	public int BuyOrdersCount
	{
		get => _buyOrdersCount.Value;
		set => _buyOrdersCount.Value = value;
	}

	/// <summary>
	/// Number of market sell orders sent when <see cref="TriggerSellBatch"/> is called.
	/// </summary>
	public int SellOrdersCount
	{
		get => _sellOrdersCount.Value;
		set => _sellOrdersCount.Value = value;
	}

	/// <summary>
	/// Percentage of the portfolio used to estimate the trade volume.
	/// </summary>
	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread expressed in price steps.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Fallback order size when risk-based sizing cannot be computed.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_buyBatchRequested = false;
		_sellBatchRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastBid = null;
		_lastAsk = null;
		_buyBatchRequested = false;
		_sellBatchRequested = false;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawOwnTrades(area);
		}

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			priceStep = 1m;
		}

		StartProtection(
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * priceStep, UnitTypes.Absolute) : null,
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute) : null);
	}

	/// <summary>
	/// Requests a batch of buy orders. The orders are submitted on the next level1 update.
	/// </summary>
	public void TriggerBuyBatch()
	{
		_buyBatchRequested = true;
	}

	/// <summary>
	/// Requests a batch of sell orders. The orders are submitted on the next level1 update.
	/// </summary>
	public void TriggerSellBatch()
	{
		_sellBatchRequested = true;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
	if (level1 == null)
	return;

	if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
	_lastBid = Convert.ToDecimal(bidValue);

	if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
	_lastAsk = Convert.ToDecimal(askValue);

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (_buyBatchRequested)
	{
	ExecuteBatch(true);
	_buyBatchRequested = false;
	}

	if (_sellBatchRequested)
	{
	ExecuteBatch(false);
	_sellBatchRequested = false;
	}

	if (_lastBid is not decimal bid || _lastAsk is not decimal ask)
	return;

	var spread = ask - bid;
	if (spread < 0m)
	spread = Math.Abs(spread);

	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	priceStep = 1m;

	var maxSpread = SlippagePoints * priceStep;
	if (spread > maxSpread)
	return;

	var volume = CalculateOrderVolume();
	if (volume <= 0m)
	return;

	var averagePrice = (bid + ask) / 2m;

	if (averagePrice > ask)
	{
	BuyMarket(volume);
	}
	else if (averagePrice < bid)
	{
	SellMarket(volume);
	}
	}

	private void ExecuteBatch(bool isBuy)
	{
	var volume = CalculateOrderVolume();
	if (volume <= 0m)
	return;

	var count = isBuy ? BuyOrdersCount : SellOrdersCount;
	for (var i = 0; i < count; i++)
	{
	if (isBuy)
	{
	BuyMarket(volume);
	}
	else
	{
	SellMarket(volume);
	}
	}
	}

	private decimal CalculateOrderVolume()
	{
	var baseVolume = NormalizeVolume(BaseVolume);
	if (Portfolio == null || RiskPercentage <= 0m)
	return baseVolume;

	var equity = Portfolio.CurrentValue ?? 0m;
	if (equity <= 0m)
	equity = Portfolio.CurrentBalance;
	if (equity <= 0m)
	equity = Portfolio.BeginValue;
	if (equity <= 0m)
	return baseVolume;

	var priceStep = Security?.PriceStep ?? 0m;
	var stepPrice = Security?.StepPrice ?? priceStep;
	if (priceStep <= 0m || stepPrice <= 0m)
	return baseVolume;

	var stopSteps = StopLossPoints > 0 ? StopLossPoints : 1;
	var riskAmount = equity * RiskPercentage / 100m;
	var riskPerContract = stopSteps * stepPrice;
	if (riskPerContract <= 0m)
	return baseVolume;

	var rawVolume = riskAmount / riskPerContract;
	if (rawVolume <= 0m)
	return baseVolume;

	var normalized = NormalizeVolume(rawVolume);
	return normalized > 0m ? normalized : baseVolume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
	var volumeStep = Security?.VolumeStep ?? 1m;
	if (volumeStep <= 0m)
	volumeStep = 1m;

	var minVolume = Security?.MinVolume ?? volumeStep;
	var maxVolume = Security?.MaxVolume;

	if (volume < minVolume)
	return minVolume;

	var steps = Math.Floor(volume / volumeStep);
	var normalized = steps * volumeStep;

	if (normalized < minVolume)
	normalized = minVolume;

	if (maxVolume != null && normalized > maxVolume.Value)
	normalized = maxVolume.Value;

	return normalized;
	}
}
