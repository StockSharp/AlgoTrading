using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that buys during extreme drawdowns based on standard deviation.
/// Enters long when drawdown falls below the mean by a threshold of standard deviations.
/// Exits the position after a fixed number of bars.
/// </summary>
public class BloodInTheStreetsStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<decimal> _stdDevThreshold;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private StandardDeviation _drawdownStdDev;
	private SimpleMovingAverage _drawdownSma;

	private int _barIndex;
	private int _entryBarIndex = -1;

	/// <summary>
	/// Lookback period for peak high calculation.
	/// </summary>
	public int LookbackPeriod
	{
	    get => _lookbackPeriod.Value;
	    set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Period for standard deviation and mean calculation.
	/// </summary>
	public int StdDevLength
	{
	    get => _stdDevLength.Value;
	    set => _stdDevLength.Value = value;
	}

	/// <summary>
	/// Threshold multiplier for standard deviation.
	/// </summary>
	public decimal StdDevThreshold
	{
	    get => _stdDevThreshold.Value;
	    set => _stdDevThreshold.Value = value;
	}

	/// <summary>
	/// Number of bars after which to exit the trade.
	/// </summary>
	public int ExitBars
	{
	    get => _exitBars.Value;
	    set => _exitBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BloodInTheStreetsStrategy"/>.
	/// </summary>
	public BloodInTheStreetsStrategy()
	{
	    _lookbackPeriod = Param(nameof(LookbackPeriod), 50)
	        .SetGreaterThanZero()
	        .SetDisplay("Lookback Period", "Bars for peak high", "General")
	        .SetCanOptimize(true)
	        .SetOptimize(20, 100, 10);

	    _stdDevLength = Param(nameof(StdDevLength), 50)
	        .SetGreaterThanZero()
	        .SetDisplay("Std Dev Length", "Bars for statistics", "General")
	        .SetCanOptimize(true)
	        .SetOptimize(20, 100, 10);

	    _stdDevThreshold = Param(nameof(StdDevThreshold), -1m)
	        .SetDisplay("Std Dev Threshold", "Multiplier for drawdown deviation", "General")
	        .SetCanOptimize(true)
	        .SetOptimize(-2m, 0m, 0.5m);

	    _exitBars = Param(nameof(ExitBars), 35)
	        .SetGreaterThanZero()
	        .SetDisplay("Exit After Bars", "Bars to hold position", "General")
	        .SetCanOptimize(true)
	        .SetOptimize(10, 100, 5);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

	    _highest = null;
	    _drawdownStdDev = null;
	    _drawdownSma = null;
	    _barIndex = 0;
	    _entryBarIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _highest = new Highest { Length = LookbackPeriod };
	    _drawdownStdDev = new StandardDeviation { Length = StdDevLength };
	    _drawdownSma = new SimpleMovingAverage { Length = StdDevLength };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(ProcessCandle)
	        .Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawOwnTrades(area);
	    }
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var highestValue = _highest.Process(candle.HighPrice);
	    if (!highestValue.IsFinal)
	    {
	        _barIndex++;
	        return;
	    }

	    var peakHigh = highestValue.GetValue<decimal>();

	    var drawdown = (candle.ClosePrice - peakHigh) / peakHigh * 100m;

	    var meanValue = _drawdownSma.Process(drawdown);
	    var stdDevValue = _drawdownStdDev.Process(drawdown);

	    if (!meanValue.IsFinal || !stdDevValue.IsFinal)
	    {
	        _barIndex++;
	        return;
	    }

	    var mean = meanValue.GetValue<decimal>();
	    var stdDev = stdDevValue.GetValue<decimal>();

	    var goLong = drawdown <= mean + StdDevThreshold * stdDev;

	    if (goLong && Position == 0 && IsFormedAndOnlineAndAllowTrading())
	    {
	        BuyMarket(Volume);
	        _entryBarIndex = _barIndex;
	    }
	    else if (Position > 0 && _entryBarIndex >= 0 && _barIndex - _entryBarIndex >= ExitBars)
	    {
	        SellMarket(Position);
	        _entryBarIndex = -1;
	    }

	    _barIndex++;
	}
}
