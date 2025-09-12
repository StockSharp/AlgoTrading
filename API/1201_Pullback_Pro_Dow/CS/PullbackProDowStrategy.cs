namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pullback strategy based on Dow Theory pivots with optional ADX filter and two-step profit targets.
/// </summary>
public class PullbackProDowStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLookback;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _riskReward1;
	private readonly StrategyParam<int> _tp1Percent;
	private readonly StrategyParam<decimal> _riskReward2;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferIndex;
	private int _bufferCount;
	
	private decimal? _lastPivotHigh;
	private decimal? _prevPivotHigh;
	private decimal? _lastPivotLow;
	private decimal? _prevPivotLow;
	private int _trendDirection;
	
	private decimal _stopLossPrice;
	private decimal _takeProfit1;
	private decimal _takeProfit2;
	private bool _tp1Hit;
	
	private decimal _prevLow;
	private decimal _prevHigh;
	private decimal _prevEma;
	
	/// <summary>
	/// Pivot lookback period.
	/// </summary>
	public int PivotLookback { get => _pivotLookback.Value; set => _pivotLookback.Value = value; }
	
	/// <summary>
	/// EMA length for pullbacks.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	
	/// <summary>
	/// First take profit risk-reward ratio.
	/// </summary>
	public decimal RiskReward1 { get => _riskReward1.Value; set => _riskReward1.Value = value; }
	
	/// <summary>
	/// Take profit 1 percent of position.
	/// </summary>
	public int Tp1Percent { get => _tp1Percent.Value; set => _tp1Percent.Value = value; }
	
	/// <summary>
	/// Second take profit risk-reward ratio.
	/// </summary>
	public decimal RiskReward2 { get => _riskReward2.Value; set => _riskReward2.Value = value; }
	
	/// <summary>
	/// Use ADX filter.
	/// </summary>
	public bool UseAdxFilter { get => _useAdxFilter.Value; set => _useAdxFilter.Value = value; }
	
	/// <summary>
	/// ADX length.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	
	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PullbackProDowStrategy"/> class.
	/// </summary>
	public PullbackProDowStrategy()
	{
		_pivotLookback = Param(nameof(PivotLookback), 10)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Lookback", "Bars on each side for pivot detection", "Dow Theory")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);
		
		_emaLength = Param(nameof(EmaLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Pullback EMA length", "Entry")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_riskReward1 = Param(nameof(RiskReward1), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP1 R:R", "Risk-reward for first target", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2m, 0.25m);
		
		_tp1Percent = Param(nameof(Tp1Percent), 50)
		.SetGreaterThanZero()
		.SetDisplay("TP1 %", "Portion closed at first target", "Risk");
		
		_riskReward2 = Param(nameof(RiskReward2), 3m)
		.SetGreaterThanZero()
		.SetDisplay("TP2 R:R", "Risk-reward for second target", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 4m, 0.5m);
		
		_useAdxFilter = Param(nameof(UseAdxFilter), true)
		.SetDisplay("Use ADX Filter", "Enable trend strength filter", "Filters");
		
		_adxLength = Param(nameof(AdxLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Length", "Period for ADX", "Filters");
		
		_adxThreshold = Param(nameof(AdxThreshold), 25m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Threshold", "Minimum ADX for entries", "Filters");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		
		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferIndex = 0;
		_bufferCount = 0;
		_lastPivotHigh = _prevPivotHigh = null;
		_lastPivotLow = _prevPivotLow = null;
		_trendDirection = 0;
		_stopLossPrice = _takeProfit1 = _takeProfit2 = 0m;
		_tp1Hit = false;
		_prevLow = _prevHigh = _prevEma = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var size = PivotLookback * 2 + 1;
		_highBuffer = new decimal[size];
		_lowBuffer = new decimal[size];
		_bufferIndex = 0;
		_bufferCount = 0;
		
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(ema, adx, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var size = _highBuffer.Length;
		
		_highBuffer[_bufferIndex] = candle.HighPrice;
		_lowBuffer[_bufferIndex] = candle.LowPrice;
		_bufferIndex = (_bufferIndex + 1) % size;
		
		if (_bufferCount < size)
		{
			_bufferCount++;
			_prevLow = candle.LowPrice;
			_prevHigh = candle.HighPrice;
			_prevEma = emaValue.ToDecimal();
			return;
		}
		
		var centerIndex = (_bufferIndex + size - PivotLookback - 1) % size;
		var centerHigh = _highBuffer[centerIndex];
		var centerLow = _lowBuffer[centerIndex];
		
		var isPivotHigh = true;
		var isPivotLow = true;
		
		for (var i = 0; i < size; i++)
		{
			if (i == centerIndex)
			continue;
			
			if (isPivotHigh && _highBuffer[i] >= centerHigh)
			isPivotHigh = false;
			if (isPivotLow && _lowBuffer[i] <= centerLow)
			isPivotLow = false;
			
			if (!isPivotHigh && !isPivotLow)
			break;
		}
		
		if (isPivotHigh)
		{
			_prevPivotHigh = _lastPivotHigh;
			_lastPivotHigh = centerHigh;
		}
		
		if (isPivotLow)
		{
			_prevPivotLow = _lastPivotLow;
			_lastPivotLow = centerLow;
		}
		
		if (_lastPivotHigh is decimal lph && _prevPivotHigh is decimal pph &&
		_lastPivotLow is decimal lpl && _prevPivotLow is decimal ppl)
		{
			var isUptrend = lph > pph && lpl > ppl;
			var isDowntrend = lph < pph && lpl < ppl;
			_trendDirection = isUptrend ? 1 : isDowntrend ? -1 : 0;
		}
		
		var ema = emaValue.ToDecimal();
		var adxStrength = ((AverageDirectionalIndexValue)adxValue).MovingAverage;
		var buyPullback = _trendDirection == 1 && _prevLow > _prevEma && candle.LowPrice <= ema;
		var sellRally = _trendDirection == -1 && _prevHigh < _prevEma && candle.HighPrice >= ema;
		var adxOk = !UseAdxFilter || adxStrength > AdxThreshold;
		var goLong = buyPullback && adxOk;
		var goShort = sellRally && adxOk;
		
		if (Position == 0)
		{
			_tp1Hit = false;
			if (goLong && _lastPivotLow is decimal sl && candle.ClosePrice > sl)
			{
				var riskSize = candle.ClosePrice - sl;
				if (riskSize > 0)
				{
					_stopLossPrice = sl;
					_takeProfit1 = candle.ClosePrice + riskSize * RiskReward1;
					_takeProfit2 = candle.ClosePrice + riskSize * RiskReward2;
					BuyMarket();
				}
			}
			else if (goShort && _lastPivotHigh is decimal sh && candle.ClosePrice < sh)
			{
				var riskSize = sh - candle.ClosePrice;
				if (riskSize > 0)
				{
					_stopLossPrice = sh;
					_takeProfit1 = candle.ClosePrice - riskSize * RiskReward1;
					_takeProfit2 = candle.ClosePrice - riskSize * RiskReward2;
					SellMarket();
				}
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopLossPrice)
			{
				SellMarket(Position);
				_tp1Hit = false;
			}
			else
			{
				if (!_tp1Hit && candle.HighPrice >= _takeProfit1)
				{
					var qty = Position * Tp1Percent / 100m;
					SellMarket(qty);
					_tp1Hit = true;
				}
				if (_tp1Hit && candle.HighPrice >= _takeProfit2)
				{
					SellMarket(Position);
					_tp1Hit = false;
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLossPrice)
			{
				BuyMarket(-Position);
				_tp1Hit = false;
			}
			else
			{
				if (!_tp1Hit && candle.LowPrice <= _takeProfit1)
				{
					var qty = -Position * Tp1Percent / 100m;
					BuyMarket(qty);
					_tp1Hit = true;
				}
				if (_tp1Hit && candle.LowPrice <= _takeProfit2)
				{
					BuyMarket(-Position);
					_tp1Hit = false;
				}
			}
		}
		
		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;
		_prevEma = ema;
	}
}
