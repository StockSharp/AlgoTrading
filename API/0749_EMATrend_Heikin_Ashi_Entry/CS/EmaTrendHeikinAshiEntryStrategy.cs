
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy using Bollinger Bands on Heikin Ashi with higher timeframe EMA trend.
/// </summary>
public class EmaTrendHeikinAshiEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe;
	
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
	private decimal _haOpen3;
	private decimal _haClose3;
	private decimal _haHigh3;
	private decimal _haLow3;
	private decimal _upperBb1;
	private decimal _lowerBb1;
	private decimal _upperBb2;
	private decimal _lowerBb2;
	private decimal _upperBb3;
	private decimal _lowerBb3;
	private decimal _prevHigh;
	private decimal _prevLow;
	
	private bool _isBullishHtf;
	private bool _isBearishHtf;
	
	public EmaTrendHeikinAshiEntryStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 9)
		.SetDisplay("Fast EMA", "Fast EMA length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
		.SetDisplay("Slow EMA", "Slow EMA length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_higherTimeframe = Param(nameof(HigherTimeframe), TimeSpan.FromMinutes(180).TimeFrame())
		.SetDisplay("Higher Timeframe", "Timeframe for trend detection", "Trend");
	}
	
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
	
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}
	
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public DataType HigherTimeframe
	{
		get => _higherTimeframe.Value;
		set => _higherTimeframe.Value = value;
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe)];
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
		_haOpen3 = default;
		_haClose3 = default;
		_haHigh3 = default;
		_haLow3 = default;
		_upperBb1 = default;
		_lowerBb1 = default;
		_upperBb2 = default;
		_lowerBb2 = default;
		_upperBb3 = default;
		_lowerBb3 = default;
		_prevHigh = default;
		_prevLow = default;
		_isBullishHtf = default;
		_isBearishHtf = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};
		
		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		
		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(bb, ProcessCandle).Start();
		
		SubscribeCandles(HigherTimeframe)
		.Bind(fastEma, slowEma, ProcessHigherTimeframe)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessHigherTimeframe(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_isBullishHtf = fast > slow;
		_isBearishHtf = fast < slow;
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
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
		var red3 = _haClose3 < _haOpen3 && (_haLow3 <= _lowerBb3 || _haClose3 <= _lowerBb3);
		
		var green1 = _haClose1 > _haOpen1 && (_haHigh1 >= _upperBb1 || _haClose1 >= _upperBb1);
		var green2 = _haClose2 > _haOpen2 && (_haHigh2 >= _upperBb2 || _haClose2 >= _upperBb2);
		var green3 = _haClose3 > _haOpen3 && (_haHigh3 >= _upperBb3 || _haClose3 >= _upperBb3);
		
		var consecutiveBears = (red1 && red2) || (red1 && red2 && red3);
		var consecutiveBulls = (green1 && green2) || (green1 && green2 && green3);
		
		var buySignal = _isBullishHtf && consecutiveBears && haClose > haOpen && haClose > lower;
		var sellSignal = _isBearishHtf && consecutiveBulls && haClose < haOpen && haClose < upper;
		
		if (buySignal && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_initialStop = _prevLow;
			_firstTarget = _entryPrice + (_entryPrice - _initialStop);
			_firstTargetReached = false;
			_trailStop = default;
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_initialStop = _prevHigh;
			_firstTarget = _entryPrice - (_initialStop - _entryPrice);
			_firstTargetReached = false;
			_trailStop = default;
			SellMarket();
		}
		
		if (Position > 0)
		{
			if (!_firstTargetReached)
			{
				if (candle.HighPrice >= _firstTarget)
				{
					ClosePosition(Position / 2m);
					_firstTargetReached = true;
					_trailStop = _entryPrice;
				}
			}
			else
			_trailStop = Math.Max(_trailStop, _prevLow);
			
			var currentStop = _firstTargetReached ? _trailStop : _initialStop;
			if (candle.LowPrice <= currentStop)
			ClosePosition();
		}
		else if (Position < 0)
		{
			if (!_firstTargetReached)
			{
				if (candle.LowPrice <= _firstTarget)
				{
					ClosePosition(Math.Abs(Position) / 2m);
					_firstTargetReached = true;
					_trailStop = _entryPrice;
				}
			}
			else
			_trailStop = Math.Min(_trailStop, _prevHigh);
			
			var currentStop = _firstTargetReached ? _trailStop : _initialStop;
			if (candle.HighPrice >= currentStop)
			ClosePosition();
		}
		
		_haOpen3 = _haOpen2;
		_haClose3 = _haClose2;
		_haHigh3 = _haHigh2;
		_haLow3 = _haLow2;
		_upperBb3 = _upperBb2;
		_lowerBb3 = _lowerBb2;
		
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
