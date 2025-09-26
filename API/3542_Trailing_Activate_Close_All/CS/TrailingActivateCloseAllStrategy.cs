namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Defines how often the trailing logic is evaluated.
/// </summary>
public enum TrailingMode
{
	/// <summary>
	/// Recalculate protection on every tick.
	/// </summary>
	EveryTick,

	/// <summary>
	/// Recalculate protection only when a candle closes.
	/// </summary>
	NewBar
}

/// <summary>
/// Risk management strategy that mirrors the MetaTrader expert "Trailing Activate Close All".
/// It attaches protective orders to existing positions, applies trailing logic and can liquidate all trades on a profit target.
/// </summary>
public class TrailingActivateCloseAllStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
	?? TryGetField("MinStopPrice")
	?? TryGetField("StopPrice")
	?? TryGetField("StopDistance");

	private static readonly Level1Fields? FreezeLevelField = TryGetField("FreezeLevel")
	?? TryGetField("FreezePrice")
	?? TryGetField("FreezeDistance");

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TrailingMode> _trailingMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingActivatePoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _targetProfit;
	private readonly StrategyParam<decimal> _freezeCoefficient;
	private readonly StrategyParam<bool> _detailedLogging;

	private decimal _pointValue;
	private decimal _priceStep;
	private decimal _volumeStep;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _stopLevel;
	private decimal? _freezeLevel;
	private Order _stopOrder;
	private Order _takeProfitOrder;
	private bool _isClosingAll;

	/// <summary>
	/// Candle series used when trailing on new bar close.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Frequency of trailing calculations.
	/// </summary>
	public TrailingMode TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum profit required before the trailing stop can move.
	/// </summary>
	public decimal TrailingActivatePoints
	{
		get => _trailingActivatePoints.Value;
		set => _trailingActivatePoints.Value = value;
	}

	/// <summary>
	/// Distance between the market price and the trailing stop.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum improvement before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Unrealised profit level that triggers closing all positions.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfit.Value;
		set => _targetProfit.Value = value;
	}

	/// <summary>
	/// Multiplier applied when exchange freeze/stop levels are not provided.
	/// </summary>
	public decimal FreezeCoefficient
	{
		get => _freezeCoefficient.Value;
		set => _freezeCoefficient.Value = value;
	}

	/// <summary>
	/// Enables verbose logging about trailing and forced exits.
	/// </summary>
	public bool DetailedLogging
	{
		get => _detailedLogging.Value;
		set => _detailedLogging.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TrailingActivateCloseAllStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for bar-based trailing.", "General");

		_trailingMode = Param(nameof(TrailingMode), TrailingMode.NewBar)
		.SetDisplay("Trailing Mode", "Frequency of trailing calculations.", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Distance to stop-loss in MetaTrader points.", "Protection")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Distance to take-profit in MetaTrader points.", "Protection")
		.SetCanOptimize(true);

		_trailingActivatePoints = Param(nameof(TrailingActivatePoints), 70m)
		.SetNotNegative()
		.SetDisplay("Trailing Activate (points)", "Profit in points required before trailing begins.", "Trailing")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (points)", "Trailing stop distance in points.", "Trailing")
		.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (points)", "Minimum improvement in points before moving the stop again.", "Trailing")
		.SetCanOptimize(true);

		_targetProfit = Param(nameof(TargetProfit), 5m)
		.SetNotNegative()
		.SetDisplay("Target Profit", "Profit level that closes all positions.", "Targets")
		.SetCanOptimize(true);

		_freezeCoefficient = Param(nameof(FreezeCoefficient), 1m)
		.SetNotNegative()
		.SetDisplay("Freeze Coefficient", "Multiplier for exchange freeze/stop distances.", "Execution");

		_detailedLogging = Param(nameof(DetailedLogging), true)
		.SetDisplay("Detailed Logging", "Write detailed log messages for adjustments.", "Diagnostics");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);

		if (TrailingMode == TrailingMode.NewBar)
		{
			yield return (Security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointValue = 0m;
		_priceStep = 0m;
		_volumeStep = 0m;
		_bestBid = null;
		_bestAsk = null;
		_stopLevel = null;
		_freezeLevel = null;
		_stopOrder = null;
		_takeProfitOrder = null;
		_isClosingAll = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			throw new InvalidOperationException("Security must be specified.");
		}

		if (Portfolio == null)
		{
			throw new InvalidOperationException("Portfolio must be specified.");
		}

		if (TrailingStopPoints > 0m && TrailingStepPoints <= 0m)
		{
			LogError("Trailing Step must be greater than zero when trailing is enabled.");
			Stop();
			return;
		}

		_pointValue = CalculatePointValue();
		_priceStep = Security.PriceStep ?? 0m;
		_volumeStep = Security.VolumeStep ?? 0m;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		if (TrailingMode == TrailingMode.NewBar)
		{
			var subscription = SubscribeCandles(CandleType);
			subscription.Bind(ProcessCandle).Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}
		else
		{
			var area = CreateChartArea();
			if (area != null)
			{
				DrawOwnTrades(area);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_isClosingAll = false;
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_bestAsk = ask;

		if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopValue))
		_stopLevel = ToDecimal(stopValue);

		if (FreezeLevelField is Level1Fields freezeField && message.Changes.TryGetValue(freezeField, out var freezeValue))
		_freezeLevel = ToDecimal(freezeValue);

		if (TrailingMode == TrailingMode.EveryTick)
		UpdateProtectionAndTrailing();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateProtectionAndTrailing(candle.ClosePrice);
	}

	private void UpdateProtectionAndTrailing(decimal? candlePrice = null)
	{
		if (!IsFormedAndOnline())
		return;

		var signedVolume = (decimal)Position;
		if (signedVolume == 0m)
		return;

		var isLong = signedVolume > 0m;
		var marketPrice = GetMarketPrice(isLong, candlePrice);
		if (marketPrice is not decimal price)
		return;

		if (HandleTargetProfit(price))
		return;

		EnsureProtection(isLong);

		if (TrailingStopPoints > 0m)
		ApplyTrailing(isLong, price);
	}

	private bool HandleTargetProfit(decimal marketPrice)
	{
		if (TargetProfit <= 0m)
		return false;

		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
		return false;

		var signedVolume = (decimal)Position;
		var profit = (marketPrice - entryPrice) * signedVolume;

		if (profit < TargetProfit)
		return false;

		if (_isClosingAll)
		return true;

		_isClosingAll = true;

		if (DetailedLogging)
		LogInfo(string.Format(CultureInfo.InvariantCulture, "Target profit reached. Closing all positions. Profit={0:F2}", profit));

		CancelActiveOrders();
		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);
		ClosePosition(signedVolume);
		return true;
	}

	private void EnsureProtection(bool isLong)
	{
		var volume = NormalizeVolume(Math.Abs((decimal)Position));
		if (volume <= 0m)
		return;

		var referencePrice = GetReferencePrice(isLong);
		if (referencePrice is not decimal price)
		return;

		var minDistance = GetMinimalDistance();

		if (StopLossPoints > 0m)
		{
			var stopDistance = Math.Max(StopLossPoints * _pointValue, minDistance);
			var stopPrice = isLong ? price - stopDistance : price + stopDistance;
			UpdateStopOrder(stopPrice, volume, isLong);
		}
		else
		{
			CancelProtectiveOrder(ref _stopOrder);
		}

		if (TakeProfitPoints > 0m)
		{
			var takeDistance = Math.Max(TakeProfitPoints * _pointValue, minDistance);
			var takePrice = isLong ? price + takeDistance : price - takeDistance;
			UpdateTakeProfitOrder(takePrice, volume, isLong);
		}
		else
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
		}
	}

	private void ApplyTrailing(bool isLong, decimal marketPrice)
	{
		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
		return;

		var trailingDistance = TrailingStopPoints * _pointValue;
		if (trailingDistance <= 0m)
		return;

		var minDistance = GetMinimalDistance();
		trailingDistance = Math.Max(trailingDistance, minDistance);

		var stepDistance = TrailingStepPoints * _pointValue;
		if (stepDistance <= 0m)
		return;

		var activationDistance = TrailingActivatePoints * _pointValue;
		var takeProfitPrice = _takeProfitOrder?.Price;

		if (isLong)
		{
			var profit = marketPrice - entryPrice;
			if (profit < trailingDistance + stepDistance)
			return;

			if (marketPrice - trailingDistance < entryPrice + activationDistance)
			return;

			var newStop = marketPrice - trailingDistance;
			if (_stopOrder is Order stopOrder && stopOrder.Price >= newStop - stepDistance)
			return;

			var bidPrice = _bestBid ?? marketPrice;
			if (takeProfitPrice is decimal tp && bidPrice >= tp - minDistance)
			return;

			UpdateStopOrder(newStop, NormalizeVolume(Math.Abs((decimal)Position)), true);

			if (DetailedLogging)
			LogInfo(string.Format(CultureInfo.InvariantCulture, "Trailing stop for long position moved to {0:F5}.", newStop));
		}
		else
		{
			var profit = entryPrice - marketPrice;
			if (profit < trailingDistance + stepDistance)
			return;

			if (marketPrice + trailingDistance > entryPrice - activationDistance)
			return;

			var newStop = marketPrice + trailingDistance;
			if (_stopOrder is Order stopOrder && stopOrder.Price <= newStop + stepDistance)
			return;

			var askPrice = _bestAsk ?? marketPrice;
			if (takeProfitPrice is decimal tp && askPrice <= tp + minDistance)
			return;

			UpdateStopOrder(newStop, NormalizeVolume(Math.Abs((decimal)Position)), false);

			if (DetailedLogging)
			LogInfo(string.Format(CultureInfo.InvariantCulture, "Trailing stop for short position moved to {0:F5}.", newStop));
		}
	}

	private decimal? GetReferencePrice(bool isLong)
	{
		return isLong
		? _bestAsk ?? _bestBid ?? Security?.LastPrice ?? PositionPrice
		: _bestBid ?? _bestAsk ?? Security?.LastPrice ?? PositionPrice;
	}

	private decimal? GetMarketPrice(bool isLong, decimal? candlePrice)
	{
		return isLong
		? _bestBid ?? candlePrice ?? Security?.LastPrice ?? PositionPrice
		: _bestAsk ?? candlePrice ?? Security?.LastPrice ?? PositionPrice;
	}

	private void ClosePosition(decimal volume)
	{
		if (volume == 0m)
		return;

		if (volume > 0m)
		SellMarket(volume);
		else
		BuyMarket(-volume);
	}

	private void UpdateStopOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice is not decimal price || price <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		if (_stopOrder == null)
		{
			_stopOrder = isLong ? SellStop(volume, normalizedPrice) : BuyStop(volume, normalizedPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_stopOrder = null;
			UpdateStopOrder(price, volume, isLong);
			return;
		}

		if (_stopOrder.Price != normalizedPrice || _stopOrder.Volume != volume)
		ReRegisterOrder(_stopOrder, normalizedPrice, volume);
	}

	private void UpdateTakeProfitOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice is not decimal price || price <= 0m)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		if (_takeProfitOrder == null)
		{
			_takeProfitOrder = isLong ? SellLimit(volume, normalizedPrice) : BuyLimit(volume, normalizedPrice);
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(price, volume, isLong);
			return;
		}

		if (_takeProfitOrder.Price != normalizedPrice || _takeProfitOrder.Volume != volume)
		ReRegisterOrder(_takeProfitOrder, normalizedPrice, volume);
	}

	private void CancelProtectiveOrder(ref Order order)
	{
		var current = order;
		if (current == null)
		return;

		if (current.State == OrderStates.Active)
		CancelOrder(current);

		order = null;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (_volumeStep <= 0m)
		return volume;

		var steps = Math.Round(volume / _volumeStep, MidpointRounding.AwayFromZero);
		return steps * _volumeStep;
	}

	private decimal GetMinimalDistance()
	{
		var coeff = FreezeCoefficient;
		var freeze = _freezeLevel ?? 0m;
		var stops = _stopLevel ?? 0m;

		if (freeze <= 0m)
		{
			if (coeff > 0m && _bestBid is decimal bid && _bestAsk is decimal ask)
			freeze = Math.Abs(ask - bid) * coeff;
		}
		else if (coeff > 0m)
		{
			freeze *= coeff;
		}

		if (stops <= 0m)
		{
			if (coeff > 0m && _bestBid is decimal bid2 && _bestAsk is decimal ask2)
			stops = Math.Abs(ask2 - bid2) * coeff;
		}
		else if (coeff > 0m)
		{
			stops *= coeff;
		}

		var level = Math.Max(freeze, stops);

		if (level <= 0m && _priceStep > 0m)
		level = _priceStep;

		return level;
	}

	private decimal CalculatePointValue()
	{
		if (Security == null)
		return 0.0001m;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = Security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
		step = 0.0001m;

		var multiplier = 1m;
		var digits = Security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
		multiplier = 10m;

		return step * multiplier;
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long lng => lng,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible, CultureInfo.InvariantCulture),
			_ => null
		};
	}
}
