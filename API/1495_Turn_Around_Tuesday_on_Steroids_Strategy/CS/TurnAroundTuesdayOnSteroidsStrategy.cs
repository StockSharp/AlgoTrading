using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Turn around Tuesday on Steroids trading strategy.
/// </summary>
public class TurnAroundTuesdayOnSteroidsStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DayOfWeek> _startingDay;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose1;
	private decimal _prevClose2;
	private decimal _prevHigh;

	/// <summary>
	/// Start date for analysis window.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date for analysis window.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Starting day of week.
	/// </summary>
	public DayOfWeek StartingDay { get => _startingDay.Value; set => _startingDay.Value = value; }

	/// <summary>
	/// Use moving average filter.
	/// </summary>
	public bool UseMaFilter { get => _useMaFilter.Value; set => _useMaFilter.Value = value; }

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TurnAroundTuesdayOnSteroidsStrategy"/>.
	/// </summary>
	public TurnAroundTuesdayOnSteroidsStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2014, 1, 1)))
			.SetDisplay("Start Time", "Start of analysis window", "Time");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2099, 1, 1)))
			.SetDisplay("End Time", "End of analysis window", "Time");

		_startingDay = Param(nameof(StartingDay), DayOfWeek.Sunday)
			.SetDisplay("Starting Day", "First day of week", "Strategy");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Enable moving average filter", "Strategy");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Strategy");
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

		_prevClose1 = 0m;
		_prevClose2 = 0m;
		_prevHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dow = candle.OpenTime.DayOfWeek;
		var inWindow = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

		var isFirstDay = StartingDay == DayOfWeek.Sunday
			? dow == DayOfWeek.Sunday || dow == DayOfWeek.Monday
			: dow == DayOfWeek.Monday || dow == DayOfWeek.Tuesday;

		var longCondition = inWindow && isFirstDay && candle.ClosePrice < _prevClose1 && _prevClose1 < _prevClose2;

		if (UseMaFilter)
			longCondition &= candle.ClosePrice > maValue;

		var exitCondition = candle.ClosePrice > _prevHigh;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (exitCondition && Position > 0)
		{
			ClosePosition();
		}

		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
	}
}

