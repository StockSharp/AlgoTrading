using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ProtoType IX strategy converted from MetaTrader 4.
/// Uses Williams %R swings combined with ATR based breakouts to detect trends.
/// Opens trades only when the potential reward versus risk is attractive enough.
/// Includes a parabolic style trailing stop that switches to ATR trailing after a delay.
/// </summary>
public class PrototypeIxStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _criteriaWpr;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _zeroBarDelay;
	private readonly StrategyParam<decimal> _minTargetInSpread;
	private readonly StrategyParam<decimal> _tpSlCriteria;
	private readonly StrategyParam<int> _maxOpenedOrders;
	private readonly StrategyParam<decimal> _maxOrderSize;
	private readonly StrategyParam<decimal> _riskDelta;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastSwingHigh;
	private decimal? _previousSwingHigh;
	private decimal? _lastSwingLow;
	private decimal? _previousSwingLow;
	private bool _trackingUpSwing;
	private bool _trackingDownSwing;
	private int _wprTrend;
	private int _barsSinceEntry;
	private decimal _entryPrice;
	private decimal _initialStopPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Williams %R indicator length.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R thresholds absolute value.
	/// </summary>
	public decimal CriteriaWpr
	{
		get => _criteriaWpr.Value;
		set => _criteriaWpr.Value = value;
	}

	/// <summary>
	/// Average True Range length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for breakout validation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Number of bars to wait before switching to ATR trailing.
	/// </summary>
	public int ZeroBarDelay
	{
		get => _zeroBarDelay.Value;
		set => _zeroBarDelay.Value = value;
	}

	/// <summary>
	/// Minimum target expressed in spread multiples.
	/// </summary>
	public decimal MinTargetInSpread
	{
		get => _minTargetInSpread.Value;
		set => _minTargetInSpread.Value = value;
	}

	/// <summary>
	/// Required take-profit to stop-loss ratio.
	/// </summary>
	public decimal TpSlCriteria
	{
		get => _tpSlCriteria.Value;
		set => _tpSlCriteria.Value = value;
	}

	/// <summary>
	/// Maximum simultaneously opened orders.
	/// </summary>
	public int MaxOpenedOrders
	{
		get => _maxOpenedOrders.Value;
		set => _maxOpenedOrders.Value = value;
	}

	/// <summary>
	/// Maximum single order size.
	/// </summary>
	public decimal MaxOrderSize
	{
		get => _maxOrderSize.Value;
		set => _maxOrderSize.Value = value;
	}

	/// <summary>
	/// Risk percentage used for position sizing.
	/// </summary>
	public decimal RiskDelta
	{
		get => _riskDelta.Value;
		set => _riskDelta.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PrototypeIxStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(4, 20, 2);

		_criteriaWpr = Param(nameof(CriteriaWpr), 25m)
		.SetGreaterThanZero()
		.SetDisplay("Criteria WPR", "Absolute threshold for the Williams %R levels", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(15m, 35m, 5m);

		_atrPeriod = Param(nameof(AtrPeriod), 40)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Length of the ATR indicator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 10);

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for breakout detection", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.3m, 1.0m, 0.1m);

		_zeroBarDelay = Param(nameof(ZeroBarDelay), 8)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Zero Bar", "Bars before activating ATR trailing", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(2, 12, 2);

		_minTargetInSpread = Param(nameof(MinTargetInSpread), 5m)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Min Target Spread", "Minimum target measured in spread multiples", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);

		_tpSlCriteria = Param(nameof(TpSlCriteria), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("TP/SL Criteria", "Required ratio between take-profit and stop-loss", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1.2m, 3.0m, 0.2m);

		_maxOpenedOrders = Param(nameof(MaxOpenedOrders), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Orders", "Maximum simultaneously opened orders", "General")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_maxOrderSize = Param(nameof(MaxOrderSize), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Max Order Size", "Upper bound for calculated order volume", "Risk Management");

		_riskDelta = Param(nameof(RiskDelta), 5.0m)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Risk %", "Risk percentage used for position sizing", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 10.0m, 1.0m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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

		_lastSwingHigh = null;
		_previousSwingHigh = null;
		_lastSwingLow = null;
		_previousSwingLow = null;
		_trackingUpSwing = false;
		_trackingDownSwing = false;
		_wprTrend = 0;
		_barsSinceEntry = 0;
		_entryPrice = 0m;
		_initialStopPrice = 0m;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var williams = new WilliamsR { Length = WilliamsPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(williams, atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!UpdateSwingPoints(candle, wprValue))
			return;

		var atrIsValid = atrValue > 0m;

		if (!atrIsValid)
			return;

		var swingTrend = DetermineSwingTrend(atrValue);

		if (Position == 0)
			{
			_barsSinceEntry = 0;

			if (Math.Abs(swingTrend) == 1 && swingTrend == _wprTrend)
				TryOpenPosition(candle, swingTrend, atrValue);
		}
		else
		{
			_barsSinceEntry++;
			UpdateTrailingProtection(candle, atrValue);
		}
	}

	private bool UpdateSwingPoints(ICandleMessage candle, decimal wprValue)
	{
		var upperThreshold = -CriteriaWpr;
		var lowerThreshold = CriteriaWpr - 100m;

		if (wprValue >= upperThreshold)
			{
			_wprTrend = 1;

			if (!_trackingUpSwing)
				{
				_previousSwingHigh = _lastSwingHigh;
				_lastSwingHigh = candle.HighPrice;
				_trackingUpSwing = true;
				_trackingDownSwing = false;
			}
			else if (_lastSwingHigh is decimal lastHigh)
				{
				_lastSwingHigh = Math.Max(lastHigh, candle.HighPrice);
			}
		}
		else if (wprValue <= lowerThreshold)
			{
			_wprTrend = -1;

			if (!_trackingDownSwing)
				{
				_previousSwingLow = _lastSwingLow;
				_lastSwingLow = candle.LowPrice;
				_trackingDownSwing = true;
				_trackingUpSwing = false;
			}
			else if (_lastSwingLow is decimal lastLow)
				{
				_lastSwingLow = Math.Min(lastLow, candle.LowPrice);
			}
		}
		else
		{
			_wprTrend = 0;
			_trackingUpSwing = false;
			_trackingDownSwing = false;
		}

		return _lastSwingHigh.HasValue && _previousSwingHigh.HasValue && _lastSwingLow.HasValue && _previousSwingLow.HasValue;
	}

	private int DetermineSwingTrend(decimal atrValue)
	{
		if (!_lastSwingHigh.HasValue || !_previousSwingHigh.HasValue || !_lastSwingLow.HasValue || !_previousSwingLow.HasValue)
			return 0;

		var lastHigh = _lastSwingHigh.Value;
		var prevHigh = _previousSwingHigh.Value;
		var lastLow = _lastSwingLow.Value;
		var prevLow = _previousSwingLow.Value;

		var atrTrigger = AtrMultiplier * atrValue;

		if ((lastHigh - prevHigh) >= atrTrigger && lastLow > prevLow)
			return 1;

		if ((prevLow - lastLow) >= atrTrigger && prevHigh > lastHigh)
			return -1;

		return 0;
	}

	private void TryOpenPosition(ICandleMessage candle, int direction, decimal atrValue)
	{
		if (MaxOpenedOrders <= 0)
			return;

		if (Math.Abs(Position) >= MaxOpenedOrders)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return;

		var spreadValue = (Security?.StepPrice ?? priceStep) * MinTargetInSpread;

		if (direction > 0)
			{
			var target = Math.Max(_lastSwingHigh!.Value, _previousSwingHigh!.Value);
			var support = Math.Max(_lastSwingLow!.Value, _previousSwingLow!.Value);
			var entryPrice = candle.ClosePrice;
			var stopDistance = entryPrice - support;
			var targetDistance = target - entryPrice;

			if (stopDistance <= 0m || targetDistance <= spreadValue)
				return;

			var ratio = targetDistance / stopDistance;
			if (ratio < TpSlCriteria)
				return;

			var volume = CalculateOrderVolume(stopDistance);
			if (volume <= 0m)
				return;

			var resultingPosition = Position + volume;
			BuyMarket(volume);
			ApplyInitialProtection(entryPrice, support, target, resultingPosition);

			_isLongPosition = true;
			_entryPrice = entryPrice;
			_initialStopPrice = support;
			LogInfo($"Open long at {entryPrice}, stop {support}, target {target}, volume {volume}, ratio {ratio:F2}");
		}
		else
		{
			var target = Math.Min(_lastSwingLow!.Value, _previousSwingLow!.Value);
			var resistance = Math.Min(_lastSwingHigh!.Value, _previousSwingHigh!.Value);
			var entryPrice = candle.ClosePrice;
			var stopDistance = resistance - entryPrice;
			var targetDistance = entryPrice - target;

			if (stopDistance <= 0m || targetDistance <= spreadValue)
				return;

			var ratio = targetDistance / stopDistance;
			if (ratio < TpSlCriteria)
				return;

			var volume = CalculateOrderVolume(stopDistance);
			if (volume <= 0m)
				return;

			var resultingPosition = Position - volume;
			SellMarket(volume);
			ApplyInitialProtection(entryPrice, resistance, target, resultingPosition);

			_isLongPosition = false;
			_entryPrice = entryPrice;
			_initialStopPrice = resistance;
			LogInfo($"Open short at {entryPrice}, stop {resistance}, target {target}, volume {volume}, ratio {ratio:F2}");
		}

		_barsSinceEntry = 0;
	}

	private void ApplyInitialProtection(decimal entryPrice, decimal stopPrice, decimal targetPrice, decimal resultingPosition)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return;

		if (stopPrice > 0m)
			{
			var stopDistanceSteps = Math.Abs(entryPrice - stopPrice) / priceStep;
			if (stopDistanceSteps > 0m)
				SetStopLoss(stopDistanceSteps, entryPrice, resultingPosition);
		}

		if (targetPrice > 0m)
			{
			var takeDistanceSteps = Math.Abs(targetPrice - entryPrice) / priceStep;
			if (takeDistanceSteps > 0m)
				SetTakeProfit(takeDistanceSteps, entryPrice, resultingPosition);
		}
	}

	private void UpdateTrailingProtection(ICandleMessage candle, decimal atrValue)
	{
		if (Position == 0)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return;

		if (_barsSinceEntry < ZeroBarDelay)
			return;

		var referencePrice = candle.ClosePrice;

		if (_isLongPosition)
			{
			var atrStop = referencePrice - (2m * atrValue);
			var newStop = Math.Max(_initialStopPrice, atrStop);

			if (newStop > 0m && newStop > _initialStopPrice && newStop < referencePrice)
				{
				var distanceSteps = (referencePrice - newStop) / priceStep;
				if (distanceSteps > 0m)
					{
					SetStopLoss(distanceSteps, referencePrice, Position);
					_initialStopPrice = newStop;
					LogInfo($"Trail long stop to {newStop}");
				}
			}
		}
		else
		{
			var atrStop = referencePrice + (2m * atrValue);
			var newStop = Math.Min(_initialStopPrice, atrStop);

			if (newStop > 0m && newStop < _initialStopPrice && newStop > referencePrice)
				{
				var distanceSteps = (newStop - referencePrice) / priceStep;
				if (distanceSteps > 0m)
					{
					SetStopLoss(distanceSteps, referencePrice, Position);
					_initialStopPrice = newStop;
					LogInfo($"Trail short stop to {newStop}");
				}
			}
		}
	}

	private decimal CalculateOrderVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepValue = Security?.StepPrice ?? 0m;
		var minVolume = Security?.MinVolume ?? 0m;
		var volumeStep = Security?.VolumeStep ?? 0m;

		if (priceStep <= 0m)
			return 0m;

		if (stepValue <= 0m)
			stepValue = priceStep;

		if (minVolume <= 0m)
			minVolume = 1m;

		if (volumeStep <= 0m)
			volumeStep = minVolume;

		var portfolioValue = Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m)
			portfolioValue = 100000m;

		var riskAmount = portfolioValue * RiskDelta / 100m;
		if (riskAmount <= 0m)
			riskAmount = stepValue * minVolume;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return minVolume;

		var volume = riskAmount / (steps * stepValue);
		var normalized = NormalizeVolume(volume, volumeStep, minVolume);

		return Math.Min(MaxOrderSize, normalized);
	}

	private static decimal NormalizeVolume(decimal volume, decimal step, decimal minVolume)
	{
		if (step <= 0m)
			return volume;

		var steps = Math.Floor(volume / step);
		var result = steps * step;

		if (result < minVolume)
			result = minVolume;

		return result;
	}
}
