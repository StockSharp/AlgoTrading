using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on consecutive candle directions.
/// Enters long after several bullish candles in a row and short after several bearish candles.
/// </summary>
public class CandleTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _trendCandles;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private int _upCount;
	private int _downCount;

	/// <summary>
	/// Number of consecutive candles required to trigger an action.
	/// </summary>
	public int TrendCandles
	{
	    get => _trendCandles.Value;
	    set => _trendCandles.Value = value;
	}

	/// <summary>
	/// Type of candles used for analysis.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit as percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
	    get => _takeProfitPercent.Value;
	    set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss as percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
	    get => _stopLossPercent.Value;
	    set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
	    get => _enableLongEntry.Value;
	    set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
	    get => _enableShortEntry.Value;
	    set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool EnableLongExit
	{
	    get => _enableLongExit.Value;
	    set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool EnableShortExit
	{
	    get => _enableShortExit.Value;
	    set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CandleTrendStrategy"/>.
	/// </summary>
	public CandleTrendStrategy()
	{
	    _trendCandles = Param(nameof(TrendCandles), 3)
	        .SetGreaterThanZero()
	        .SetDisplay("Trend Candles", "Number of candles in one direction", "General")
	        .SetCanOptimize(true);

	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles for analysis", "General");

	    _takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
	        .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
	        .SetCanOptimize(true);
	    _stopLossPercent = Param(nameof(StopLossPercent), 0m)
	        .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
	        .SetCanOptimize(true);

	    _enableLongEntry = Param(nameof(EnableLongEntry), true)
	        .SetDisplay("Enable Long Entry", "Permission to enter long", "General");

	    _enableShortEntry = Param(nameof(EnableShortEntry), true)
	        .SetDisplay("Enable Short Entry", "Permission to enter short", "General");

	    _enableLongExit = Param(nameof(EnableLongExit), true)
	        .SetDisplay("Enable Long Exit", "Permission to exit long", "General");

	    _enableShortExit = Param(nameof(EnableShortExit), true)
	        .SetDisplay("Enable Short Exit", "Permission to exit short", "General");
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
	    _upCount = 0;
	    _downCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var tp = TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null;
	    var sl = StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : null;
	    StartProtection(tp, sl);

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(ProcessCandle)
	        .Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    var isBull = candle.ClosePrice > candle.OpenPrice;
	    var isBear = candle.ClosePrice < candle.OpenPrice;

	    if (isBull)
	    {
	        _upCount++;
	        _downCount = 0;
	    }
	    else if (isBear)
	    {
	        _downCount++;
	        _upCount = 0;
	    }
	    else
	    {
	        _upCount = 0;
	        _downCount = 0;
	    }

	    if (_upCount >= TrendCandles)
	    {
	        if (Position < 0 && EnableLongExit)
	            BuyMarket(Math.Abs(Position));

	        if (Position <= 0 && EnableLongEntry)
	            BuyMarket(Volume + Math.Abs(Position));
	    }
	    else if (_downCount >= TrendCandles)
	    {
	        if (Position > 0 && EnableShortExit)
	            SellMarket(Math.Abs(Position));

	        if (Position >= 0 && EnableShortEntry)
	            SellMarket(Volume + Math.Abs(Position));
	    }
	}
}
