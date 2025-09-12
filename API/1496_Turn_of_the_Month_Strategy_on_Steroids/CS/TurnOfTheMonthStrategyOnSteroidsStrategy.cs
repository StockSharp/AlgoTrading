using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Turn of the Month Strategy on Steroids.
/// </summary>
public class TurnOfTheMonthStrategyOnSteroidsStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<int> _dayOfMonth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose1;
	private decimal _prevClose2;

	/// <summary>
	/// Start date for analysis window.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date for analysis window.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Minimum day of month to allow entries.
	/// </summary>
	public int DayOfMonth { get => _dayOfMonth.Value; set => _dayOfMonth.Value = value; }

	/// <summary>
	/// RSI indicator length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI exit threshold.
	/// </summary>
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TurnOfTheMonthStrategyOnSteroidsStrategy"/>.
	/// </summary>
	public TurnOfTheMonthStrategyOnSteroidsStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2014, 1, 1)))
			.SetDisplay("Start Time", "Start of analysis window", "Time");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2099, 1, 1)))
			.SetDisplay("End Time", "End of analysis window", "Time");

		_dayOfMonth = Param(nameof(DayOfMonth), 25)
			.SetDisplay("Day Of Month", "Minimum day for entries", "Strategy");

		_rsiLength = Param(nameof(RsiLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI indicator length", "Strategy");

		_rsiThreshold = Param(nameof(RsiThreshold), 65m)
			.SetDisplay("RSI Exit", "RSI exit threshold", "Strategy");

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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dom = candle.OpenTime.Day;
		var inWindow = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

		var longCondition = inWindow && dom >= DayOfMonth && candle.ClosePrice < _prevClose1 && _prevClose1 < _prevClose2;
		var exitCondition = rsi > RsiThreshold;

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
	}
}

