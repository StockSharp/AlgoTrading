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
/// Detects a tight European session range and trades breakouts during the U.S. session.
/// Ported from the "Session breakout" MetaTrader expert advisor.
/// </summary>
public class SessionBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _europeSessionStartHour;
	private readonly StrategyParam<int> _europeSessionEndHour;
	private readonly StrategyParam<int> _usSessionStartHour;
	private readonly StrategyParam<int> _usSessionEndHour;
	private readonly StrategyParam<int> _smallSessionThresholdPips;
	private readonly StrategyParam<int> _breakoutBufferPips;
	private readonly StrategyParam<bool> _tradeOnMonday;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDate;
	private decimal? _sessionHigh;
	private decimal? _sessionLow;
	private bool _sessionRangeComputed;
	private bool _smallSession;
	private bool _longExecuted;
	private bool _shortExecuted;
	private decimal _pipSize;


	/// <summary>
	/// Hour when the European session monitoring starts.
	/// </summary>
	public int EuropeSessionStartHour
	{
		get => _europeSessionStartHour.Value;
		set => _europeSessionStartHour.Value = value;
	}

	/// <summary>
	/// Hour when the European session monitoring stops.
	/// </summary>
	public int EuropeSessionEndHour
	{
		get => _europeSessionEndHour.Value;
		set => _europeSessionEndHour.Value = value;
	}

	/// <summary>
	/// Hour when the U.S. session monitoring starts.
	/// </summary>
	public int UsSessionStartHour
	{
		get => _usSessionStartHour.Value;
		set => _usSessionStartHour.Value = value;
	}

	/// <summary>
	/// Hour when the U.S. session monitoring stops.
	/// </summary>
	public int UsSessionEndHour
	{
		get => _usSessionEndHour.Value;
		set => _usSessionEndHour.Value = value;
	}

	/// <summary>
	/// Maximum number of pips allowed for the European session range to qualify.
	/// </summary>
	public int SmallSessionThresholdPips
	{
		get => _smallSessionThresholdPips.Value;
		set => _smallSessionThresholdPips.Value = value;
	}

	/// <summary>
	/// Additional buffer in pips applied above or below the European range before trading.
	/// </summary>
	public int BreakoutBufferPips
	{
		get => _breakoutBufferPips.Value;
		set => _breakoutBufferPips.Value = value;
	}

	/// <summary>
	/// Enables trading on Mondays.
	/// </summary>
	public bool TradeOnMonday
	{
		get => _tradeOnMonday.Value;
		set => _tradeOnMonday.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type used for detecting the sessions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionBreakoutStrategy"/> class.
	/// </summary>
	public SessionBreakoutStrategy()
	{

		_europeSessionStartHour = Param(nameof(EuropeSessionStartHour), 6)
		.SetDisplay("EU start hour", "Hour when the European session begins", "Sessions")
		.SetCanOptimize(true);

		_europeSessionEndHour = Param(nameof(EuropeSessionEndHour), 12)
		.SetDisplay("EU end hour", "Hour when the European session ends", "Sessions")
		.SetCanOptimize(true);

		_usSessionStartHour = Param(nameof(UsSessionStartHour), 12)
		.SetDisplay("US start hour", "Hour when the U.S. session begins", "Sessions")
		.SetCanOptimize(true);

		_usSessionEndHour = Param(nameof(UsSessionEndHour), 16)
		.SetDisplay("US end hour", "Hour when the U.S. session ends", "Sessions")
		.SetCanOptimize(true);

		_smallSessionThresholdPips = Param(nameof(SmallSessionThresholdPips), 30)
		.SetGreaterThanZero()
		.SetDisplay("Small session (pips)", "Maximum European range to allow trades", "Filters")
		.SetCanOptimize(true);

		_breakoutBufferPips = Param(nameof(BreakoutBufferPips), 3)
		.SetNotNegative()
		.SetDisplay("Breakout buffer (pips)", "Additional buffer above or below the range", "Filters")
		.SetCanOptimize(true);

		_tradeOnMonday = Param(nameof(TradeOnMonday), false)
		.SetDisplay("Trade on Monday", "Allow entries on Mondays", "Filters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 15)
		.SetGreaterThanZero()
		.SetDisplay("Take profit (pips)", "Take-profit distance", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 12)
		.SetGreaterThanZero()
		.SetDisplay("Stop loss (pips)", "Stop-loss distance", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle type", "Timeframe used for the session analysis", "General");
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

		_currentDate = default;
		_sessionHigh = null;
		_sessionLow = null;
		_sessionRangeComputed = false;
		_smallSession = false;
		_longExecuted = false;
		_shortExecuted = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var candleDate = candle.CloseTime.Date;

		if (candleDate != _currentDate)
		ResetDailyState(candleDate);

		if (!IsTradingDay(candleDate))
		return;

		var hour = candle.CloseTime.Hour;

		if (IsWithinEuSession(hour))
		UpdateEuropeanRange(candle);

		if (!_sessionRangeComputed && hour >= UsSessionStartHour)
		ComputeSessionRange();

		if (!_sessionRangeComputed || !_smallSession)
		return;

		if (hour < UsSessionStartHour || hour >= UsSessionEndHour)
		return;

		if (hour <= EuropeSessionStartHour + 5 || hour >= EuropeSessionStartHour + 10)
		return;

		TryEnterPositions(candle);
	}

	private void ResetDailyState(DateTime date)
	{
		_currentDate = date;
		_sessionHigh = null;
		_sessionLow = null;
		_sessionRangeComputed = false;
		_smallSession = false;
		_longExecuted = false;
		_shortExecuted = false;
	}

	private bool IsTradingDay(DateTime date)
	{
		var day = date.DayOfWeek;

		if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
		return false;

		if (day == DayOfWeek.Monday && !TradeOnMonday)
		return false;

		return true;
	}

	private bool IsWithinEuSession(int hour)
	{
		return hour >= EuropeSessionStartHour && hour < EuropeSessionEndHour;
	}

	private void UpdateEuropeanRange(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (_sessionHigh is null || high > _sessionHigh)
		_sessionHigh = high;

		if (_sessionLow is null || low < _sessionLow)
		_sessionLow = low;
	}

	private void ComputeSessionRange()
	{
		if (_sessionHigh is null || _sessionLow is null)
		return;

		var range = _sessionHigh.Value - _sessionLow.Value;
		_smallSession = range <= SmallSessionThresholdPips * _pipSize;
		_sessionRangeComputed = true;

		LogInfo($"EU range captured. High={_sessionHigh:F5}, Low={_sessionLow:F5}, Range={range:F5}, Small session={_smallSession}");
	}

	private void TryEnterPositions(ICandleMessage candle)
	{
		if (_sessionHigh is null || _sessionLow is null)
		return;

		var buffer = BreakoutBufferPips * _pipSize;
		var longTrigger = _sessionHigh.Value + buffer;
		var shortTrigger = _sessionLow.Value - buffer;
		var price = candle.ClosePrice;

		if (!_longExecuted && Position <= 0 && candle.LowPrice > longTrigger)
		{
			EnterLong(price);
		}

		if (!_shortExecuted && Position >= 0 && candle.HighPrice < shortTrigger)
		{
			EnterShort(price);
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		var volume = Volume + Math.Max(-Position, 0m);

		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_longExecuted = true;

		var resultingPosition = Position + volume;

		if (TakeProfitPips > 0)
		SetTakeProfit(TakeProfitPips, referencePrice, resultingPosition);

		if (StopLossPips > 0)
		SetStopLoss(StopLossPips, referencePrice, resultingPosition);
	}

	private void EnterShort(decimal referencePrice)
	{
		var volume = Volume + Math.Max(Position, 0m);

		if (volume <= 0m)
		return;

		SellMarket(volume);
		_shortExecuted = true;

		var resultingPosition = Position - volume;

		if (TakeProfitPips > 0)
		SetTakeProfit(TakeProfitPips, referencePrice, resultingPosition);

		if (StopLossPips > 0)
		SetStopLoss(StopLossPips, referencePrice, resultingPosition);
	}
}

