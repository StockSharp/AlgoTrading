using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 3 Down, 3 Up strategy. Buys after consecutive down closes and exits after up closes. Optionally filters entries by EMA.
/// </summary>
public class ThreeDownThreeUpStrategy : Strategy
{
	private readonly StrategyParam<int> _buyTrigger;
	private readonly StrategyParam<int> _sellTrigger;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private ExponentialMovingAverage _ema;
	private int _aboveCount;
	private int _belowCount;
	private decimal? _previousClose;

	/// <summary>
	/// Number of consecutive down closes required to enter.
	/// </summary>
	public int BuyTrigger { get => _buyTrigger.Value; set => _buyTrigger.Value = value; }

	/// <summary>
	/// Number of consecutive up closes required to exit.
	/// </summary>
	public int SellTrigger { get => _sellTrigger.Value; set => _sellTrigger.Value = value; }

	/// <summary>
	/// Enable EMA trend filter.
	/// </summary>
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }

	/// <summary>
	/// EMA period for optional trend filter.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start of trading window.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End of trading window.
	/// </summary>
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeDownThreeUpStrategy()
	{
	_buyTrigger = Param(nameof(BuyTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Buy Trigger", "Consecutive down closes for entry", "Strategy Settings");

	_sellTrigger = Param(nameof(SellTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Sell Trigger", "Consecutive up closes for exit", "Strategy Settings");

	_useEmaFilter = Param(nameof(UseEmaFilter), false)
		.SetDisplay("Use EMA Filter", "Enable EMA trend filter", "Trend Filter");

	_emaPeriod = Param(nameof(EmaPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Period for EMA filter", "Trend Filter");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

	_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Time", "Start of trading window", "Time Settings");

	_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("End Time", "End of trading window", "Time Settings");
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

	_aboveCount = 0;
	_belowCount = 0;
	_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_ema = new ExponentialMovingAverage { Length = EmaPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(_ema, ProcessCandle)
		.Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _ema);
		DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
	if (candle.State != CandleStates.Finished)
		return;

	if (UseEmaFilter && !_ema.IsFormed)
		return;

	if (_previousClose != null)
	{
		if (candle.ClosePrice > _previousClose)
		{
		_aboveCount++;
		_belowCount = 0;
		}
		else if (candle.ClosePrice < _previousClose)
		{
		_belowCount++;
		_aboveCount = 0;
		}
		else
		{
		_aboveCount = 0;
		_belowCount = 0;
		}
	}

	_previousClose = candle.ClosePrice;

	var withinWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

	var longCondition = _belowCount >= BuyTrigger && withinWindow;
	if (UseEmaFilter)
		longCondition &= candle.ClosePrice > emaValue;

	var exitCondition = _aboveCount >= SellTrigger;

	if (Position <= 0 && longCondition)
	{
		BuyMarket();
		_aboveCount = 0;
	}
	else if (Position > 0 && exitCondition)
	{
		RegisterSell(Math.Abs(Position));
		_belowCount = 0;
	}
	}
}

