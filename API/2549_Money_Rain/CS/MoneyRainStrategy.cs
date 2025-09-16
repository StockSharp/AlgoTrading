using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money Rain strategy converted from the original MQL5 expert advisor.
/// </summary>
public class MoneyRainStrategy : Strategy
{
	private enum ExitReason
	{
		None,
		StopLoss,
		TakeProfit
	}

	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _lossLimit;
	private readonly StrategyParam<bool> _fastOptimize;
	private readonly StrategyParam<DataType> _candleType;

	private DeMarker _deMarker;
	private decimal _adjustedPoint;
	private decimal _takeProfitOffset;
	private decimal _stopLossOffset;
	private decimal _lastSpreadPoints;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _activeVolume;
	private int _consecutiveLosses;
	private int _consecutiveProfits;
	private decimal _lossesVolume;
	private bool _exitOrderActive;
	private ExitReason _pendingExitReason;
	private Sides? _currentSide;

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in DeMarker-style points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in DeMarker-style points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed consecutive losses.
	/// </summary>
	public int LossLimit
	{
		get => _lossLimit.Value;
		set => _lossLimit.Value = value;
	}

	/// <summary>
	/// Enables lightweight optimisation mode that disables money management.
	/// </summary>
	public bool FastOptimize
	{
		get => _fastOptimize.Value;
		set => _fastOptimize.Value = value;
	}

	/// <summary>
	/// Candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults close to the MQL version.
	/// </summary>
	public MoneyRainStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 31)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "DeMarker indicator averaging period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 5);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Take-profit distance expressed in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 15m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 60m, 5m);

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Lot size used when no recovery is required", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_lossLimit = Param(nameof(LossLimit), 1000000)
		.SetGreaterThanZero()
		.SetDisplay("Loss Limit", "Maximum consecutive losses before trading is paused", "Risk");

		_fastOptimize = Param(nameof(FastOptimize), false)
		.SetDisplay("Fast Optimisation", "Disable adaptive position sizing during rough optimisation", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for indicator calculations", "Data");
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

		_deMarker = null;
		_adjustedPoint = 0m;
		_takeProfitOffset = 0m;
		_stopLossOffset = 0m;
		_lastSpreadPoints = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_activeVolume = 0m;
		_consecutiveLosses = 0;
		_consecutiveProfits = 0;
		_lossesVolume = 0m;
		_exitOrderActive = false;
		_pendingExitReason = ExitReason.None;
		_currentSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateOffsets();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		_deMarker = new DeMarker
		{
			Length = DeMarkerPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_deMarker, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (_adjustedPoint <= 0m)
		return;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) &&
		level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) &&
		bidObj is decimal bid &&
		askObj is decimal ask &&
		ask > bid &&
		bid > 0m)
		{
			_lastSpreadPoints = (ask - bid) / _adjustedPoint;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_deMarker == null || !_deMarker.IsFormed)
		return;

		if (Position != 0 || _exitOrderActive)
		return;

		if (LossLimit > 0 && _consecutiveLosses >= LossLimit)
		{
			LogInfo($"Trading paused after reaching loss limit of {LossLimit} consecutive losses.");
			return;
		}

		if (_adjustedPoint <= 0m)
		UpdateOffsets();

		var volume = GetTradeVolume();
		if (volume <= 0m)
		return;

		if (deMarkerValue > 0.5m)
		{
			EnterPosition(Sides.Buy, volume, candle.ClosePrice, deMarkerValue);
		}
		else
		{
			EnterPosition(Sides.Sell, volume, candle.ClosePrice, deMarkerValue);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_currentSide == null || Position == 0 || _exitOrderActive)
		return;

		var hasStop = _stopLossOffset > 0m;
		var hasTake = _takeProfitOffset > 0m;

		var hitStop = false;
		var hitTake = false;

		switch (_currentSide)
		{
		case Sides.Buy:
			hitStop = hasStop && candle.LowPrice <= _stopPrice;
			hitTake = hasTake && candle.HighPrice >= _takePrice;
			break;
		case Sides.Sell:
			hitStop = hasStop && candle.HighPrice >= _stopPrice;
			hitTake = hasTake && candle.LowPrice <= _takePrice;
			break;
	}

	if (!hitStop && !hitTake)
	return;

	_exitOrderActive = true;
	_pendingExitReason = hitStop ? ExitReason.StopLoss : ExitReason.TakeProfit;

	ClosePosition();

	var exitPrice = hitStop ? _stopPrice : _takePrice;
	LogInfo(hitStop
	? $"Stop-loss triggered near {exitPrice} (range {candle.LowPrice} - {candle.HighPrice})."
	: $"Take-profit triggered near {exitPrice} (range {candle.LowPrice} - {candle.HighPrice}).");
}

