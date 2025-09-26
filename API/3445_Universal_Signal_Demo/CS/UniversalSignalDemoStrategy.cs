using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader "Universal Signal" example to StockSharp.
/// The strategy aggregates eight weighted pattern checks into a single score
/// and opens or closes positions once the configured thresholds are crossed.
/// </summary>
public class UniversalSignalDemoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalThresholdOpen;
	private readonly StrategyParam<int> _signalThresholdClose;
	private readonly StrategyParam<decimal> _priceLevel;
	private readonly StrategyParam<decimal> _stopLevel;
	private readonly StrategyParam<decimal> _takeLevel;
	private readonly StrategyParam<int> _signalExpiration;
	private readonly StrategyParam<int>[] _patternWeights;
	private readonly StrategyParam<decimal> _universalWeight;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _trendSmaPeriod;
	private readonly StrategyParam<int> _volumeSmaPeriod;

	private ExponentialMovingAverage _shortEma = null!;
	private ExponentialMovingAverage _longEma = null!;
	private RelativeStrengthIndex _rsi = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private BollingerBands _bollinger = null!;
	private SimpleMovingAverage _trendSma = null!;
	private SimpleMovingAverage _volumeSma = null!;

	private decimal _previousClose;
	private Order _pendingBuyOrder;
	private Order _pendingSellOrder;
	private DateTimeOffset? _buyOrderExpiry;
	private DateTimeOffset? _sellOrderExpiry;

	/// <summary>
	/// Initializes a new instance of the <see cref="UniversalSignalDemoStrategy"/> class.
	/// </summary>
	public UniversalSignalDemoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");

		_signalThresholdOpen = Param(nameof(SignalThresholdOpen), 10)
			.SetDisplay("Open Threshold", "Score required to open", "Signals")
			.SetRange(0, 500)
			.SetCanOptimize(true);

		_signalThresholdClose = Param(nameof(SignalThresholdClose), 10)
			.SetDisplay("Close Threshold", "Score required to close", "Signals")
			.SetRange(0, 500)
			.SetCanOptimize(true);

		_priceLevel = Param(nameof(PriceLevel), 0m)
			.SetDisplay("Price Level", "Offset in price units for pending entries", "Orders")
			.SetCanOptimize(true);

		_stopLevel = Param(nameof(StopLevel), 50m)
			.SetDisplay("Stop Loss", "Absolute stop-loss distance", "Risk")
			.SetRange(0m, 10000m)
			.SetCanOptimize(true);

		_takeLevel = Param(nameof(TakeLevel), 50m)
			.SetDisplay("Take Profit", "Absolute take-profit distance", "Risk")
			.SetRange(0m, 10000m)
			.SetCanOptimize(true);

		_signalExpiration = Param(nameof(SignalExpiration), 4)
			.SetDisplay("Signal Expiration", "Lifetime of pending entries in bars", "Orders")
			.SetRange(0, 200);

		_patternWeights = new StrategyParam<int>[8];
		for (var i = 0; i < _patternWeights.Length; i++)
		{
			var index = i;
			_patternWeights[index] = Param($"Pattern{index}Weight", 100)
				.SetDisplay($"Pattern {index}", $"Weight for pattern #{index}", "Signals")
				.SetRange(0, 200)
				.SetCanOptimize(true);
		}

		_universalWeight = Param(nameof(UniversalWeight), 1m)
			.SetDisplay("Global Weight", "Multiplier applied to the final score", "Signals")
			.SetRange(0m, 5m)
			.SetCanOptimize(true);

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 10)
			.SetDisplay("Short EMA", "Length of the fast EMA", "Indicators")
			.SetRange(1, 200)
			.SetCanOptimize(true);

		_longMaPeriod = Param(nameof(LongMaPeriod), 30)
			.SetDisplay("Long EMA", "Length of the slow EMA", "Indicators")
			.SetRange(1, 400)
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Length of the RSI", "Indicators")
			.SetRange(2, 200)
			.SetCanOptimize(true);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Length of the Bollinger basis", "Indicators")
			.SetRange(2, 400)
			.SetCanOptimize(true);

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetDisplay("Bollinger Width", "Band width multiplier", "Indicators")
			.SetRange(0.5m, 5m)
			.SetCanOptimize(true);

		_trendSmaPeriod = Param(nameof(TrendSmaPeriod), 50)
			.SetDisplay("Trend SMA", "Long-term SMA length", "Indicators")
			.SetRange(2, 400)
			.SetCanOptimize(true);

		_volumeSmaPeriod = Param(nameof(VolumeSmaPeriod), 20)
			.SetDisplay("Volume SMA", "Smoothing period for volume", "Indicators")
			.SetRange(2, 400)
			.SetCanOptimize(true);
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
	/// Score threshold for opening new positions.
	/// </summary>
	public int SignalThresholdOpen
	{
		get => _signalThresholdOpen.Value;
		set => _signalThresholdOpen.Value = value;
	}

	/// <summary>
	/// Score threshold for closing positions.
	/// </summary>
	public int SignalThresholdClose
	{
		get => _signalThresholdClose.Value;
		set => _signalThresholdClose.Value = value;
	}

	/// <summary>
	/// Offset in price units for pending entries when the signal requests a better fill.
	/// </summary>
	public decimal PriceLevel
	{
		get => _priceLevel.Value;
		set => _priceLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeLevel
	{
		get => _takeLevel.Value;
		set => _takeLevel.Value = value;
	}

	/// <summary>
	/// Number of bars after which pending entries are cancelled.
	/// </summary>
	public int SignalExpiration
	{
		get => _signalExpiration.Value;
		set => _signalExpiration.Value = value;
	}

	/// <summary>
	/// Global multiplier applied to the summed pattern score.
	/// </summary>
	public decimal UniversalWeight
	{
		get => _universalWeight.Value;
		set => _universalWeight.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA used in pattern checks.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA used in pattern checks.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands lookback length.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Trend SMA period used as a longer-term filter.
	/// </summary>
	public int TrendSmaPeriod
	{
		get => _trendSmaPeriod.Value;
		set => _trendSmaPeriod.Value = value;
	}

	/// <summary>
	/// Volume smoothing period.
	/// </summary>
	public int VolumeSmaPeriod
	{
		get => _volumeSmaPeriod.Value;
		set => _volumeSmaPeriod.Value = value;
	}

	/// <summary>
	/// Weight accessor for individual patterns.
	/// </summary>
	public int GetPatternWeight(int index)
	{
		return _patternWeights[index].Value;
	}

	/// <summary>
	/// Allows UI bindings to update pattern weights.
	/// </summary>
	public void SetPatternWeight(int index, int weight)
	{
		_patternWeights[index].Value = weight;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = 0m;
		_pendingBuyOrder = null;
		_pendingSellOrder = null;
		_buyOrderExpiry = null;
		_sellOrderExpiry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortEma = new ExponentialMovingAverage { Length = ShortMaPeriod };
		_longEma = new ExponentialMovingAverage { Length = LongMaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
		Macd =
		{
		ShortMa = { Length = ShortMaPeriod },
		LongMa = { Length = LongMaPeriod }
		},
		SignalMa = { Length = Math.Max(9, ShortMaPeriod / 2) }
		};
		_bollinger = new BollingerBands
		{
		Length = BollingerPeriod,
		Width = BollingerWidth
		};
		_trendSma = new SimpleMovingAverage { Length = TrendSmaPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([_shortEma, _longEma, _rsi, _macd, _bollinger], ProcessCandle)
		.Start();

		StartProtection(
		takeProfit: new Unit(TakeLevel, UnitTypes.Absolute),
		stopLoss: new Unit(StopLevel, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
	UpdatePendingOrdersLifetime(candle);

	if (candle.State != CandleStates.Finished)
	return;

	UpdatePendingOrderHandles();

	var shortEma = values[0].ToDecimal();
	var longEma = values[1].ToDecimal();
	var rsiValue = values[2].ToDecimal();

	if (values[3] is not MovingAverageConvergenceDivergenceSignalValue macdValue)
	return;

	if (values[4] is not BollingerBandsValue bollingerValue)
	return;

	var trendSmaValue = _trendSma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
	var volumeAverage = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

	if (!_shortEma.IsFormed || !_longEma.IsFormed || !_rsi.IsFormed || !_macd.IsFormed || !_bollinger.IsFormed || !_trendSma.IsFormed || !_volumeSma.IsFormed)
	{
	_previousClose = candle.ClosePrice;
	return;
	}

	var histogram = macdValue.Macd - macdValue.Signal;

	if (bollingerValue.MovingAverage is not decimal middleBand)
	return;

	var score = 0m;
	score += GetContribution(candle.ClosePrice > shortEma, GetPatternWeight(0));
	score += GetContribution(shortEma > longEma, GetPatternWeight(1));
	score += GetContribution(rsiValue > 50m, GetPatternWeight(2));
	score += GetContribution(histogram > 0, GetPatternWeight(3));
	score += GetContribution(candle.ClosePrice > middleBand, GetPatternWeight(4));
	score += GetContribution(candle.ClosePrice > trendSmaValue, GetPatternWeight(5));
	score += GetContribution(candle.ClosePrice > candle.OpenPrice, GetPatternWeight(6));
	score += GetContribution(candle.TotalVolume > volumeAverage, GetPatternWeight(7));

	score *= UniversalWeight;

	if (Position > 0 && score <= -SignalThresholdClose)
	{
	SellMarket(Position);
	CancelPendingOrder(ref _pendingSellOrder, ref _sellOrderExpiry);
	}
	else if (Position < 0 && score >= SignalThresholdClose)
	{
	BuyMarket(Math.Abs(Position));
	CancelPendingOrder(ref _pendingBuyOrder, ref _buyOrderExpiry);
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousClose = candle.ClosePrice;
	return;
	}

	var priceStep = Security?.StepPrice ?? 1m;
	var expiration = GetExpirationSpan();

	if (score >= SignalThresholdOpen && Position <= 0)
	{
	var volume = Volume + Math.Max(0m, -Position);
	PlaceLongEntry(candle, volume, priceStep, expiration);
	}
	else if (score <= -SignalThresholdOpen && Position >= 0)
	{
	var volume = Volume + Math.Max(0m, Position);
	PlaceShortEntry(candle, volume, priceStep, expiration);
	}

	_previousClose = candle.ClosePrice;
	}

	private static decimal GetContribution(bool condition, int weight)
	{
	if (weight == 0)
	return 0m;

	return condition ? weight : -weight;
	}

	private void PlaceLongEntry(ICandleMessage candle, decimal volume, decimal priceStep, TimeSpan expiration)
	{
	CancelPendingOrder(ref _pendingBuyOrder, ref _buyOrderExpiry);

	if (PriceLevel <= 0m)
	{
	BuyMarket(volume);
	return;
	}

	var limitPrice = candle.ClosePrice - PriceLevel * priceStep;
	limitPrice = Security != null ? Security.ShrinkPrice(limitPrice) : limitPrice;

	_pendingBuyOrder = BuyLimit(volume, limitPrice);
	_buyOrderExpiry = expiration == TimeSpan.Zero ? null : candle.CloseTime + expiration;
	}

	private void PlaceShortEntry(ICandleMessage candle, decimal volume, decimal priceStep, TimeSpan expiration)
	{
	CancelPendingOrder(ref _pendingSellOrder, ref _sellOrderExpiry);

	if (PriceLevel <= 0m)
	{
	SellMarket(volume);
	return;
	}

	var limitPrice = candle.ClosePrice + PriceLevel * priceStep;
	limitPrice = Security != null ? Security.ShrinkPrice(limitPrice) : limitPrice;

	_pendingSellOrder = SellLimit(volume, limitPrice);
	_sellOrderExpiry = expiration == TimeSpan.Zero ? null : candle.CloseTime + expiration;
	}

	private void UpdatePendingOrderHandles()
	{
	if (_pendingBuyOrder != null && _pendingBuyOrder.State != OrderStates.Active)
	{
	_pendingBuyOrder = null;
	_buyOrderExpiry = null;
	}

	if (_pendingSellOrder != null && _pendingSellOrder.State != OrderStates.Active)
	{
	_pendingSellOrder = null;
	_sellOrderExpiry = null;
	}
	}

	private void UpdatePendingOrdersLifetime(ICandleMessage candle)
	{
	if (_pendingBuyOrder != null && _pendingBuyOrder.State == OrderStates.Active && _buyOrderExpiry is DateTimeOffset buyExpiry && candle.CloseTime >= buyExpiry)
	{
	CancelPendingOrder(ref _pendingBuyOrder, ref _buyOrderExpiry);
	}

	if (_pendingSellOrder != null && _pendingSellOrder.State == OrderStates.Active && _sellOrderExpiry is DateTimeOffset sellExpiry && candle.CloseTime >= sellExpiry)
	{
	CancelPendingOrder(ref _pendingSellOrder, ref _sellOrderExpiry);
	}
	}

	private void CancelPendingOrder(ref Order order, ref DateTimeOffset? expiry)
	{
	if (order == null)
	{
	expiry = null;
	return;
	}

	if (order.State == OrderStates.Active)
	CancelOrder(order);

	order = null;
	expiry = null;
	}

	private TimeSpan GetExpirationSpan()
	{
	if (SignalExpiration <= 0)
	return TimeSpan.Zero;

	if (CandleType.Arg is TimeSpan timeFrame && timeFrame > TimeSpan.Zero)
	return TimeSpan.FromTicks(timeFrame.Ticks * SignalExpiration);

	return TimeSpan.FromMinutes(SignalExpiration);
	}
}
