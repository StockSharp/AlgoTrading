using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XDPO Histogram strategy built on double smoothed detrended price oscillator.
/// </summary>
public class XdpoHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _firstMaLength;
	private readonly StrategyParam<int> _secondMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _ma1 = new();
	private readonly SimpleMovingAverage _ma2 = new();

	private decimal _prev1;
	private decimal _prev2;
	private bool _initialized;

	public XdpoHistogramStrategy()
	{
	    _firstMaLength = Param(nameof(FirstMaLength), 12)
	        .SetDisplay("First MA Length", "Length of the initial moving average.", "Indicators")
	        .SetCanOptimize(true);

	    _secondMaLength = Param(nameof(SecondMaLength), 5)
	        .SetDisplay("Second MA Length", "Length of the moving average applied to the difference.", "Indicators")
	        .SetCanOptimize(true);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles for strategy calculations.", "General");
	}

	public int FirstMaLength
	{
	    get => _firstMaLength.Value;
	    set => _firstMaLength.Value = value;
	}

	public int SecondMaLength
	{
	    get => _secondMaLength.Value;
	    set => _secondMaLength.Value = value;
	}

	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	    => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	    base.OnReseted();

	    _ma1.Reset();
	    _ma2.Reset();

	    _prev1 = 0m;
	    _prev2 = 0m;
	    _initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _ma1.Length = FirstMaLength;
	    _ma2.Length = SecondMaLength;

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

	    // Calculate oscillator value
	    var diff = candle.ClosePrice - _ma1.Process(candle.ClosePrice).ToDecimal();
	    var xdpo = _ma2.Process(diff).ToDecimal();

	    if (!_initialized)
	    {
	        _prev1 = xdpo;
	        _prev2 = xdpo;
	        _initialized = true;
	        return;
	    }

	    if (_prev1 < _prev2)
	    {
	        // Close short positions on upward shift
	        if (Position < 0)
	            BuyMarket(Math.Abs(Position));

	        // Open long on rising oscillator
	        if (xdpo > _prev1 && Position <= 0)
	            BuyMarket(Volume + Math.Abs(Position));
	    }
	    else if (_prev1 > _prev2)
	    {
	        // Close long positions on downward shift
	        if (Position > 0)
	            SellMarket(Math.Abs(Position));

	        // Open short on falling oscillator
	        if (xdpo < _prev1 && Position >= 0)
	            SellMarket(Volume + Math.Abs(Position));
	    }

	    _prev2 = _prev1;
	    _prev1 = xdpo;
	}
}
