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
/// Multi-timeframe MACD confirmation strategy that aligns 5m, 15m, 1h and 4h trends.
/// </summary>
public class MacdMultiTimeframeExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _fiveMinuteType;
	private readonly StrategyParam<DataType> _fifteenMinuteType;
	private readonly StrategyParam<DataType> _hourType;
	private readonly StrategyParam<DataType> _fourHourType;

	private MovingAverageConvergenceDivergenceSignal _macdFiveMinute;
	private MovingAverageConvergenceDivergenceSignal _macdFifteenMinute;
	private MovingAverageConvergenceDivergenceSignal _macdHour;
	private MovingAverageConvergenceDivergenceSignal _macdFourHour;

	private int? _relationFiveMinute;
	private int? _relationFifteenMinute;
	private int? _relationHour;
	private int? _relationFourHour;

	private decimal _entryPrice;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA period used by MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period used by MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for the primary five-minute timeframe.
	/// </summary>
	public DataType FiveMinuteCandleType
	{
		get => _fiveMinuteType.Value;
		set => _fiveMinuteType.Value = value;
	}

	/// <summary>
	/// Candle type for the fifteen-minute confirmation timeframe.
	/// </summary>
	public DataType FifteenMinuteCandleType
	{
		get => _fifteenMinuteType.Value;
		set => _fifteenMinuteType.Value = value;
	}

	/// <summary>
	/// Candle type for the one-hour confirmation timeframe.
	/// </summary>
	public DataType HourCandleType
	{
		get => _hourType.Value;
		set => _hourType.Value = value;
	}

	/// <summary>
	/// Candle type for the four-hour confirmation timeframe.
	/// </summary>
	public DataType FourHourCandleType
	{
		get => _fourHourType.Value;
		set => _fourHourType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdMultiTimeframeExpertStrategy"/> class.
	/// </summary>
	public MacdMultiTimeframeExpertStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Position size in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Take-profit distance in points", "Risk");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Max Spread", "Maximum allowed spread in points", "Risk");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period", "MACD");

		_fiveMinuteType = Param(nameof(FiveMinuteCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("5 Minute", "Primary execution timeframe", "Timeframes");

		_fifteenMinuteType = Param(nameof(FifteenMinuteCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("15 Minute", "First confirmation timeframe", "Timeframes");

		_hourType = Param(nameof(HourCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("1 Hour", "Second confirmation timeframe", "Timeframes");

		_fourHourType = Param(nameof(FourHourCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("4 Hour", "Third confirmation timeframe", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, FiveMinuteCandleType);
		yield return (Security, FifteenMinuteCandleType);
		yield return (Security, HourCandleType);
		yield return (Security, FourHourCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macdFiveMinute = CreateMacd();
		_macdFifteenMinute = CreateMacd();
		_macdHour = CreateMacd();
		_macdFourHour = CreateMacd();

		// Bind the execution timeframe to the MACD handler.
		var fiveMinuteSubscription = SubscribeCandles(FiveMinuteCandleType);
		fiveMinuteSubscription
			.Bind(ProcessFiveMinuteCandleRaw)
			.Start();

		SubscribeCandles(FifteenMinuteCandleType)
			.Bind(ProcessFifteenMinuteCandleRaw)
			.Start();

		SubscribeCandles(HourCandleType)
			.Bind(ProcessHourCandleRaw)
			.Start();

		SubscribeCandles(FourHourCandleType)
			.Bind(ProcessFourHourCandleRaw)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, fiveMinuteSubscription);
			DrawOwnTrades(area);
		}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
	return new MovingAverageConvergenceDivergenceSignal
	{
		Macd =
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod }
		},
		SignalMa = { Length = SignalPeriod }
	};
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
	_bestBidPrice = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _bestBidPrice;
	_bestAskPrice = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _bestAskPrice;
	}

	private void ProcessFiveMinuteCandleRaw(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdFiveMinute.Process(candle);
		if (!TryUpdateRelation(macdValue, out var relation))
			return;

		_relationFiveMinute = relation;

		if (!HasAllRelations())
			return;

		// Manage protective exits whenever a position is open.
		if (Position != 0)
		{
			ManageOpenPosition(candle);
			return;
		}

		if (OrderVolume <= 0)
			return;

		if (AllRelationsEqual(1))
		{
			BuyMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
		}
		else if (AllRelationsEqual(-1))
		{
			SellMarket(OrderVolume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private void ProcessFifteenMinuteCandleRaw(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdFifteenMinute.Process(candle);
		if (TryUpdateRelation(macdValue, out var relation))
			_relationFifteenMinute = relation;
	}

	private void ProcessHourCandleRaw(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdHour.Process(candle);
		if (TryUpdateRelation(macdValue, out var relation))
			_relationHour = relation;
	}

	private void ProcessFourHourCandleRaw(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macdFourHour.Process(candle);
		if (TryUpdateRelation(macdValue, out var relation))
			_relationFourHour = relation;
	}

	private bool TryUpdateRelation(IIndicatorValue macdValue, out int relation)
	{
	relation = 0;

	if (!macdValue.IsFinal)
	return false;

	var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

	if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
	return false;

	if (signal > macd)
	relation = 1;
	else if (signal < macd)
	relation = -1;
	else
	relation = 0;

	return true;
	}

	private bool HasAllRelations()
	{
	return _relationFiveMinute.HasValue &&
	_relationFifteenMinute.HasValue &&
	_relationHour.HasValue &&
	_relationFourHour.HasValue;
	}

	private bool AllRelationsEqual(int relation)
	{
	return _relationFiveMinute == relation &&
	_relationFifteenMinute == relation &&
	_relationHour == relation &&
	_relationFourHour == relation;
	}

	private bool TryGetSpread(out decimal spreadPoints)
	{
	spreadPoints = 0m;

	if (_bestBidPrice is not decimal bid || _bestAskPrice is not decimal ask)
	return false;

	if (bid <= 0 || ask <= 0 || ask <= bid)
	return false;

	// Convert the raw price spread into points using the instrument price step.
	var step = Security?.PriceStep ?? 0m;
	if (step > 0)
	spreadPoints = (ask - bid) / step;
	else
	spreadPoints = ask - bid;

	return true;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
	// Derive the point value. Fall back to 1 if the security lacks a price step.
	var point = Security?.PriceStep ?? 0m;
	if (point <= 0)
	point = 1m;

	if (Position > 0)
	{
	if (TakeProfitPoints > 0 && candle.HighPrice >= _entryPrice + TakeProfitPoints * point)
	{
	SellMarket(Position);
	_entryPrice = 0m;
	return;
	}

	if (StopLossPoints > 0 && candle.LowPrice <= _entryPrice - StopLossPoints * point)
	{
	SellMarket(Position);
	_entryPrice = 0m;
	}
	}
	else if (Position < 0)
	{
	var volume = Math.Abs(Position);

	if (TakeProfitPoints > 0 && candle.LowPrice <= _entryPrice - TakeProfitPoints * point)
	{
	BuyMarket(volume);
	_entryPrice = 0m;
	return;
	}

	if (StopLossPoints > 0 && candle.HighPrice >= _entryPrice + StopLossPoints * point)
	{
	BuyMarket(volume);
	_entryPrice = 0m;
	}
	}
	else
	{
	_entryPrice = 0m;
	}
	}
}