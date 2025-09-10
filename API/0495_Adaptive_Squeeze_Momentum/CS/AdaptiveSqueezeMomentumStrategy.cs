using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Squeeze Momentum strategy.
/// Detects squeeze release with Bollinger Bands and Keltner Channels
/// and confirms breakout using momentum and optional RSI/EMA filters.
/// Includes ATR-based exits and time-based holding period.
/// </summary>
public class AdaptiveSqueezeMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<bool> _useAtrStops;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _minVolatility;
	private readonly StrategyParam<decimal> _holdingPeriodMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _momentumMultiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevRsi;
	private bool _squeezeOffPrev;
	private decimal _stopPrice;
	private decimal _profitTarget;
	private int _barsHeld;
	
	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}
	
	/// <summary>
	/// Keltner Channels period.
	/// </summary>
	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}
	
	/// <summary>
	/// Keltner Channels multiplier.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}
	
	/// <summary>
	/// Momentum calculation length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}
	
	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}
	
	/// <summary>
	/// Use ATR-based stops.
	/// </summary>
	public bool UseAtrStops
	{
		get => _useAtrStops.Value;
		set => _useAtrStops.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for take-profit.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	/// <summary>
	/// Minimum volatility in percent.
	/// </summary>
	public decimal MinVolatility
	{
		get => _minVolatility.Value;
		set => _minVolatility.Value = value;
	}
	
	/// <summary>
	/// Holding period multiplier.
	/// </summary>
	public decimal HoldingPeriodMultiplier
	{
		get => _holdingPeriodMultiplier.Value;
		set => _holdingPeriodMultiplier.Value = value;
	}
	
	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}
	
	/// <summary>
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	
	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}
	
	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}
	
	/// <summary>
	/// Momentum multiplier for threshold.
	/// </summary>
	public decimal MomentumMultiplier
	{
		get => _momentumMultiplier.Value;
		set => _momentumMultiplier.Value = value;
	}
	
	/// <summary>
	/// Allow long positions.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}
	
	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptiveSqueezeMomentumStrategy"/>.
	/// </summary>
	public AdaptiveSqueezeMomentumStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Periods for Bollinger Bands", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);
		
		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2.0m)
		.SetNotNegative()
		.SetDisplay("Bollinger Multiplier", "Deviation multiplier for Bollinger Bands", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 3.0m, 0.5m);
		
		_keltnerPeriod = Param(nameof(KeltnerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Keltner Period", "EMA period for Keltner Channels", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);
		
		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Keltner Multiplier", "ATR multiplier for Keltner Channels", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 3.0m, 0.5m);
		
		_momentumLength = Param(nameof(MomentumLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Periods for momentum calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);
		
		_trendMaLength = Param(nameof(TrendMaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Trend EMA Length", "EMA period for trend filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);
		
		_useAtrStops = Param(nameof(UseAtrStops), true)
		.SetDisplay("Use ATR Stops", "Use ATR-based stop-loss and take-profit", "Risk");
		
		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Stop Mult", "ATR multiplier for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 3.0m, 0.5m);
		
		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Take Mult", "ATR multiplier for take-profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 4.0m, 0.5m);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "Period for ATR", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);
		
		_minVolatility = Param(nameof(MinVolatility), 0.5m)
		.SetNotNegative()
		.SetDisplay("Min Volatility %", "Minimum ATR percent to trade", "Risk");
		
		_holdingPeriodMultiplier = Param(nameof(HoldingPeriodMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Holding Mult", "Multiplier for holding period", "Risk");
		
		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use Trend Filter", "Filter entries by EMA trend", "Filters");
		
		_useRsiFilter = Param(nameof(UseRsiFilter), true)
		.SetDisplay("Use RSI Filter", "Filter entries by RSI crosses", "Filters");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "Period for RSI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);
		
		_rsiOversold = Param(nameof(RsiOversold), 40m)
		.SetNotNegative()
		.SetDisplay("RSI Oversold", "Oversold level", "Indicators");
		
		_rsiOverbought = Param(nameof(RsiOverbought), 60m)
		.SetNotNegative()
		.SetDisplay("RSI Overbought", "Overbought level", "Indicators");
		
		_momentumMultiplier = Param(nameof(MomentumMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Multiplier", "Multiplier for momentum threshold", "Indicators");
		
		_allowLong = Param(nameof(AllowLong), true)
		.SetDisplay("Allow Long", "Enable long trades", "General");
		
		_allowShort = Param(nameof(AllowShort), true)
		.SetDisplay("Allow Short", "Enable short trades", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};
		
		var keltner = new KeltnerChannels
		{
			Length = KeltnerPeriod,
			Multiplier = KeltnerMultiplier
		};
		
		var momentum = new Momentum
		{
			Length = MomentumLength
		};
		
		var momentumStd = new StandardDeviation
		{
			Length = MomentumLength
		};
		
		var rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};
		
		var trendEma = new ExponentialMovingAverage
		{
			Length = TrendMaLength
		};
		
		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.BindEx(bollinger, keltner, momentum, momentumStd, rsi, trendEma, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, keltner);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue keltnerValue, IIndicatorValue momentumValue, IIndicatorValue stdValue, IIndicatorValue rsiValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!bollingerValue.IsFinal || !keltnerValue.IsFinal || !momentumValue.IsFinal || !stdValue.IsFinal || !rsiValue.IsFinal || !emaValue.IsFinal || !atrValue.IsFinal)
		return;
		
		var bb = (BollingerBandsValue)bollingerValue;
		var kc = (KeltnerChannelsValue)keltnerValue;
		
		if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower || kc.Upper is not decimal kcUpper || kc.Lower is not decimal kcLower)
		return;
		
		var momentum = momentumValue.GetValue<decimal>();
		var stdDev = stdValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var trend = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		
		var squeezeOff = bbLower < kcLower && bbUpper > kcUpper;
		var dynamicThreshold = stdDev * MomentumMultiplier;
		var strongPositive = momentum > dynamicThreshold;
		var strongNegative = momentum < -dynamicThreshold;
		
		var rsiBuy = _prevRsi <= RsiOversold && rsi > RsiOversold;
		var rsiSell = _prevRsi >= RsiOverbought && rsi < RsiOverbought;
		_prevRsi = rsi;
		
		var bullishTrend = candle.ClosePrice > trend;
		var bearishTrend = candle.ClosePrice < trend;
		
		var atrPercent = (atr / candle.ClosePrice) * 100m;
		var sufficientVol = atrPercent > MinVolatility;
		
		bool buySignal = _squeezeOffPrev && strongPositive && sufficientVol && (!UseTrendFilter || bullishTrend) && (!UseRsiFilter || rsiBuy);
		bool sellSignal = _squeezeOffPrev && strongNegative && sufficientVol && (!UseTrendFilter || bearishTrend) && (!UseRsiFilter || rsiSell);
		_squeezeOffPrev = squeezeOff;
		
		if (AllowLong && buySignal && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			
			if (UseAtrStops)
			{
				_stopPrice = candle.ClosePrice - atr * AtrMultiplierSl;
				_profitTarget = candle.ClosePrice + atr * AtrMultiplierTp;
			}
			
			_barsHeld = 0;
		}
		else if (AllowShort && sellSignal && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			
			if (UseAtrStops)
			{
				_stopPrice = candle.ClosePrice + atr * AtrMultiplierSl;
				_profitTarget = candle.ClosePrice - atr * AtrMultiplierTp;
			}
			
			_barsHeld = 0;
		}
		else
		{
			if (UseAtrStops && Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.ClosePrice <= _stopPrice || candle.HighPrice >= _profitTarget)
				{
					SellMarket(Math.Abs(Position));
				}
			}
			else if (UseAtrStops && Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.ClosePrice >= _stopPrice || candle.LowPrice <= _profitTarget)
				{
					BuyMarket(Math.Abs(Position));
				}
			}
		}
		
		if (Position != 0)
		{
			_barsHeld++;
			var maxBars = (int)Math.Round(HoldingPeriodMultiplier * MomentumLength, MidpointRounding.AwayFromZero);
			if (_barsHeld >= maxBars)
			{
				if (Position > 0)
				SellMarket(Math.Abs(Position));
				else
				BuyMarket(Math.Abs(Position));
				
				_barsHeld = 0;
			}
		}
	}
}
