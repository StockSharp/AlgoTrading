using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "KA-Gold Bot".
/// Trades breakouts of a Keltner-style channel confirmed by trend filters from EMA(10) and EMA(200).
/// Applies spread filtering, trading session control, configurable position sizing, and trailing stop management.
/// </summary>
public class KAGoldBotStrategy : Strategy
{
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useRiskPercent;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingTriggerPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private ExponentialMovingAverage _keltnerEma = null!;
	private SimpleMovingAverage _rangeAverage = null!;

	private decimal? _closePrev1;
	private decimal? _closePrev2;
	private decimal? _fastPrev1;
	private decimal? _fastPrev2;
	private decimal? _slowPrev1;
	private decimal? _upperPrev1;
	private decimal? _upperPrev2;
	private decimal? _lowerPrev1;
	private decimal? _lowerPrev2;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingTriggerDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	private bool _longTrailingArmed;
	private bool _shortTrailingArmed;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	/// <summary>
	/// Keltner channel length used for the midline EMA and range average.
	/// </summary>
	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period (original EMA10).
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period (original EMA200).
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Base volume step used when position sizing is fixed or rounded.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enables risk-based position sizing using account capital.
	/// </summary>
	public bool UseRiskPercent
	{
		get => _useRiskPercent.Value;
		set => _useRiskPercent.Value = value;
	}

	/// <summary>
	/// Percentage of account equity allocated per trade when risk sizing is active.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in pips (0 disables the target).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Profit threshold that activates the trailing stop (expressed in pips).
	/// </summary>
	public decimal TrailingTriggerPips
	{
		get => _trailingTriggerPips.Value;
		set => _trailingTriggerPips.Value = value;
	}

