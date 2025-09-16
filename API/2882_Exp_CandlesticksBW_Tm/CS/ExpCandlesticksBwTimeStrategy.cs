using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Candlesticks BW based strategy with optional trading session filter.
/// </summary>
public class ExpCandlesticksBwTimeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _awesomeOscillator = null!;
	private SimpleMovingAverage _aoAverage = null!;

	private readonly List<int> _colorHistory = new();
	private readonly List<DateTimeOffset> _barTimes = new();

	private TimeSpan _timeFrame;
	private decimal? _prevAo;
	private decimal? _prevAc;

	private DateTimeOffset? _lastBuyOpenSignalTime;
	private DateTimeOffset? _lastSellOpenSignalTime;
	private DateTimeOffset? _lastBuyCloseSignalTime;
	private DateTimeOffset? _lastSellCloseSignalTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpCandlesticksBwTimeStrategy"/> class.
	/// </summary>
	public ExpCandlesticksBwTimeStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume for entries", "Risk")
			.SetGreaterThanZero();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Offset of the analysed candle", "Signals")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (points)", "Protective stop distance in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (points)", "Take profit distance in price points", "Risk");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening buy positions", "Signals");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening sell positions", "Signals");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing buy positions", "Signals");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing sell positions", "Signals");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Limit trading to selected hours", "Time");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading window start hour", "Time")
			.SetRange(0, 23);

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Trading window start minute", "Time")
			.SetRange(0, 59);

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading window end hour", "Time")
			.SetRange(0, 23);

		_endMinute = Param(nameof(EndMinute), 59)
			.SetDisplay("End Minute", "Trading window end minute", "Time")
			.SetRange(0, 59);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for CandlesticksBW", "General");
	}

	/// <summary>
	/// Trade volume for new orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Number of bars to offset when evaluating signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Enables closing long positions when the exit condition appears.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Enables closing short positions when the exit condition appears.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Enables the trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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

		_colorHistory.Clear();
		_barTimes.Clear();
		_prevAo = null;
		_prevAc = null;
		_lastBuyOpenSignalTime = null;
		_lastSellOpenSignalTime = null;
		_lastBuyCloseSignalTime = null;
		_lastSellCloseSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_awesomeOscillator = new AwesomeOscillator
		{
			ShortPeriod = 5,
			LongPeriod = 34
		};

		_aoAverage = new SimpleMovingAverage { Length = 5 };

		_timeFrame = CandleType.Arg is TimeSpan frame ? frame : TimeSpan.Zero;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_awesomeOscillator, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null,
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _awesomeOscillator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue awesomeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!awesomeValue.IsFinal || !awesomeValue.IsFormed)
			return;

		var ao = awesomeValue.GetValue<decimal>();
		var averageValue = _aoAverage.Process(ao);
		if (!averageValue.IsFinal)
			return;

		var ac = ao - averageValue.GetValue<decimal>();

		if (_prevAo is null || _prevAc is null)
		{
			_prevAo = ao;
			_prevAc = ac;
			return;
		}

		var color = GetColorIndex(candle, ao, ac, _prevAo.Value, _prevAc.Value);
		_colorHistory.Add(color);
		_barTimes.Add(candle.OpenTime);

		_prevAo = ao;
		_prevAc = ac;

		var maxHistory = SignalBar + 4;
		if (_colorHistory.Count > maxHistory)
		{
			_colorHistory.RemoveAt(0);
			_barTimes.RemoveAt(0);
		}

		if (_colorHistory.Count <= SignalBar + 1)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signalIndex = _colorHistory.Count - 1 - SignalBar;
		if (signalIndex <= 0)
			return;

		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[signalIndex - 1];
		var signalTime = _barTimes[signalIndex] + _timeFrame;

		var withinWindow = !UseTimeFilter || IsWithinTradingWindow(signalTime);

		if (UseTimeFilter && !withinWindow)
		{
			CloseOutsideSession();
		}

		HandleCloseSignals(signalTime, previousColor);

		if (!withinWindow)
			return;

		HandleOpenSignals(signalTime, previousColor, currentColor);
	}

	private void HandleOpenSignals(DateTimeOffset signalTime, int previousColor, int currentColor)
	{
		var bullishSetup = previousColor < 2;
		var bearishSetup = previousColor > 3;
		var buyTriggered = EnableLongEntries && bullishSetup && currentColor > 1;
		var sellTriggered = EnableShortEntries && bearishSetup && currentColor < 4;

		if (buyTriggered && signalTime != _lastBuyOpenSignalTime && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_lastBuyOpenSignalTime = signalTime;
		}

		if (sellTriggered && signalTime != _lastSellOpenSignalTime && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_lastSellOpenSignalTime = signalTime;
		}
	}

	private void HandleCloseSignals(DateTimeOffset signalTime, int previousColor)
	{
		var bullishSetup = previousColor < 2;
		var bearishSetup = previousColor > 3;

		if (EnableShortExits && bullishSetup && signalTime != _lastSellCloseSignalTime && Position < 0)
		{
			BuyMarket(-Position);
			_lastSellCloseSignalTime = signalTime;
		}

		if (EnableLongExits && bearishSetup && signalTime != _lastBuyCloseSignalTime && Position > 0)
		{
			SellMarket(Position);
			_lastBuyCloseSignalTime = signalTime;
		}
	}

	private void CloseOutsideSession()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var hour = time.Hour;
		var minute = time.Minute;

		if (StartHour < EndHour || (StartHour == EndHour && StartMinute < EndMinute))
		{
			if (hour == StartHour && minute >= StartMinute)
				return true;
			if (hour > StartHour && hour < EndHour)
				return true;
			if (hour == EndHour && minute < EndMinute)
				return true;
			return false;
		}

		if (StartHour == EndHour)
			return hour == StartHour && minute >= StartMinute && minute < EndMinute;

		if (hour > StartHour || (hour == StartHour && minute >= StartMinute))
			return true;
		if (hour < EndHour || (hour == EndHour && minute < EndMinute))
			return true;
		return false;
	}

	private static int GetColorIndex(ICandleMessage candle, decimal ao, decimal ac, decimal previousAo, decimal previousAc)
	{
		var bullishBody = candle.ClosePrice >= candle.OpenPrice;
		var aoGrowing = ao >= previousAo;
		var acGrowing = ac >= previousAc;
		var aoFalling = ao <= previousAo;
		var acFalling = ac <= previousAc;

		if (aoGrowing && acGrowing)
			return bullishBody ? 0 : 1;

		if (aoFalling && acFalling)
			return bullishBody ? 4 : 5;

		return bullishBody ? 2 : 3;
	}
}
