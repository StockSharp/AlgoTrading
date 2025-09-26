using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader BollingerBandsEA (version 3.0).
/// Trades reversals from the Bollinger Bands after session-based filters are met.
/// Applies optional trailing stop, break-even logic, and timed trade liquidation.
/// </summary>
public class BollingerBandsSessionReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _dailyMaLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useRiskVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _sessionStartOffsetMinutes;
	private readonly StrategyParam<int> _sessionEndOffsetMinutes;
	private readonly StrategyParam<bool> _closeOnMiddleBand;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingFactor;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenFactor;
	private readonly StrategyParam<int> _closeLosingAfterMinutes;

	private BollingerBands _bollinger = null!;
	private SMA _dailySma = null!;

	private ICandleMessage _previousCandle;
	private decimal _previousUpperBand;
	private decimal _previousLowerBand;
	private decimal _previousMiddleBand;
	private bool _bandsReady;

	private DateTime? _currentDay;
	private decimal _currentDayHigh;
	private decimal _currentDayLow;
	private decimal _previousDayHigh;
	private decimal _previousDayLow;

	private decimal _dailyMaValue;
	private bool _dailyMaReady;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private DateTimeOffset? _entryTime;
	private bool _isLongPosition;

	private decimal _dayPnLBase;

	/// <summary>
	/// Candle type used for the main trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the Bollinger Bands moving average.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Width multiplier applied to the Bollinger Bands.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Length of the daily moving average filter.
	/// </summary>
	public int DailyMaLength
	{
		get => _dailyMaLength.Value;
		set => _dailyMaLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables risk-based position sizing.
	/// </summary>
	public bool UseRiskVolume
	{
		get => _useRiskVolume.Value;
		set => _useRiskVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage of the account balance used when sizing positions.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed volume used when risk-based sizing is disabled or not possible.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Minutes added after the session open before entries are allowed.
	/// </summary>
	public int SessionStartOffsetMinutes
	{
		get => _sessionStartOffsetMinutes.Value;
		set => _sessionStartOffsetMinutes.Value = value;
	}

	/// <summary>
	/// Minutes removed before the session close when new entries are blocked.
	/// </summary>
	public int SessionEndOffsetMinutes
	{
		get => _sessionEndOffsetMinutes.Value;
		set => _sessionEndOffsetMinutes.Value = value;
	}

	/// <summary>
	/// Enables closing open positions once price crosses the Bollinger middle band.
	/// </summary>
	public bool CloseOnMiddleBand
	{
		get => _closeOnMiddleBand.Value;
		set => _closeOnMiddleBand.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Multiplier that defines when the trailing stop becomes active.
	/// </summary>
	public decimal TrailingFactor
	{
		get => _trailingFactor.Value;
		set => _trailingFactor.Value = value;
	}

	/// <summary>
	/// Enables the break-even adjustment.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Multiplier that defines when the break-even stop is moved to the entry price.
	/// </summary>
	public decimal BreakEvenFactor
	{
		get => _breakEvenFactor.Value;
		set => _breakEvenFactor.Value = value;
	}

	/// <summary>
	/// Minutes after entry to close losing positions.
	/// </summary>
	public int CloseLosingAfterMinutes
	{
		get => _closeLosingAfterMinutes.Value;
		set => _closeLosingAfterMinutes.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with sensible defaults.
	/// </summary>
	public BollingerBandsSessionReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used for trading", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Moving average period for Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerWidth = Param(nameof(BollingerWidth), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Band width multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3.0m, 0.25m);

		_dailyMaLength = Param(nameof(DailyMaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Daily MA Length", "Length of the daily trend filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Risk distance used for money management", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 25m);

		_useRiskVolume = Param(nameof(UseRiskVolume), true)
			.SetDisplay("Use Risk Volume", "Enable risk-based position sizing", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percentage of equity risked per trade", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3.0m, 0.5m);

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Fallback volume when risk sizing is disabled", "Risk Management");

		_sessionStartOffsetMinutes = Param(nameof(SessionStartOffsetMinutes), 420)
			.SetDisplay("Session Start Offset", "Minutes after the trading day start to allow entries", "Session");

		_sessionEndOffsetMinutes = Param(nameof(SessionEndOffsetMinutes), 5)
			.SetDisplay("Session End Offset", "Minutes before the trading day close to block entries", "Session");

		_closeOnMiddleBand = Param(nameof(CloseOnMiddleBand), false)
			.SetDisplay("Close On Middle Band", "Exit positions when price crosses the Bollinger middle band", "Exits");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activate trailing stop logic", "Exits");

		_trailingFactor = Param(nameof(TrailingFactor), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Factor", "Multiple of the stop distance required to trail", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3.0m, 0.25m);

		_enableBreakEven = Param(nameof(EnableBreakEven), false)
			.SetDisplay("Enable Break Even", "Move stop to entry when initial target is reached", "Exits");

		_breakEvenFactor = Param(nameof(BreakEvenFactor), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Factor", "Multiple of the stop distance before moving to break even", "Exits")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.25m);

		_closeLosingAfterMinutes = Param(nameof(CloseLosingAfterMinutes), 30)
			.SetGreaterThanZero()
			.SetDisplay("Close Losing Minutes", "Close trades that stay negative for the specified minutes", "Exits");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bollinger = null!;
		_dailySma = null!;

		_previousCandle = null;
		_previousUpperBand = 0m;
		_previousLowerBand = 0m;
		_previousMiddleBand = 0m;
		_bandsReady = false;

		_currentDay = null;
		_currentDayHigh = 0m;
		_currentDayLow = 0m;
		_previousDayHigh = 0m;
		_previousDayLow = 0m;

		_dailyMaValue = 0m;
		_dailyMaReady = false;

		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_entryTime = null;
		_isLongPosition = false;

		_dayPnLBase = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_dayPnLBase = PnL;

		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerWidth
		};

		_dailySma = new SMA
		{
			Length = DailyMaLength
		};

		var intradaySubscription = SubscribeCandles(CandleType);

		intradaySubscription
			.Bind(_bollinger, ProcessIntradayCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());

		dailySubscription
			.Bind(_dailySma, ProcessDailyCandle)
			.Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			var pricePane = chartArea.CreateSeries<CandleIndicatorValue>(Security, CandleType, "Price");
			pricePane.AddIndicator(_bollinger);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dailyMaValue = smaValue;
		_dailyMaReady = _dailySma.IsFormed;
	}

	private void ProcessIntradayCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateDailyLevels(candle);

		if (!_dailyMaReady)
			return;

		var dayPnL = PnL - _dayPnLBase;
		if (dayPnL > 0m)
		{
			return;
		}

		var step = Security.Step ?? 0.0001m;
		var stopDistance = StopLossPoints * step;
		if (stopDistance <= 0m)
			return;

		if (!_bandsReady)
		{
			PrepareBandsState(candle, upper, lower, middle);
			return;
		}

		ManageActivePosition(candle, middle, stopDistance);

		if (!IsSessionOpen(candle.OpenTime))
		{
			PrepareBandsState(candle, upper, lower, middle);
			return;
		}

		if (Position != 0)
		{
			PrepareBandsState(candle, upper, lower, middle);
			return;
		}

		if (_previousCandle == null)
		{
			PrepareBandsState(candle, upper, lower, middle);
			return;
		}

		var rangeUpper = (_previousUpperBand - _previousMiddleBand) / step;
		var rangeLower = (_previousMiddleBand - _previousLowerBand) / step;

		var prev = _previousCandle;
		var currentClose = candle.ClosePrice;
		var previousClose = prev.ClosePrice;
		var previousOpen = prev.OpenPrice;

		var highestRecent = Math.Max(prev.HighPrice, candle.HighPrice);
		var lowestRecent = Math.Min(prev.LowPrice, candle.LowPrice);

		var allowShort = previousOpen > previousClose
			&& previousClose > _previousUpperBand
			&& currentClose > _previousUpperBand
			&& rangeUpper > StopLossPoints * 2m
			&& currentClose < _dailyMaValue
			&& (currentClose > _currentDayHigh || currentClose > _previousDayHigh);

		if (allowShort)
		{
			EnterPosition(false, candle, stopDistance, highestRecent + stopDistance, _currentDayLow);
			PrepareBandsState(candle, upper, lower, middle);
			return;
		}

		var allowLong = previousOpen < previousClose
			&& previousClose < _previousLowerBand
			&& currentClose < _previousLowerBand
			&& rangeLower > StopLossPoints * 2m
			&& currentClose > _dailyMaValue
			&& (currentClose < _currentDayLow || currentClose < _previousDayLow);

		if (allowLong)
		{
			EnterPosition(true, candle, stopDistance, lowestRecent - stopDistance, _currentDayHigh);
		}

		PrepareBandsState(candle, upper, lower, middle);
	}

	private void ManageActivePosition(ICandleMessage candle, decimal middle, decimal stopDistance)
	{
		if (Position == 0)
			return;

		if (_entryTime.HasValue && CloseLosingAfterMinutes > 0)
		{
			var limit = _entryTime.Value + TimeSpan.FromMinutes(CloseLosingAfterMinutes);
			if (candle.CloseTime >= limit)
			{
				var losing = _isLongPosition
					? candle.ClosePrice < _entryPrice
					: candle.ClosePrice > _entryPrice;
				if (losing)
				{
					ClosePosition();
					return;
				}
			}
		}

		if (CloseOnMiddleBand)
		{
			if (_isLongPosition && candle.ClosePrice <= middle)
			{
				ClosePosition();
				return;
			}

			if (!_isLongPosition && candle.ClosePrice >= middle)
			{
				ClosePosition();
				return;
			}
		}

		if (_isLongPosition)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				ClosePosition();
				return;
			}

			if (candle.HighPrice >= _takeProfitPrice && _takeProfitPrice > 0m)
			{
				ClosePosition();
				return;
			}

			ApplyRiskManagementAdjustments(candle.ClosePrice, stopDistance);
		}
		else
		{
			if (candle.HighPrice >= _stopPrice)
			{
				ClosePosition();
				return;
			}

			if (candle.LowPrice <= _takeProfitPrice && _takeProfitPrice > 0m)
			{
				ClosePosition();
				return;
			}

			ApplyRiskManagementAdjustments(candle.ClosePrice, stopDistance);
		}
	}

	private void ApplyRiskManagementAdjustments(decimal closePrice, decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return;

		var profitDistance = _isLongPosition
			? closePrice - _entryPrice
			: _entryPrice - closePrice;

		if (EnableBreakEven && profitDistance >= stopDistance * BreakEvenFactor)
		{
			_stopPrice = _isLongPosition
				? Math.Max(_stopPrice, _entryPrice)
				: Math.Min(_stopPrice, _entryPrice);
		}

		if (EnableTrailing && profitDistance >= stopDistance * TrailingFactor)
		{
			var newStop = _isLongPosition
				? closePrice - stopDistance
				: closePrice + stopDistance;

			_stopPrice = _isLongPosition
				? Math.Max(_stopPrice, newStop)
				: Math.Min(_stopPrice, newStop);
		}
	}

	private void EnterPosition(bool isLong, ICandleMessage candle, decimal stopDistance, decimal stopPrice, decimal targetPrice)
	{
		var volume = GetTradeVolume(stopDistance);
		if (volume <= 0m)
			return;

		if (isLong)
		{
			BuyMarket(volume + Math.Abs(Position));
		}
		else
		{
			SellMarket(volume + Math.Abs(Position));
		}

		_entryPrice = candle.ClosePrice;
		_stopPrice = stopPrice;
		_takeProfitPrice = targetPrice;
		_entryTime = candle.CloseTime;
		_isLongPosition = isLong;
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}

		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_entryTime = null;
	}

	private decimal GetTradeVolume(decimal stopDistance)
	{
		if (!UseRiskVolume || Portfolio == null)
			return FixedVolume;

		var balance = Portfolio.CurrentValue;
		if (balance <= 0m)
			return FixedVolume;

		var riskMoney = balance * (RiskPercent / 100m);
		if (riskMoney <= 0m)
			return FixedVolume;

		var step = Security.Step ?? 0.0001m;
		var stepPrice = Security.StepPrice ?? 0m;
		if (stepPrice <= 0m || stopDistance <= 0m)
			return FixedVolume;

		var stepsToStop = stopDistance / step;
		if (stepsToStop <= 0m)
			return FixedVolume;

		var lossPerLot = stepsToStop * stepPrice;
		if (lossPerLot <= 0m)
			return FixedVolume;

		var volume = riskMoney / lossPerLot;
		return volume > 0m ? volume : FixedVolume;
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var candleDay = candle.OpenTime.DateTime.Date;
		if (_currentDay == null)
		{
			_currentDay = candleDay;
			_currentDayHigh = candle.HighPrice;
			_currentDayLow = candle.LowPrice;
			_previousDayHigh = candle.HighPrice;
			_previousDayLow = candle.LowPrice;
			return;
		}

		if (_currentDay != candleDay)
		{
			_previousDayHigh = _currentDayHigh;
			_previousDayLow = _currentDayLow;
			_currentDay = candleDay;
			_currentDayHigh = candle.HighPrice;
			_currentDayLow = candle.LowPrice;
			_dayPnLBase = PnL;
			return;
		}

		_currentDayHigh = Math.Max(_currentDayHigh, candle.HighPrice);
		_currentDayLow = Math.Min(_currentDayLow, candle.LowPrice);
	}

	private bool IsSessionOpen(DateTimeOffset candleTime)
	{
		var dayStart = new DateTimeOffset(candleTime.Date, candleTime.Offset);
		var start = dayStart + TimeSpan.FromMinutes(SessionStartOffsetMinutes);
		var end = dayStart + TimeSpan.FromDays(1) - TimeSpan.FromMinutes(SessionEndOffsetMinutes);
		return candleTime >= start && candleTime <= end;
	}

	private void PrepareBandsState(ICandleMessage candle, decimal upper, decimal lower, decimal middle)
	{
		_previousCandle = candle;
		_previousUpperBand = upper;
		_previousLowerBand = lower;
		_previousMiddleBand = middle;
		_bandsReady = _bollinger.IsFormed;
	}
}
