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
/// Turn around Tuesday strategy using RSI momentum with EMA trend filter.
/// </summary>
public class TurnAroundTuesdayOnSteroidsStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DayOfWeek> _startingDay;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	public DayOfWeek StartingDay { get => _startingDay.Value; set => _startingDay.Value = value; }
	public bool UseMaFilter { get => _useMaFilter.Value; set => _useMaFilter.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

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

		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = MaPeriod };
		var emaFast = new ExponentialMovingAverage { Length = 8 };
		var emaSlow = new ExponentialMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, emaFast, emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0 || _prevFast == 0 || _prevSlow == 0)
		{
			_prevRsi = rsiVal;
			_prevFast = emaFast;
			_prevSlow = emaSlow;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevRsi = rsiVal;
			_prevFast = emaFast;
			_prevSlow = emaSlow;
			return;
		}

		var hist = emaFast - emaSlow;
		var histUp = hist > 0m;
		var histDown = hist < 0m;

		var rsiCrossUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiCrossDown = _prevRsi >= 50m && rsiVal < 50m;

		// Exit
		if (Position > 0 && rsiCrossDown)
		{
			SellMarket();
			_cooldown = 80;
		}
		else if (Position < 0 && rsiCrossUp)
		{
			BuyMarket();
			_cooldown = 80;
		}

		// Entry
		if (Position == 0)
		{
			if (rsiCrossUp && histUp)
			{
				BuyMarket();
				_cooldown = 80;
			}
			else if (rsiCrossDown && histDown)
			{
				SellMarket();
				_cooldown = 80;
			}
		}

		_prevRsi = rsiVal;
		_prevFast = emaFast;
		_prevSlow = emaSlow;
	}
}
