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
/// AIS2 Trading Robot (modification 20005) converted from MetaTrader 4 expert advisor.
/// Implements range breakout entries with capital preservation, adaptive trailing and trade cooldown.
/// </summary>
public class Ais2TradingRobot20005Strategy : Strategy
{
	private readonly StrategyParam<decimal> _accountReserve;
	private readonly StrategyParam<decimal> _orderReserve;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _secondaryCandleType;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;
	private readonly StrategyParam<decimal> _trailFactor;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _stopBufferTicks;
	private readonly StrategyParam<decimal> _freezeBufferTicks;
	private readonly StrategyParam<decimal> _trailStepMultiplier;
	private readonly StrategyParam<int> _tradingPauseSeconds;

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _quoteSpread;
	private decimal _stopBuffer;
	private decimal _freezeBuffer;
	private decimal _trailStepDistance;
	private decimal _primaryAverage;
	private decimal _quoteTakeDistance;
	private decimal _quoteStopDistance;
	private decimal _quoteTrailDistance;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTargetPrice;

	private DateTimeOffset? _lastTradeTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="Ais2TradingRobot20005Strategy"/> class.
	/// </summary>
	public Ais2TradingRobot20005Strategy()
	{
		_accountReserve = Param(nameof(AccountReserve), 0.20m)
		.SetDisplay("Account Reserve", "Fraction of equity locked as capital reserve", "Risk")
		.SetNotNegative()
		.SetLessOrEquals(0.95m)
		.SetCanOptimize(true);

		_orderReserve = Param(nameof(OrderReserve), 0.04m)
		.SetDisplay("Order Reserve", "Fraction of equity committed per trade", "Risk")
		.SetNotNegative()
		.SetLessOrEquals(0.5m)
		.SetCanOptimize(true);

		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle", "Primary timeframe used for entries", "General");

		_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Secondary Candle", "Secondary timeframe driving trailing distance", "General");

		_takeFactor = Param(nameof(TakeFactor), 1.7m)
		.SetDisplay("Take Factor", "Multiplier applied to the primary range for targets", "Targets")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_stopFactor = Param(nameof(StopFactor), 1.7m)
		.SetDisplay("Stop Factor", "Multiplier applied to the primary range for stops", "Targets")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_trailFactor = Param(nameof(TrailFactor), 0.5m)
		.SetDisplay("Trail Factor", "Multiplier applied to the secondary range for trailing", "Targets")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Fallback volume if risk sizing is not available", "Risk")
		.SetGreaterThanZero();

		_stopBufferTicks = Param(nameof(StopBufferTicks), 0m)
		.SetDisplay("Stop Buffer", "Extra ticks added to stop distance checks", "Execution")
		.SetNotNegative();

		_freezeBufferTicks = Param(nameof(FreezeBufferTicks), 0m)
		.SetDisplay("Freeze Buffer", "Extra ticks protecting stop re-placements", "Execution")
		.SetNotNegative();

		_trailStepMultiplier = Param(nameof(TrailStepMultiplier), 1m)
		.SetDisplay("Trail Step Mult", "Spread multiplier limiting trailing frequency", "Execution")
		.SetGreaterThanZero();

		_tradingPauseSeconds = Param(nameof(TradingPauseSeconds), 5)
		.SetDisplay("Trading Pause", "Cooldown between consecutive trades in seconds", "Execution")
		.SetNotNegative()
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Portion of equity locked as capital reserve.
	/// </summary>
	public decimal AccountReserve
	{
		get => _accountReserve.Value;
		set => _accountReserve.Value = value;
	}

	/// <summary>
	/// Portion of equity committed per trade.
	/// </summary>
	public decimal OrderReserve
	{
		get => _orderReserve.Value;
		set => _orderReserve.Value = value;
	}

	/// <summary>
	/// Primary candle type used for breakout evaluation.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Secondary candle type providing volatility for trailing stops.
	/// </summary>
	public DataType SecondaryCandleType
	{
		get => _secondaryCandleType.Value;
		set => _secondaryCandleType.Value = value;
	}

	/// <summary>
	/// Take profit multiplier relative to the primary candle range.
	/// </summary>
	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier relative to the primary candle range.
	/// </summary>
	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	/// <summary>
	/// Trailing distance multiplier relative to the secondary candle range.
	/// </summary>
	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <summary>
	/// Fallback volume used when risk sizing cannot be derived.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Additional stop buffer expressed in ticks.
	/// </summary>
	public decimal StopBufferTicks
	{
		get => _stopBufferTicks.Value;
		set => _stopBufferTicks.Value = value;
	}

	/// <summary>
	/// Additional freeze buffer expressed in ticks.
	/// </summary>
	public decimal FreezeBufferTicks
	{
		get => _freezeBufferTicks.Value;
		set => _freezeBufferTicks.Value = value;
	}

	/// <summary>
	/// Multiplier applied to spread while validating trailing updates.
	/// </summary>
	public decimal TrailStepMultiplier
	{
		get => _trailStepMultiplier.Value;
		set => _trailStepMultiplier.Value = value;
	}

	/// <summary>
	/// Cooldown in seconds between consecutive trades.
	/// </summary>
	public int TradingPauseSeconds
	{
		get => _tradingPauseSeconds.Value;
		set => _tradingPauseSeconds.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, PrimaryCandleType);
		yield return (Security, SecondaryCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_quoteSpread = 0m;
		_stopBuffer = 0m;
		_freezeBuffer = 0m;
		_trailStepDistance = 0m;
		_primaryAverage = 0m;
		_quoteTakeDistance = 0m;
		_quoteStopDistance = 0m;
		_quoteTrailDistance = 0m;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
		_lastTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		OnReseted();

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
		.Bind(ProcessPrimaryCandle)
		.Start();

		var secondarySubscription = SubscribeCandles(SecondaryCandleType);
		secondarySubscription
		.Bind(ProcessSecondaryCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdatePrimaryMetrics(candle);
		TryManagePosition(candle);
		TryEnterTrade(candle);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateSecondaryMetrics(candle);
		TryManagePosition(candle);
	}

	private void UpdatePrimaryMetrics(ICandleMessage candle)
	{
		// Update breakout thresholds using the latest higher timeframe candle.
		_primaryAverage = (candle.HighPrice + candle.LowPrice) / 2m;
		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteTakeDistance = range * TakeFactor;
		_quoteStopDistance = range * StopFactor;

		var priceStep = Security?.PriceStep ?? 0m;
		var spread = _bestAsk > 0m && _bestBid > 0m ? _bestAsk - _bestBid : priceStep;
		if (spread <= 0m && priceStep > 0m)
		spread = priceStep;
		_quoteSpread = Math.Max(0m, spread);

		_stopBuffer = StopBufferTicks * priceStep;
		_freezeBuffer = FreezeBufferTicks * priceStep;
		_trailStepDistance = _quoteSpread * TrailStepMultiplier;
	}

	private void UpdateSecondaryMetrics(ICandleMessage candle)
	{
		// Refresh trailing stop distance based on the secondary timeframe volatility.
		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteTrailDistance = range * TrailFactor;
	}

	private void TryEnterTrade(ICandleMessage candle)
	{
		// Evaluate new positions only when trading is allowed and cooldown has passed.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var now = CurrentTime ?? candle.CloseTime;
		if (_lastTradeTime is DateTimeOffset previous && TradingPauseSeconds > 0)
		{
			var delta = now - previous;
			if (delta < TimeSpan.FromSeconds(TradingPauseSeconds))
			return;
		}

		GetBestPrices(candle, out var bid, out var ask);
		// Use the freshest bid/ask snapshot to mirror MetaTrader execution checks.
		if (bid <= 0m || ask <= 0m)
		return;

		var longCondition = candle.ClosePrice > _primaryAverage && ask > candle.HighPrice + _quoteSpread;
		// Breakout above the mid-price and previous high triggers a long setup.
		if (longCondition && Position <= 0)
		{
			var stopPrice = candle.HighPrice + _quoteSpread - _quoteStopDistance;
			var takePrice = ask + _quoteTakeDistance;

			if (!ValidateRiskDistances(ask, stopPrice, takePrice))
			return;

			var volume = CalculatePositionVolume(ask, stopPrice) + Math.Max(0m, -Position);
			if (volume <= 0m)
			return;

			BuyMarket(volume);
			_longEntryPrice = ask;
			_longStopPrice = stopPrice;
			_longTargetPrice = takePrice;
			_shortStopPrice = null;
			_shortTargetPrice = null;
			_lastTradeTime = now;
		}

		var shortCondition = candle.ClosePrice < _primaryAverage && bid < candle.LowPrice;
		// Breakdown below the mid-price and previous low enables a short opportunity.
		if (shortCondition && Position >= 0)
		{
			var stopPrice = candle.LowPrice + _quoteStopDistance;
			var takePrice = bid - _quoteTakeDistance;

			if (!ValidateRiskDistances(bid, stopPrice, takePrice, isLong: false))
			return;

			var volume = CalculatePositionVolume(bid, stopPrice) + Math.Max(0m, Position);
			if (volume <= 0m)
			return;

			SellMarket(volume);
			_shortEntryPrice = bid;
			_shortStopPrice = stopPrice;
			_shortTargetPrice = takePrice;
			_longStopPrice = null;
			_longTargetPrice = null;
			_lastTradeTime = now;
		}
	}

	private bool ValidateRiskDistances(decimal entryPrice, decimal stopPrice, decimal takePrice, bool isLong = true)
	{
		// Respect broker stop/freeze levels so the order passes validation.
		if (stopPrice <= 0m || takePrice <= 0m)
		return false;

		if (isLong)
		{
			if (stopPrice >= entryPrice)
			return false;

			if (takePrice - entryPrice <= _stopBuffer)
			return false;

			if (entryPrice - _quoteSpread - stopPrice <= _stopBuffer)
			return false;
		}
		else
		{
			if (stopPrice <= entryPrice)
			return false;

			if (entryPrice - takePrice <= _stopBuffer)
			return false;

			if (stopPrice - entryPrice - _quoteSpread <= _stopBuffer)
			return false;
		}

		return true;
	}

	private void TryManagePosition(ICandleMessage candle)
	{
		// Monitor open exposure for manual exits when stop or target levels are breached.
		GetBestPrices(candle, out var bid, out var ask);
		// Use the freshest bid/ask snapshot to mirror MetaTrader execution checks.

		if (Position > 0)
		{
			UpdateLongTrailing(bid);

			if (_longStopPrice is decimal longStop && bid <= longStop)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_longTargetPrice is decimal longTarget && bid >= longTarget)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			UpdateShortTrailing(ask);

			if (_shortStopPrice is decimal shortStop && ask >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_shortTargetPrice is decimal shortTarget && ask <= shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
	}

	private void UpdateLongTrailing(decimal bid)
	{
		// Nudge the long stop toward price only after the market moves in our favor.
		if (_quoteTrailDistance <= 0m)
		return;

		if (bid <= 0m || _longEntryPrice <= 0m)
		return;

		if (bid <= _longEntryPrice)
		return;

		if (_quoteTrailDistance <= _stopBuffer || _quoteTrailDistance <= _freezeBuffer)
		return;

		var newStop = bid - _quoteTrailDistance;
		if (_longStopPrice is decimal currentStop)
		{
			if (newStop <= currentStop)
			return;

			if (newStop - currentStop <= _trailStepDistance)
			return;
		}

		_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(decimal ask)
	{
		// Mirror trailing logic for short positions using ask prices.
		if (_quoteTrailDistance <= 0m)
		return;

		if (ask <= 0m || _shortEntryPrice <= 0m)
		return;

		if (ask >= _shortEntryPrice)
		return;

		if (_quoteTrailDistance <= _stopBuffer || _quoteTrailDistance <= _freezeBuffer)
		return;

		var newStop = ask + _quoteTrailDistance;
		if (_shortStopPrice is decimal currentStop)
		{
			if (newStop >= currentStop)
			return;

			if (currentStop - newStop <= _trailStepDistance)
			return;
		}

		_shortStopPrice = newStop;
	}

	private decimal CalculatePositionVolume(decimal entryPrice, decimal stopPrice)
	{
		// Convert equity based risk allowances into an executable order size.
		var riskPerUnit = Math.Abs(entryPrice - stopPrice);
		if (riskPerUnit <= 0m)
		return BaseVolume;

		if (Portfolio == null)
		return BaseVolume;

		var equity = Portfolio.CurrentValue;
		if (equity <= 0m)
		return BaseVolume;

		var reserve = Math.Clamp(AccountReserve, 0m, 0.95m);
		var allocation = Math.Clamp(OrderReserve, 0m, 1m);

		var reservedEquity = equity * reserve;
		var tradableEquity = equity - reservedEquity;
		if (tradableEquity <= 0m)
		return 0m;

		var varLimit = equity * allocation;
		if (reservedEquity < varLimit)
		return 0m;

		var riskBudget = tradableEquity * allocation;
		if (riskBudget <= 0m)
		return 0m;

		var volume = riskBudget / riskPerUnit;
		volume = AdjustVolume(volume);

		return volume > 0m ? volume : 0m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		// Align the raw volume with exchange volume limits and step.
		if (Security == null)
		return Math.Max(volume, 0m);

		var minVolume = Security.MinVolume ?? 0m;
		var maxVolume = Security.MaxVolume ?? decimal.MaxValue;
		var step = Security.VolumeStep ?? 0m;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		if (step > 0m)
		{
			var offset = minVolume > 0m ? minVolume : 0m;
			var steps = Math.Floor((volume - offset) / step);
			volume = offset + step * steps;
		}

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		return Math.Max(volume, 0m);
	}

	private void GetBestPrices(ICandleMessage candle, out decimal bid, out decimal ask)
	{
		// Fall back to candle close when there is no live order book snapshot.
		bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
		ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
	}
}
