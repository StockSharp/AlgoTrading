using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High-low breakout strategy with ATR-based trailing stop.
/// </summary>
public class HighLowBreakoutAtrTrailingStopStrategy : Strategy
{
	public enum TradeDirection
	{
		Both,
		LongOnly,
		ShortOnly
	}

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _accountSize;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionStartMinute;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _firstHigh;
	private decimal? _firstLow;
	private DateTime _currentDay;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevClose;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Risk per trade in percent.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Account size.
	/// </summary>
	public decimal AccountSize
	{
		get => _accountSize.Value;
		set => _accountSize.Value = value;
	}

	/// <summary>
	/// Allowed trading direction.
	/// </summary>
	public TradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int SessionStartMinute
	{
		get => _sessionStartMinute.Value;
		set => _sessionStartMinute.Value = value;
	}

	/// <summary>
	/// Exit hour.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Exit minute.
	/// </summary>
	public int ExitMinute
	{
		get => _exitMinute.Value;
		set => _exitMinute.Value = value;
	}

	/// <summary>
	/// Candle type for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public HighLowBreakoutAtrTrailingStopStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Risk")
			.SetGreaterThanZero();

		_atrMultiplier = Param(nameof(AtrMultiplier), 3.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stops", "Risk")
			.SetGreaterThanZero();

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetDisplay("Risk %", "Risk per trade percentage", "Risk")
			.SetGreaterThanZero();

		_accountSize = Param(nameof(AccountSize), 10000m)
			.SetDisplay("Account Size", "Total account size", "Risk")
			.SetGreaterThanZero();

		_direction = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed trading direction", "General");

		_sessionStartHour = Param(nameof(SessionStartHour), 9)
			.SetDisplay("Session Start Hour", "Trading session start hour", "Session");

		_sessionStartMinute = Param(nameof(SessionStartMinute), 15)
			.SetDisplay("Session Start Minute", "Trading session start minute", "Session");

		_exitHour = Param(nameof(ExitHour), 15)
			.SetDisplay("Exit Hour", "Hour to close all positions", "Session");

		_exitMinute = Param(nameof(ExitMinute), 15)
			.SetDisplay("Exit Minute", "Minute to close all positions", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_firstHigh = null;
		_firstLow = null;
		_currentDay = default;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_firstHigh = null;
			_firstLow = null;
		}

		var sessionStart = day.AddHours(SessionStartHour).AddMinutes(SessionStartMinute);
		var firstEnd = sessionStart.AddMinutes(30);
		var inFirstPeriod = candle.OpenTime >= sessionStart && candle.OpenTime < firstEnd;

		if (inFirstPeriod)
		{
			_firstHigh = _firstHigh is null ? candle.HighPrice : Math.Max(_firstHigh.Value, candle.HighPrice);
			_firstLow = _firstLow is null ? candle.LowPrice : Math.Min(_firstLow.Value, candle.LowPrice);
		}

		if (candle.OpenTime.Hour == ExitHour && candle.OpenTime.Minute == ExitMinute)
			CloseAll();

		if (_firstHigh is null || _firstLow is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var allowLong = Direction != TradeDirection.ShortOnly;
		var allowShort = Direction != TradeDirection.LongOnly;

		var stopDistance = atr * AtrMultiplier;
		var riskAmount = AccountSize * (RiskPerTrade / 100m);
		var positionSize = stopDistance > 0m ? riskAmount / stopDistance : 0m;

		var longBreak = allowLong && _prevClose <= _firstHigh && candle.ClosePrice > _firstHigh && Position <= 0;
		var shortBreak = allowShort && _prevClose >= _firstLow && candle.ClosePrice < _firstLow && Position >= 0;

		if (longBreak)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - stopDistance;
			_takePrice = _entryPrice + stopDistance;
			BuyMarket(positionSize);
		}
		else if (shortBreak)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + stopDistance;
			_takePrice = _entryPrice - stopDistance;
			SellMarket(positionSize);
		}
		else if (Position > 0)
		{
			var trail = candle.ClosePrice - stopDistance;
			if (trail > _stopPrice)
				_stopPrice = trail;

			if (candle.LowPrice <= _stopPrice)
				SellMarket(Position);
			else if (candle.HighPrice >= _takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var trail = candle.ClosePrice + stopDistance;
			if (trail < _stopPrice)
				_stopPrice = trail;

			if (candle.HighPrice >= _stopPrice)
				BuyMarket(-Position);
			else if (candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
		}

		_prevClose = candle.ClosePrice;
	}

	private void CloseAll()
	{
		CancelActiveOrders();
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
