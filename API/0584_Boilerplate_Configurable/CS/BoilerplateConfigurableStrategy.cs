using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Boilerplate Configurable Strategy - supports SMA crossover and Bollinger squeeze breakout modes with configurable filters.
/// </summary>
public class BoilerplateConfigurableStrategy : Strategy
{
	public enum StrategyMode
	{
		Squeeze,
		SmaCross
	}

	public enum TradeSide
	{
		Long,
		Short,
		Both
	}

	public enum RrMode
	{
		Atr,
		Static
	}

	private readonly StrategyParam<StrategyMode> _mode;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _wideMultiplier;
	private readonly StrategyParam<decimal> _narrowMultiplier;
	private readonly StrategyParam<TradeSide> _tradeDirection;
	private readonly StrategyParam<bool> _tradeInverse;
	private readonly StrategyParam<decimal> _maxLossPerc;
	private readonly StrategyParam<RrMode> _rrMode;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _staticRr;
	private readonly StrategyParam<bool> _enableNewsFilter;
	private readonly StrategyParam<DateTimeOffset> _newsTime;
	private readonly StrategyParam<int> _newsWindow;
	private readonly StrategyParam<string> _sessionInput;
	private readonly StrategyParam<string> _exitTime;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _enableMon;
	private readonly StrategyParam<bool> _enableTue;
	private readonly StrategyParam<bool> _enableWed;
	private readonly StrategyParam<bool> _enableThu;
	private readonly StrategyParam<bool> _enableFri;
	private readonly StrategyParam<bool> _enableSat;
	private readonly StrategyParam<bool> _enableSun;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maxDrawdown;

	private SimpleMovingAverage _maFast;
	private SimpleMovingAverage _maSlow;
	private BollingerBands _bbWide;
	private BollingerBands _bbNarrow;
	private AverageTrueRange _atr;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevHlc3;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _isLong;
	private decimal _peakEquity;
	private bool _drawdownBreached;

