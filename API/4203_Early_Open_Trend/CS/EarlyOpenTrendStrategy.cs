using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor located in MQL/9826 that trades based on the daily open trend.
/// </summary>
public class EarlyOpenTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _orderType;
	private readonly StrategyParam<int> _rangeFilterPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<int> _holdingHours;
	private readonly StrategyParam<int> _summerTimeStartDay;
	private readonly StrategyParam<int> _winterTimeStartDay;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _dailyOpen;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private DateTime _currentDay;
	private bool _longTradeTaken;
	private bool _shortTradeTaken;
	private DateTimeOffset? _lastEntryTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="EarlyOpenTrendStrategy"/> class.
	/// </summary>
	public EarlyOpenTrendStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Market order size used for entries", "Trading")
			.SetCanOptimize(true);

		_orderType = Param(nameof(OrderType), 0)
			.SetDisplay("Order Type", "0 = long & short, 1 = long only, 2 = short only", "Trading")
			.SetCanOptimize(true);

		_rangeFilterPips = Param(nameof(RangeFilterPips), 1)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Range Filter (pips)", "Minimum wick size from the daily open", "Filters")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Optional take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 1000)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Optional stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 7)
			.SetDisplay("Session Start Hour", "Hour of day (local exchange time) when entries become valid", "Session")
			.SetCanOptimize(true);

		_endHour = Param(nameof(EndHour), 18)
			.SetDisplay("Session End Hour", "Hour of day (local exchange time) when new entries stop", "Session")
			.SetCanOptimize(true);

		_closingHour = Param(nameof(ClosingHour), 20)
			.SetDisplay("Forced Close Hour", "Hour of day to flatten positions", "Session")
			.SetCanOptimize(true);

		_holdingHours = Param(nameof(HoldingHours), 0)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Holding Limit (hours)", "Maximum holding time before forcing an exit", "Risk")
			.SetCanOptimize(true);

		_summerTimeStartDay = Param(nameof(SummerTimeStartDay), 87)
			.SetDisplay("DST Start Day", "Day of year when the summer offset becomes active", "Session");

		_winterTimeStartDay = Param(nameof(WinterTimeStartDay), 297)
			.SetDisplay("DST End Day", "Day of year when the winter offset resumes", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Intraday candle series used for calculations", "Data");
	}

	/// <summary>
	/// Market order size applied to generated signals.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Directional filter: 0 = both sides, 1 = long only, 2 = short only.
	/// </summary>
	public int OrderType
	{
		get => _orderType.Value;
		set => _orderType.Value = value;
	}

	/// <summary>
	/// Minimum wick distance from the daily open expressed in pips.
	/// </summary>
	public int RangeFilterPips
	{
		get => _rangeFilterPips.Value;
		set => _rangeFilterPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Zero disables the stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// First hour when the strategy is allowed to open new trades.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last hour (exclusive) when the strategy can open new trades.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Hour when open positions are forcefully closed.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// Maximum holding duration expressed in hours (0 disables the limit).
	/// </summary>
	public int HoldingHours
	{
		get => _holdingHours.Value;
		set => _holdingHours.Value = value;
	}

	/// <summary>
	/// Day of year when the summer daylight-saving offset activates.
	/// </summary>
	public int SummerTimeStartDay
	{
		get => _summerTimeStartDay.Value;
		set => _summerTimeStartDay.Value = value;
	}

	/// <summary>
	/// Day of year when the winter offset resumes.
	/// </summary>
	public int WinterTimeStartDay
	{
		get => _winterTimeStartDay.Value;
		set => _winterTimeStartDay.Value = value;
	}

	/// <summary>
	/// Candle series used to evaluate intraday signals.
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

		_dailyOpen = 0m;
		_dailyHigh = 0m;
		_dailyLow = 0m;
		_currentDay = default;
		_longTradeTaken = false;
		_shortTradeTaken = false;
		_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Pre-calculate the pip size to convert pip-based parameters into absolute prices.
		_pipSize = CalculatePipSize();
		Volume = OrderVolume;

		Unit? stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		Unit? takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyProfile(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Manage existing positions before evaluating new entries.
		if (HandleOpenPosition(candle))
			return;

		if (Position != 0)
			return;

		if (!IsWithinTradingSession(candle.OpenTime))
			return;

		if (_dailyOpen <= 0m)
			return;

		var closePrice = candle.ClosePrice;
		var wickBelowOpen = _dailyOpen - _dailyLow;
		var wickAboveOpen = _dailyHigh - _dailyOpen;
		var minimumWick = RangeFilterPips * _pipSize;

		// Long entry: price trades above the daily open after forming a downside wick of configurable size.
		if (OrderType != 2 && !_longTradeTaken && closePrice > _dailyOpen && wickBelowOpen > minimumWick)
		{
			BuyMarket();
			_longTradeTaken = true;
			_lastEntryTime = candle.CloseTime;
			return;
		}

		// Short entry: price trades below the daily open after forming an upside wick of configurable size.
		if (OrderType != 1 && !_shortTradeTaken && closePrice < _dailyOpen && wickAboveOpen > minimumWick)
		{
			SellMarket();
			_shortTradeTaken = true;
			_lastEntryTime = candle.CloseTime;
		}
	}

	private void UpdateDailyProfile(ICandleMessage candle)
	{
		var date = candle.OpenTime.Date;

		if (date != _currentDay)
		{
			_currentDay = date;
			_dailyOpen = candle.OpenPrice;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
			_longTradeTaken = false;
			_shortTradeTaken = false;
			if (Position == 0)
				_lastEntryTime = null;
			return;
		}

		_dailyHigh = Math.Max(_dailyHigh, candle.HighPrice);
		_dailyLow = Math.Min(_dailyLow, candle.LowPrice);
	}

	private bool HandleOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
			return false;

		var time = candle.CloseTime;

		var shouldCloseByTime = HasReachedClosingTime(time);

		if (!shouldCloseByTime && HoldingHours > 0 && _lastEntryTime is DateTimeOffset entryTime)
		{
			var holdingLimit = entryTime + TimeSpan.FromHours(HoldingHours);
			if (time >= holdingLimit)
				shouldCloseByTime = true;
		}

		if (!shouldCloseByTime)
			return false;

		ClosePosition();
		_lastEntryTime = null;
		return true;
	}

	private bool HasReachedClosingTime(DateTimeOffset time)
	{
		if (ClosingHour <= 0)
			return false;

		var offset = GetDstOffset(time);
		var closingHour = ClosingHour - offset;

		return time.Hour >= closingHour;
	}

	private bool IsWithinTradingSession(DateTimeOffset time)
	{
		var offset = GetDstOffset(time);
		var startHour = Math.Max(0, StartHour - offset);
		var endHour = Math.Max(0, EndHour - offset);

		if (endHour <= startHour)
			return false;

		var hour = time.Hour;
		return hour >= startHour && hour < endHour;
	}

	private int GetDstOffset(DateTimeOffset time)
	{
		var dayOfYear = time.DayOfYear;
		return dayOfYear >= SummerTimeStartDay && dayOfYear <= WinterTimeStartDay ? 2 : 1;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = CountDecimals(priceStep);
		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 8)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
