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
/// Bollinger Breakout + Momentum Squeeze strategy.
/// </summary>
public class BbBreakoutMomentumSqueezeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _squeezeLength;
	private readonly StrategyParam<decimal> _squeezeBbMultiplier;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rrRatio;
	
	private BollingerBands _bbBreakout;
	private BollingerBands _squeezeBb;
	private KeltnerChannels _keltner;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _bullNum;
	private SimpleMovingAverage _bullDen;
	private SimpleMovingAverage _bearNum;
	private SimpleMovingAverage _bearDen;
	private SimpleMovingAverage _upperBandMa;
	private SimpleMovingAverage _lowerBandMa;
	
	private decimal? _prevBull;
	private decimal? _prevBear;
	private decimal _longEntry;
	private decimal _longSl;
	private decimal _longTp;
	private decimal _shortEntry;
	private decimal _shortSl;
	private decimal _shortTp;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BbBreakoutMomentumSqueezeStrategy"/>.
	/// </summary>
	public BbBreakoutMomentumSqueezeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_bbLength = Param(nameof(BbLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("BB Breakout Length", "Length for Bollinger breakout calculation", "BB Breakout")
		
		.SetOptimize(10, 30, 2);
		
		_bbMultiplier = Param(nameof(BbMultiplier), 1.0m)
		.SetDisplay("BB Breakout Mult", "Bollinger breakout multiplier", "BB Breakout");
		
		_threshold = Param(nameof(Threshold), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Threshold", "Middle line threshold", "BB Breakout");
		
		_squeezeLength = Param(nameof(SqueezeLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Squeeze Length", "Length for squeeze calculation", "Squeeze");
		
		_squeezeBbMultiplier = Param(nameof(SqueezeBbMultiplier), 2.0m)
		.SetDisplay("Bollinger Mult", "Bollinger Band std multiplier for squeeze", "Squeeze");
		
		_kcMultiplier = Param(nameof(KcMultiplier), 2.0m)
		.SetDisplay("Keltner Mult", "Keltner Channel multiplier", "Squeeze");
		
		_atrLength = Param(nameof(AtrLength), 30)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation length", "ATR");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.4m)
		.SetDisplay("ATR Multiplier", "ATR stop multiplier", "ATR");
		
		_rrRatio = Param(nameof(RrRatio), 1.5m)
		.SetDisplay("RR Ratio", "Risk reward ratio", "Risk");
	}
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Bollinger breakout length.
	/// </summary>
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	
	/// <summary>
	/// Bollinger breakout multiplier.
	/// </summary>
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }
	
	/// <summary>
	/// Midline threshold for bull/bear oscillator.
	/// </summary>
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	
	/// <summary>
	/// Squeeze calculation length.
	/// </summary>
	public int SqueezeLength { get => _squeezeLength.Value; set => _squeezeLength.Value = value; }
	
	/// <summary>
	/// Bollinger multiplier for squeeze detection.
	/// </summary>
	public decimal SqueezeBbMultiplier { get => _squeezeBbMultiplier.Value; set => _squeezeBbMultiplier.Value = value; }
	
	/// <summary>
	/// Keltner multiplier for squeeze detection.
	/// </summary>
	public decimal KcMultiplier { get => _kcMultiplier.Value; set => _kcMultiplier.Value = value; }
	
	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR stop multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RrRatio { get => _rrRatio.Value; set => _rrRatio.Value = value; }
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_prevBull = null;
		_prevBear = null;
		_longEntry = _longSl = _longTp = default;
		_shortEntry = _shortSl = _shortTp = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		
		_bbBreakout = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_squeezeBb = new BollingerBands { Length = SqueezeLength, Width = SqueezeBbMultiplier };
		_keltner = new KeltnerChannels { Length = SqueezeLength, Multiplier = KcMultiplier };
		_atr = new AverageTrueRange { Length = AtrLength };
		_bullNum = new SMA { Length = BbLength };
		_bullDen = new SMA { Length = BbLength };
		_bearNum = new SMA { Length = BbLength };
		_bearDen = new SMA { Length = BbLength };
		_upperBandMa = new SMA { Length = 3 };
		_lowerBandMa = new SMA { Length = 3 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([_bbBreakout, _squeezeBb, _keltner, _atr], ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (values[0] is not BollingerBandsValue bbBreakoutValue ||
			bbBreakoutValue.UpBand is not decimal breakoutUpper ||
			bbBreakoutValue.LowBand is not decimal breakoutLower)
			return;

		if (values[1] is not BollingerBandsValue squeezeBbValue ||
			squeezeBbValue.UpBand is not decimal squeezeUpper ||
			squeezeBbValue.LowBand is not decimal squeezeLower)
			return;

		if (values[2] is not KeltnerChannelsValue keltnerValue ||
			keltnerValue.Upper is not decimal kcUpper ||
			keltnerValue.Lower is not decimal kcLower)
			return;

		if (values[3].ToNullableDecimal() is not decimal atr)
			return;
		
		var close = candle.ClosePrice;
		var time = candle.ServerTime;

		var bullNumVal = _bullNum.Process(new DecimalIndicatorValue(_bullNum, Math.Max(close - breakoutUpper, 0m), time)).ToNullableDecimal();
		var bullDenVal = _bullDen.Process(new DecimalIndicatorValue(_bullDen, Math.Abs(close - breakoutUpper), time)).ToNullableDecimal();
		var bearNumVal = _bearNum.Process(new DecimalIndicatorValue(_bearNum, Math.Max(breakoutLower - close, 0m), time)).ToNullableDecimal();
		var bearDenVal = _bearDen.Process(new DecimalIndicatorValue(_bearDen, Math.Abs(breakoutLower - close), time)).ToNullableDecimal();
		
		if (bullNumVal is not decimal bullNum || bullDenVal is not decimal bullDen ||
		bearNumVal is not decimal bearNum || bearDenVal is not decimal bearDen)
		return;
		
		if (!_bullNum.IsFormed || !_bullDen.IsFormed || !_bearNum.IsFormed || !_bearDen.IsFormed)
		return;
		
		var bull = bullDen == 0 ? 0m : bullNum / bullDen * 100m;
		var bear = bearDen == 0 ? 0m : bearNum / bearDen * 100m;
		
		var bullCross = _prevBull.HasValue && _prevBull <= Threshold && bull > Threshold;
		var bearCross = _prevBear.HasValue && _prevBear <= Threshold && bear > Threshold;
		
		_prevBull = bull;
		_prevBear = bear;
		
		var squeezeDotGreen = squeezeLower < kcLower || squeezeUpper > kcUpper;
		
		var upperBandVal = _upperBandMa.Process(new DecimalIndicatorValue(_upperBandMa, close + atr * AtrMultiplier, time)).ToNullableDecimal();
		var lowerBandVal = _lowerBandMa.Process(new DecimalIndicatorValue(_lowerBandMa, close - atr * AtrMultiplier, time)).ToNullableDecimal();
		
		if (upperBandVal is not decimal upperBand || lowerBandVal is not decimal lowerBand)
		return;
		
		if (!_upperBandMa.IsFormed || !_lowerBandMa.IsFormed)
		return;
		
		if (bullCross && Position <= 0)
		{
			_longEntry = close;
			_longSl = lowerBand;
			_longTp = _longEntry + (_longEntry - _longSl) * RrRatio;
			BuyMarket();
		}
		
		if (bearCross && Position >= 0)
		{
			_shortEntry = close;
			_shortSl = upperBand;
			_shortTp = _shortEntry - (_shortSl - _shortEntry) * RrRatio;
			SellMarket();
		}
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _longSl || candle.HighPrice >= _longTp)
			SellMarket();
		}
		
		if (Position < 0)
		{
			if (candle.HighPrice >= _shortSl || candle.LowPrice <= _shortTp)
			BuyMarket();
		}
	}
}
