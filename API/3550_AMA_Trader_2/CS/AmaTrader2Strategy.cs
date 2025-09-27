
namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Adaptive moving average averaging strategy with RSI confirmation and
/// extended money-management inspired by the "AMA Trader 2" MetaTrader expert.
/// </summary>
public class AmaTrader2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _stepLength;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingActivation;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minStep;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _useTimeWindow;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;

	private readonly Queue<decimal> _rsiValues = new();

	private RSI _rsi = null!;
	private KaufmanAdaptiveMovingAverage _ama = null!;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private DateTimeOffset? _lastLongEntryBar;
	private DateTimeOffset? _lastShortEntryBar;
	private DateTimeOffset? _pendingLongBar;
	private DateTimeOffset? _pendingShortBar;

	/// <summary>
	/// Initializes a new instance of the <see cref="AmaTrader2Strategy"/> class.
	/// </summary>
	public AmaTrader2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicators and trading logic.", "General");

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Lot Size", "Volume submitted with each averaging order.", "Trading")
			.SetGreaterThanZero();

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Number of periods used for the RSI filter.", "RSI")
			.SetGreaterThanZero();

		_stepLength = Param(nameof(StepLength), 3)
			.SetDisplay("Step Length", "How many recent RSI readings must confirm a signal (0 = latest only).", "RSI")
			.SetNotNegative();

		_rsiLevelUp = Param(nameof(RsiLevelUp), 70m)
			.SetDisplay("RSI Upper Level", "Overbought threshold that allows short setups.", "RSI");

		_rsiLevelDown = Param(nameof(RsiLevelDown), 30m)
			.SetDisplay("RSI Lower Level", "Oversold threshold that allows long setups.", "RSI");

		_amaLength = Param(nameof(AmaLength), 15)
			.SetDisplay("AMA Length", "Averaging period for the adaptive moving average.", "AMA")
			.SetGreaterThanZero();

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
			.SetDisplay("AMA Fast Period", "Fast smoothing constant used by AMA.", "AMA")
			.SetGreaterThanZero();

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
			.SetDisplay("AMA Slow Period", "Slow smoothing constant used by AMA.", "AMA")
			.SetGreaterThanZero();

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Protective stop distance in price units (0 disables).", "Risk")
			.SetNotNegative();

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Profit target distance in price units (0 disables).", "Risk")
			.SetNotNegative();

		_trailingActivation = Param(nameof(TrailingActivation), 0m)
			.SetDisplay("Trailing Activation", "Minimum profit required before the trailing stop is armed.", "Risk")
			.SetNotNegative();

		_trailingDistance = Param(nameof(TrailingDistance), 0m)
			.SetDisplay("Trailing Distance", "Distance between price and trailing stop once activated.", "Risk")
			.SetNotNegative();

		_trailingStep = Param(nameof(TrailingStep), 0m)
			.SetDisplay("Trailing Step", "Minimum improvement required before the trailing stop is tightened.", "Risk")
			.SetNotNegative();

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetDisplay("Max Positions", "Maximum averaging entries allowed per direction (0 disables).", "Trading")
			.SetNotNegative();

		_minStep = Param(nameof(MinStep), 0m)
			.SetDisplay("Min Step", "Minimum price distance between consecutive entries (0 disables).", "Trading")
			.SetNotNegative();

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Whether to flatten opposite exposure before opening a new trade.", "Trading");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One Position", "Disallow new entries while any position is open.", "Trading");

		_useTimeWindow = Param(nameof(UseTimeWindow), false)
			.SetDisplay("Use Time Window", "Restrict trading to the configured intraday window.", "Timing");

		_startTime = Param(nameof(StartTime), new TimeSpan(10, 0, 0))
			.SetDisplay("Start Time", "Session start time (UTC).", "Timing");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 0, 0))
			.SetDisplay("End Time", "Session end time (UTC).", "Timing");
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume submitted with each averaging order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Number of RSI readings that must confirm a signal.
	/// </summary>
	public int StepLength
	{
		get => _stepLength.Value;
		set => _stepLength.Value = value;
	}

	/// <summary>
	/// Overbought threshold for short entries.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// Oversold threshold for long entries.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	/// <summary>
	/// Adaptive moving average period.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// Fast smoothing constant for AMA.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing constant for AMA.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Profit threshold that arms the trailing stop.
	/// </summary>
	public decimal TrailingActivation
	{
		get => _trailingActivation.Value;
		set => _trailingActivation.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop once active.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Minimum improvement required for trailing adjustments.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Maximum number of averaging entries per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum distance between consecutive entries.
	/// </summary>
	public decimal MinStep
	{
		get => _minStep.Value;
		set => _minStep.Value = value;
	}

	/// <summary>
	/// Whether to close the opposite side before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Whether only one net position can exist at any time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Enable intraday time filtering.
	/// </summary>
	public bool UseTimeWindow
	{
		get => _useTimeWindow.Value;
		set => _useTimeWindow.Value = value;
	}

	/// <summary>
	/// Session start time used when <see cref="UseTimeWindow"/> is enabled.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Session end time used when <see cref="UseTimeWindow"/> is enabled.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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

		_rsiValues.Clear();
		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_lastLongEntryPrice = null;
		_lastShortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastLongEntryBar = null;
		_lastShortEntryBar = null;
		_pendingLongBar = null;
		_pendingShortBar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;

		_rsi = new RSI
		{
			Length = RsiLength
		};

		_ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = AmaFastPeriod,
			SlowSCPeriod = AmaSlowPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _ama, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ama);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal amaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
			return;

		Volume = LotSize;

		UpdateRsiValues(rsiValue);
		UpdateTrailingStops(candle);

		var stepLength = Math.Max(1, StepLength);

		var oversold = false;
		var overbought = false;
		var inspected = 0;
		foreach (var value in _rsiValues)
		{
			if (value < RsiLevelDown)
				oversold = true;
			if (value > RsiLevelUp)
				overbought = true;

			inspected++;
			if (inspected >= stepLength)
				break;
		}

		var price = candle.ClosePrice;
		var barTime = candle.OpenTime;

		if (price > amaValue && oversold && !IsSameBar(_lastLongEntryBar, barTime))
			TryEnterLong(price, barTime);
		else if (price < amaValue && overbought && !IsSameBar(_lastShortEntryBar, barTime))
			TryEnterShort(price, barTime);
	}

	private void TryEnterLong(decimal price, DateTimeOffset barTime)
	{
		if (!CanEnterLong(price))
			return;

		var unrealized = GetLongUnrealizedPnL(price);
		if (unrealized < 0m)
		{
			if (!CanEnterLong(price))
				return;

			ExecuteLongEntry(price, barTime);
		}

		if (!CanEnterLong(price))
			return;

		ExecuteLongEntry(price, barTime);
	}

	private void TryEnterShort(decimal price, DateTimeOffset barTime)
	{
		if (!CanEnterShort(price))
			return;

		var unrealized = GetShortUnrealizedPnL(price);
		if (unrealized < 0m)
		{
			if (!CanEnterShort(price))
				return;

			ExecuteShortEntry(price, barTime);
		}

		if (!CanEnterShort(price))
			return;

		ExecuteShortEntry(price, barTime);
	}

	private bool CanEnterLong(decimal price)
	{
		if (OnlyOnePosition && (_longVolume > 0m || _shortVolume > 0m))
			return false;

		if (_shortVolume > 0m)
		{
			if (CloseOpposite)
			{
				BuyMarket(_shortVolume);
				return false;
			}

			return false;
		}

		if (MaxPositions > 0 && LotSize > 0m)
		{
			var current = (int)(_longVolume / LotSize);
			if (current >= MaxPositions)
				return false;
		}

		if (MinStep > 0m && _lastLongEntryPrice.HasValue)
		{
			var distance = Math.Abs(price - _lastLongEntryPrice.Value);
			if (distance < MinStep)
				return false;
		}

		return true;
	}

	private bool CanEnterShort(decimal price)
	{
		if (OnlyOnePosition && (_longVolume > 0m || _shortVolume > 0m))
			return false;

		if (_longVolume > 0m)
		{
			if (CloseOpposite)
			{
				SellMarket(_longVolume);
				return false;
			}

			return false;
		}

		if (MaxPositions > 0 && LotSize > 0m)
		{
			var current = (int)(_shortVolume / LotSize);
			if (current >= MaxPositions)
				return false;
		}

		if (MinStep > 0m && _lastShortEntryPrice.HasValue)
		{
			var distance = Math.Abs(price - _lastShortEntryPrice.Value);
			if (distance < MinStep)
				return false;
		}

		return true;
	}

	private void ExecuteLongEntry(decimal price, DateTimeOffset barTime)
	{
		_pendingLongBar = barTime;
		BuyMarket();

		var resultingPosition = Position + Volume;
		ApplyProtection(price, resultingPosition);

		_longTrailingStop = null;
	}

	private void ExecuteShortEntry(decimal price, DateTimeOffset barTime)
	{
		_pendingShortBar = barTime;
		SellMarket();

		var resultingPosition = Position - Volume;
		ApplyProtection(price, resultingPosition);

		_shortTrailingStop = null;
	}

	private void ApplyProtection(decimal price, decimal resultingPosition)
	{
		if (TakeProfit > 0m)
			SetTakeProfit(TakeProfit, price, resultingPosition);

		if (StopLoss > 0m)
			SetStopLoss(StopLoss, price, resultingPosition);
	}

	private void UpdateRsiValues(decimal rsiValue)
	{
		var stepLength = Math.Max(1, StepLength);

		_rsiValues.Enqueue(rsiValue);

		while (_rsiValues.Count > stepLength)
			_rsiValues.Dequeue();
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingDistance <= 0m || TrailingStep <= 0m || TrailingActivation <= 0m)
			return;

		var price = candle.ClosePrice;

		if (_longVolume > 0m)
		{
			var activation = _longAveragePrice + TrailingActivation;
			if (price > activation)
			{
				var candidate = price - TrailingDistance;
				if (!_longTrailingStop.HasValue || candidate - _longTrailingStop.Value >= TrailingStep)
				{
					_longTrailingStop = candidate;
					var distance = price - candidate;
					SetStopLoss(distance, price, Position);
				}
			}
		}

		if (_shortVolume > 0m)
		{
			var activation = _shortAveragePrice - TrailingActivation;
			if (price < activation)
			{
				var candidate = price + TrailingDistance;
				if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - candidate >= TrailingStep)
				{
					_shortTrailingStop = candidate;
					var distance = candidate - price;
					SetStopLoss(distance, price, Position);
				}
			}
		}
	}

	private decimal GetLongUnrealizedPnL(decimal price)
	{
		return _longVolume <= 0m ? 0m : _longVolume * (price - _longAveragePrice);
	}

	private decimal GetShortUnrealizedPnL(decimal price)
	{
		return _shortVolume <= 0m ? 0m : _shortVolume * (_shortAveragePrice - price);
	}

	private static bool IsSameBar(DateTimeOffset? left, DateTimeOffset right)
	{
		return left.HasValue && left.Value == right;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeWindow)
			return true;

		var start = StartTime;
		var end = EndTime;
		var current = time.TimeOfDay;

		if (start == end)
			return true;

		return start < end
			? current >= start && current < end
			: current >= start || current < end;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closing = Math.Min(_shortVolume, volume);
				_shortVolume -= closing;
				volume -= closing;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
					_shortTrailingStop = null;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
				_lastLongEntryPrice = price;
				_lastLongEntryBar = _pendingLongBar;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closing = Math.Min(_longVolume, volume);
				_longVolume -= closing;
				volume -= closing;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
					_longTrailingStop = null;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
				_lastShortEntryPrice = price;
				_lastShortEntryBar = _pendingShortBar;
			}
		}
	}
}