private void EnterPosition(Sides side, decimal volume, decimal referencePrice, decimal deMarkerValue)
{
	CancelActiveOrders();

	_currentSide = side;
	_exitOrderActive = false;
	_pendingExitReason = ExitReason.None;
	_entryPrice = referencePrice;
	_activeVolume = volume;

	if (side == Sides.Buy)
	{
		_stopPrice = referencePrice - _stopLossOffset;
		_takePrice = referencePrice + _takeProfitOffset;
		BuyMarket(volume);
		LogInfo($"Entered long at {referencePrice} (DeMarker={deMarkerValue:F4}) with volume {volume}.");
	}
	else
	{
		_stopPrice = referencePrice + _stopLossOffset;
		_takePrice = referencePrice - _takeProfitOffset;
		SellMarket(volume);
		LogInfo($"Entered short at {referencePrice} (DeMarker={deMarkerValue:F4}) with volume {volume}.");
	}
}

private decimal GetTradeVolume()
{
	var volume = BaseVolume;
	if (volume <= 0m)
	return 0m;

	if (FastOptimize)
	return volume;

	if (_lossesVolume <= 0.5m || _consecutiveProfits > 0)
	return volume;

	var spread = Math.Max(0m, _lastSpreadPoints);
	var denominator = TakeProfitPoints - spread;
	if (denominator <= 0m)
	return volume;

	var multiplier = _lossesVolume * (StopLossPoints + spread) / denominator;
	if (multiplier <= 0m)
	return volume;

	return volume * multiplier;
}

private void UpdateTradeStats(bool isProfit)
{
	if (isProfit)
	{
		_consecutiveLosses = 0;

		if (_consecutiveProfits > 1)
		_lossesVolume = 0m;

		_consecutiveProfits++;

		LogInfo($"Take-profit confirmed. Profit streak = {_consecutiveProfits}.");
	}
	else
	{
		_consecutiveLosses++;
		_consecutiveProfits = 0;

		if (BaseVolume > 0m)
		_lossesVolume += _activeVolume / BaseVolume;

		LogInfo($"Stop-loss confirmed. Loss streak = {_consecutiveLosses}, accumulated loss volume = {_lossesVolume:F2}.");
	}
}

protected override void OnOwnTradeReceived(MyTrade trade)
{
	base.OnOwnTradeReceived(trade);

	if (_exitOrderActive)
	{
		if (Position != 0)
		return;

		UpdateTradeStats(_pendingExitReason == ExitReason.TakeProfit);

		_exitOrderActive = false;
		_pendingExitReason = ExitReason.None;
		_currentSide = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_activeVolume = 0m;
		return;
	}

	if (_currentSide != null && Position != 0)
	{
		_entryPrice = trade.Trade.Price;
		_activeVolume = trade.Trade.Volume;
	}
}

private void UpdateOffsets()
{
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	priceStep = 0.0001m;

	var decimals = Security?.Decimals ?? 0;
	var digitsAdjust = (decimals == 3 || decimals == 5) ? 10m : 1m;

	_adjustedPoint = priceStep * digitsAdjust;
	_takeProfitOffset = TakeProfitPoints * _adjustedPoint;
	_stopLossOffset = StopLossPoints * _adjustedPoint;
}
}
