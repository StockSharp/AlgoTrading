using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto adjusted Pearson's R oscillator strategy.
/// Trades when price crosses regression channel based on optimal Pearson's R.
/// </summary>
public class PearsonsROscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minPeriod;
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<decimal> _idealPositive;
	private readonly StrategyParam<decimal> _idealNegative;
	private readonly StrategyParam<decimal> _deviations;
	private readonly StrategyParam<bool> _tradeMid;
	private readonly StrategyParam<bool> _tradeUpper;
	private readonly StrategyParam<bool> _tradeLower;

	private decimal[] _prices;
	private int _index;
	private decimal _prevClose;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum period to search.
	/// </summary>
	public int MinPeriod
	{
	    get => _minPeriod.Value;
	    set => _minPeriod.Value = value;
	}

	/// <summary>
	/// Maximum period to search.
	/// </summary>
	public int MaxPeriod
	{
	    get => _maxPeriod.Value;
	    set
	    {
	        _maxPeriod.Value = value;
	        _prices = new decimal[value];
	    }
	}

	/// <summary>
	/// Step for period decrement.
	/// </summary>
	public int Step
	{
	    get => _step.Value;
	    set => _step.Value = value;
	}

	/// <summary>
	/// Ideal positive Pearson's R.
	/// </summary>
	public decimal IdealPositive
	{
	    get => _idealPositive.Value;
	    set => _idealPositive.Value = value;
	}

	/// <summary>
	/// Ideal negative Pearson's R.
	/// </summary>
	public decimal IdealNegative
	{
	    get => _idealNegative.Value;
	    set => _idealNegative.Value = value;
	}

	/// <summary>
	/// Deviation multiplier.
	/// </summary>
	public decimal Deviations
	{
	    get => _deviations.Value;
	    set => _deviations.Value = value;
	}

	/// <summary>
	/// Trade on midline crosses.
	/// </summary>
	public bool TradeMid
	{
	    get => _tradeMid.Value;
	    set => _tradeMid.Value = value;
	}

	/// <summary>
	/// Trade on upper line crosses.
	/// </summary>
	public bool TradeUpper
	{
	    get => _tradeUpper.Value;
	    set => _tradeUpper.Value = value;
	}

	/// <summary>
	/// Trade on lower line crosses.
	/// </summary>
	public bool TradeLower
	{
	    get => _tradeLower.Value;
	    set => _tradeLower.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PearsonsROscillatorStrategy()
	{
	    _candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles to use", "General");

	    _minPeriod = Param(nameof(MinPeriod), 48)
	        .SetGreaterThanZero()
	        .SetDisplay("Min Period", "Minimum period", "Parameters");

	    _maxPeriod = Param(nameof(MaxPeriod), 360)
	        .SetGreaterThanZero()
	        .SetDisplay("Max Period", "Maximum period", "Parameters");

	    _step = Param(nameof(Step), 12)
	        .SetGreaterThanZero()
	        .SetDisplay("Step", "Step for period decrement", "Parameters");

	    _idealPositive = Param(nameof(IdealPositive), 0.85m)
	        .SetDisplay("Ideal Positive", "Positive Pearson's R threshold", "Parameters");

	    _idealNegative = Param(nameof(IdealNegative), -0.85m)
	        .SetDisplay("Ideal Negative", "Negative Pearson's R threshold", "Parameters");

	    _deviations = Param(nameof(Deviations), 2m)
	        .SetDisplay("Deviations", "Deviation multiplier", "Parameters");

	    _tradeMid = Param(nameof(TradeMid), true)
	        .SetDisplay("Midline Cross", "Trade on midline cross", "Trading");

	    _tradeUpper = Param(nameof(TradeUpper), true)
	        .SetDisplay("Upperline Cross", "Trade on upper line cross", "Trading");

	    _tradeLower = Param(nameof(TradeLower), true)
	        .SetDisplay("Lowerline Cross", "Trade on lower line cross", "Trading");

	    _prices = new decimal[MaxPeriod];
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

	    _prices = new decimal[MaxPeriod];
	    _index = 0;
	    _prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

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

	    _prices[_index % MaxPeriod] = candle.ClosePrice;
	    _index++;

	    if (_index < MaxPeriod)
	    {
	        _prevClose = candle.ClosePrice;
	        return;
	    }

	    var currentClose = candle.ClosePrice;

	    decimal pearsons = 0m;
	    double slope = 0d;
	    double regression = 0d;
	    double deviation = 0d;
	    double startY = 0d;

	    for (var k = MaxPeriod; k >= MinPeriod; k -= Step)
	    {
	        double Ex = 0d, Ey = 0d, Ex2 = 0d, Ey2 = 0d, Exy = 0d;
	        for (var i = 0; i < k; i++)
	        {
	            var price = (double)GetPrice(i);
	            Ex += i;
	            Ey += price;
	            Ex2 += i * i;
	            Ey2 += price * price;
	            Exy += price * i;
	        }

	        var ExT2 = Ex * Ex;
	        var EyT2 = Ey * Ey;
	        var denom1 = Ex2 - ExT2 / k;
	        var denom2 = Ey2 - EyT2 / k;

	        if (denom1 == 0 || denom2 == 0)
	            continue;

	        pearsons = (decimal)((Exy - (Ex * Ey) / k) / Math.Sqrt(denom1 * denom2));

	        slope = Ex2 == ExT2 ? 0d : (k * Exy - Ex * Ey) / (k * Ex2 - ExT2);
	        regression = (Ey - slope * Ex) / k;

	        var intercept = regression + (_index - 1) * slope;
	        double dev = 0d;
	        for (var i = 0; i < k; i++)
	        {
	            var price = (double)GetPrice(i);
	            dev += Math.Pow(price - (intercept - slope * (_index - 1 - i)), 2.0);
	        }
	        deviation = (double)Deviations * Math.Sqrt(dev / (k - 1));
	        startY = regression + slope * (k - 1);

	        if (pearsons >= IdealPositive || pearsons <= IdealNegative)
	            break;
	    }

	    var median = (decimal)(startY - (startY - regression));
	    var upper = (decimal)(startY - (startY - (regression + deviation)));
	    var lower = (decimal)(startY - (startY - (regression - deviation)));

	    var crossMid = TradeMid && Cross(_prevClose, currentClose, median);
	    var crossUpper = TradeUpper && Cross(_prevClose, currentClose, upper);
	    var crossLower = TradeLower && Cross(_prevClose, currentClose, lower);

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    if (crossUpper && Position <= 0)
	        BuyMarket();
	    else if (crossLower && Position >= 0)
	        SellMarket();
	    else if (crossMid)
	    {
	        if (Position > 0)
	            SellMarket();
	        else if (Position < 0)
	            BuyMarket();
	    }

	    _prevClose = currentClose;
	}

	private decimal GetPrice(int offset)
	{
	    var idx = (_index - 1 - offset) % MaxPeriod;
	    if (idx < 0)
	        idx += MaxPeriod;
	    return _prices[idx];
	}

	private static bool Cross(decimal prev, decimal current, decimal level)
	{
	    return (prev <= level && current > level) || (prev >= level && current < level);
	}
}
