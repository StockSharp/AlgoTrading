using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku strategy with Stochastic oscillator and ATR trailing stop.
/// </summary>
public class KumoTradeIchimokuStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	
	private decimal _prevStochD;
	private decimal _prevHigh;
	private decimal _prevKijun;
	private decimal? _trailStopLong;
	private decimal? _trailStopShort;
	private decimal _highestClose;
	private decimal _lowestLow;
	
	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}
	
	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}
	
	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouPeriod
	{
		get => _senkouPeriod.Value;
		set => _senkouPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}
	
	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="KumoTradeIchimokuStrategy"/>.
	/// </summary>
	public KumoTradeIchimokuStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
		.SetDisplay("Tenkan-sen Period", "Period for Tenkan line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(7, 13, 2);
		
		_kijunPeriod = Param(nameof(KijunPeriod), 26)
		.SetDisplay("Kijun-sen Period", "Period for Kijun line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(20, 30, 2);
		
		_senkouPeriod = Param(nameof(SenkouPeriod), 52)
		.SetDisplay("Senkou Span B Period", "Period for Senkou B", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(40, 60, 4);
		
		_stochK = Param(nameof(StochK), 70)
		.SetDisplay("Stochastic %K", "Period for %K line", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(50, 90, 5);
		
		_stochD = Param(nameof(StochD), 15)
		.SetDisplay("Stochastic %D", "Smoothing for %D line", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(10, 25, 5);
		
		_atrPeriod = Param(nameof(AtrPeriod), 5)
		.SetDisplay("ATR Period", "Period for ATR stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2024, 5, 1, 22, 0, 0), TimeSpan.Zero))
		.SetDisplay("Start Time", "Trading window start", "Time");
		
		_endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(2025, 1, 1, 19, 30, 0), TimeSpan.Zero))
		.SetDisplay("End Time", "Trading window end", "Time");
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
		_prevStochD = 0m;
		_prevHigh = 0m;
		_prevKijun = 0m;
		_trailStopLong = null;
		_trailStopShort = null;
		_highestClose = 0m;
		_lowestLow = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouPeriod }
		};
		
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};
		
		var atr = new AverageTrueRange { Length = AtrPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(ichimoku, stochastic, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue stochValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (candle.OpenTime < StartTime || candle.OpenTime > EndTime)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var ichimokuTyped = (IchimokuValue)ichimokuValue;
		if (ichimokuTyped.Tenkan is not decimal tenkan)
		return;
		if (ichimokuTyped.Kijun is not decimal kijun)
		return;
		if (ichimokuTyped.SenkouA is not decimal senkouA)
		return;
		if (ichimokuTyped.SenkouB is not decimal senkouB)
		return;
		
		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.D is not decimal stochD)
		return;
		
		if (!atrValue.IsFinal)
		return;
		var atr = atrValue.GetValue<decimal>();
		
		var upperCloud = Math.Max(senkouA, senkouB);
		var lowerCloud = Math.Min(senkouA, senkouB);
		var noKumo = candle.HighPrice < (lowerCloud - atr / 2m) || candle.LowPrice > (upperCloud + atr);
		var kumoRed = senkouB > senkouA;
		
		var longCond = Position <= 0 &&
		candle.LowPrice > kijun &&
		kijun > tenkan &&
		candle.ClosePrice < senkouA &&
		candle.ClosePrice > candle.OpenPrice &&
		noKumo &&
		stochD < 29m;
		
		var crossedAboveKijun = candle.HighPrice > kijun && _prevHigh <= _prevKijun;
		var shortCond = Position >= 0 &&
		candle.ClosePrice < lowerCloud &&
		crossedAboveKijun &&
		stochD >= 90m &&
		_prevStochD > stochD &&
		kumoRed;
		
		if (longCond)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailStopLong = null;
			_highestClose = candle.ClosePrice;
		}
		else if (shortCond)
		{
			SellMarket(Volume + Math.Abs(Position));
			_trailStopShort = null;
			_lowestLow = candle.LowPrice;
		}
		
		if (Position > 0)
		{
			_highestClose = Math.Max(_highestClose, candle.ClosePrice);
			var temp = _highestClose - atr * 3m;
			if (_trailStopLong == null || temp > _trailStopLong)
			_trailStopLong = temp;
			if (_trailStopLong != null && candle.ClosePrice < _trailStopLong)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			_lowestLow = Math.Min(_lowestLow, candle.LowPrice);
			var temp = _lowestLow + atr * 3m;
			if (_trailStopShort == null || temp < _trailStopShort)
			_trailStopShort = temp;
			if (_trailStopShort != null && candle.ClosePrice > _trailStopShort)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevStochD = stochD;
		_prevHigh = candle.HighPrice;
		_prevKijun = kijun;
	}
}
