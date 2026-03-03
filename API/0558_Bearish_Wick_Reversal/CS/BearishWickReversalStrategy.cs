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
/// Bearish Wick Reversal strategy.
/// Enter long when a bearish candle shows a large lower wick.
/// Exit when price closes above previous candle high.
/// </summary>
public class BearishWickReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private ExponentialMovingAverage _ema;
	private decimal _previousHigh;
	private decimal _previousLow;
	private int _cooldown;

	/// <summary>
	/// Percentage threshold for lower wick (negative value).
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Use EMA filter.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading start time.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading end time.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BearishWickReversalStrategy"/>.
	/// </summary>
	public BearishWickReversalStrategy()
	{
		_threshold = Param(nameof(Threshold), -1.5m)
			.SetRange(-5m, 0m)
			.SetDisplay("Long Threshold", "Percentage threshold for lower wick", "Strategy Settings")
			;

		_useEmaFilter = Param(nameof(UseEmaFilter), false)
			.SetDisplay("Use EMA Filter", "Enable EMA trend filter", "Trend Filter");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA trend filter", "Trend Filter")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2014, 1, 1)))
			.SetDisplay("Start Time", "Trading window start", "Time Settings");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(2099, 1, 1)))
			.SetDisplay("End Time", "Trading window end", "Time Settings");
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
		_previousHigh = 0m;
		_previousLow = 0m;
		_cooldown = 0;
		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new EMA { Length = EmaPeriod };

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

		var time = candle.OpenTime;
		if (time < StartTime || time > EndTime)
		{
			_previousHigh = candle.HighPrice;
			return;
		}

		if (_cooldown > 0)
			_cooldown--;

		var longCondition = false;
		var shortCondition = false;

		// Bearish candle with large lower wick -> potential reversal up
		if (candle.ClosePrice < candle.OpenPrice)
		{
			var percentageSize = 100m * (candle.LowPrice - candle.ClosePrice) / candle.ClosePrice;
			longCondition = percentageSize <= Threshold;
		}

		// Bullish candle with large upper wick -> potential reversal down
		if (candle.ClosePrice > candle.OpenPrice)
		{
			var percentageSize = 100m * (candle.HighPrice - candle.ClosePrice) / candle.ClosePrice;
			shortCondition = percentageSize >= -Threshold;
		}

		if (longCondition && Position <= 0 && _cooldown == 0)
		{
			BuyMarket();
			_cooldown = 60;
		}
		else if (shortCondition && Position >= 0 && _cooldown == 0)
		{
			SellMarket();
			_cooldown = 60;
		}

		// Exit long when close above previous high
		if (Position > 0 && _previousHigh > 0 && candle.ClosePrice > _previousHigh)
		{
			SellMarket();
			_cooldown = 60;
		}
		// Exit short when close below previous low
		else if (Position < 0 && _previousLow > 0 && candle.ClosePrice < _previousLow)
		{
			BuyMarket();
			_cooldown = 60;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}
