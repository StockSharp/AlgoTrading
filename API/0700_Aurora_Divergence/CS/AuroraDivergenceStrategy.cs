using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aurora divergence strategy based on price and OBV slopes.
/// </summary>
public class AuroraDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<bool> _useZFilter;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _htfCandleType;
	private readonly StrategyParam<int> _htfMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _stopAtrMult;
	private readonly StrategyParam<decimal> _profitAtrMult;
	private readonly StrategyParam<int> _maxBarsInTrade;
	private readonly StrategyParam<int> _cooldownBars;
	
	private OnBalanceVolume _obv;
	private LinearRegression _priceSlope;
	private LinearRegression _obvSlope;
	private SimpleMovingAverage _zMean;
	private StandardDeviation _zStd;
	private SimpleMovingAverage _htfMa;
	private AverageTrueRange _atr;
	
	private decimal? _htfMaValue;
	private bool _prevBullDiv;
	private bool _prevBearDiv;
	private int _cooldownCounter;
	private decimal? _entryPrice;
	private int _barsInTrade;
	
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}
	
	public int ZLength
	{
		get => _zLength.Value;
		set => _zLength.Value = value;
	}
	
	public decimal ZThreshold
	{
		get => _zThreshold.Value;
		set => _zThreshold.Value = value;
	}
	
	public bool UseZFilter
	{
		get => _useZFilter.Value;
		set => _useZFilter.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public DataType HtfCandleType
	{
		get => _htfCandleType.Value;
		set => _htfCandleType.Value = value;
	}
	
	public int HtfMaLength
	{
		get => _htfMaLength.Value;
		set => _htfMaLength.Value = value;
	}
	
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}
	
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMult.Value;
		set => _stopAtrMult.Value = value;
	}
	
	public decimal ProfitAtrMultiplier
	{
		get => _profitAtrMult.Value;
		set => _profitAtrMult.Value = value;
	}
	
	public int MaxBarsInTrade
	{
		get => _maxBarsInTrade.Value;
		set => _maxBarsInTrade.Value = value;
	}
	
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}
	
	public AuroraDivergenceStrategy()
	{
		_lookback = Param(nameof(Lookback), 9)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Period for slope calculation", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_zLength = Param(nameof(ZLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Z-Length", "Z-score lookback", "Z-Score Filter")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);
		
		_zThreshold = Param(nameof(ZThreshold), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Z Threshold", "Maximum absolute z-score", "Z-Score Filter")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_useZFilter = Param(nameof(UseZFilter), true)
		.SetDisplay("Use Z Filter", "Enable z-score filter", "Z-Score Filter");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Main candle timeframe", "General");
		
		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("HTF Candle Type", "Higher timeframe for trend", "General");
		
		_htfMaLength = Param(nameof(HtfMaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("HTF MA Length", "Moving average length on higher timeframe", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);
		
		_atrThreshold = Param(nameof(AtrThreshold), 1m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Threshold", "Minimum ATR to trade", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);
		
		_stopAtrMult = Param(nameof(StopAtrMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop ATR Mult", "ATR multiplier for stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);
		
		_profitAtrMult = Param(nameof(ProfitAtrMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Profit ATR Mult", "ATR multiplier for target", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_maxBarsInTrade = Param(nameof(MaxBarsInTrade), 8)
		.SetGreaterThanZero()
		.SetDisplay("Max Bars In Trade", "Maximum bars to hold position", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);
		
		_cooldownBars = Param(nameof(CooldownBars), 2)
		.SetGreaterThanZero()
		.SetDisplay("Cooldown Bars", "Bars to wait after trade", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HtfCandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_htfMaValue = null;
		_prevBullDiv = false;
		_prevBearDiv = false;
		_cooldownCounter = 0;
		_entryPrice = null;
		_barsInTrade = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_obv = new OnBalanceVolume();
		_priceSlope = new LinearRegression { Length = Lookback };
		_obvSlope = new LinearRegression { Length = Lookback };
		_zMean = new SimpleMovingAverage { Length = ZLength };
		_zStd = new StandardDeviation { Length = ZLength };
		_htfMa = new SimpleMovingAverage { Length = HtfMaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_obv, _zMean, _zStd, _atr, ProcessCandle)
		.Start();
		
		var htfSubscription = SubscribeCandles(HtfCandleType);
		htfSubscription
		.Bind(_htfMa, ProcessHtfCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessHtfCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_htfMaValue = maValue;
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal obvValue, decimal meanValue, decimal stdValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (_cooldownCounter > 0)
		_cooldownCounter--;
		
		var priceSlopeTyped = (LinearRegressionValue)_priceSlope.Process(candle.ClosePrice, candle.ServerTime, true);
		if (!priceSlopeTyped.IsFinal || priceSlopeTyped.LinearReg is not decimal priceSlope)
		return;
		
		var obvSlopeTyped = (LinearRegressionValue)_obvSlope.Process(obvValue, candle.ServerTime, true);
		if (!obvSlopeTyped.IsFinal || obvSlopeTyped.LinearReg is not decimal obvSlope)
		return;
		
		var zScore = stdValue != 0m ? (candle.ClosePrice - meanValue) / stdValue : 0m;
		var zOk = !UseZFilter || Math.Abs(zScore) < ZThreshold;
		var atrOk = atrValue > AtrThreshold;
		
		var trendUp = _htfMaValue is decimal maUp && candle.ClosePrice > maUp;
		var trendDown = _htfMaValue is decimal maDn && candle.ClosePrice < maDn;
		
		var bullDiv = priceSlope < 0 && obvSlope > 0 && trendUp;
		var bearDiv = priceSlope > 0 && obvSlope < 0 && trendDown;
		
		var bullDiv2 = bullDiv && _prevBullDiv;
		var bearDiv2 = bearDiv && _prevBearDiv;
		
		_prevBullDiv = bullDiv;
		_prevBearDiv = bearDiv;
		
		var canSignal = _cooldownCounter == 0 && atrOk && zOk;
		
		if (canSignal)
		{
			if (bullDiv2 && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_barsInTrade = 0;
				_cooldownCounter = CooldownBars;
				return;
			}
			if (bearDiv2 && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_barsInTrade = 0;
				_cooldownCounter = CooldownBars;
				return;
			}
		}
		
		if (Position != 0 && _entryPrice is decimal entry)
		{
			_barsInTrade++;
			
			var stopPrice = Position > 0
			? entry - StopAtrMultiplier * atrValue
			: entry + StopAtrMultiplier * atrValue;
			
			var profitPrice = Position > 0
			? entry + ProfitAtrMultiplier * atrValue
			: entry - ProfitAtrMultiplier * atrValue;
			
			var exit =
			_barsInTrade >= MaxBarsInTrade ||
			!atrOk || !zOk ||
			(Position > 0 && (candle.ClosePrice <= stopPrice || candle.ClosePrice >= profitPrice)) ||
			(Position < 0 && (candle.ClosePrice >= stopPrice || candle.ClosePrice <= profitPrice));
			
			if (exit)
			{
				if (Position > 0)
				SellMarket(Position);
				else
				BuyMarket(-Position);
				
				_entryPrice = null;
				_barsInTrade = 0;
			}
		}
	}
}
