using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday volume swing based breakout strategy.
/// Buys when price pushes into swing high regions and sells on swing low
/// regions.
/// </summary>
public class IntradayVolumeSwingsStrategy : Strategy {
	private readonly StrategyParam<bool> _regionMustClose;
	private readonly StrategyParam<DataType> _candleType;

	public bool RegionMustClose {
		get => _regionMustClose.Value;
		set => _regionMustClose.Value = value;
	}

	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public IntradayVolumeSwingsStrategy() {
		_regionMustClose =
			Param(nameof(RegionMustClose), true)
				.SetDisplay("Region Must Close", "Close in region to trigger",
							"General");

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
		base.OnReseted();

		_currentDay = default;
		_prevOpen = _prevClose = _high1 = _high2 = _low1 = _low2 = _volume1 =
			0m;
		_lowBar1 = _lowBar2 = _highBar1 = _highBar2 = false;
		_prevSwingLow = _prevSwingHigh = false;
		_currentSwingLowTop = _currentSwingLowBottom = _currentSwingHighTop =
			_currentSwingHighBottom = null;
		_dailySwingLowTop = _dailySwingLowBottom = _dailySwingHighTop =
			_dailySwingHighBottom = null;
		_prevDaySwingLowTop = _prevDaySwingLowBottom = _prevDaySwingHighTop =
			_prevDaySwingHighBottom = null;
	}

	private DateTime _currentDay;
	private decimal _prevOpen;
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
	private decimal? _currentSwingLowTop;
	private decimal? _currentSwingLowBottom;
	private decimal? _currentSwingHighTop;
	private decimal? _currentSwingHighBottom;
	private decimal? _dailySwingLowTop;
	private decimal? _dailySwingLowBottom;
	private decimal? _dailySwingHighTop;
	private decimal? _dailySwingHighBottom;
	private decimal? _prevDaySwingLowTop;
	private decimal? _prevDaySwingLowBottom;
	private decimal? _prevDaySwingHighTop;
	private decimal? _prevDaySwingHighBottom;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle) {
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.UtcDateTime.Date;
		var isNewDay = _currentDay != day;
		if (isNewDay) {
			_currentDay = day;
			_prevDaySwingLowTop = _dailySwingLowTop;
			_prevDaySwingLowBottom = _dailySwingLowBottom;
			_prevDaySwingHighTop = _dailySwingHighTop;
			_prevDaySwingHighBottom = _dailySwingHighBottom;
			_dailySwingLowTop = _dailySwingLowBottom = null;
			_dailySwingHighTop = _dailySwingHighBottom = null;
		}

		var increasingVolume = candle.TotalVolume > _volume1;
		var lowerLow = candle.LowPrice < _low1;
		var higherHigh = candle.HighPrice > _high1;

		var lowBar = increasingVolume && lowerLow;
		var highBar = increasingVolume && higherHigh;

		var swingLow = lowBar && _lowBar1 && _lowBar2;
		var swingHigh = highBar && _highBar1 && _highBar2;

		var hh3 = Math.Max(candle.HighPrice, Math.Max(_high1, _high2));
		var ll3 = Math.Min(candle.LowPrice, Math.Min(_low1, _low2));

		if (swingLow && !_prevSwingLow) {
			_currentSwingLowTop = hh3;
			_currentSwingLowBottom = ll3;
		} else if (swingLow && _prevSwingLow) {
			_currentSwingLowTop =
				Math.Max(_currentSwingLowTop ?? hh3, candle.HighPrice);
			_currentSwingLowBottom =
				Math.Min(_currentSwingLowBottom ?? ll3, candle.LowPrice);
		}

		if (swingHigh && !_prevSwingHigh) {
			_currentSwingHighTop = hh3;
			_currentSwingHighBottom = ll3;
		} else if (swingHigh && _prevSwingHigh) {
			_currentSwingHighTop =
				Math.Max(_currentSwingHighTop ?? hh3, candle.HighPrice);
			_currentSwingHighBottom =
				Math.Min(_currentSwingHighBottom ?? ll3, candle.LowPrice);
		}

		if (_prevSwingLow && !swingLow && _currentSwingLowBottom.HasValue) {
			if (!_dailySwingLowBottom.HasValue ||
				_currentSwingLowBottom < _dailySwingLowBottom) {
				_dailySwingLowTop = _currentSwingLowTop;
				_dailySwingLowBottom = _currentSwingLowBottom;
			}
			_currentSwingLowTop = _currentSwingLowBottom = null;
		}

		if (_prevSwingHigh && !swingHigh && _currentSwingHighTop.HasValue) {
			if (!_dailySwingHighTop.HasValue ||
				_currentSwingHighTop > _dailySwingHighTop) {
				_dailySwingHighTop = _currentSwingHighTop;
				_dailySwingHighBottom = _currentSwingHighBottom;
			}
			_currentSwingHighTop = _currentSwingHighBottom = null;
		}

		if (IsFormedAndOnlineAndAllowTrading()) {
			CheckRegions(candle);
		}

		_volume1 = candle.TotalVolume;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
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

	private void CheckRegions(ICandleMessage candle) {
		if (_prevDaySwingHighBottom.HasValue) {
			var level = _prevDaySwingHighBottom.Value;
			if (RegionMustClose) {
				if (_prevOpen < level && _prevClose >= level && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			} else {
				if (candle.OpenPrice < level && candle.HighPrice >= level &&
					Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
		}

		if (_prevDaySwingLowTop.HasValue) {
			var level = _prevDaySwingLowTop.Value;
			if (RegionMustClose) {
				if (_prevOpen > level && _prevClose <= level && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			} else {
				if (candle.OpenPrice > level && candle.LowPrice <= level &&
					Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (_dailySwingHighBottom.HasValue) {
			var level = _dailySwingHighBottom.Value;
			if (RegionMustClose) {
				if (_prevOpen < level && _prevClose >= level && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			} else {
				if (candle.OpenPrice < level && candle.HighPrice >= level &&
					Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
		}

		if (_dailySwingLowTop.HasValue) {
			var level = _dailySwingLowTop.Value;
			if (RegionMustClose) {
				if (_prevOpen > level && _prevClose <= level && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			} else {
				if (candle.OpenPrice > level && candle.LowPrice <= level &&
					Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}
