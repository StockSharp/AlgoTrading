namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class UdyIvanMadumereStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _firstLookback;
	private readonly StrategyParam<int> _secondLookback;
	private readonly StrategyParam<decimal> _longDeltaPoints;
	private readonly StrategyParam<decimal> _shortDeltaPoints;
	private readonly StrategyParam<decimal> _takeProfitLongPoints;
	private readonly StrategyParam<decimal> _stopLossLongPoints;
	private readonly StrategyParam<decimal> _takeProfitShortPoints;
	private readonly StrategyParam<decimal> _stopLossShortPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useAutoVolume;
	private readonly StrategyParam<decimal> _bigLotMultiplier;
	private readonly StrategyParam<int> _maxHoldingHours;

	private readonly List<decimal> _openHistory = new();

	private bool _canTrade = true;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	private decimal? _shortBestPrice;
	private decimal? _shortTrailingStopPrice;

	private decimal _currentBaseVolume;
	private decimal _referenceBalance;
	private decimal _lastCalculatedBalance;

	public UdyIvanMadumereStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("Candle type", "Timeframe used for signal calculations.", "General");

	_tradeHour = Param(nameof(TradeHour), 18)
	.SetDisplay("Trade hour", "Hour of the day (0-23) when the entry rules are evaluated.", "Trading")
	.SetCanOptimize(true)
	.SetOptimize(0, 23, 1);

	_firstLookback = Param(nameof(FirstLookback), 6)
	.SetNotNegative()
	.SetDisplay("First lookback", "Number of completed candles referenced as Open[FirstLookback].", "Signal");

	_secondLookback = Param(nameof(SecondLookback), 2)
	.SetNotNegative()
	.SetDisplay("Second lookback", "Number of completed candles referenced as Open[SecondLookback].", "Signal");

	_longDeltaPoints = Param(nameof(LongDeltaPoints), 6m)
	.SetNotNegative()
	.SetDisplay("Long delta (points)", "Minimum bullish open-price distance expressed in MetaTrader points.", "Signal");

	_shortDeltaPoints = Param(nameof(ShortDeltaPoints), 21m)
	.SetNotNegative()
	.SetDisplay("Short delta (points)", "Minimum bearish open-price distance expressed in MetaTrader points.", "Signal");

	_takeProfitLongPoints = Param(nameof(TakeProfitLongPoints), 39m)
	.SetNotNegative()
	.SetDisplay("Long take profit (points)", "Take-profit distance for long positions in MetaTrader points.", "Risk");

	_stopLossLongPoints = Param(nameof(StopLossLongPoints), 147m)
	.SetNotNegative()
	.SetDisplay("Long stop loss (points)", "Stop-loss distance for long positions in MetaTrader points.", "Risk");

	_takeProfitShortPoints = Param(nameof(TakeProfitShortPoints), 200m)
	.SetNotNegative()
	.SetDisplay("Short take profit (points)", "Take-profit distance for short positions in MetaTrader points.", "Risk");

	_stopLossShortPoints = Param(nameof(StopLossShortPoints), 267m)
	.SetNotNegative()
	.SetDisplay("Short stop loss (points)", "Stop-loss distance for short positions in MetaTrader points.", "Risk");

	_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
	.SetNotNegative()
	.SetDisplay("Short trailing stop (points)", "Trailing stop applied to short positions once they are in profit.", "Risk");

	_baseVolume = Param(nameof(BaseVolume), 0.01m)
	.SetGreaterThanZero()
	.SetDisplay("Base volume", "Initial lot size used before applying the auto-volume ladder.", "Trading");

	_useAutoVolume = Param(nameof(UseAutoVolume), true)
	.SetDisplay("Use auto volume", "Enable the MetaTrader-style balance ladder for adjusting the base lot size.", "Trading");

	_bigLotMultiplier = Param(nameof(BigLotMultiplier), 1m)
	.SetNotNegative()
	.SetDisplay("Big lot multiplier", "Multiplier applied when the balance dropped below the previous snapshot.", "Trading");

	_maxHoldingHours = Param(nameof(MaxHoldingHours), 504)
	.SetNotNegative()
	.SetDisplay("Max holding (hours)", "Maximum time a position may remain open before being closed.", "Risk");
	}

	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	public int TradeHour
	{
	get => _tradeHour.Value;
	set => _tradeHour.Value = value;
	}

	public int FirstLookback
	{
	get => _firstLookback.Value;
	set => _firstLookback.Value = value;
	}

	public int SecondLookback
	{
	get => _secondLookback.Value;
	set => _secondLookback.Value = value;
	}

	public decimal LongDeltaPoints
	{
	get => _longDeltaPoints.Value;
	set => _longDeltaPoints.Value = value;
	}

	public decimal ShortDeltaPoints
	{
	get => _shortDeltaPoints.Value;
	set => _shortDeltaPoints.Value = value;
	}

	public decimal TakeProfitLongPoints
	{
	get => _takeProfitLongPoints.Value;
	set => _takeProfitLongPoints.Value = value;
	}

	public decimal StopLossLongPoints
	{
	get => _stopLossLongPoints.Value;
	set => _stopLossLongPoints.Value = value;
	}

	public decimal TakeProfitShortPoints
	{
	get => _takeProfitShortPoints.Value;
	set => _takeProfitShortPoints.Value = value;
	}

	public decimal StopLossShortPoints
	{
	get => _stopLossShortPoints.Value;
	set => _stopLossShortPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
	get => _trailingStopPoints.Value;
	set => _trailingStopPoints.Value = value;
	}

	public decimal BaseVolume
	{
	get => _baseVolume.Value;
	set => _baseVolume.Value = value;
	}

	public bool UseAutoVolume
	{
	get => _useAutoVolume.Value;
	set => _useAutoVolume.Value = value;
	}

	public decimal BigLotMultiplier
	{
	get => _bigLotMultiplier.Value;
	set => _bigLotMultiplier.Value = value;
	}

	public int MaxHoldingHours
	{
	get => _maxHoldingHours.Value;
	set => _maxHoldingHours.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_openHistory.Clear();
	_canTrade = true;

	_longEntryPrice = null;
	_shortEntryPrice = null;
	_longEntryTime = null;
	_shortEntryTime = null;
	_shortBestPrice = null;
	_shortTrailingStopPrice = null;

	_currentBaseVolume = BaseVolume;
	_referenceBalance = 0m;
	_lastCalculatedBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_currentBaseVolume = BaseVolume;
	Volume = _currentBaseVolume;

	_referenceBalance = GetPortfolioBalance();
	_lastCalculatedBalance = _referenceBalance;

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
	base.OnPositionChanged(delta);

	if (Position > 0m)
	{
	_longEntryPrice = PositionPrice;
	_longEntryTime = CurrentTime;

	_shortEntryPrice = null;
	_shortEntryTime = null;
	_shortBestPrice = null;
	_shortTrailingStopPrice = null;
	}
	else if (Position < 0m)
	{
	_shortEntryPrice = PositionPrice;
	_shortEntryTime = CurrentTime;
	_shortBestPrice = PositionPrice;
	_shortTrailingStopPrice = null;

	_longEntryPrice = null;
	_longEntryTime = null;
	}
	else
	{
	_longEntryPrice = null;
	_shortEntryPrice = null;
	_longEntryTime = null;
	_shortEntryTime = null;
	_shortBestPrice = null;
	_shortTrailingStopPrice = null;
	}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return; // Wait for fully formed candles to mirror the MetaTrader behaviour.

	if (candle.OpenTime.Hour > TradeHour)
	_canTrade = true; // Unlock trading once the configured hour has passed.

	AddOpenPrice(candle.OpenPrice);

	ManageOpenPositions(candle);

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (Position != 0m)
	return; // The original expert keeps at most one open position.

	if (!_canTrade || candle.OpenTime.Hour != TradeHour)
	return;

	var step = GetPriceStep();
	if (step <= 0m)
	return; // Price step is required to convert MetaTrader points to native prices.

	var openFirst = GetOpenPrice(FirstLookback);
	var openSecond = GetOpenPrice(SecondLookback);
	if (openFirst is null || openSecond is null)
	return; // Not enough historical candles collected yet.

	var shortThreshold = ShortDeltaPoints * step;
	var longThreshold = LongDeltaPoints * step;

	if (shortThreshold > 0m && openFirst.Value - openSecond.Value > shortThreshold)
	{
	TryOpenShort(candle);
	return;
	}

	if (longThreshold > 0m && openSecond.Value - openFirst.Value > longThreshold)
	{
	TryOpenLong(candle);
	}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
	if (Position == 0m)
	return;

	var step = GetPriceStep();
	var longTakeDistance = step > 0m ? TakeProfitLongPoints * step : 0m;
	var longStopDistance = step > 0m ? StopLossLongPoints * step : 0m;
	var shortTakeDistance = step > 0m ? TakeProfitShortPoints * step : 0m;
	var shortStopDistance = step > 0m ? StopLossShortPoints * step : 0m;
	var trailingDistance = step > 0m ? TrailingStopPoints * step : 0m;

	var maxHolding = MaxHoldingHours > 0 ? TimeSpan.FromHours(MaxHoldingHours) : (TimeSpan?)null;

	if (Position > 0m && _longEntryPrice is decimal longEntry)
	{
	if (longTakeDistance > 0m && candle.ClosePrice >= longEntry + longTakeDistance)
	{
	SellMarket(Position);
	return;
	}

	if (longStopDistance > 0m && candle.ClosePrice <= longEntry - longStopDistance)
	{
	SellMarket(Position);
	return;
	}

	if (maxHolding.HasValue && _longEntryTime is DateTimeOffset longTime && candle.CloseTime - longTime >= maxHolding.Value)
	{
	SellMarket(Position);
	}
	}
	else if (Position < 0m && _shortEntryPrice is decimal shortEntry)
	{
	var absPosition = Math.Abs(Position);

	if (shortTakeDistance > 0m && candle.ClosePrice <= shortEntry - shortTakeDistance)
	{
	BuyMarket(absPosition);
	return;
	}

	if (shortStopDistance > 0m && candle.ClosePrice >= shortEntry + shortStopDistance)
	{
	BuyMarket(absPosition);
	return;
	}

	if (trailingDistance > 0m)
	UpdateShortTrailing(candle, trailingDistance, absPosition);

	if (maxHolding.HasValue && _shortEntryTime is DateTimeOffset shortTime && candle.CloseTime - shortTime >= maxHolding.Value)
	{
	BuyMarket(absPosition);
	}
	}
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal trailingDistance, decimal absPosition)
	{
	if (_shortBestPrice is null)
	_shortBestPrice = candle.LowPrice;
	else if (candle.LowPrice < _shortBestPrice.Value)
	_shortBestPrice = candle.LowPrice;

	if (_shortBestPrice is not decimal best)
	return;

	var candidate = best + trailingDistance;
	if (!_shortTrailingStopPrice.HasValue || candidate < _shortTrailingStopPrice.Value)
	_shortTrailingStopPrice = candidate;

	if (_shortTrailingStopPrice is decimal stop && candle.ClosePrice >= stop)
	{
	BuyMarket(absPosition);
	}
	}

	private void TryOpenLong(ICandleMessage candle)
	{
	var volume = CalculateEntryVolume();
	if (volume <= 0m)
	return;

	BuyMarket(volume);

	FinalizeEntrySnapshot(candle);
	}

	private void TryOpenShort(ICandleMessage candle)
	{
	var volume = CalculateEntryVolume();
	if (volume <= 0m)
	return;

	SellMarket(volume);

	FinalizeEntrySnapshot(candle);
	}

	private void FinalizeEntrySnapshot(ICandleMessage candle)
	{
	_canTrade = false;
	_referenceBalance = _lastCalculatedBalance;
	}

	private decimal CalculateEntryVolume()
	{
	var balance = GetPortfolioBalance();
	_lastCalculatedBalance = balance;

	var baseVolume = UseAutoVolume ? CalculateAutoVolume(balance) : BaseVolume;
	_currentBaseVolume = baseVolume;
	Volume = _currentBaseVolume;

	var volume = baseVolume;
	if (BigLotMultiplier > 1m && balance < _referenceBalance)
	volume *= BigLotMultiplier;

	return AdjustVolume(volume);
	}

	private decimal CalculateAutoVolume(decimal balance)
	{
	var volume = BaseVolume;

	ReadOnlySpan<(decimal threshold, decimal volume)> ladder =
	[
	(50m, 0.02m),
	(100m, 0.04m),
	(200m, 0.08m),
	(300m, 0.12m),
	(400m, 0.16m),
	(500m, 0.2m),
	(600m, 0.24m),
	(700m, 0.28m),
	(800m, 0.32m),
	(900m, 0.36m),
	(1000m, 0.4m),
	(1500m, 0.6m),
	(2000m, 0.8m),
	(2500m, 1m),
	(3000m, 1.2m),
	(3500m, 1.4m),
	(4000m, 1.6m),
	(4500m, 1.8m),
	(5000m, 2m),
	(5500m, 2.2m),
	(6000m, 2.4m),
	(7000m, 2.8m),
	(8000m, 3.2m),
	(9000m, 3.6m),
	(10000m, 4m),
	(15000m, 6m),
	(20000m, 8m),
	(30000m, 12m),
	(40000m, 16m),
	(50000m, 20m),
	(60000m, 24m),
	(70000m, 28m),
	(80000m, 32m),
	(90000m, 36m),
	(100000m, 40m),
	(200000m, 80m)
	];

	for (var i = 0; i < ladder.Length; i++)
	{
	var threshold = ladder[i].threshold;
	var level = ladder[i].volume;

	if (balance >= threshold)
	volume = level;
	}

	return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
	var security = Security;
	if (security == null)
	return volume;

	var step = security.VolumeStep;
	if (step > 0m)
	{
	var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
	volume = steps * step;
	}

	var minVolume = security.MinVolume;
	if (minVolume > 0m && volume < minVolume.Value)
	volume = minVolume.Value;

	var maxVolume = security.MaxVolume;
	if (maxVolume > 0m && volume > maxVolume.Value)
	volume = maxVolume.Value;

	return volume;
	}

	private void AddOpenPrice(decimal openPrice)
	{
	_openHistory.Add(openPrice);
	var maxSize = Math.Max(FirstLookback, SecondLookback) + 1;
	while (_openHistory.Count > maxSize)
	_openHistory.RemoveAt(0);
	}

	private decimal? GetOpenPrice(int offset)
	{
	if (offset < 0)
	return null;

	var index = _openHistory.Count - 1 - offset;
	if (index < 0 || index >= _openHistory.Count)
	return null;

	return _openHistory[index];
	}

	private decimal GetPriceStep()
	{
	var security = Security;
	return security?.PriceStep ?? 0m;
	}

	private decimal GetPortfolioBalance()
	{
	var portfolio = Portfolio;
	if (portfolio == null)
	return 0m;

	return portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
	}
}
