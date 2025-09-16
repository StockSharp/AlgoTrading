using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on normalized momentum of SMA and EMA.
/// Opens long when SMA or EMA momentum reaches +100, short when -100.
/// </summary>
public class ExpMuvNorDiffCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Momentum _smaMomentum;
	private readonly Momentum _emaMomentum;
	private readonly Highest _smaHigh;
	private readonly Lowest _smaLow;
	private readonly Highest _emaHigh;
	private readonly Lowest _emaLow;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
	    get => _maPeriod.Value;
	    set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod
	{
	    get => _momentumPeriod.Value;
	    set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Extremum search period.
	/// </summary>
	public int KPeriod
	{
	    get => _kPeriod.Value;
	    set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ExpMuvNorDiffCloudStrategy"/>.
	/// </summary>
	public ExpMuvNorDiffCloudStrategy()
	{
	    _maPeriod = Param(nameof(MaPeriod), 14)
	        .SetDisplay("MA Period", "Moving average period", "Parameters")
	        .SetGreaterThanZero()
	        .SetCanOptimize(true)
	        .SetOptimize(7, 28, 7);

	    _momentumPeriod = Param(nameof(MomentumPeriod), 1)
	        .SetDisplay("Momentum", "Momentum period", "Parameters")
	        .SetGreaterThanZero()
	        .SetCanOptimize(true)
	        .SetOptimize(1, 5, 1);

	    _kPeriod = Param(nameof(KPeriod), 14)
	        .SetDisplay("K Period", "Extremum period", "Parameters")
	        .SetGreaterThanZero()
	        .SetCanOptimize(true)
	        .SetOptimize(7, 28, 7);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");

	    _smaMomentum = new Momentum { Length = 1 };
	    _emaMomentum = new Momentum { Length = 1 };
	    _smaHigh = new Highest();
	    _smaLow = new Lowest();
	    _emaHigh = new Highest();
	    _emaLow = new Lowest();
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

	    // configure indicator lengths
	    _smaMomentum.Length = MomentumPeriod;
	    _emaMomentum.Length = MomentumPeriod;
	    _smaHigh.Length = KPeriod;
	    _smaLow.Length = KPeriod;
	    _emaHigh.Length = KPeriod;
	    _emaLow.Length = KPeriod;

	    var sma = new SMA { Length = MaPeriod };
	    var ema = new EMA { Length = MaPeriod };

	    var sub = SubscribeCandles(CandleType);
	    sub.Bind(sma, ema, ProcessCandle).Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, sub);
	        DrawIndicator(area, sma);
	        DrawIndicator(area, ema);
	        DrawOwnTrades(area);
	    }
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal emaValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    var smaMomVal = _smaMomentum.Process(new DecimalIndicatorValue(_smaMomentum, smaValue, candle.OpenTime));
	    var emaMomVal = _emaMomentum.Process(new DecimalIndicatorValue(_emaMomentum, emaValue, candle.OpenTime));

	    if (!smaMomVal.IsFinal || !emaMomVal.IsFinal)
	        return;

	    var smaMom = smaMomVal.ToDecimal();
	    var emaMom = emaMomVal.ToDecimal();

	    var smaMaxVal = _smaHigh.Process(new DecimalIndicatorValue(_smaHigh, smaMom, candle.OpenTime));
	    var smaMinVal = _smaLow.Process(new DecimalIndicatorValue(_smaLow, smaMom, candle.OpenTime));
	    var emaMaxVal = _emaHigh.Process(new DecimalIndicatorValue(_emaHigh, emaMom, candle.OpenTime));
	    var emaMinVal = _emaLow.Process(new DecimalIndicatorValue(_emaLow, emaMom, candle.OpenTime));

	    if (!smaMaxVal.IsFinal || !smaMinVal.IsFinal || !emaMaxVal.IsFinal || !emaMinVal.IsFinal)
	        return;

	    var smaNorm = Normalize(smaMom, smaMaxVal.ToDecimal(), smaMinVal.ToDecimal());
	    var emaNorm = Normalize(emaMom, emaMaxVal.ToDecimal(), emaMinVal.ToDecimal());

	    if (smaNorm == 100m || emaNorm == 100m)
	    {
	        if (Position <= 0)
	            BuyMarket(Volume + Math.Abs(Position));
	    }
	    else if (smaNorm == -100m || emaNorm == -100m)
	    {
	        if (Position >= 0)
	            SellMarket(Volume + Math.Abs(Position));
	    }
	}

	private static decimal Normalize(decimal value, decimal max, decimal min)
	{
	    var range = max - min;
	    return range > 0m ? 100m - 200m * (max - value) / range : 100m;
	}
}
