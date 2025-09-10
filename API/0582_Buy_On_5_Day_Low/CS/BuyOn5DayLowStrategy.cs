using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys when the close drops below the previous N-period low and exits when the close rises above the previous high.
/// </summary>
public class BuyOn5DayLowStrategy : Strategy
{
	private readonly StrategyParam<int> _lowestPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private Lowest _lowest;
	private decimal _prevLowest;
	private decimal _prevHigh;

	public int LowestPeriod { get => _lowestPeriod.Value; set => _lowestPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	public BuyOn5DayLowStrategy()
	{
		_lowestPeriod = Param(nameof(LowestPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Period", "Period for lowest low calculation", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of trading window", "Time Settings");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading window", "Time Settings");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_lowest = default;
		_prevLowest = 0m;
		_prevHigh = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lowest = new Lowest { Length = LowestPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_lowest.IsFormed)
		{
			_prevLowest = lowestValue;
			_prevHigh = candle.HighPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevLowest = lowestValue;
			_prevHigh = candle.HighPrice;
			return;
		}

		var withinWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		var buyCondition = withinWindow && candle.ClosePrice < _prevLowest;
		var exitCondition = candle.ClosePrice > _prevHigh;

		if (Position <= 0 && buyCondition)
			RegisterBuy();
		else if (Position > 0 && exitCondition)
			RegisterSell(Math.Abs(Position));

		_prevLowest = lowestValue;
		_prevHigh = candle.HighPrice;
	}
}
