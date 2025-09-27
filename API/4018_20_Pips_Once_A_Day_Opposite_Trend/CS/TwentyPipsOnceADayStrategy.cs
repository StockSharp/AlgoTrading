using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "20pipsOnceADayOppositeLastNHourTrend".
/// Trades once per configured hour against the drift of the last N hourly candles and applies martingale style sizing.
/// Includes daily session control, optional trailing protection, and automatic position aging.
/// </summary>
public class TwentyPipsOnceADayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<int> _tradingHour;
	private readonly StrategyParam<string> _tradingDayHours;
	private readonly StrategyParam<int> _hoursToCheckTrend;
	private readonly StrategyParam<int> _orderMaxAgeSeconds;
	private readonly StrategyParam<int> _firstMultiplier;
	private readonly StrategyParam<int> _secondMultiplier;
	private readonly StrategyParam<int> _thirdMultiplier;
	private readonly StrategyParam<int> _fourthMultiplier;
	private readonly StrategyParam<int> _fifthMultiplier;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();
	private readonly List<bool> _recentLosses = new(5);
	private readonly HashSet<int> _allowedHours = new();

	private DateTime? _lastTradeBarTime;
	private DateTimeOffset? _entryTime;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryVolume;
	private int _positionDirection;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of <see cref="TwentyPipsOnceADayStrategy"/>.
	/// </summary>
	public TwentyPipsOnceADayStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk");

		_minVolume = Param(nameof(MinVolume), 0.1m)
		.SetDisplay("Min Volume", "Lower volume bound applied after sizing", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 5m)
		.SetDisplay("Max Volume", "Upper volume bound applied after sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetDisplay("Risk Percent", "Percentage of portfolio value converted into volume when fixed size is disabled", "Risk");

		_maxOrders = Param(nameof(MaxOrders), 1)
		.SetGreaterThanZero()
		.SetDisplay("Max Orders", "Maximum number of simultaneously open positions", "Trading");

		_tradingHour = Param(nameof(TradingHour), 7)
		.SetRange(0, 23)
		.SetDisplay("Trading Hour", "Hour of day (0-23) when the strategy evaluates signals", "Schedule");

		_tradingDayHours = Param(nameof(TradingDayHours), "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23")
		.SetDisplay("Trading Day Hours", "Comma separated list or ranges of allowed session hours", "Schedule");

		_hoursToCheckTrend = Param(nameof(HoursToCheckTrend), 30)
		.SetGreaterThanZero()
		.SetDisplay("Hours To Check", "Number of historical hourly closes used for the contrarian check", "Signals");

		_orderMaxAgeSeconds = Param(nameof(OrderMaxAgeSeconds), 75600)
		.SetGreaterThanZero()
		.SetDisplay("Max Position Age (s)", "Maximum holding time in seconds before forcing an exit", "Risk");

		_firstMultiplier = Param(nameof(FirstMultiplier), 4)
		.SetGreaterThanZero()
		.SetDisplay("First Multiplier", "Multiplier applied after the most recent loss", "Money Management");

		_secondMultiplier = Param(nameof(SecondMultiplier), 2)
		.SetGreaterThanZero()
		.SetDisplay("Second Multiplier", "Multiplier applied when the last win was preceded by a loss", "Money Management");

		_thirdMultiplier = Param(nameof(ThirdMultiplier), 5)
		.SetGreaterThanZero()
		.SetDisplay("Third Multiplier", "Multiplier applied when the third latest trade was a loss", "Money Management");

		_fourthMultiplier = Param(nameof(FourthMultiplier), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fourth Multiplier", "Multiplier applied when the fourth latest trade was a loss", "Money Management");

		_fifthMultiplier = Param(nameof(FifthMultiplier), 1)
		.SetGreaterThanZero()
		.SetDisplay("Fifth Multiplier", "Multiplier applied when the fifth latest trade was a loss", "Money Management");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signal calculations", "Market Data");
	}

	/// <summary>
	/// Fixed trading volume. Set to zero to enable risk based sizing.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Minimum allowed trading volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed trading volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Portfolio percentage converted into volume when <see cref="FixedVolume"/> equals zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open positions.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Hour of day when new positions may be opened.
	/// </summary>
	public int TradingHour
	{
		get => _tradingHour.Value;
		set => _tradingHour.Value = value;
	}

	/// <summary>
	/// Comma separated list or ranges of allowed trading hours.
	/// </summary>
	public string TradingDayHours
	{
		get => _tradingDayHours.Value;
		set
		{
			_tradingDayHours.Value = value;
			UpdateTradingHours();
		}
	}

	/// <summary>
	/// Lookback depth measured in hourly candles.
	/// </summary>
	public int HoursToCheckTrend
	{
		get => _hoursToCheckTrend.Value;
		set => _hoursToCheckTrend.Value = value;
	}

	/// <summary>
	/// Maximum holding time before a position is forcefully closed.
	/// </summary>
	public int OrderMaxAgeSeconds
	{
		get => _orderMaxAgeSeconds.Value;
		set => _orderMaxAgeSeconds.Value = value;
	}

	/// <summary>
	/// Multiplier used after the latest loss.
	/// </summary>
	public int FirstMultiplier
	{
		get => _firstMultiplier.Value;
		set => _firstMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used when only the second latest trade was a loss.
	/// </summary>
	public int SecondMultiplier
	{
		get => _secondMultiplier.Value;
		set => _secondMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used when the third latest trade was a loss.
	/// </summary>
	public int ThirdMultiplier
	{
		get => _thirdMultiplier.Value;
		set => _thirdMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used when the fourth latest trade was a loss.
	/// </summary>
	public int FourthMultiplier
	{
		get => _fourthMultiplier.Value;
		set => _fourthMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used when the fifth latest trade was a loss.
	/// </summary>
	public int FifthMultiplier
	{
		get => _fifthMultiplier.Value;
		set => _fifthMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used to process signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_closeHistory.Clear();
		_recentLosses.Clear();
		_lastTradeBarTime = null;
		_entryTime = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryVolume = 0m;
		_positionDirection = 0;
		_pipSize = 0m;
		UpdateTradingHours();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		UpdateTradingHours();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		AddCloseToHistory(candle.ClosePrice);

		if (_positionDirection != 0)
		{
			ManageOpenPosition(candle);
			if (_positionDirection != 0)
			{
				EnforceSessionLimits(candle);
			}
		}

		TryOpenPosition(candle);
	}

	private void AddCloseToHistory(decimal closePrice)
	{
		if (HoursToCheckTrend <= 0)
		return;

		_closeHistory.Insert(0, closePrice);

		var required = Math.Max(HoursToCheckTrend, 5);
		if (_closeHistory.Count > required)
		{
			_closeHistory.RemoveRange(required, _closeHistory.Count - required);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_positionDirection == 0 || _entryPrice is not decimal entryPrice)
		return;

		var direction = _positionDirection;
		var closePrice = candle.ClosePrice;

		var stopDistance = StopLossPips * _pipSize;
		if (_stopPrice is null && stopDistance > 0m)
		{
			_stopPrice = direction > 0
			? entryPrice - stopDistance
			: entryPrice + stopDistance;
		}

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance > 0m)
		{
			if (direction > 0)
			{
				var profit = closePrice - entryPrice;
				if (profit > trailingDistance)
				{
					var candidate = closePrice - trailingDistance;
					if (_stopPrice is null || candidate > _stopPrice.Value)
					{
						_stopPrice = candidate;
					}
				}
			}
			else
			{
				var profit = entryPrice - closePrice;
				if (profit > trailingDistance)
				{
					var candidate = closePrice + trailingDistance;
					if (_stopPrice is null || candidate < _stopPrice.Value)
					{
						_stopPrice = candidate;
					}
				}
			}
		}

		if (_takeProfitPrice is decimal target)
		{
			var hitTarget = direction > 0
			? candle.HighPrice >= target
			: candle.LowPrice <= target;

			if (hitTarget)
			{
				ExitPosition(target);
				return;
			}
		}

		if (_stopPrice is decimal stopLevel)
		{
			var hitStop = direction > 0
			? candle.LowPrice <= stopLevel
			: candle.HighPrice >= stopLevel;

			if (hitStop)
			{
				ExitPosition(stopLevel);
				return;
			}
		}

		if (OrderMaxAgeSeconds > 0 && _entryTime is DateTimeOffset entryTime)
		{
			var age = candle.CloseTime - entryTime;
			if (age.TotalSeconds >= OrderMaxAgeSeconds)
			{
				ExitPosition(candle.ClosePrice);
			}
		}
	}

	private void EnforceSessionLimits(ICandleMessage candle)
	{
		if (_positionDirection == 0)
		return;

		var nextHour = candle.CloseTime.Hour;
		if (!IsHourAllowed(nextHour))
		{
			ExitPosition(candle.ClosePrice);
		}
	}

	private void TryOpenPosition(ICandleMessage candle)
	{
		if (MaxOrders <= 0 || _positionDirection != 0)
		return;

		var nextHour = candle.CloseTime.Hour;
		if (nextHour != TradingHour || !IsHourAllowed(nextHour))
		return;

		if (_lastTradeBarTime.HasValue && _lastTradeBarTime.Value == candle.CloseTime.DateTime)
		return;

		if (_closeHistory.Count < HoursToCheckTrend)
		return;

		var lastClose = _closeHistory[0];
		var index = HoursToCheckTrend - 1;
		if (index < 0 || index >= _closeHistory.Count)
		return;

		var referenceClose = _closeHistory[index];
		if (lastClose == referenceClose)
		return;

		var goLong = referenceClose > lastClose;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		var entryPrice = candle.ClosePrice;

		if (goLong)
		{
			BuyMarket(volume);
			_positionDirection = 1;
		}
		else
		{
			SellMarket(volume);
			_positionDirection = -1;
		}

		_entryPrice = entryPrice;
		_entryTime = candle.CloseTime;
		_entryVolume = volume;
		_lastTradeBarTime = candle.CloseTime.DateTime;

		var stopDistance = StopLossPips * _pipSize;
		_stopPrice = stopDistance > 0m
		? _positionDirection > 0
		? entryPrice - stopDistance
		: entryPrice + stopDistance
		: null;

		var takeDistance = TakeProfitPips * _pipSize;
		_takeProfitPrice = takeDistance > 0m
		? _positionDirection > 0
		? entryPrice + takeDistance
		: entryPrice - takeDistance
		: null;
	}

	private void ExitPosition(decimal exitPrice)
	{
		var direction = _positionDirection;
		if (direction == 0)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			volume = Math.Abs(_entryVolume);
		}

		if (volume <= 0m)
		{
			ResetPositionState();
			return;
		}

		if (direction > 0)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}

		if (_entryPrice is decimal entryPrice)
		{
			var isLoss = direction > 0
			? exitPrice < entryPrice
			: exitPrice > entryPrice;

			RegisterTradeResult(isLoss);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void RegisterTradeResult(bool isLoss)
	{
		_recentLosses.Insert(0, isLoss);
		if (_recentLosses.Count > 5)
		{
			_recentLosses.RemoveRange(5, _recentLosses.Count - 5);
		}

		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_positionDirection = 0;
		_entryPrice = null;
		_entryTime = null;
		_entryVolume = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = FixedVolume;

		if (baseVolume <= 0m)
		{
			baseVolume = CalculateRiskVolume();
		}

		if (baseVolume <= 0m)
		return 0m;

		var multiplier = GetMultiplierFromHistory();
		var desired = AlignVolume(baseVolume * multiplier);

		return desired;
	}

	private decimal CalculateRiskVolume()
	{
		if (RiskPercent <= 0m)
		return MinVolume > 0m ? MinVolume : 0m;

		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.CurrentBalance ?? 0m;
		if (balance <= 0m)
		return MinVolume > 0m ? MinVolume : 0m;

		var raw = balance * RiskPercent / 1000m;
		return raw;
	}

	private decimal GetMultiplierFromHistory()
	{
		for (var index = 0; index < _recentLosses.Count && index < 5; index++)
		{
			if (!_recentLosses[index])
			continue;

			return index switch
			{
				0 => FirstMultiplier,
				1 => SecondMultiplier,
				2 => ThirdMultiplier,
				3 => FourthMultiplier,
				4 => FifthMultiplier,
				_ => 1m,
			};
		}

		return 1m;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var min = security.VolumeMin ?? 0m;
			var max = security.VolumeMax ?? decimal.MaxValue;
			var step = security.VolumeStep ?? 0m;

			if (step > 0m)
			{
				volume = Math.Round(volume / step) * step;
			}

			if (min > 0m && volume < min)
			volume = min;

			if (max > 0m && volume > max)
			volume = max;
		}

		if (MinVolume > 0m && volume < MinVolume)
		volume = MinVolume;

		if (MaxVolume > 0m && volume > MaxVolume)
		volume = MaxVolume;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals;

		if ((decimals == 3 || decimals == 5) && step > 0m)
		{
			return step * 10m;
		}

		return step > 0m ? step : 0.0001m;
	}

	private bool IsHourAllowed(int hour)
	{
		if (_allowedHours.Count == 0)
		return true;

		return _allowedHours.Contains(hour);
	}

	private void UpdateTradingHours()
	{
		_allowedHours.Clear();

		var raw = _tradingDayHours.Value;
		if (raw.IsEmptyOrWhiteSpace())
		{
			FillFullDay();
			return;
		}

		var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			if (trimmed.Length == 0)
			continue;

			if (trimmed.Contains('-', StringComparison.Ordinal))
			{
				var rangeParts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
				if (rangeParts.Length != 2)
				continue;

				if (TryParseHour(rangeParts[0], out var start) && TryParseHour(rangeParts[1], out var end))
				{
					if (end < start)
					{
						(end, start) = (start, end);
					}

					for (var hour = start; hour <= end; hour++)
					{
						_allowedHours.Add(hour);
					}
				}
			}
			else if (TryParseHour(trimmed, out var value))
			{
				_allowedHours.Add(value);
			}
		}

		if (_allowedHours.Count == 0)
		{
			FillFullDay();
		}
	}

	private static bool TryParseHour(string text, out int hour)
	{
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out hour))
		{
			if (hour >= 0 && hour <= 23)
			return true;
		}

		hour = 0;
		return false;
	}

	private void FillFullDay()
	{
		_allowedHours.Clear();
		for (var hour = 0; hour < 24; hour++)
		{
			_allowedHours.Add(hour);
		}
	}
}
