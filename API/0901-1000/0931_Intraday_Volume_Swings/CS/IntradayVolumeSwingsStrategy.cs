using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday volume swings strategy.
/// A swing high forms when three consecutive candles make higher highs on rising volume, a swing low when three make lower lows
/// on rising volume. While the run continues the swing region spans the high and low of its candles. Once it ends, the most
/// extreme region of the day is kept, and the previous day's regions stay active as well. Price pushing up into a high swing
/// region (from the current or previous day) goes long, price pushing down into a low swing region goes short, reversing an
/// opposite position. With RegionMustClose the candle has to close inside the region, otherwise touching it is enough.
/// </summary>
public class IntradayVolumeSwingsStrategy : Strategy
{
	private readonly StrategyParam<bool> _regionMustClose;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private int _barCount;
	private decimal _prevClose;
	private decimal _high1;
	private decimal _high2;
	private decimal _low1;
	private decimal _low2;
	private decimal _volume1;
	private bool _lowBar1;
	private bool _lowBar2;
	private bool _highBar1;
	private bool _highBar2;
	private bool _prevSwingLow;
	private bool _prevSwingHigh;
	private decimal? _runLowTop;
	private decimal? _runLowBottom;
	private decimal? _runHighTop;
	private decimal? _runHighBottom;
	private decimal? _dayLowTop;
	private decimal? _dayLowBottom;
	private decimal? _dayHighTop;
	private decimal? _dayHighBottom;
	private decimal? _prevDayLowTop;
	private decimal? _prevDayLowBottom;
	private decimal? _prevDayHighTop;
	private decimal? _prevDayHighBottom;

	/// <summary>
	/// Require the candle to close inside the region to trigger.
	/// </summary>
	public bool RegionMustClose
	{
		get => _regionMustClose.Value;
		set => _regionMustClose.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IntradayVolumeSwingsStrategy()
	{
		_regionMustClose = Param(nameof(RegionMustClose), true)
			.SetDisplay("Region Must Close", "Candle must close inside the region to trigger", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_currentDay = default;
		_barCount = 0;
		_prevClose = _high1 = _high2 = _low1 = _low2 = _volume1 = 0m;
		_lowBar1 = _lowBar2 = _highBar1 = _highBar2 = false;
		_prevSwingLow = _prevSwingHigh = false;
		_runLowTop = _runLowBottom = _runHighTop = _runHighBottom = null;
		_dayLowTop = _dayLowBottom = _dayHighTop = _dayHighBottom = null;
		_prevDayLowTop = _prevDayLowBottom = _prevDayHighTop = _prevDayHighBottom = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			if (_currentDay != default)
			{
				_prevDayLowTop = _dayLowTop;
				_prevDayLowBottom = _dayLowBottom;
				_prevDayHighTop = _dayHighTop;
				_prevDayHighBottom = _dayHighBottom;
			}

			_currentDay = day;
			_dayLowTop = _dayLowBottom = _dayHighTop = _dayHighBottom = null;
		}

		var hasHistory = _barCount > 0;
		var risingVolume = hasHistory && candle.TotalVolume > _volume1;
		var lowBar = risingVolume && candle.LowPrice < _low1;
		var highBar = risingVolume && candle.HighPrice > _high1;

		var swingLow = lowBar && _lowBar1 && _lowBar2;
		var swingHigh = highBar && _highBar1 && _highBar2;

		var top3 = Math.Max(candle.HighPrice, Math.Max(_high1, _high2));
		var bottom3 = Math.Min(candle.LowPrice, Math.Min(_low1, _low2));

		if (swingLow)
		{
			_runLowTop = _prevSwingLow && _runLowTop is decimal t ? Math.Max(t, candle.HighPrice) : top3;
			_runLowBottom = _prevSwingLow && _runLowBottom is decimal b ? Math.Min(b, candle.LowPrice) : bottom3;
		}
		else if (_prevSwingLow && _runLowTop is decimal lowTop && _runLowBottom is decimal lowBottom)
		{
			// Keep the lowest swing low region of the day.
			if (_dayLowBottom is not decimal dayBottom || lowBottom < dayBottom)
			{
				_dayLowTop = lowTop;
				_dayLowBottom = lowBottom;
			}

			_runLowTop = _runLowBottom = null;
		}

		if (swingHigh)
		{
			_runHighTop = _prevSwingHigh && _runHighTop is decimal t ? Math.Max(t, candle.HighPrice) : top3;
			_runHighBottom = _prevSwingHigh && _runHighBottom is decimal b ? Math.Min(b, candle.LowPrice) : bottom3;
		}
		else if (_prevSwingHigh && _runHighTop is decimal highTop && _runHighBottom is decimal highBottom)
		{
			// Keep the highest swing high region of the day.
			if (_dayHighTop is not decimal dayTop || highTop > dayTop)
			{
				_dayHighTop = highTop;
				_dayHighBottom = highBottom;
			}

			_runHighTop = _runHighBottom = null;
		}

		if (hasHistory && IsFormedAndOnlineAndAllowTrading())
		{
			var goLong = EntersHighRegion(candle, _dayHighBottom) || EntersHighRegion(candle, _prevDayHighBottom);
			var goShort = EntersLowRegion(candle, _dayLowTop) || EntersLowRegion(candle, _prevDayLowTop);

			if (goLong && !goShort && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (goShort && !goLong && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_barCount++;
		_prevClose = candle.ClosePrice;
		_volume1 = candle.TotalVolume;
		_high2 = _high1;
		_high1 = candle.HighPrice;
		_low2 = _low1;
		_low1 = candle.LowPrice;
		_lowBar2 = _lowBar1;
		_lowBar1 = lowBar;
		_highBar2 = _highBar1;
		_highBar1 = highBar;
		_prevSwingLow = swingLow;
		_prevSwingHigh = swingHigh;
	}

	// Price comes from below the region bottom and reaches it.
	private bool EntersHighRegion(ICandleMessage candle, decimal? bottom)
	{
		if (bottom is not decimal level || _prevClose >= level)
			return false;

		return RegionMustClose ? candle.ClosePrice >= level : candle.HighPrice >= level;
	}

	// Price comes from above the region top and reaches it.
	private bool EntersLowRegion(ICandleMessage candle, decimal? top)
	{
		if (top is not decimal level || _prevClose <= level)
			return false;

		return RegionMustClose ? candle.ClosePrice <= level : candle.LowPrice <= level;
	}
}
