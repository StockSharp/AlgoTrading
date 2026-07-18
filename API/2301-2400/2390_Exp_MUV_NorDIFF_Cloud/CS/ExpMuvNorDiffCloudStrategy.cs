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
/// Strategy based on normalized momentum of SMA and EMA.
/// Opens long when SMA or EMA momentum reaches +100, short when -100.
/// </summary>
public class ExpMuvNorDiffCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _smaHigh;
	private Lowest _smaLow;
	private Highest _emaHigh;
	private Lowest _emaLow;
	private decimal _prevSma = decimal.MinValue;
	private decimal _prevEma = decimal.MinValue;

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
	        
	        .SetOptimize(7, 28, 7);

	    _momentumPeriod = Param(nameof(MomentumPeriod), 1)
	        .SetDisplay("Momentum", "Momentum period", "Parameters")
	        .SetGreaterThanZero()
	        
	        .SetOptimize(1, 5, 1);

	    _kPeriod = Param(nameof(KPeriod), 14)
	        .SetDisplay("K Period", "Extremum period", "Parameters")
	        .SetGreaterThanZero()
	        
	        .SetOptimize(7, 28, 7);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");
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

	    _smaHigh = null;
	    _smaLow = null;
	    _emaHigh = null;
	    _emaLow = null;
	    _prevSma = decimal.MinValue;
	    _prevEma = decimal.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
	    base.OnStarted2(time);

	    _smaHigh = new Highest { Length = KPeriod };
	    _smaLow = new Lowest { Length = KPeriod };
	    _emaHigh = new Highest { Length = KPeriod };
	    _emaLow = new Lowest { Length = KPeriod };
	    _prevSma = decimal.MinValue;
	    _prevEma = decimal.MinValue;

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

	    var t = candle.OpenTime;

	    if (_prevSma == decimal.MinValue || _prevEma == decimal.MinValue)
	    {
	        _prevSma = smaValue;
	        _prevEma = emaValue;
	        return;
	    }

	    var smaMom = smaValue - _prevSma;
	    var emaMom = emaValue - _prevEma;

	    var smaMaxVal = _smaHigh.Process(smaMom, t, true);
	    var smaMinVal = _smaLow.Process(smaMom, t, true);
	    var emaMaxVal = _emaHigh.Process(emaMom, t, true);
	    var emaMinVal = _emaLow.Process(emaMom, t, true);

	    if (!_smaHigh.IsFormed || !_smaLow.IsFormed || !_emaHigh.IsFormed || !_emaLow.IsFormed)
	        return;

	    var smaNorm = Normalize(smaMom, smaMaxVal.ToDecimal(), smaMinVal.ToDecimal());
	    var emaNorm = Normalize(emaMom, emaMaxVal.ToDecimal(), emaMinVal.ToDecimal());

	    if (smaNorm == 100m || emaNorm == 100m)
	    {
	        if (Position <= 0)
	            BuyMarket();
	    }
	    else if (smaNorm == -100m || emaNorm == -100m)
	    {
	        if (Position >= 0)
	            SellMarket();
	    }

	    _prevSma = smaValue;
	    _prevEma = emaValue;
	}

	private static decimal Normalize(decimal value, decimal max, decimal min)
	{
	    var range = max - min;
	    return range > 0m ? 100m - 200m * (max - value) / range : 100m;
	}
}
