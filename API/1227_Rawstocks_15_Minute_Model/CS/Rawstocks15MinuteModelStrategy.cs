using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rawstocks 15-Minute Model strategy.
/// Uses swing-based order blocks with Fibonacci retracements and time filter.
/// </summary>
public class Rawstocks15MinuteModelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _entryCutoffHour;
	private readonly StrategyParam<int> _entryCutoffMinute;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinute;
	private readonly StrategyParam<decimal> _fibLevel;
	private readonly StrategyParam<decimal> _minSwingSize;
	private readonly StrategyParam<decimal> _rrRatio;

	private AverageTrueRange _atr = null!;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;

	private decimal? _bullOb;
	private decimal? _bearOb;
	private decimal? _swingTop;
	private decimal? _swingBot;

	private bool _forceClosed;
	private DateTime _lastDate;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Initialize <see cref="Rawstocks15MinuteModelStrategy"/>.
	/// </summary>
	public Rawstocks15MinuteModelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (ET)", "Time");

		_startMinute = Param(nameof(StartMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Session start minute", "Time");

		_entryCutoffHour = Param(nameof(EntryCutoffHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Last Entry Hour", "Last entry hour (ET)", "Time");

		_entryCutoffMinute = Param(nameof(EntryCutoffMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Last Entry Minute", "Last entry minute", "Time");

		_closeHour = Param(nameof(CloseHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Force Close Hour", "Force close hour (ET)", "Time");

		_closeMinute = Param(nameof(CloseMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Force Close Minute", "Force close minute", "Time");

		_fibLevel = Param(nameof(FibLevelPercent), 61.8m)
			.SetDisplay("Fib Level (%)", "Fibonacci level percent", "Trading");

		_minSwingSize = Param(nameof(MinSwingSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Min Swing Size (%)", "Minimum swing size as % of ATR", "Indicators");

		_rrRatio = Param(nameof(RrRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "Trading");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int EntryCutoffHour { get => _entryCutoffHour.Value; set => _entryCutoffHour.Value = value; }
	public int EntryCutoffMinute { get => _entryCutoffMinute.Value; set => _entryCutoffMinute.Value = value; }
	public int CloseHour { get => _closeHour.Value; set => _closeHour.Value = value; }
	public int CloseMinute { get => _closeMinute.Value; set => _closeMinute.Value = value; }
	public decimal FibLevelPercent { get => _fibLevel.Value; set => _fibLevel.Value = value; }
	public decimal MinSwingSize { get => _minSwingSize.Value; set => _minSwingSize.Value = value; }
	public decimal RrRatio { get => _rrRatio.Value; set => _rrRatio.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bullOb = _bearOb = _swingTop = _swingBot = null;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_forceClosed = false;
		_lastDate = DateTime.MinValue;
		_stopPrice = _takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var eastern = candle.OpenTime.ToOffset(TimeSpan.FromHours(-4));
		if (eastern.Date != _lastDate)
		{
		_forceClosed = false;
		_lastDate = eastern.Date;
		}

		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		var swingHigh = _h3 >= _h1 && _h3 >= _h2 && _h3 >= _h4 && _h3 >= _h5 && (_h3 - _l3) >= atr * MinSwingSize / 100m;
		var swingLow = _l3 <= _l1 && _l3 <= _l2 && _l3 <= _l4 && _l3 <= _l5 && (_h3 - _l3) >= atr * MinSwingSize / 100m;

		if (swingLow)
		{
		_bullOb = _l3;
		_swingBot = _l3;
		}

		if (swingHigh)
		{
		_bearOb = _h3;
		_swingTop = _h3;
		}

		decimal? fib618 = null;
		decimal? fib79 = null;
		if (_swingTop is decimal top && _swingBot is decimal bot)
		{
		var range = top - bot;
		fib618 = bot + range * (FibLevelPercent / 100m);
		fib79 = bot + range * 0.79m;
		}

		var sessionTime = eastern.TimeOfDay;
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var cutoff = new TimeSpan(EntryCutoffHour, EntryCutoffMinute, 0);
		var close = new TimeSpan(CloseHour, CloseMinute, 0);

		var validEntry = sessionTime >= start && sessionTime <= cutoff;

		var longCond = _bullOb is decimal bull && fib618 is decimal f618 && fib79 is decimal f79 &&
			candle.LowPrice <= bull && (candle.ClosePrice >= f618 || candle.ClosePrice >= f79);
		var shortCond = _bearOb is decimal bear && fib618 is decimal f6182 && fib79 is decimal f792 &&
			candle.HighPrice >= bear && (candle.ClosePrice <= f6182 || candle.ClosePrice <= f792);

		if (longCond && validEntry && Position <= 0)
		{
		_stopPrice = candle.LowPrice - atr;
		_takePrice = candle.ClosePrice + atr * RrRatio;
		BuyMarket();
		}
		else if (shortCond && validEntry && Position >= 0)
		{
		_stopPrice = candle.HighPrice + atr;
		_takePrice = candle.ClosePrice - atr * RrRatio;
		SellMarket();
		}

		if (Position > 0)
		{
		if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
		SellMarket();
		}
		else if (Position < 0)
		{
		if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
		BuyMarket();
		}

		if (!_forceClosed && sessionTime >= close && sessionTime < close.Add(TimeSpan.FromMinutes(1)))
		{
		CloseAll();
		_forceClosed = true;
		}
	}
}
