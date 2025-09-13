using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// New York opening range breakout with moving average stop options.
/// </summary>
public class NyOpeningRangeBreakoutMaStopStrategy : Strategy
{
public enum TakeProfitOptions
	{
		FixedRiskReward,
		MovingAverage,
		Both
	}

	public enum MovingAverageTypes
	{
		SMA,
		EMA,
		WMA,
		VWMA
	}

private readonly StrategyParam<int> _cutoffHour;
private readonly StrategyParam<int> _cutoffMinute;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<TakeProfitOptions> _takeProfitType;
	private readonly StrategyParam<decimal> _tpRatio;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeSet;
	private bool _longBreakout;
	private bool _shortBreakout;
	private DateTime _currentDay;
	private decimal _stopPrice;
	private decimal? _takePrice;
	private ICandleMessage _prevCandle;
	private readonly TimeZoneInfo _nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	/// <summary>
	/// Entry cutoff hour (NY time).
	/// </summary>
	public int CutoffHour
	{
		get => _cutoffHour.Value;
		set => _cutoffHour.Value = value;
	}

	/// <summary>
	/// Entry cutoff minute (NY time).
	/// </summary>
	public int CutoffMinute
	{
		get => _cutoffMinute.Value;
		set => _cutoffMinute.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
public Sides? Direction
{
	get => _direction.Value;
	set => _direction.Value = value;
}

	/// <summary>
	/// Take profit calculation mode.
	/// </summary>
	public TakeProfitOptions TakeProfitType
	{
		get => _takeProfitType.Value;
		set => _takeProfitType.Value = value;
	}

	/// <summary>
	/// Risk reward ratio for fixed take profit.
	/// </summary>
	public decimal TpRatio
	{
		get => _tpRatio.Value;
		set => _tpRatio.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public NyOpeningRangeBreakoutMaStopStrategy()
	{
		_cutoffHour = Param(nameof(CutoffHour), 12)
			.SetRange(0, 23)
			.SetDisplay("Cutoff Hour", "Entry cutoff hour", "General");

		_cutoffMinute = Param(nameof(CutoffMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Cutoff Minute", "Entry cutoff minute", "General");
	 _direction = Param(nameof(Direction), Sides.Buy)
	        .SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_takeProfitType = Param(nameof(TakeProfitType), TakeProfitOptions.FixedRiskReward)
			.SetDisplay("Take Profit Type", "Take profit mode", "Risk");

		_tpRatio = Param(nameof(TpRatio), 2.5m)
			.SetRange(1m, 5m)
			.SetDisplay("TP Ratio", "Risk reward ratio", "Risk");

		_maType = Param(nameof(MaType), MovingAverageTypes.SMA)
			.SetDisplay("MA Type", "Moving average type", "General");

		_maLength = Param(nameof(MaLength), 100)
			.SetRange(5, 200)
			.SetDisplay("MA Length", "Moving average length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_rangeHigh = null;
		_rangeLow = null;
		_rangeSet = false;
		_longBreakout = false;
		_shortBreakout = false;
		_currentDay = default;
		_stopPrice = 0m;
		_takePrice = null;
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var ma = MaType switch
		{
			MovingAverageTypes.EMA => new ExponentialMovingAverage { Length = MaLength },
			MovingAverageTypes.WMA => new WeightedMovingAverage { Length = MaLength },
			MovingAverageTypes.VWMA => new VolumeWeightedMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength }
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyTimeZone);

		if (_currentDay != nyTime.Date)
		{
			_currentDay = nyTime.Date;
			_rangeHigh = null;
			_rangeLow = null;
			_rangeSet = false;
			_longBreakout = false;
			_shortBreakout = false;
		}

		if (nyTime.Hour == 9 && nyTime.Minute >= 30 && nyTime.Minute < 45)
		{
			_rangeHigh = _rangeHigh.HasValue ? Math.Max(_rangeHigh.Value, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow.HasValue ? Math.Min(_rangeLow.Value, candle.LowPrice) : candle.LowPrice;
		}
		else if (!_rangeSet && _rangeHigh.HasValue && _rangeLow.HasValue && nyTime.Hour * 60 + nyTime.Minute >= 9 * 60 + 45)
		{
			_rangeSet = true;
		}

		var pastCutoffCurrent = PastCutoff(nyTime);
		var pastCutoffPrev = _prevCandle is not null && PastCutoff(TimeZoneInfo.ConvertTime(_prevCandle.OpenTime.UtcDateTime, _nyTimeZone));
	 var prevLongBreakout = false;
	var prevShortBreakout = false;
	var allowLong = Direction is null || Direction == Sides.Buy;
	var allowShort = Direction is null || Direction == Sides.Sell;
	 if (_rangeSet && _prevCandle is not null)
	{
	        if (!_longBreakout && !pastCutoffPrev && allowLong && _prevCandle.ClosePrice > _rangeHigh)
	                prevLongBreakout = true;
	         if (!_shortBreakout && !pastCutoffPrev && allowShort && _prevCandle.ClosePrice < _rangeLow)
	                prevShortBreakout = true;
	}

		if (prevLongBreakout)
			_longBreakout = true;

		if (prevShortBreakout)
			_shortBreakout = true;

		var longMaOk = TakeProfitType is TakeProfitOptions.MovingAverage or TakeProfitOptions.Both ? candle.ClosePrice > maValue : true;
		var shortMaOk = TakeProfitType is TakeProfitOptions.MovingAverage or TakeProfitOptions.Both ? candle.ClosePrice < maValue : true;
	 var longEntry = _rangeSet && prevLongBreakout && !pastCutoffCurrent && Position == 0 && longMaOk;
	var shortEntry = _rangeSet && prevShortBreakout && !pastCutoffCurrent && Position == 0 && shortMaOk;

		if (longEntry)
		{
			var entryPrice = candle.ClosePrice;
			var risk = entryPrice - _rangeLow.Value;
			_stopPrice = _rangeLow.Value;
			_takePrice = TakeProfitType != TakeProfitOptions.MovingAverage ? entryPrice + risk * TpRatio : null;
			BuyMarket();
		}
		else if (shortEntry)
		{
			var entryPrice = candle.ClosePrice;
			var risk = _rangeHigh.Value - entryPrice;
			_stopPrice = _rangeHigh.Value;
			_takePrice = TakeProfitType != TakeProfitOptions.MovingAverage ? entryPrice - risk * TpRatio : null;
			SellMarket();
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position.Abs());
				ResetPosition();
			}
			else if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Position.Abs());
				ResetPosition();
			}
			else if ((TakeProfitType == TakeProfitOptions.MovingAverage || TakeProfitType == TakeProfitOptions.Both) && candle.ClosePrice < maValue)
			{
				SellMarket(Position.Abs());
				ResetPosition();
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Position.Abs());
				ResetPosition();
			}
			else if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Position.Abs());
				ResetPosition();
			}
			else if ((TakeProfitType == TakeProfitOptions.MovingAverage || TakeProfitType == TakeProfitOptions.Both) && candle.ClosePrice > maValue)
			{
				BuyMarket(Position.Abs());
				ResetPosition();
			}
		}

		_prevCandle = candle;
	}

	private bool PastCutoff(DateTime time)
	{
		var cutoffMinutes = CutoffHour * 60 + CutoffMinute;
		var currentMinutes = time.Hour * 60 + time.Minute;
		return currentMinutes >= cutoffMinutes;
	}

	private void ResetPosition()
	{
		_stopPrice = 0m;
		_takePrice = null;
	}
}
