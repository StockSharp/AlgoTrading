using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;



/// <summary>
/// Strategy using Bollinger Bands with Heikin Ashi entries.
/// </summary>
public class BollingerHeikinAshiEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _initialStop;
	private decimal _firstTarget;
	private bool _firstTargetReached;
	private decimal _trailStop;

	private bool _isHaInitialized;
	private decimal _haOpen1;
	private decimal _haClose1;
	private decimal _haHigh1;
	private decimal _haLow1;
	private decimal _haOpen2;
	private decimal _haClose2;
	private decimal _haHigh2;
	private decimal _haLow2;
	private decimal _upperBb1;
	private decimal _lowerBb1;
	private decimal _upperBb2;
	private decimal _lowerBb2;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _cooldown;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerHeikinAshiEntryStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators")
			
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.5m)
			.SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation", "Indicators")
			
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_entryPrice = default;
		_initialStop = default;
		_firstTarget = default;
		_firstTargetReached = default;
		_trailStop = default;
		_isHaInitialized = default;
		_haOpen1 = default;
		_haClose1 = default;
		_haHigh1 = default;
		_haLow1 = default;
		_haOpen2 = default;
		_haClose2 = default;
		_haHigh2 = default;
		_haLow2 = default;
		_upperBb1 = default;
		_lowerBb1 = default;
		_upperBb2 = default;
		_lowerBb2 = default;
		_prevHigh = default;
		_prevLow = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (bbValue is not BollingerBandsValue bb ||
			bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal haOpen;

		if (!_isHaInitialized)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_isHaInitialized = true;
		}
		else
			haOpen = (_haOpen1 + _haClose1) / 2m;

		var haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
		var haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);

		var red1 = _haClose1 < _haOpen1 && (_haLow1 <= _lowerBb1 || _haClose1 <= _lowerBb1);
		var red2 = _haClose2 < _haOpen2 && (_haLow2 <= _lowerBb2 || _haClose2 <= _lowerBb2);
		var green1 = _haClose1 > _haOpen1 && (_haHigh1 >= _upperBb1 || _haClose1 >= _upperBb1);
		var green2 = _haClose2 > _haOpen2 && (_haHigh2 >= _upperBb2 || _haClose2 >= _upperBb2);

		var buySignal = red1 && haClose > haOpen && haClose > lower;
		var sellSignal = green1 && haClose < haOpen && haClose < upper;

		if (_cooldown > 0)
			_cooldown--;

		if (buySignal && Position <= 0 && _cooldown == 0)
		{
			_entryPrice = candle.ClosePrice;
			_initialStop = _prevLow > 0 ? _prevLow : candle.LowPrice;
			_firstTarget = _entryPrice + (_entryPrice - _initialStop);
			_firstTargetReached = false;
			_trailStop = default;
			_cooldown = 20;
			BuyMarket();
		}
		else if (sellSignal && Position >= 0 && _cooldown == 0)
		{
			_entryPrice = candle.ClosePrice;
			_initialStop = _prevHigh > 0 ? _prevHigh : candle.HighPrice;
			_firstTarget = _entryPrice - (_initialStop - _entryPrice);
			_firstTargetReached = false;
			_trailStop = default;
			_cooldown = 20;
			SellMarket();
		}

		if (Position > 0)
		{
			if (candle.HighPrice >= _firstTarget)
			{
				_firstTargetReached = true;
				_trailStop = Math.Max(_entryPrice, _prevLow);
			}

			if (_firstTargetReached)
				_trailStop = Math.Max(_trailStop, _prevLow);

			var currentStop = _firstTargetReached ? _trailStop : _initialStop;
			if (currentStop > 0 && candle.LowPrice <= currentStop)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _firstTarget)
			{
				_firstTargetReached = true;
				_trailStop = Math.Min(_entryPrice, _prevHigh);
			}

			if (_firstTargetReached && _trailStop > 0)
				_trailStop = Math.Min(_trailStop, _prevHigh);

			var currentStop = _firstTargetReached ? _trailStop : _initialStop;
			if (currentStop > 0 && candle.HighPrice >= currentStop)
				BuyMarket();
		}

		_haOpen2 = _haOpen1;
		_haClose2 = _haClose1;
		_haHigh2 = _haHigh1;
		_haLow2 = _haLow1;
		_upperBb2 = _upperBb1;
		_lowerBb2 = _lowerBb1;

		_haOpen1 = haOpen;
		_haClose1 = haClose;
		_haHigh1 = haHigh;
		_haLow1 = haLow;
		_upperBb1 = upper;
		_lowerBb1 = lower;

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
