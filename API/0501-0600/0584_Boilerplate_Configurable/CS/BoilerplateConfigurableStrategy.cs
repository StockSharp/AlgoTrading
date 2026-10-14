using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Boilerplate Configurable strategy.
/// In SMA cross mode a cross of SMA(FastLength) over SMA(Length) gives the signal; in squeeze mode a close beyond the
/// WideMultiplier Bollinger band that stays inside the NarrowMultiplier band does. Signals can be inverted and limited to one
/// side, and an opposite signal reverses the position (or only closes it when that side is disabled). Entries are allowed only
/// on selected days, inside the session and the date range, outside the daily exit period and the news window; the exit period
/// and the news window also close open positions. Each entry freezes a stop of AtrMultiplier ATRs or MaxLossPerc of the price
/// and a take profit StaticRr times that distance. Trading stops once the equity drawdown reaches MaxDrawdown.
/// </summary>
public class BoilerplateConfigurableStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSqueeze;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _wideMultiplier;
	private readonly StrategyParam<decimal> _narrowMultiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<bool> _invert;
	private readonly StrategyParam<bool> _useAtrStops;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _maxLossPerc;
	private readonly StrategyParam<decimal> _staticRr;
	private readonly StrategyParam<decimal> _maxDrawdown;
	private readonly StrategyParam<bool> _tradeWeekends;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<DateTime> _startDate;
	private readonly StrategyParam<DateTime> _endDate;
	private readonly StrategyParam<bool> _useExitPeriod;
	private readonly StrategyParam<int> _exitStartHour;
	private readonly StrategyParam<int> _exitEndHour;
	private readonly StrategyParam<bool> _useNewsFilter;
	private readonly StrategyParam<int> _newsHour;
	private readonly StrategyParam<int> _newsMinute;
	private readonly StrategyParam<int> _newsWindow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _initialEquity;
	private decimal _peakEquity;
	private bool _halted;

	/// <summary>
	/// Use the Bollinger squeeze mode instead of the SMA cross.
	/// </summary>
	public bool UseSqueeze
	{
		get => _useSqueeze.Value;
		set => _useSqueeze.Value = value;
	}

	/// <summary>
	/// Fast SMA period of the cross mode.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA and Bollinger period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Deviation multiplier of the band price has to break.
	/// </summary>
	public decimal WideMultiplier
	{
		get => _wideMultiplier.Value;
		set => _wideMultiplier.Value = value;
	}

	/// <summary>
	/// Deviation multiplier of the band price has to stay inside.
	/// </summary>
	public decimal NarrowMultiplier
	{
		get => _narrowMultiplier.Value;
		set => _narrowMultiplier.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Swap long and short signals.
	/// </summary>
	public bool Invert
	{
		get => _invert.Value;
		set => _invert.Value = value;
	}

	/// <summary>
	/// Size the stop with ATR instead of a fixed percent.
	/// </summary>
	public bool UseAtrStops
	{
		get => _useAtrStops.Value;
		set => _useAtrStops.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple of the stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop distance as a fraction of the entry price.
	/// </summary>
	public decimal MaxLossPerc
	{
		get => _maxLossPerc.Value;
		set => _maxLossPerc.Value = value;
	}

	/// <summary>
	/// Take profit distance as a multiple of the stop distance.
	/// </summary>
	public decimal StaticRr
	{
		get => _staticRr.Value;
		set => _staticRr.Value = value;
	}

	/// <summary>
	/// Equity drawdown fraction that stops trading.
	/// </summary>
	public decimal MaxDrawdown
	{
		get => _maxDrawdown.Value;
		set => _maxDrawdown.Value = value;
	}

	/// <summary>
	/// Allow entries on Saturday and Sunday.
	/// </summary>
	public bool TradeWeekends
	{
		get => _tradeWeekends.Value;
		set => _tradeWeekends.Value = value;
	}

	/// <summary>
	/// Session start hour (UTC).
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Session end hour (UTC, exclusive).
	/// </summary>
	public int SessionEndHour
	{
		get => _sessionEndHour.Value;
		set => _sessionEndHour.Value = value;
	}

	/// <summary>
	/// First date entries are allowed.
	/// </summary>
	public DateTime StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Last date entries are allowed.
	/// </summary>
	public DateTime EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Close positions and block entries in the daily exit period.
	/// </summary>
	public bool UseExitPeriod
	{
		get => _useExitPeriod.Value;
		set => _useExitPeriod.Value = value;
	}

	/// <summary>
	/// Exit period start hour (UTC).
	/// </summary>
	public int ExitStartHour
	{
		get => _exitStartHour.Value;
		set => _exitStartHour.Value = value;
	}

	/// <summary>
	/// Exit period end hour (UTC, exclusive).
	/// </summary>
	public int ExitEndHour
	{
		get => _exitEndHour.Value;
		set => _exitEndHour.Value = value;
	}

	/// <summary>
	/// Close positions and block entries around the daily news time.
	/// </summary>
	public bool UseNewsFilter
	{
		get => _useNewsFilter.Value;
		set => _useNewsFilter.Value = value;
	}

	/// <summary>
	/// News hour (UTC).
	/// </summary>
	public int NewsHour
	{
		get => _newsHour.Value;
		set => _newsHour.Value = value;
	}

	/// <summary>
	/// News minute.
	/// </summary>
	public int NewsMinute
	{
		get => _newsMinute.Value;
		set => _newsMinute.Value = value;
	}

	/// <summary>
	/// Minutes before and after the news time.
	/// </summary>
	public int NewsWindow
	{
		get => _newsWindow.Value;
		set => _newsWindow.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BoilerplateConfigurableStrategy()
	{
		_useSqueeze = Param(nameof(UseSqueeze), false)
			.SetDisplay("Squeeze Mode", "Use the Bollinger squeeze mode instead of the SMA cross", "Mode");

		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period of the cross mode", "Indicators");

		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Slow SMA and Bollinger period", "Indicators");

		_wideMultiplier = Param(nameof(WideMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Wide Multiplier", "Deviation multiplier of the band price has to break", "Indicators");

		_narrowMultiplier = Param(nameof(NarrowMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Narrow Multiplier", "Deviation multiplier of the band price has to stay inside", "Indicators");

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Allow long entries", "Direction");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Allow short entries", "Direction");

		_invert = Param(nameof(Invert), false)
			.SetDisplay("Invert", "Swap long and short signals", "Direction");

		_useAtrStops = Param(nameof(UseAtrStops), true)
			.SetDisplay("ATR Stops", "Size the stop with ATR instead of a fixed percent", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple of the stop distance", "Risk");

		_maxLossPerc = Param(nameof(MaxLossPerc), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss", "Stop distance as a fraction of the entry price", "Risk");

		_staticRr = Param(nameof(StaticRr), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Take profit distance as a multiple of the stop distance", "Risk");

		_maxDrawdown = Param(nameof(MaxDrawdown), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Max Drawdown", "Equity drawdown fraction that stops trading", "Risk");

		_tradeWeekends = Param(nameof(TradeWeekends), true)
			.SetDisplay("Trade Weekends", "Allow entries on Saturday and Sunday", "Filters");

		_sessionStartHour = Param(nameof(SessionStartHour), 0)
			.SetRange(0, 23)
			.SetDisplay("Session Start", "Session start hour (UTC)", "Filters");

		_sessionEndHour = Param(nameof(SessionEndHour), 24)
			.SetRange(1, 24)
			.SetDisplay("Session End", "Session end hour (UTC, exclusive)", "Filters");

		_startDate = Param(nameof(StartDate), new DateTime(2000, 1, 1))
			.SetDisplay("Start Date", "First date entries are allowed", "Filters");

		_endDate = Param(nameof(EndDate), new DateTime(2100, 1, 1))
			.SetDisplay("End Date", "Last date entries are allowed", "Filters");

		_useExitPeriod = Param(nameof(UseExitPeriod), false)
			.SetDisplay("Use Exit Period", "Close positions and block entries in the daily exit period", "Filters");

		_exitStartHour = Param(nameof(ExitStartHour), 22)
			.SetRange(0, 23)
			.SetDisplay("Exit Start", "Exit period start hour (UTC)", "Filters");

		_exitEndHour = Param(nameof(ExitEndHour), 24)
			.SetRange(1, 24)
			.SetDisplay("Exit End", "Exit period end hour (UTC, exclusive)", "Filters");

		_useNewsFilter = Param(nameof(UseNewsFilter), false)
			.SetDisplay("Use News Filter", "Close positions and block entries around the daily news time", "News");

		_newsHour = Param(nameof(NewsHour), 12)
			.SetRange(0, 23)
			.SetDisplay("News Hour", "News hour (UTC)", "News");

		_newsMinute = Param(nameof(NewsMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("News Minute", "News minute", "News");

		_newsWindow = Param(nameof(NewsWindow), 5)
			.SetNotNegative()
			.SetDisplay("News Window", "Minutes before and after the news time", "News");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = null;
		_takePrice = null;
		_initialEquity = 0m;
		_peakEquity = 0m;
		_halted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();
		_initialEquity = Portfolio?.BeginValue is decimal begin && begin > 0 ? begin : Portfolio?.CurrentValue ?? 0m;
		_peakEquity = _initialEquity;

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = Length };
		var deviation = new StandardDeviation { Length = Length };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, slow, deviation, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue deviationValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !deviationValue.IsFormed || !atrValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Stop and take profit frozen at entry.
		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake && (candle.LowPrice <= longStop || candle.HighPrice >= longTake))
		{
			ClosePosition();
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake && (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake))
		{
			ClosePosition();
			return;
		}

		if (_initialEquity > 0)
		{
			var equity = _initialEquity + PnL;
			_peakEquity = Math.Max(_peakEquity, equity);

			if (_peakEquity > 0 && (_peakEquity - equity) / _peakEquity >= MaxDrawdown)
				_halted = true;
		}

		var time = candle.OpenTime;

		if (_halted || InExitPeriod(time) || InNewsWindow(time))
		{
			ClosePosition();
			return;
		}

		bool longSignal;
		bool shortSignal;

		if (UseSqueeze)
		{
			var sigma = deviationValue.GetValue<decimal>();
			longSignal = close > slow + WideMultiplier * sigma && close < slow + NarrowMultiplier * sigma;
			shortSignal = close < slow - WideMultiplier * sigma && close > slow - NarrowMultiplier * sigma;
		}
		else
		{
			longSignal = prevFast is decimal pf && prevSlow is decimal ps && pf <= ps && fast > slow;
			shortSignal = prevFast is decimal pf2 && prevSlow is decimal ps2 && pf2 >= ps2 && fast < slow;
		}

		if (Invert)
			(longSignal, shortSignal) = (shortSignal, longSignal);

		var canEnter = InSession(time);
		var stopDistance = UseAtrStops ? AtrMultiplier * atrValue.GetValue<decimal>() : MaxLossPerc * close;

		if (longSignal && Position <= 0)
		{
			if (AllowLong && canEnter && stopDistance > 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = close - stopDistance;
				_takePrice = close + StaticRr * stopDistance;
			}
			else if (Position < 0)
			{
				ClosePosition();
			}
		}
		else if (shortSignal && Position >= 0)
		{
			if (AllowShort && canEnter && stopDistance > 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = close + stopDistance;
				_takePrice = close - StaticRr * stopDistance;
			}
			else if (Position > 0)
			{
				ClosePosition();
			}
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_stopPrice = null;
		_takePrice = null;
	}

	private bool InSession(DateTime time)
	{
		if (!TradeWeekends && (time.DayOfWeek == DayOfWeek.Saturday || time.DayOfWeek == DayOfWeek.Sunday))
			return false;

		if (time.Date < StartDate.Date || time.Date > EndDate.Date)
			return false;

		return time.Hour >= SessionStartHour && time.Hour < SessionEndHour;
	}

	private bool InExitPeriod(DateTime time)
		=> UseExitPeriod && time.Hour >= ExitStartHour && time.Hour < ExitEndHour;

	private bool InNewsWindow(DateTime time)
	{
		if (!UseNewsFilter)
			return false;

		var news = time.Date.AddHours(NewsHour).AddMinutes(NewsMinute);
		return Math.Abs((time - news).TotalMinutes) <= NewsWindow;
	}
}
