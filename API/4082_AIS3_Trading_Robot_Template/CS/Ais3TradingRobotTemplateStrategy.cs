using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS3 breakout template converted from MetaTrader with range based stop and trailing rules.
/// </summary>
public class Ais3TradingRobotTemplateStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accountReserve;
	private readonly StrategyParam<decimal> _orderReserve;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _secondaryCandleType;
	private readonly StrategyParam<decimal> _takeMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<decimal> _trailMultiplier;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _stopBufferTicks;
	private readonly StrategyParam<decimal> _freezeBufferTicks;
	private readonly StrategyParam<decimal> _trailStepMultiplier;

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _quoteSpread;
	private decimal _quoteStopsBuffer;
	private decimal _quoteFreezeBuffer;
	private decimal _trailStepDistance;
	private decimal _quoteTakeDistance;
	private decimal _quoteStopDistance;
	private decimal _quoteTrailDistance;

	private decimal _primaryAverage;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;

	private decimal? _longStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTargetPrice;

	/// <summary>
	/// Fraction of equity reserved for drawdowns (0-1).
	/// </summary>
	public decimal AccountReserve
	{
		get => _accountReserve.Value;
		set => _accountReserve.Value = value;
	}

	/// <summary>
	/// Fraction of equity allocated per trade (0-1).
	/// </summary>
	public decimal OrderReserve
	{
		get => _orderReserve.Value;
		set => _orderReserve.Value = value;
	}

	/// <summary>
	/// Primary candle type used for breakout detection.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Secondary candle type used for trailing distance measurement.
	/// </summary>
	public DataType SecondaryCandleType
	{
		get => _secondaryCandleType.Value;
		set => _secondaryCandleType.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier relative to the primary candle range.
	/// </summary>
	public decimal TakeMultiplier
	{
		get => _takeMultiplier.Value;
		set => _takeMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss multiplier relative to the primary candle range.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// Trailing distance multiplier relative to the secondary candle range.
	/// </summary>
	public decimal TrailMultiplier
	{
		get => _trailMultiplier.Value;
		set => _trailMultiplier.Value = value;
	}

	/// <summary>
	/// Fallback volume used when portfolio metrics are not accessible.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Additional safety buffer expressed in ticks for stop checks.
	/// </summary>
	public decimal StopBufferTicks
	{
		get => _stopBufferTicks.Value;
		set => _stopBufferTicks.Value = value;
	}

	/// <summary>
	/// Additional freeze buffer expressed in ticks to avoid rapid stop updates.
	/// </summary>
	public decimal FreezeBufferTicks
	{
		get => _freezeBufferTicks.Value;
		set => _freezeBufferTicks.Value = value;
	}

	/// <summary>
	/// Spread multiplier that defines the minimum trailing step distance.
	/// </summary>
	public decimal TrailStepMultiplier
	{
		get => _trailStepMultiplier.Value;
		set => _trailStepMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Ais3TradingRobotTemplateStrategy"/> class.
	/// </summary>
	public Ais3TradingRobotTemplateStrategy()
	{
		_accountReserve = Param(nameof(AccountReserve), 0.20m)
		.SetDisplay("Account Reserve", "Fraction of equity kept as reserve", "Risk")
		.SetGreaterOrEqual(0m)
		.SetLessOrEquals(0.95m);

		_orderReserve = Param(nameof(OrderReserve), 0.04m)
		.SetDisplay("Order Reserve", "Fraction of equity allocated per trade", "Risk")
		.SetGreaterOrEqual(0m)
		.SetLessOrEquals(0.50m);

		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle", "Primary timeframe for breakout detection", "General");

		_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Secondary Candle", "Secondary timeframe for trailing logic", "General");

		_takeMultiplier = Param(nameof(TakeMultiplier), 1.0m)
		.SetDisplay("Take Multiplier", "Take-profit multiplier of the primary range", "Targets")
		.SetGreaterThanZero();

		_stopMultiplier = Param(nameof(StopMultiplier), 2.0m)
		.SetDisplay("Stop Multiplier", "Stop-loss multiplier of the primary range", "Targets")
		.SetGreaterThanZero();

		_trailMultiplier = Param(nameof(TrailMultiplier), 3.0m)
		.SetDisplay("Trail Multiplier", "Trailing distance multiplier of the secondary range", "Targets")
		.SetGreaterThanZero();

		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Fallback volume when risk sizing fails", "Risk")
		.SetGreaterThanZero();

		_stopBufferTicks = Param(nameof(StopBufferTicks), 0m)
		.SetDisplay("Stop Buffer Ticks", "Extra ticks added on top of broker stop limits", "Execution")
		.SetGreaterOrEqualZero();

		_freezeBufferTicks = Param(nameof(FreezeBufferTicks), 0m)
		.SetDisplay("Freeze Buffer Ticks", "Extra ticks preventing frequent stop updates", "Execution")
		.SetGreaterOrEqualZero();

		_trailStepMultiplier = Param(nameof(TrailStepMultiplier), 1m)
		.SetDisplay("Trail Step Mult", "Spread multiplier for minimal trailing step", "Execution")
		.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, PrimaryCandleType),
			(Security, SecondaryCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_quoteSpread = 0m;
		_quoteStopsBuffer = 0m;
		_quoteFreezeBuffer = 0m;
		_trailStepDistance = 0m;
		_quoteTakeDistance = 0m;
		_quoteStopDistance = 0m;
		_quoteTrailDistance = 0m;
		_primaryAverage = 0m;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		OnReseted();

		// Subscribe to the primary timeframe candles that drive breakout detection.
		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
		.Bind(ProcessPrimaryCandle)
		.Start();

		// Subscribe to the secondary timeframe candles used for trailing distance.
		var secondarySubscription = SubscribeCandles(SecondaryCandleType);
		secondarySubscription
		.Bind(ProcessSecondaryCandle)
		.Start();

		// Keep track of the best bid/ask prices to mimic the original MetaTrader feed usage.
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
		// Compute candle midpoint and range for entry calculations.
		_primaryAverage = (candle.HighPrice + candle.LowPrice) / 2m;
		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteTakeDistance = range * TakeMultiplier;
		_quoteStopDistance = range * StopMultiplier;

		// Estimate current spread from best bid/ask or fall back to the price step.
		var priceStep = Security?.PriceStep ?? 0m;
		var spread = _bestAsk > 0m && _bestBid > 0m ? _bestAsk - _bestBid : priceStep;
		if (spread <= 0m && priceStep > 0m)
		spread = priceStep;
		_quoteSpread = Math.Max(0m, spread);

		// Translate stop and freeze buffers from ticks into price units.
		_quoteStopsBuffer = StopBufferTicks * priceStep;
		_quoteFreezeBuffer = FreezeBufferTicks * priceStep;

		// Minimal trail step is proportional to the spread as in the MetaTrader template.
		_trailStepDistance = _quoteSpread * TrailStepMultiplier;
	}

	private void UpdateSecondaryMetrics(ICandleMessage candle)
	{
		// Secondary range defines the trailing distance.
		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteTrailDistance = range * TrailMultiplier;
	}

	private void TryEnterTrade(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
		var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
		return;

		var stopBuffer = _quoteStopsBuffer;

		// Long breakout: close above midpoint and ask breaking previous high plus spread.
		var longCondition = candle.ClosePrice > _primaryAverage && ask > candle.HighPrice + _quoteSpread;
		if (longCondition && Position <= 0)
		{
			var stopPrice = candle.HighPrice + _quoteSpread - _quoteStopDistance;
			var takePrice = ask + _quoteTakeDistance;

			if (stopPrice <= 0m || takePrice <= 0m)
			return;

			// Replicate MetaTrader safety checks against broker stop limitations.
			if (takePrice - ask <= stopBuffer)
			return;

			if (ask - _quoteSpread - stopPrice <= stopBuffer)
			return;

			if (stopPrice >= ask)
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
		}

		// Short breakout: close below midpoint and bid taking out the previous low.
		var shortCondition = candle.ClosePrice < _primaryAverage && bid < candle.LowPrice;
		if (shortCondition && Position >= 0)
		{
			var stopPrice = candle.LowPrice + _quoteStopDistance;
			var takePrice = bid - _quoteTakeDistance;

			if (stopPrice <= 0m || takePrice <= 0m)
			return;

			if (bid - takePrice <= stopBuffer)
			return;

			if (stopPrice - bid - _quoteSpread <= stopBuffer)
			return;

			if (stopPrice <= bid)
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
		}
	}

	private void TryManagePosition(ICandleMessage candle)
	{
		var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
		var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;

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
		if (_quoteTrailDistance <= 0m)
		return;

		if (bid <= 0m || _longEntryPrice <= 0m)
		return;

		if (bid <= _longEntryPrice)
		return;

		if (_quoteTrailDistance <= _quoteStopsBuffer || _quoteTrailDistance <= _quoteFreezeBuffer)
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
		if (_quoteTrailDistance <= 0m)
		return;

		if (ask <= 0m || _shortEntryPrice <= 0m)
		return;

		if (ask >= _shortEntryPrice)
		return;

		if (_quoteTrailDistance <= _quoteStopsBuffer || _quoteTrailDistance <= _quoteFreezeBuffer)
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
		var riskPerUnit = Math.Abs(entryPrice - stopPrice);
		if (riskPerUnit <= 0m)
		return BaseVolume;

		if (Portfolio == null)
		return BaseVolume;

		var equity = Portfolio.CurrentValue;
		if (equity <= 0m)
		return BaseVolume;

		var reserve = AccountReserve;
		if (reserve < 0m)
		reserve = 0m;
		else if (reserve > 0.95m)
		reserve = 0.95m;

		var allocation = OrderReserve;
		if (allocation < 0m)
		allocation = 0m;
		else if (allocation > 1m)
		allocation = 1m;

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