	/// <summary>
	/// Distance between market price and trailing stop once armed (in pips).
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal step required before the trailing stop is advanced (in pips).
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables the time filter that restricts signal evaluation.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start hour (inclusive) in platform time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute (inclusive) in platform time.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session end hour (exclusive) in platform time.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Session end minute (exclusive) in platform time.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread expressed in price steps (0 disables the check).
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="KAGoldBotStrategy"/>.
	/// </summary>
	public KAGoldBotStrategy()
	{
		_keltnerPeriod = Param(nameof(KeltnerPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "Length of the EMA and range average", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA filter", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA trend filter", "Indicators");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Minimal volume step used for entries", "Trading");

		_useRiskPercent = Param(nameof(UseRiskPercent), true)
			.SetDisplay("Use Risk %", "Toggle balance based position sizing", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Percentage of capital allocated per trade", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk");

		_trailingTriggerPips = Param(nameof(TrailingTriggerPips), 300m)
			.SetDisplay("Trail Trigger (pips)", "Profit required before trailing activates", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 300m)
			.SetDisplay("Trail Distance (pips)", "Distance kept between price and trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 100m)
			.SetDisplay("Trail Step (pips)", "Minimal improvement before stop is moved", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Session Filter", "Enable trading session restrictions", "Session");

		_startHour = Param(nameof(StartHour), 2)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 30)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_endHour = Param(nameof(EndHour), 21)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 0)
			.SetDisplay("End Minute", "Session end minute", "Session");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 65m)
			.SetDisplay("Max Spread", "Maximum allowed spread in price steps", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_closePrev1 = null;
		_closePrev2 = null;
		_fastPrev1 = null;
		_fastPrev2 = null;
		_slowPrev1 = null;
		_upperPrev1 = null;
		_upperPrev2 = null;
		_lowerPrev1 = null;
		_lowerPrev2 = null;

		_longTrailingArmed = false;
		_shortTrailingArmed = false;
		_stopOrder = null;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_keltnerEma = new ExponentialMovingAverage { Length = KeltnerPeriod };
		_rangeAverage = new SimpleMovingAverage { Length = KeltnerPeriod };

		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;
		_pipSize = step;
		if (decimals == 3 || decimals == 5 || decimals == 1)
			_pipSize = step * 10m;

		_stopLossDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		_takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		_trailingTriggerDistance = TrailingTriggerPips > 0 ? TrailingTriggerPips * _pipSize : 0m;
		_trailingStopDistance = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
		_trailingStepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _keltnerEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var midValue = _keltnerEma.Process(candle).ToNullableDecimal();
		var rangeValue = _rangeAverage.Process(new DecimalIndicatorValue(_rangeAverage, candle.HighPrice - candle.LowPrice, candle.OpenTime)).ToNullableDecimal();

		if (midValue is not decimal mid || rangeValue is not decimal avgRange)
		{
			UpdateHistory(candle, fastValue, slowValue, null, null);
			return;
		}

		var upper = mid + avgRange;
		var lower = mid - avgRange;

		TryManagePosition(candle);

		if (CanEvaluateSignals())
		{
			var sessionOk = !UseTimeFilter || IsWithinSession(candle.OpenTime.TimeOfDay);
			var spreadOk = IsSpreadAcceptable();

			if (sessionOk && spreadOk && Position == 0)
			{
				if (IsBuySignal())
					EnterPosition(true);
				else if (IsSellSignal())
					EnterPosition(false);
			}
		}

		UpdateHistory(candle, fastValue, slowValue, upper, lower);
	}

	private void EnterPosition(bool isLong)
	{
	var volume = GetTradeVolume();
	if (volume <= 0)
	return;

	if (isLong)
	{
	BuyMarket(volume);
	_longTrailingArmed = false;
	_shortTrailingArmed = false;
	}
	else
	{
	SellMarket(volume);
	_longTrailingArmed = false;
	_shortTrailingArmed = false;
	}
	}

	private decimal GetTradeVolume()
	{
		var volume = BaseVolume;
		if (!UseRiskPercent)
			return Math.Max(volume, 0m);

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return Math.Max(volume, 0m);

		var target = equity * (RiskPercent / 100m) / 100000m;
		var steps = Math.Floor(target / BaseVolume);
		if (steps < 1m)
			steps = 1m;

		volume = steps * BaseVolume;

		var min = Security?.MinVolume ?? BaseVolume;
		var max = Security?.MaxVolume ?? volume;
		var step = Security?.VolumeStep ?? 0m;

		if (volume < min)
			volume = min;
		if (volume > max)
			volume = max;

		if (step > 0m)
		{
			var stepCount = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = stepCount * step;

			if (volume < min)
				volume = min;
			if (volume > max)
				volume = max;
		}

		return volume;
	}

	private bool CanEvaluateSignals()
	{
	return _closePrev1.HasValue && _closePrev2.HasValue &&
	_fastPrev1.HasValue && _fastPrev2.HasValue &&
	_slowPrev1.HasValue &&
	_upperPrev1.HasValue && _upperPrev2.HasValue &&
	_lowerPrev1.HasValue && _lowerPrev2.HasValue;
	}

	private bool IsBuySignal()
	{
	if (!CanEvaluateSignals())
	return false;

	var entryBuy1 = _closePrev1 > _upperPrev1;
	var entryBuy2 = _closePrev1 > _slowPrev1;
	var entryBuy3 = _fastPrev2 < _upperPrev2 && _fastPrev1 > _upperPrev1;

	return entryBuy1 && entryBuy2 && entryBuy3;
	}

	private bool IsSellSignal()
	{
	if (!CanEvaluateSignals())
	return false;

	var entrySell1 = _closePrev1 < _lowerPrev1;
	var entrySell2 = _closePrev1 < _slowPrev1;
	var entrySell3 = _fastPrev2 > _lowerPrev2 && _fastPrev1 < _lowerPrev1;

	return entrySell1 && entrySell2 && entrySell3;
	}

	private bool IsWithinSession(TimeSpan time)
	{
	var start = new TimeSpan(StartHour, StartMinute, 0);
	var end = new TimeSpan(EndHour, EndMinute, 0);

	return start <= end ? time >= start && time < end : time >= start || time < end;
	}

	private bool IsSpreadAcceptable()
	{
	if (MaxSpreadPoints <= 0)
	return true;

	var bestAsk = Security?.BestAskPrice ?? 0m;
	var bestBid = Security?.BestBidPrice ?? 0m;
	if (bestAsk == 0m || bestBid == 0m)
	return true;

	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	return true;

	var spreadPoints = (bestAsk - bestBid) / step;
	return spreadPoints <= MaxSpreadPoints;
	}

	private void TryManagePosition(ICandleMessage candle)
	{
	if (Position == 0)
	return;

	var entryPrice = PositionPrice ?? candle.ClosePrice;
	var isLong = Position > 0;
	EnsureProtection(isLong, entryPrice);
	UpdateTrailing(isLong, candle.ClosePrice, entryPrice);
	}

	private void EnsureProtection(bool isLong, decimal referencePrice)
	{
	var volume = Math.Abs(Position);
	if (volume <= 0)
	return;

	if (_stopOrder == null && _stopLossDistance > 0)
	{
	var stopPrice = isLong ? referencePrice - _stopLossDistance : referencePrice + _stopLossDistance;
	_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
	}

	if (_takeProfitOrder == null && _takeProfitDistance > 0)
	{
	var tpPrice = isLong ? referencePrice + _takeProfitDistance : referencePrice - _takeProfitDistance;
	_takeProfitOrder = isLong ? SellLimit(volume, tpPrice) : BuyLimit(volume, tpPrice);
	}
	}

	private void UpdateTrailing(bool isLong, decimal currentPrice, decimal entryPrice)
	{
	if (_trailingStopDistance <= 0)
	return;

	var profit = isLong ? currentPrice - entryPrice : entryPrice - currentPrice;
	if (profit <= 0)
	return;

	if (isLong)
	{
	if (!_longTrailingArmed && profit >= _trailingTriggerDistance)
	_longTrailingArmed = true;

	if (_longTrailingArmed)
	{
	var desiredStop = currentPrice - _trailingStopDistance;
	var currentStop = _stopOrder?.Price;
	var shouldMove = currentStop == null || (desiredStop - currentStop >= _trailingStepDistance && desiredStop > currentStop);

	if (shouldMove)
	MoveStop(true, desiredStop);
	}
	}
	else
	{
	if (!_shortTrailingArmed && profit >= _trailingTriggerDistance)
	_shortTrailingArmed = true;

	if (_shortTrailingArmed)
	{
	var desiredStop = currentPrice + _trailingStopDistance;
	var currentStop = _stopOrder?.Price;
	var shouldMove = currentStop == null || (currentStop - desiredStop >= _trailingStepDistance && desiredStop < currentStop);

	if (shouldMove)
	MoveStop(false, desiredStop);
	}
	}
	}

	private void MoveStop(bool isLong, decimal price)
	{
	if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
	CancelOrder(_stopOrder);

	var volume = Math.Abs(Position);
	if (volume <= 0)
	{
	_stopOrder = null;
	return;
	}

	_stopOrder = isLong ? SellStop(volume, price) : BuyStop(volume, price);
	}

	private void UpdateHistory(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal? upper, decimal? lower)
	{
	_closePrev2 = _closePrev1;
	_closePrev1 = candle.ClosePrice;

	_fastPrev2 = _fastPrev1;
	_fastPrev1 = fastValue;

	_slowPrev1 = slowValue;

	_upperPrev2 = _upperPrev1;
	_upperPrev1 = upper;

	_lowerPrev2 = _lowerPrev1;
	_lowerPrev1 = lower;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
	base.OnOrderChanged(order);

	if (_stopOrder != null && order == _stopOrder && order.State.IsFinished())
	_stopOrder = null;

	if (_takeProfitOrder != null && order == _takeProfitOrder && order.State.IsFinished())
	_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
	base.OnPositionChanged(delta);

	if (Position == 0)
	{
	_longTrailingArmed = false;
	_shortTrailingArmed = false;

	if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
	CancelOrder(_stopOrder);
	if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
	CancelOrder(_takeProfitOrder);

	_stopOrder = null;
	_takeProfitOrder = null;
	}
	}
}
