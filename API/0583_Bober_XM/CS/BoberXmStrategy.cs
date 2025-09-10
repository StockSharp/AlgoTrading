using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bober XM dual channel strategy.
/// Uses Keltner channel breakouts with WMA, OBV and ADX confirmations.
/// </summary>
public class BoberXmStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _obvMaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal KeltnerMultiplier { get => _keltnerMultiplier.Value; set => _keltnerMultiplier.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }
	public int ObvMaPeriod { get => _obvMaPeriod.Value; set => _obvMaPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="BoberXmStrategy"/>.
	/// </summary>
	public BoberXmStrategy()
	{
	_emaPeriod = Param(nameof(EmaPeriod), 20)
	.SetGreaterThanZero()
	.SetDisplay("EMA Period", "EMA period for Keltner base", "Keltner");

	_atrPeriod = Param(nameof(AtrPeriod), 10)
	.SetGreaterThanZero()
	.SetDisplay("ATR Period", "ATR period for Keltner width", "Keltner");

	_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1.5m)
	.SetGreaterThanZero()
	.SetDisplay("Keltner Multiplier", "ATR multiplier for bands", "Keltner");

	_wmaPeriod = Param(nameof(WmaPeriod), 15)
	.SetGreaterThanZero()
	.SetDisplay("WMA Period", "Period for entry confirmation", "Filters");

	_obvMaPeriod = Param(nameof(ObvMaPeriod), 22)
	.SetGreaterThanZero()
	.SetDisplay("OBV MA Period", "Period for OBV moving average", "Exit");

	_adxPeriod = Param(nameof(AdxPeriod), 60)
	.SetGreaterThanZero()
	.SetDisplay("ADX Period", "Period for ADX calculation", "Filters");

	_adxThreshold = Param(nameof(AdxThreshold), 35m)
	.SetGreaterThanZero()
	.SetDisplay("ADX Threshold", "Minimum ADX value for signals", "Filters");

	_stopLossPercent = Param(nameof(StopLossPercent), 2m)
	.SetNotNegative()
	.SetDisplay("Stop Loss %", "Percent stop-loss", "Risk Management");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
	}


	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	    return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var ema = new ExponentialMovingAverage { Length = EmaPeriod };
	    var atr = new AverageTrueRange { Length = AtrPeriod };
	    var wma = new WeightedMovingAverage { Length = WmaPeriod };
	    var obv = new OnBalanceVolume();
	    var obvMa = new SimpleMovingAverage { Length = ObvMaPeriod };
	    var adx = new AverageDirectionalIndex { Length = AdxPeriod };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .BindEx(ema, atr, wma, obv, obvMa, adx, ProcessCandle)
	        .Start();

	    StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawIndicator(area, ema);
	        DrawIndicator(area, wma);
	        DrawIndicator(area, obv);
	        DrawIndicator(area, obvMa);
	        DrawIndicator(area, adx);
	        DrawOwnTrades(area);
	    }
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue wmaValue, IIndicatorValue obvValue, IIndicatorValue obvMaValue, IIndicatorValue adxValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    var ema = emaValue.ToDecimal();
	    var atr = atrValue.ToDecimal();
	    var wma = wmaValue.ToDecimal();
	    var obv = obvValue.ToDecimal();
	    var obvMa = obvMaValue.ToDecimal();

	    var adxTyped = (AverageDirectionalIndexValue)adxValue;
	    if (adxTyped.MovingAverage is not decimal adx)
	        return;

	    var upperBand = ema + KeltnerMultiplier * atr;
	    var lowerBand = ema - KeltnerMultiplier * atr;
	    var price = candle.ClosePrice;

	    if (price > upperBand && price > wma && adx > AdxThreshold && Position <= 0)
	    {
	        BuyMarket(Volume + Math.Abs(Position));
	    }
	    else if (price < lowerBand && price < wma && adx > AdxThreshold && Position >= 0)
	    {
	        SellMarket(Volume + Math.Abs(Position));
	    }
	    else
	    {
	        if (Position > 0 && obv < obvMa && adx > AdxThreshold)
	            SellMarket(Position);
	        else if (Position < 0 && obv > obvMa && adx > AdxThreshold)
	            BuyMarket(Math.Abs(Position));
	    }
	}
}
