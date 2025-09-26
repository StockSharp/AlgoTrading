using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random trade generator with ATR or pip based risk management and optional breakeven handling.
/// Mirrors the MetaTrader random trader expert advisor by opening a single position at a time.
/// </summary>
public class RandomTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<LossMode> _lossMode;
	private readonly StrategyParam<decimal> _lossAtrMultiplier;
	private readonly StrategyParam<decimal> _lossPipDistance;
	private readonly StrategyParam<decimal> _riskPercentPerTrade;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<decimal> _breakevenDistancePips;
	private readonly StrategyParam<bool> _useMaxMargin;

	private readonly AverageTrueRange _atr = new() { Length = 10 };

	private Random _random;
	private decimal _pipSize;
	private bool _breakevenActivated;
	private Sides? _pendingSide;
	private decimal _pendingStopDistance;
	private decimal _pendingTakeDistance;
	private bool _orderPending;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomTraderStrategy"/> class.
	/// </summary>
	public RandomTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type processed by the strategy.", "Data");

		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Reward/Risk", "Reward to risk ratio used for take profit calculation.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_lossMode = Param(nameof(LossType), LossMode.Pip)
		.SetDisplay("Stop Mode", "Select between fixed pip or ATR based stop calculation.", "Risk");

		_lossAtrMultiplier = Param(nameof(LossAtrMultiplier), 5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR(10) for stop distance.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 10m, 1m);

		_lossPipDistance = Param(nameof(LossPipDistance), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Fixed stop distance expressed in pips.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_riskPercentPerTrade = Param(nameof(RiskPercentPerTrade), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Portfolio percentage risked per trade.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_useBreakeven = Param(nameof(UseBreakeven), true)
		.SetDisplay("Use Breakeven", "Move the stop to entry price after profit threshold.", "Risk");

		_breakevenDistancePips = Param(nameof(BreakevenDistancePips), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Breakeven Trigger (pips)", "Profit in pips required before breakeven activates.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

		_useMaxMargin = Param(nameof(UseMaxMargin), true)
		.SetDisplay("Use Max Volume", "Allow using the maximum tradable volume when risk sizing is too small.", "Risk");
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio applied to the stop distance.
	/// </summary>
	public decimal RewardRiskRatio
	{
		get => _rewardRiskRatio.Value;
		set => _rewardRiskRatio.Value = value;
	}

	/// <summary>
	/// Stop loss calculation mode.
	/// </summary>
	public LossMode LossType
	{
		get => _lossMode.Value;
		set => _lossMode.Value = value;
	}

	/// <summary>
	/// ATR multiplier used when <see cref="LossType"/> equals <see cref="LossMode.Atr"/>.
	/// </summary>
	public decimal LossAtrMultiplier
	{
		get => _lossAtrMultiplier.Value;
		set => _lossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Fixed stop distance measured in pips.
	/// </summary>
	public decimal LossPipDistance
	{
		get => _lossPipDistance.Value;
		set => _lossPipDistance.Value = value;
	}

	/// <summary>
	/// Portfolio percentage risked per trade.
	/// </summary>
	public decimal RiskPercentPerTrade
	{
		get => _riskPercentPerTrade.Value;
		set => _riskPercentPerTrade.Value = value;
	}

	/// <summary>
	/// Enables breakeven stop adjustments.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required to move the stop to breakeven.
	/// </summary>
	public decimal BreakevenDistancePips
	{
		get => _breakevenDistancePips.Value;
		set => _breakevenDistancePips.Value = value;
	}

	/// <summary>
	/// Allows using the maximum available volume when risk sizing is too small.
	/// </summary>
	public bool UseMaxMargin
	{
		get => _useMaxMargin.Value;
		set => _useMaxMargin.Value = value;
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

		_random = null;
		_pipSize = 0m;
		_breakevenActivated = false;
		_pendingSide = null;
		_pendingStopDistance = 0m;
		_pendingTakeDistance = 0m;
		_orderPending = false;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_random = new Random(Environment.TickCount);
		_pipSize = DetectPipSize();
		_breakevenActivated = false;
		_pendingSide = null;
		_pendingStopDistance = 0m;
		_pendingTakeDistance = 0m;
		_orderPending = false;

		// Subscribe to the working timeframe so every finished candle triggers the decision logic.
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Update protective levels for the current position before attempting a new entry.
		UpdateOpenPosition(candle);

		if (Position != 0 || _orderPending)
		return;

		// Determine stop and take-profit distances based on the configured risk model.
		var stopDistance = GetStopDistance(atrValue);
		if (stopDistance <= 0m)
		return;

		var takeDistance = stopDistance * RewardRiskRatio;
		if (takeDistance <= 0m)
		return;

		// Convert the monetary risk into an executable volume.
		var volume = CalculateOrderVolume(stopDistance);
		if (volume <= 0m)
		return;

		if (_random == null)
		return;

		// Flip a virtual coin to decide between a long or short entry.
		var isBuy = _random.Next(0, 2) == 0;

		_pendingSide = isBuy ? Sides.Buy : Sides.Sell;
		_pendingStopDistance = stopDistance;
		_pendingTakeDistance = takeDistance;
		_breakevenActivated = false;
		_orderPending = true;

		if (isBuy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private decimal GetStopDistance(IIndicatorValue atrValue)
	{
		if (LossType == LossMode.Pip)
		{
			var pip = _pipSize;
			if (pip <= 0m)
			pip = Security?.PriceStep ?? 0m;

			return pip > 0m ? LossPipDistance * pip : 0m;
		}

		if (!atrValue.IsFinal)
		return 0m;

		var atr = atrValue.GetValue<decimal>();
		if (atr <= 0m)
		return 0m;

		return LossAtrMultiplier * atr;
	}

	private decimal CalculateOrderVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
		return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
		return 0m;

		var riskAmount = portfolioValue * RiskPercentPerTrade / 100m;
		if (riskAmount <= 0m)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		decimal lossPerUnit;

		if (step > 0m && stepPrice > 0m)
		lossPerUnit = stopDistance / step * stepPrice;
		else
		lossPerUnit = stopDistance;

		if (lossPerUnit <= 0m)
		return 0m;

		var rawVolume = riskAmount / lossPerUnit;
		var volume = NormalizeVolume(rawVolume);

		if (volume <= 0m && UseMaxMargin)
		{
			var maxVolume = Security?.MaxVolume ?? 0m;
			if (maxVolume > 0m)
			volume = NormalizeVolume(maxVolume);
		}

		if (volume <= 0m)
		{
			var fallback = Volume > 0m ? Volume : 0m;
			volume = NormalizeVolume(fallback);
		}

		return volume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		if (Security?.VolumeStep is decimal step && step > 0m)
		volume = Math.Floor(volume / step) * step;

		if (Security?.MinVolume is decimal minVolume && minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (Security?.MaxVolume is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private void UpdateOpenPosition(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice is null)
		return;

		// Move the stop to breakeven when the configured profit threshold is reached.
		if (UseBreakeven && !_breakevenActivated && BreakevenDistancePips > 0m)
		{
			var trigger = (_pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m) * BreakevenDistancePips;
			if (trigger > 0m)
			{
				if (Position > 0 && candle.HighPrice >= _entryPrice.Value + trigger)
				{
					_stopPrice = RoundPrice(_entryPrice.Value);
					_breakevenActivated = true;
				}
				else if (Position < 0 && candle.LowPrice <= _entryPrice.Value - trigger)
				{
					_stopPrice = RoundPrice(_entryPrice.Value);
					_breakevenActivated = true;
				}
			}
		}

		if (Position > 0)
		{
			// Close the long position if either target or stop level is violated within the candle.
			if (_takePrice is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(Position);
				return;
			}

			if (_stopPrice is decimal stopLoss && candle.LowPrice <= stopLoss)
			{
				SellMarket(Position);
				return;
			}
		}
		else
		{
			var volume = Math.Abs(Position);
			// Close the short position if the desired take profit or stop price has been reached.
			if (_takePrice is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(volume);
				return;
			}

			if (_stopPrice is decimal stopLoss && candle.HighPrice >= stopLoss)
			{
				BuyMarket(volume);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
		return;

		// Capture the executed entry price to derive protective levels aligned with the fill.
		if (_pendingSide.HasValue && trade.Order.Side == _pendingSide.Value)
		{
			var price = trade.Trade.Price;
			_entryPrice = price;
			_stopPrice = CalculateStopPrice(price, _pendingSide.Value, _pendingStopDistance);
			_takePrice = CalculateTakePrice(price, _pendingSide.Value, _pendingTakeDistance);
			_orderPending = false;
		}

		if (Position == 0)
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(OrderFail fail)
	{
		base.OnOrderFailed(fail);

		if (fail.Order.Security != Security)
		return;

		_orderPending = false;
		_pendingSide = null;
		_pendingStopDistance = 0m;
		_pendingTakeDistance = 0m;
	}

	private decimal? CalculateStopPrice(decimal entryPrice, Sides side, decimal distance)
	{
		if (distance <= 0m)
		return null;

		var price = side == Sides.Buy
		? entryPrice - distance
		: entryPrice + distance;

		return RoundPrice(price);
	}

	private decimal? CalculateTakePrice(decimal entryPrice, Sides side, decimal distance)
	{
		if (distance <= 0m)
		return null;

		var price = side == Sides.Buy
		? entryPrice + distance
		: entryPrice - distance;

		return RoundPrice(price);
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return price;

		var ticks = Math.Round(price / step, MidpointRounding.AwayFromZero);
		return ticks * step;
	}

	private decimal DetectPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 2 || decimals == 3)
		return step;

		if (decimals == 4 || decimals == 5)
		return step * 10m;

		return step;
	}

	private void ResetPositionState()
	{
		_pendingSide = null;
		_pendingStopDistance = 0m;
		_pendingTakeDistance = 0m;
		_orderPending = false;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_breakevenActivated = false;
	}
}

/// <summary>
/// Stop loss calculation modes supported by <see cref="RandomTraderStrategy"/>.
/// </summary>
public enum LossMode
{
	/// <summary>
	/// Calculate stop distance from the ATR(10) indicator.
	/// </summary>
	Atr,

	/// <summary>
	/// Use fixed pip distance for the stop.
	/// </summary>
	Pip
}
