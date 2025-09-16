using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS2 breakout strategy converted from MetaTrader with range based targets and trailing.
/// Combines multi-timeframe signals, risk managed position sizing and adaptive trailing stops.
/// </summary>
public class Ais2TradingRobotStrategy : Strategy
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
	/// Primary timeframe used for entry logic.
	/// </summary>
	public DataType PrimaryCandleType
	{
	get => _primaryCandleType.Value;
	set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Secondary timeframe used for trailing logic.
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
	/// Trailing distance multiplier based on the secondary candle range.
	/// </summary>
	public decimal TrailFactor
	{
	get => _trailFactor.Value;
	set => _trailFactor.Value = value;
	}

	/// <summary>
	/// Minimum fallback volume if risk based sizing is not available.
	/// </summary>
	public decimal BaseVolume
	{
	get => _baseVolume.Value;
	set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Additional stop buffer expressed in ticks to respect broker restrictions.
	/// </summary>
	public decimal StopBufferTicks
	{
	get => _stopBufferTicks.Value;
	set => _stopBufferTicks.Value = value;
	}

	/// <summary>
	/// Additional freeze buffer expressed in ticks to avoid frequent stop updates.
	/// </summary>
	public decimal FreezeBufferTicks
	{
	get => _freezeBufferTicks.Value;
	set => _freezeBufferTicks.Value = value;
	}

	/// <summary>
	/// Multiplier applied to spread when computing the minimal trailing step.
	/// </summary>
	public decimal TrailStepMultiplier
	{
	get => _trailStepMultiplier.Value;
	set => _trailStepMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Ais2TradingRobotStrategy"/> class.
	/// </summary>
	public Ais2TradingRobotStrategy()
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
		.SetDisplay("Primary Candle", "Primary timeframe for entries", "General");

	_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Secondary Candle", "Secondary timeframe for trailing", "General");

	_takeFactor = Param(nameof(TakeFactor), 1.7m)
		.SetDisplay("Take Factor", "Take profit multiplier of primary range", "Targets")
		.SetGreaterThanZero();

	_stopFactor = Param(nameof(StopFactor), 1.7m)
		.SetDisplay("Stop Factor", "Stop loss multiplier of primary range", "Targets")
		.SetGreaterThanZero();

	_trailFactor = Param(nameof(TrailFactor), 0.5m)
		.SetDisplay("Trail Factor", "Trailing multiplier of secondary range", "Targets")
		.SetGreaterThanZero();

	_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Fallback volume when risk sizing fails", "Risk")
		.SetGreaterThanZero();

	_stopBufferTicks = Param(nameof(StopBufferTicks), 0m)
		.SetDisplay("Stop Buffer Ticks", "Extra ticks added to stop checks", "Execution")
		.SetGreaterOrEqualZero();

	_freezeBufferTicks = Param(nameof(FreezeBufferTicks), 0m)
		.SetDisplay("Freeze Buffer Ticks", "Extra ticks avoiding rapid stop updates", "Execution")
		.SetGreaterOrEqualZero();

	_trailStepMultiplier = Param(nameof(TrailStepMultiplier), 1m)
		.SetDisplay("Trail Step Mult", "Spread multiplier for minimal trail step", "Execution")
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
	_primaryAverage = (candle.HighPrice + candle.LowPrice) / 2m;
	var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
	_quoteTakeDistance = range * TakeFactor;
	_quoteStopDistance = range * StopFactor;

	var priceStep = Security?.PriceStep ?? 0m;
	var spread = _bestAsk > 0m && _bestBid > 0m ? _bestAsk - _bestBid : priceStep;
	if (spread <= 0m && priceStep > 0m)
		spread = priceStep;
	_quoteSpread = Math.Max(0m, spread);

	_quoteStopsBuffer = StopBufferTicks * priceStep;
	_quoteFreezeBuffer = FreezeBufferTicks * priceStep;
	_trailStepDistance = _quoteSpread * TrailStepMultiplier;
	}

	private void UpdateSecondaryMetrics(ICandleMessage candle)
	{
	var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
	_quoteTrailDistance = range * TrailFactor;
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

	var longCondition = candle.ClosePrice > _primaryAverage && ask > candle.HighPrice + _quoteSpread;
	if (longCondition && Position <= 0)
	{
		var stopPrice = candle.HighPrice + _quoteSpread - _quoteStopDistance;
		var takePrice = ask + _quoteTakeDistance;

		if (stopPrice <= 0m || takePrice <= 0m)
			return;

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
