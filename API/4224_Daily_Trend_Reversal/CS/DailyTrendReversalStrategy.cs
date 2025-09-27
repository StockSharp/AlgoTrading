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
/// Port of the MetaTrader strategy dailyTrendReversal_D1.
/// Combines daily open/high/low levels with a multi-step filter and CCI trend confirmation.
/// Applies strict session control, optional reversal exits, and a configurable daily profit stop.
/// </summary>
public class DailyTrendReversalStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableAutoTrading;
	private readonly StrategyParam<bool> _enableReversal;
	private readonly StrategyParam<int> _trendSteps;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _profitStop;
	private readonly StrategyParam<int> _gmtDiff;
	private readonly StrategyParam<int> _gmtStartHour;
	private readonly StrategyParam<int> _gmtEndHour;
	private readonly StrategyParam<int> _gmtClosingHour;
	private readonly StrategyParam<int> _holdingHours;
	private readonly StrategyParam<int> _riskPips;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;

	private readonly List<decimal> _cciHistory = new(3);

	private decimal _pipSize;
	private decimal _tenPips;

	private DateTime? _currentDay;
	private decimal _dailyOpen;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal _dayPnLBase;

	private bool _tradingSuspended;

	private decimal _lastClose;
	private DateTimeOffset _lastCandleTime;

	private decimal _longEntryPrice;
	private DateTimeOffset? _longEntryTime;
	private decimal _longTakeProfitPrice;
	private decimal _longStopPrice;
	private bool _longBreakEvenActive;

	private decimal _shortEntryPrice;
	private DateTimeOffset? _shortEntryTime;
	private decimal _shortTakeProfitPrice;
	private decimal _shortStopPrice;
	private bool _shortBreakEvenActive;

	private enum TrendDirection
	{
		Flat,
		Up,
		Down,
	}

	/// <summary>
	/// Enables automated entries within the trading window.
	/// </summary>
	public bool EnableAutoTrading
	{
		get => _enableAutoTrading.Value;
		set => _enableAutoTrading.Value = value;
	}

	/// <summary>
	/// Enables closing positions when the opposite trend is confirmed.
	/// </summary>
	public bool EnableReversal
	{
		get => _enableReversal.Value;
		set => _enableReversal.Value = value;
	}

	/// <summary>
	/// Number of step filters applied to the daily trend evaluation.
	/// </summary>
	public int TrendSteps
	{
		get => _trendSteps.Value;
		set => _trendSteps.Value = value;
	}


	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Daily profit target that halts trading when reached (includes floating PnL).
	/// </summary>
	public decimal ProfitStop
	{
		get => _profitStop.Value;
		set => _profitStop.Value = value;
	}

	/// <summary>
	/// Difference between chart time and GMT in hours.
	/// </summary>
	public int GmtDiff
	{
		get => _gmtDiff.Value;
		set => _gmtDiff.Value = value;
	}

	/// <summary>
	/// GMT start hour of the active trading window.
	/// </summary>
	public int GmtStartHour
	{
		get => _gmtStartHour.Value;
		set => _gmtStartHour.Value = value;
	}

	/// <summary>
	/// GMT end hour for accepting new entries.
	/// </summary>
	public int GmtEndHour
	{
		get => _gmtEndHour.Value;
		set => _gmtEndHour.Value = value;
	}

	/// <summary>
	/// GMT hour when all trades should be protected or closed.
	/// </summary>
	public int GmtClosingHour
	{
		get => _gmtClosingHour.Value;
		set => _gmtClosingHour.Value = value;
	}

	/// <summary>
	/// Maximum holding time in hours before forcing exits.
	/// </summary>
	public int HoldingHours
	{
		get => _holdingHours.Value;
		set => _holdingHours.Value = value;
	}

	/// <summary>
	/// Risk threshold in pips used by the trend step filter.
	/// </summary>
	public int RiskPips
	{
		get => _riskPips.Value;
		set => _riskPips.Value = value;
	}

	/// <summary>
	/// Length of the Commodity Channel Index indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Candle type that drives the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DailyTrendReversalStrategy"/> class.
	/// </summary>
	public DailyTrendReversalStrategy()
	{
		_enableAutoTrading = Param(nameof(EnableAutoTrading), true)
		.SetDisplay("Auto Trading", "Enable automated entries inside the session", "Trading");

		_enableReversal = Param(nameof(EnableReversal), true)
		.SetDisplay("Reversal Exit", "Close positions on confirmed opposite trend", "Trading");

		_trendSteps = Param(nameof(TrendSteps), 3)
		.SetRange(0, 3)
		.SetDisplay("Trend Steps", "Number of filters used for daily direction", "Trend Filter");


		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
		.SetRange(0m, 1000m)
		.SetDisplay("Take Profit (pips)", "Distance to fixed take profit (0 disables)", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetRange(0m, 1000m)
		.SetDisplay("Stop Loss (pips)", "Distance to protective stop loss (0 disables)", "Risk");

		_profitStop = Param(nameof(ProfitStop), 100m)
		.SetRange(0m, 100000m)
		.SetDisplay("Profit Stop", "Daily profit target that pauses trading", "Risk");

		_gmtDiff = Param(nameof(GmtDiff), 0)
		.SetDisplay("GMT Diff", "Chart time minus GMT in hours", "Session");

		_gmtStartHour = Param(nameof(GmtStartHour), 5)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Session start hour in GMT", "Session");

		_gmtEndHour = Param(nameof(GmtEndHour), 14)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Session end hour for new trades (GMT)", "Session");

		_gmtClosingHour = Param(nameof(GmtClosingHour), 18)
		.SetRange(0, 23)
		.SetDisplay("Closing Hour", "Session close hour for active trades (GMT)", "Session");

		_holdingHours = Param(nameof(HoldingHours), 10)
		.SetRange(0, 48)
		.SetDisplay("Holding Hours", "Maximum holding time for positions", "Risk");

		_riskPips = Param(nameof(RiskPips), 30)
		.SetRange(0, 1000)
		.SetDisplay("Risk (pips)", "Risk filter threshold used by trend steps", "Trend Filter");

		_cciPeriod = Param(nameof(CciPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Length of the Commodity Channel Index", "Trend Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cciHistory.Clear();
		_currentDay = null;
		_dailyOpen = 0m;
		_dailyHigh = 0m;
		_dailyLow = 0m;
		_dayPnLBase = 0m;
		_tradingSuspended = false;
		_lastClose = 0m;
		_lastCandleTime = default;

		_longEntryPrice = 0m;
		_longEntryTime = null;
		_longTakeProfitPrice = 0m;
		_longStopPrice = 0m;
		_longBreakEvenActive = false;

		_shortEntryPrice = 0m;
		_shortEntryTime = null;
		_shortTakeProfitPrice = 0m;
		_shortStopPrice = 0m;
		_shortBreakEvenActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		_tenPips = 10m * _pipSize;
		_dayPnLBase = PnL;
		_tradingSuspended = false;

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_lastCandleTime = candle.CloseTime;
		_lastClose = candle.ClosePrice;

		UpdateDailyLevels(candle);
		UpdateCciHistory(cciValue);

		var riskDistance = Math.Max(0, RiskPips) * _pipSize;
		var trend = GetDirectionalTrend(candle, riskDistance);
		var rangeTrend = GetRangeTrend();
		var cciTrend = GetCciTrend();

		ManageExistingPositions(candle, rangeTrend, cciTrend, riskDistance);
		HandleProfitStop();

		if (!CanOpenPositions(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		EvaluateEntries(candle, trend, rangeTrend, cciTrend);
	}

	private void EvaluateEntries(ICandleMessage candle, TrendDirection trend, TrendDirection rangeTrend, TrendDirection cciTrend)
	{
		var price = candle.ClosePrice;

		if (trend == TrendDirection.Up && rangeTrend == TrendDirection.Up && cciTrend == TrendDirection.Up && price > _dailyOpen && Position <= 0m)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Enter long at {price} due to daily trend confirmation.");
			}
		}

		if (trend == TrendDirection.Down && rangeTrend == TrendDirection.Down && cciTrend == TrendDirection.Down && price < _dailyOpen && Position >= 0m)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Enter short at {price} due to daily trend confirmation.");
			}
		}
	}

	private void ManageExistingPositions(ICandleMessage candle, TrendDirection rangeTrend, TrendDirection cciTrend, decimal riskDistance)
	{
		if (Position > 0m)
		{
			ManageLongPosition(candle, rangeTrend, cciTrend, riskDistance);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle, rangeTrend, cciTrend, riskDistance);
		}
	}

	private void ManageLongPosition(ICandleMessage candle, TrendDirection rangeTrend, TrendDirection cciTrend, decimal riskDistance)
	{
		var price = candle.ClosePrice;

		if (_longTakeProfitPrice > 0m && price >= _longTakeProfitPrice)
		{
			SellMarket(Position);
			LogInfo("Long take profit reached.");
			return;
		}

		if (_longStopPrice > 0m && price <= _longStopPrice)
		{
			SellMarket(Position);
			LogInfo("Long stop loss triggered.");
			return;
		}

		var holdingExceeded = HoldingHours > 0 && _longEntryTime is DateTimeOffset longEntry && candle.CloseTime - longEntry >= TimeSpan.FromHours(HoldingHours);
		var closingHourReached = GmtClosingHour > 0 && IsAfterOrEqualHour(candle.CloseTime, GmtClosingHour + GmtDiff);

		if ((holdingExceeded || closingHourReached) && _longEntryPrice != 0m)
		{
			if (price > _longEntryPrice)
			{
				SellMarket(Position);
				LogInfo("Long closed with profit due to session or holding limit.");
				return;
			}

			if (!_longBreakEvenActive)
			{
				_longBreakEvenActive = true;
				LogInfo("Long switched to break-even mode due to session or holding limit.");
			}
		}

		if (_longBreakEvenActive && _longEntryPrice != 0m && price >= _longEntryPrice)
		{
			SellMarket(Position);
			_longBreakEvenActive = false;
			LogInfo("Long closed at break-even after session limit.");
			return;
		}

		if (EnableReversal && _dailyOpen != 0m)
		{
			var step1 = TrendSteps >= 0 && price - _dailyLow > riskDistance;
			var step2 = TrendSteps >= 2 && _dailyHigh - _dailyOpen >= riskDistance && _dailyOpen - price <= _tenPips;

			if (price < _dailyOpen && (step1 || step2) && rangeTrend == TrendDirection.Down && cciTrend == TrendDirection.Down)
			{
				SellMarket(Position);
				LogInfo("Long reversed due to opposite trend confirmation.");
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle, TrendDirection rangeTrend, TrendDirection cciTrend, decimal riskDistance)
	{
		var price = candle.ClosePrice;

		if (_shortTakeProfitPrice > 0m && price <= _shortTakeProfitPrice)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo("Short take profit reached.");
			return;
		}

		if (_shortStopPrice > 0m && price >= _shortStopPrice)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo("Short stop loss triggered.");
			return;
		}

		var holdingExceeded = HoldingHours > 0 && _shortEntryTime is DateTimeOffset shortEntry && candle.CloseTime - shortEntry >= TimeSpan.FromHours(HoldingHours);
		var closingHourReached = GmtClosingHour > 0 && IsAfterOrEqualHour(candle.CloseTime, GmtClosingHour + GmtDiff);

		if ((holdingExceeded || closingHourReached) && _shortEntryPrice != 0m)
		{
			if (price < _shortEntryPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo("Short closed with profit due to session or holding limit.");
				return;
			}

			if (!_shortBreakEvenActive)
			{
				_shortBreakEvenActive = true;
				LogInfo("Short switched to break-even mode due to session or holding limit.");
			}
		}

		if (_shortBreakEvenActive && _shortEntryPrice != 0m && price <= _shortEntryPrice)
		{
			BuyMarket(Math.Abs(Position));
			_shortBreakEvenActive = false;
			LogInfo("Short closed at break-even after session limit.");
			return;
		}

		if (EnableReversal && _dailyOpen != 0m)
		{
			var step1 = TrendSteps >= 0 && _dailyHigh - price > riskDistance;
			var step2 = TrendSteps >= 2 && _dailyOpen - _dailyLow >= riskDistance && price - _dailyOpen <= _tenPips;

			if (price > _dailyOpen && (step1 || step2) && rangeTrend == TrendDirection.Up && cciTrend == TrendDirection.Up)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo("Short reversed due to opposite trend confirmation.");
			}
		}
	}

	private void HandleProfitStop()
	{
		if (ProfitStop <= 0m || _tradingSuspended)
		return;

		var realized = PnL - _dayPnLBase;
		var floating = 0m;

		if (Position != 0m && PositionPrice is decimal entryPrice)
		floating = Position * (_lastClose - entryPrice);

		var total = realized + floating;

		if (total >= ProfitStop)
		{
			_tradingSuspended = true;
			CloseAll("Daily profit stop reached");
			LogInfo($"Trading suspended after reaching daily profit stop of {ProfitStop}.");
		}
	}

	private bool CanOpenPositions(ICandleMessage candle)
	{
		if (!EnableAutoTrading || _tradingSuspended)
		return false;

		if (_currentDay is null)
		return false;

		if (!IsWeekday(candle.OpenTime))
		return false;

		if (!IsWithinTradingWindow(candle.OpenTime))
		return false;

		return true;
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;
			_dailyOpen = candle.OpenPrice;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
			_dayPnLBase = PnL;
			_longBreakEvenActive = false;
			_shortBreakEvenActive = false;
		}
		else
		{
			_dailyHigh = Math.Max(_dailyHigh, candle.HighPrice);
			_dailyLow = Math.Min(_dailyLow, candle.LowPrice);
		}
	}

	private void UpdateCciHistory(decimal cciValue)
	{
		if (_cci.IsFormed)
		{
			_cciHistory.Insert(0, cciValue);
			if (_cciHistory.Count > 3)
			_cciHistory.RemoveAt(_cciHistory.Count - 1);
		}
	}

	private TrendDirection GetCciTrend()
	{
		if (_cciHistory.Count < 3)
		return TrendDirection.Flat;

		var current = _cciHistory[0];
		var previous = _cciHistory[1];
		var older = _cciHistory[2];

		if (current >= previous && previous >= older)
		return TrendDirection.Up;

		if (current <= previous && previous <= older)
		return TrendDirection.Down;

		return TrendDirection.Flat;
	}

	private TrendDirection GetDirectionalTrend(ICandleMessage candle, decimal riskDistance)
	{
		if (_currentDay is null)
		return TrendDirection.Flat;

		var price = candle.ClosePrice;

		if (price > _dailyOpen)
		{
			var step1 = TrendSteps >= 0 && _dailyHigh - price > riskDistance;
			var step2 = TrendSteps >= 2 && _dailyOpen - _dailyLow >= riskDistance && price - _dailyOpen <= _tenPips;
			var step3 = TrendSteps >= 3 && price - _dailyOpen <= _tenPips && candle.ClosePrice > candle.OpenPrice;

			if (step1 || step2 || step3)
			return TrendDirection.Up;
		}
		else if (price < _dailyOpen)
		{
			var step1 = TrendSteps >= 0 && price - _dailyLow > riskDistance;
			var step2 = TrendSteps >= 2 && _dailyHigh - _dailyOpen >= riskDistance && _dailyOpen - price <= _tenPips;
			var step3 = TrendSteps >= 3 && _dailyOpen - price <= _tenPips && candle.ClosePrice < candle.OpenPrice;

			if (step1 || step2 || step3)
			return TrendDirection.Down;
		}

		return TrendDirection.Flat;
	}

	private TrendDirection GetRangeTrend()
	{
		var upDistance = _dailyHigh - _dailyOpen;
		var downDistance = _dailyOpen - _dailyLow;

		if (upDistance > downDistance)
		return TrendDirection.Up;

		if (upDistance < downDistance)
		return TrendDirection.Down;

		return TrendDirection.Flat;
	}

	private bool IsWeekday(DateTimeOffset time)
	{
		var day = time.DayOfWeek;
		return day is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = NormalizeHour(GmtStartHour + GmtDiff);
		var end = NormalizeHour(GmtEndHour + GmtDiff);

		if (start == end)
		return false;

		return start < end ? hour >= start && hour < end : hour >= start || hour < end;
	}

	private bool IsAfterOrEqualHour(DateTimeOffset time, int targetHour)
	{
		var hour = time.Hour;
		var normalizedTarget = NormalizeHour(targetHour);
		return hour >= normalizedTarget;
	}

	private static int NormalizeHour(int hour)
	{
		var normalized = hour % 24;
		return normalized < 0 ? normalized + 24 : normalized;
	}

	private decimal GetPipSize()
	{
		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? 0;

		if (decimals >= 3)
		return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice ?? _lastClose;
			_longEntryTime = _lastCandleTime;
			_longTakeProfitPrice = TakeProfitPips > 0m ? _longEntryPrice + TakeProfitPips * _pipSize : 0m;
			_longStopPrice = StopLossPips > 0m ? _longEntryPrice - StopLossPips * _pipSize : 0m;
			_longBreakEvenActive = false;
		}
		else
		{
			_longEntryTime = null;
			_longTakeProfitPrice = 0m;
			_longStopPrice = 0m;
			if (Position <= 0m)
			_longEntryPrice = 0m;
		}

		if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice ?? _lastClose;
			_shortEntryTime = _lastCandleTime;
			_shortTakeProfitPrice = TakeProfitPips > 0m ? _shortEntryPrice - TakeProfitPips * _pipSize : 0m;
			_shortStopPrice = StopLossPips > 0m ? _shortEntryPrice + StopLossPips * _pipSize : 0m;
			_shortBreakEvenActive = false;
		}
		else
		{
			_shortEntryTime = null;
			_shortTakeProfitPrice = 0m;
			_shortStopPrice = 0m;
			if (Position >= 0m)
			_shortEntryPrice = 0m;
		}
	}
}