	public StrategyMode Mode { get => _mode.Value; set => _mode.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal WideMultiplier { get => _wideMultiplier.Value; set => _wideMultiplier.Value = value; }
	public decimal NarrowMultiplier { get => _narrowMultiplier.Value; set => _narrowMultiplier.Value = value; }
	public TradeSide TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
	public bool TradeInverse { get => _tradeInverse.Value; set => _tradeInverse.Value = value; }
	public decimal MaxLossPerc { get => _maxLossPerc.Value; set => _maxLossPerc.Value = value; }
	public RrMode RiskRewardMode { get => _rrMode.Value; set => _rrMode.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StaticRr { get => _staticRr.Value; set => _staticRr.Value = value; }
	public bool EnableNewsFilter { get => _enableNewsFilter.Value; set => _enableNewsFilter.Value = value; }
	public DateTimeOffset NewsTime { get => _newsTime.Value; set => _newsTime.Value = value; }
	public int NewsWindow { get => _newsWindow.Value; set => _newsWindow.Value = value; }
	public string SessionInput { get => _sessionInput.Value; set => _sessionInput.Value = value; }
	public string ExitTime { get => _exitTime.Value; set => _exitTime.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal MaxDrawdown { get => _maxDrawdown.Value; set => _maxDrawdown.Value = value; }
	public bool EnableMon { get => _enableMon.Value; set => _enableMon.Value = value; }
	public bool EnableTue { get => _enableTue.Value; set => _enableTue.Value = value; }
	public bool EnableWed { get => _enableWed.Value; set => _enableWed.Value = value; }
	public bool EnableThu { get => _enableThu.Value; set => _enableThu.Value = value; }
	public bool EnableFri { get => _enableFri.Value; set => _enableFri.Value = value; }
	public bool EnableSat { get => _enableSat.Value; set => _enableSat.Value = value; }
	public bool EnableSun { get => _enableSun.Value; set => _enableSun.Value = value; }

	public BoilerplateConfigurableStrategy()
	{
		_mode = Param(nameof(Mode), StrategyMode.Squeeze)
			.SetDisplay("Strategy Type", "Entry strategy mode", "General");

		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Indicator length", "General");

		_wideMultiplier = Param(nameof(WideMultiplier), 1.5m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("Wide BB Mult", "Wide Bollinger multiplier", "Squeeze");

		_narrowMultiplier = Param(nameof(NarrowMultiplier), 2m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("Narrow BB Mult", "Narrow Bollinger multiplier", "Squeeze");

		_tradeDirection = Param(nameof(TradeDirection), TradeSide.Both)
			.SetDisplay("Trade Direction", "Allowed trade direction", "Trading");

		_tradeInverse = Param(nameof(TradeInverse), false)
			.SetDisplay("Inverse", "Inverse trade side", "Trading");

		_maxLossPerc = Param(nameof(MaxLossPerc), 0.02m)
			.SetRange(0.0001m, 1m)
			.SetDisplay("Max Loss %", "Max loss per trade", "Risk");

		_rrMode = Param(nameof(RiskRewardMode), RrMode.Atr)
			.SetDisplay("RR Mode", "Risk reward mode", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("ATR Mult", "ATR multiplier", "Risk");

		_staticRr = Param(nameof(StaticRr), 2m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("Static RR", "Static risk reward", "Risk");

		_enableNewsFilter = Param(nameof(EnableNewsFilter), false)
			.SetDisplay("News Filter", "Enable news filter", "News");

		_newsTime = Param(nameof(NewsTime), DateTimeOffset.Now)
			.SetDisplay("News Time", "News time UTC", "News");

		_newsWindow = Param(nameof(NewsWindow), 5)
			.SetGreaterThanZero()
			.SetDisplay("News Window", "News window minutes", "News");

		_sessionInput = Param(nameof(SessionInput), "0000-0000")
			.SetDisplay("Session", "Trading session", "Time");

		_exitTime = Param(nameof(ExitTime), "1600-1700")
			.SetDisplay("Exit Time", "Daily exit period", "Time");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2024, 1, 1)))
			.SetDisplay("Start Date", "Start date", "Dates");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2025, 1, 31)))
			.SetDisplay("End Date", "End date", "Dates");

		_enableMon = Param(nameof(EnableMon), true).SetDisplay("Monday", "Trade Monday", "Days");
		_enableTue = Param(nameof(EnableTue), true).SetDisplay("Tuesday", "Trade Tuesday", "Days");
		_enableWed = Param(nameof(EnableWed), true).SetDisplay("Wednesday", "Trade Wednesday", "Days");
		_enableThu = Param(nameof(EnableThu), true).SetDisplay("Thursday", "Trade Thursday", "Days");
		_enableFri = Param(nameof(EnableFri), true).SetDisplay("Friday", "Trade Friday", "Days");
		_enableSat = Param(nameof(EnableSat), true).SetDisplay("Saturday", "Trade Saturday", "Days");
		_enableSun = Param(nameof(EnableSun), true).SetDisplay("Sunday", "Trade Sunday", "Days");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_maxDrawdown = Param(nameof(MaxDrawdown), 0.1m)
			.SetRange(0m, 1m)
			.SetDisplay("Max DD", "Max drawdown percent", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevHlc3 = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_isLong = false;
		_peakEquity = 0m;
		_drawdownBreached = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maFast = new SimpleMovingAverage { Length = Length };
		_maSlow = new SimpleMovingAverage { Length = Length };
		_bbWide = new BollingerBands { Length = Length, Width = WideMultiplier };
		_bbNarrow = new BollingerBands { Length = Length, Width = NarrowMultiplier };
		_atr = new AverageTrueRange { Length = AtrLength };

		_peakEquity = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bbWide);
			DrawIndicator(area, _bbNarrow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartDate || candle.OpenTime > EndDate)
			return;

		if (!IsTradingDay(candle.OpenTime))
			return;

		if (!InSession(candle.OpenTime))
			return;

		if (IsExitPeriod(candle.OpenTime))
		{
			CloseAll();
			return;
		}

		if (EnableNewsFilter && IsNewsTime(candle.OpenTime))
		{
			CloseAll();
			return;
		}

		UpdateDrawdown();
		if (_drawdownBreached)
			return;

		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var bbWideVal = (BollingerBandsValue)_bbWide.Process(hlc3, candle.OpenTime, true);
		var bbNarrowVal = (BollingerBandsValue)_bbNarrow.Process(hlc3, candle.OpenTime, true);
		var fastVal = _maFast.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
		var slowVal = _maSlow.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
		var atrVal = _atr.Process(candle);

		if (!_bbWide.IsFormed || !_bbNarrow.IsFormed || !_maFast.IsFormed || !_maSlow.IsFormed || !_atr.IsFormed)
			return;

		if (bbWideVal.UpBand is not decimal upper1 || bbWideVal.LowBand is not decimal lower1)
			return;
		if (bbNarrowVal.UpBand is not decimal upper2 || bbNarrowVal.LowBand is not decimal lower2)
			return;

		bool longCondition = false;
		bool shortCondition = false;

		switch (Mode)
		{
			case StrategyMode.SmaCross:
				longCondition = _prevFast <= _prevSlow && fastVal > slowVal;
				shortCondition = _prevFast >= _prevSlow && fastVal < slowVal;
				break;
			case StrategyMode.Squeeze:
				var squeeze = hlc3 < upper2 && hlc3 > lower2;
				longCondition = _prevHlc3 <= upper1 && hlc3 > upper1 && squeeze;
				shortCondition = _prevHlc3 >= lower1 && hlc3 < lower1 && squeeze;
				break;
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
		_prevHlc3 = hlc3;

		if (TradeInverse)
		{
			var tmp = longCondition;
			longCondition = shortCondition;
			shortCondition = tmp;
		}

		var allowLong = TradeDirection == TradeSide.Long || TradeDirection == TradeSide.Both;
		var allowShort = TradeDirection == TradeSide.Short || TradeDirection == TradeSide.Both;

		if (allowLong && longCondition && Position <= 0)
			Enter(true, candle.ClosePrice, atrVal);
		else if (allowShort && shortCondition && Position >= 0)
			Enter(false, candle.ClosePrice, atrVal);

		if (Position != 0)
			CheckStops(candle.ClosePrice);
	}

	private void Enter(bool isLong, decimal price, IIndicatorValue atrValue)
	{
		var atr = atrValue.ToDecimal();
		var volume = CalculateVolume(atr);
		_entryPrice = price;
		_isLong = isLong;

		var stop = RiskRewardMode == RrMode.Atr ? atr * AtrMultiplier : price * MaxLossPerc;
		var take = stop * StaticRr;

		if (isLong)
		{
			_stopPrice = price - stop;
			_takeProfitPrice = price + take;
			BuyMarket(volume + Math.Abs(Position));
		}
		else
		{
			_stopPrice = price + stop;
			_takeProfitPrice = price - take;
			SellMarket(volume + Math.Abs(Position));
		}
	}

	private void CheckStops(decimal price)
	{
		if (_isLong && Position > 0)
		{
			if (price <= _stopPrice || price >= _takeProfitPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (!_isLong && Position < 0)
		{
			if (price >= _stopPrice || price <= _takeProfitPrice)
				BuyMarket(Math.Abs(Position));
		}
	}

	private decimal CalculateVolume(decimal atr)
	{
		var equity = Portfolio.CurrentValue ?? 0m;
		var size = atr > 0m ? equity * MaxLossPerc / atr : Volume;
		return size > 0m ? size : Volume;
	}

	private bool IsTradingDay(DateTimeOffset time)
	{
		return (EnableMon && time.DayOfWeek == DayOfWeek.Monday)
			|| (EnableTue && time.DayOfWeek == DayOfWeek.Tuesday)
			|| (EnableWed && time.DayOfWeek == DayOfWeek.Wednesday)
			|| (EnableThu && time.DayOfWeek == DayOfWeek.Thursday)
			|| (EnableFri && time.DayOfWeek == DayOfWeek.Friday)
			|| (EnableSat && time.DayOfWeek == DayOfWeek.Saturday)
			|| (EnableSun && time.DayOfWeek == DayOfWeek.Sunday);
	}

	private bool InSession(DateTimeOffset time)
	{
		ParseSession(SessionInput, out var start, out var end);
		var t = time.TimeOfDay;
		return t >= start && t <= end;
	}

	private bool IsExitPeriod(DateTimeOffset time)
	{
		ParseSession(ExitTime, out var start, out var end);
		var t = time.TimeOfDay;
		return t >= start && t <= end;
	}

	private bool IsNewsTime(DateTimeOffset time)
	{
		var window = TimeSpan.FromMinutes(NewsWindow);
		return time >= NewsTime - window && time <= NewsTime + window;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (string.IsNullOrWhiteSpace(input))
			return;
		var parts = input.Split('-');
		if (parts.Length != 2)
			return;
		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}

	private void CloseAll()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private void UpdateDrawdown()
	{
		var equity = Portfolio.CurrentValue ?? 0m;
		if (equity > _peakEquity)
		{
			_peakEquity = equity;
			_drawdownBreached = false;
		}
		if (_peakEquity <= 0m)
			return;
		var dd = (_peakEquity - equity) / _peakEquity;
		if (dd >= MaxDrawdown && !_drawdownBreached)
		{
			_drawdownBreached = true;
			CloseAll();
		}
	}
}
