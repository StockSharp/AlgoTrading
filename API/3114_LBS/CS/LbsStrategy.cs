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
/// London Breakout Strategy (LBS) ported from the MetaTrader expert.
/// Places stop orders around the recent candle extremes at configured hours.
/// Replicates fixed lot and risk-based position sizing with trailing protection.
/// </summary>
public class LbsStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<MoneyManagementModes> _moneyMode;
	private readonly StrategyParam<decimal> _volumeOrRisk;
	private readonly StrategyParam<int> _hour1;
	private readonly StrategyParam<int> _hour2;
	private readonly StrategyParam<int> _hour3;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopOrder;
	private decimal? _stopPrice;
	private bool _stopForLong;
	private decimal? _pendingLongStopPrice;
	private decimal? _pendingShortStopPrice;

	/// <summary>
	/// Stop-loss distance expressed in pips. A value of zero disables protective stops.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips. Zero disables trailing.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pip distance before the trailing stop moves. Must be positive when trailing is enabled.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Money management mode controlling how the volume parameter is interpreted.
	/// </summary>
	public MoneyManagementModes MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Fixed volume (lots) or risk percent depending on <see cref="MoneyMode"/>.
	/// </summary>
	public decimal VolumeOrRisk
	{
		get => _volumeOrRisk.Value;
		set => _volumeOrRisk.Value = value;
	}

	/// <summary>
	/// First hour (0-23). Set to zero to disable.
	/// </summary>
	public int Hour1
	{
		get => _hour1.Value;
		set => _hour1.Value = value;
	}

	/// <summary>
	/// Second hour (0-23). Set to zero to disable.
	/// </summary>
	public int Hour2
	{
		get => _hour2.Value;
		set => _hour2.Value = value;
	}

	/// <summary>
	/// Third hour (0-23). Set to zero to disable.
	/// </summary>
	public int Hour3
	{
		get => _hour3.Value;
		set => _hour3.Value = value;
	}

	/// <summary>
	/// Candle type used for breakout detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults that mirror the original expert.
	/// </summary>
	public LbsStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips. Zero disables the stop.", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Risk Management")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 15)
		.SetDisplay("Trailing Step (pips)", "Additional pips required before the trailing stop moves.", "Risk Management")
		.SetCanOptimize(true);

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementModes.FixedLot)
		.SetDisplay("Money Mode", "Use fixed lots or risk percentage for sizing.", "Money Management");

		_volumeOrRisk = Param(nameof(VolumeOrRisk), 1m)
		.SetDisplay("Volume / Risk %", "Fixed lot size or risk percentage depending on the money mode.", "Money Management")
		.SetCanOptimize(true);

		_hour1 = Param(nameof(Hour1), 10)
		.SetDisplay("Hour 1", "First hour (0 disables).", "Timing")
		.SetCanOptimize(true);

		_hour2 = Param(nameof(Hour2), 11)
		.SetDisplay("Hour 2", "Second hour (0 disables).", "Timing")
		.SetCanOptimize(true);

		_hour3 = Param(nameof(Hour3), 12)
		.SetDisplay("Hour 3", "Third hour (0 disables).", "Timing")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe used for breakout calculations.", "General");
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

		_bestBid = null;
		_bestAsk = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopOrder = null;
		_stopPrice = null;
		_stopForLong = false;
		_pendingLongStopPrice = null;
		_pendingShortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_pipSize = CalculatePipSize();
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 1m;

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
				_bestBid = bid;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
				_bestAsk = ask;
		}

		EnsureInitialStop();

		if (TrailingStopPips <= 0)
			return;

		decimal? price = null;

		if (Position > 0 && _bestBid is decimal bid)
			price = bid;
		else if (Position < 0 && _bestAsk is decimal ask)
			price = ask;

		if (price is decimal validPrice && validPrice > 0m)
			ApplyTrailing(validPrice);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		EnsureInitialStop();

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EnsureInitialStop();

		if (Position != 0m)
			return;

		var hour = candle.CloseTime.Hour;
		if (!IsTradingHour(hour))
			return;

		var ask = _bestAsk ?? candle.ClosePrice;
		var bid = _bestBid ?? candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
		{
			LogWarning("Skip breakout placement because bid/ask are unavailable. Close price={0}", candle.ClosePrice);
			return;
		}

		var spread = Math.Max(ask - bid, 0m);
		var freezeLevel = Math.Max(10m * _pipSize, spread * 3m);
		var stopLevel = Math.Max(10m * _pipSize, spread * 3m);

		var rawBuyPrice = Math.Max(candle.HighPrice, ask + freezeLevel);
		var buyPrice = AlignPrice(rawBuyPrice, true);

		var rawSellPrice = Math.Min(candle.LowPrice, bid - freezeLevel);
		var sellPrice = AlignPrice(rawSellPrice, false);

		if (buyPrice <= 0m && sellPrice <= 0m)
		{
			LogWarning("Both breakout prices are invalid. Buy={0}, Sell={1}", buyPrice, sellPrice);
			return;
		}

		decimal? buyStopLoss = GetStopPriceForLong(buyPrice, stopLevel);
		decimal? sellStopLoss = GetStopPriceForShort(sellPrice, stopLevel);

		var buyVolume = CalculateOrderVolume(buyPrice, buyStopLoss);
		var sellVolume = CalculateOrderVolume(sellPrice, sellStopLoss);

		PlaceBuyStopOrder(buyPrice, buyVolume, buyStopLoss);
		PlaceSellStopOrder(sellPrice, sellVolume, sellStopLoss);
	}

	private void PlaceBuyStopOrder(decimal price, decimal volume, decimal? stopLoss)
	{
		CancelOrderIfActive(_buyStopOrder);
		_buyStopOrder = null;

		if (price <= 0m || volume <= 0m)
		{
			_pendingLongStopPrice = null;
			return;
		}

		_buyStopOrder = BuyStop(volume, price);
		_pendingLongStopPrice = stopLoss;
	}

	private void PlaceSellStopOrder(decimal price, decimal volume, decimal? stopLoss)
	{
		CancelOrderIfActive(_sellStopOrder);
		_sellStopOrder = null;

		if (price <= 0m || volume <= 0m)
		{
			_pendingShortStopPrice = null;
			return;
		}

		_sellStopOrder = SellStop(volume, price);
		_pendingShortStopPrice = stopLoss;
	}

	private void EnsureInitialStop()
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		{
			_pendingLongStopPrice = null;
			_pendingShortStopPrice = null;
			ResetStop();
			return;
		}

		if (_stopOrder != null)
			return;

		if (Position > 0 && _pendingLongStopPrice is decimal longStop && longStop > 0m)
		{
			var aligned = AlignPrice(longStop, false);
			PlaceProtectiveStop(Sides.Sell, aligned, positionVolume, true);
			_pendingLongStopPrice = null;
		}
		else if (Position < 0 && _pendingShortStopPrice is decimal shortStop && shortStop > 0m)
		{
			var aligned = AlignPrice(shortStop, true);
			PlaceProtectiveStop(Sides.Buy, aligned, positionVolume, false);
			_pendingShortStopPrice = null;
		}
	}

	private void ApplyTrailing(decimal marketPrice)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		{
			ResetStop();
			return;
		}

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (trailingStop <= 0m || trailingStep <= 0m)
			return;

		if (Position > 0)
		{
			var profit = marketPrice - PositionPrice;
			if (profit <= trailingStop + trailingStep)
				return;

			var targetPrice = AlignPrice(marketPrice - trailingStop, false);
			if (targetPrice <= 0m)
				return;

			if (_stopOrder == null || !_stopForLong || _stopPrice is null || targetPrice > _stopPrice.Value + _pipSize / 2m)
				PlaceProtectiveStop(Sides.Sell, targetPrice, positionVolume, true);
		}
		else
		{
			var profit = PositionPrice - marketPrice;
			if (profit <= trailingStop + trailingStep)
				return;

			var targetPrice = AlignPrice(marketPrice + trailingStop, true);
			if (targetPrice <= 0m)
				return;

			if (_stopOrder == null || _stopForLong || _stopPrice is null || targetPrice < _stopPrice.Value - _pipSize / 2m)
				PlaceProtectiveStop(Sides.Buy, targetPrice, positionVolume, false);
		}
	}

	private void PlaceProtectiveStop(Sides side, decimal price, decimal volume, bool forLong)
	{
		if (price <= 0m || volume <= 0m)
			return;

		CancelOrderIfActive(_stopOrder);

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);

		_stopPrice = price;
		_stopForLong = forLong;
	}

	private decimal? GetStopPriceForLong(decimal entryPrice, decimal stopLevel)
	{
		if (StopLossPips <= 0)
			return null;

		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance <= 0m)
			return null;

		var distance = Math.Max(stopDistance, stopLevel);
		var price = entryPrice - distance;

		return price > 0m ? AlignPrice(price, false) : null;
	}

	private decimal? GetStopPriceForShort(decimal entryPrice, decimal stopLevel)
	{
		if (StopLossPips <= 0)
			return null;

		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance <= 0m)
			return null;

		var distance = Math.Max(stopDistance, stopLevel);
		var price = entryPrice + distance;

		return price > 0m ? AlignPrice(price, true) : null;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal? stopPrice)
	{
		if (entryPrice <= 0m)
			return 0m;

		return MoneyMode switch
		{
			MoneyManagementModes.FixedLot => NormalizeVolume(VolumeOrRisk),
			MoneyManagementModes.RiskPercent => CalculateRiskVolume(entryPrice, stopPrice),
			_ => 0m
		};
	}

	private decimal CalculateRiskVolume(decimal entryPrice, decimal? stopPrice)
	{
		if (stopPrice is null)
			return 0m;

		var riskPercent = VolumeOrRisk;
		if (riskPercent <= 0m)
			return 0m;

		var portfolioValue = GetPortfolioValue();
		if (portfolioValue <= 0m)
			return 0m;

		var riskAmount = portfolioValue * riskPercent / 100m;
		if (riskAmount <= 0m)
			return 0m;

		var stopDistance = Math.Abs(entryPrice - stopPrice.Value);
		if (stopDistance <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = Security?.StepPrice ?? priceStep;
		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return 0m;

		var riskPerVolume = steps * stepPrice;
		if (riskPerVolume <= 0m)
			return 0m;

		var rawVolume = riskAmount / riskPerVolume;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			var stepsCount = Math.Ceiling(volume / step);
			volume = stepsCount * step;
		}

		if (Security?.VolumeMin is decimal min && min > 0m && volume < min)
			volume = min;

		if (Security?.VolumeMax is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue ?? 0m;
		if (current > 0m)
			return current;

		var begin = Portfolio?.BeginValue ?? 0m;
		return begin > 0m ? begin : current;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private decimal AlignPrice(decimal price, bool roundUp)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var ratio = price / step;
		var rounded = roundUp ? Math.Ceiling(ratio) : Math.Floor(ratio);
		return rounded * step;
	}

	private bool IsTradingHour(int hour)
	{
		if (Hour1 > 0 && hour == Hour1)
			return true;

		if (Hour2 > 0 && hour == Hour2)
			return true;

		if (Hour3 > 0 && hour == Hour3)
			return true;

		return false;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order is null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);
	}

	private void ResetStop()
	{
		if (_stopOrder != null)
		{
			CancelOrderIfActive(_stopOrder);
			_stopOrder = null;
		}

		_stopPrice = null;
		_stopForLong = false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order is null)
			return;

		if (order == _buyStopOrder)
		{
			CancelOrderIfActive(_sellStopOrder);
			_pendingShortStopPrice = null;
		}
		else if (order == _sellStopOrder)
		{
			CancelOrderIfActive(_buyStopOrder);
			_pendingLongStopPrice = null;
		}

		EnsureInitialStop();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == _buyStopOrder && order.State.IsFinal())
			_buyStopOrder = null;
		else if (order == _sellStopOrder && order.State.IsFinal())
			_sellStopOrder = null;
		else if (order == _stopOrder && order.State.IsFinal())
		{
			_stopOrder = null;
			_stopPrice = null;
			_stopForLong = false;
		}
	}

	public enum MoneyManagementModes
	{
		/// <summary>
		/// Use a fixed trading volume.
		/// </summary>
		FixedLot,

		/// <summary>
		/// Risk a percentage of the portfolio based on stop distance.
		/// </summary>
		RiskPercent
	}
}
