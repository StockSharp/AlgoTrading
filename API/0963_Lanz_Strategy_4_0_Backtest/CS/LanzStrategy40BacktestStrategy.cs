
using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on pivot highs and lows with dynamic risk management.
/// </summary>
public class LanzStrategy40BacktestStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<decimal> _slBufferPoints;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _pipValueUsd;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal? _lastTop;
	private decimal? _lastBottom;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private int _trendDir;
	private bool _topCrossed;
	private bool _bottomCrossed;
	private bool _topWasStrong;
	private bool _bottomWasStrong;

	private decimal? _entryPriceBuy;
	private decimal? _entryPriceSell;
	private bool _signalTriggeredBuy;
	private bool _signalTriggeredSell;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Pivot swing length.
	/// </summary>
	public int SwingLength
	{
	    get => _swingLength.Value;
	    set => _swingLength.Value = value;
	}

	/// <summary>
	/// Stop loss buffer in points.
	/// </summary>
	public decimal SlBufferPoints
	{
	    get => _slBufferPoints.Value;
	    set => _slBufferPoints.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier.
	/// </summary>
	public decimal RiskReward
	{
	    get => _riskReward.Value;
	    set => _riskReward.Value = value;
	}

	/// <summary>
	/// Risk percent of equity.
	/// </summary>
	public decimal RiskPercent
	{
	    get => _riskPercent.Value;
	    set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Pip value in USD for one lot.
	/// </summary>
	public decimal PipValueUsd
	{
	    get => _pipValueUsd.Value;
	    set => _pipValueUsd.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LanzStrategy40BacktestStrategy"/>.
	/// </summary>
	public LanzStrategy40BacktestStrategy()
	{
	    _swingLength = Param(nameof(SwingLength), 180)
	        .SetDisplay("Swing Length", "Pivot swing length", "General")
	        .SetGreaterThanZero();

	    _slBufferPoints = Param(nameof(SlBufferPoints), 50m)
	        .SetDisplay("SL Buffer", "Stop loss buffer (points)", "Risk")
	        .SetGreaterThanZero();

	    _riskReward = Param(nameof(RiskReward), 1m)
	        .SetDisplay("TP RR", "Take profit risk-reward", "Risk")
	        .SetGreaterThanZero();

	    _riskPercent = Param(nameof(RiskPercent), 1m)
	        .SetDisplay("Risk %", "Risk percent per trade", "Risk")
	        .SetGreaterThanZero();

	    _pipValueUsd = Param(nameof(PipValueUsd), 10m)
	        .SetDisplay("Pip Value USD", "Pip value for one lot", "Risk")
	        .SetGreaterThanZero();

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	    => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	    base.OnReseted();
	    _lastTop = null;
	    _lastBottom = null;
	    _prevHigh = null;
	    _prevLow = null;
	    _trendDir = 0;
	    _topCrossed = false;
	    _bottomCrossed = false;
	    _topWasStrong = false;
	    _bottomWasStrong = false;
	    _entryPriceBuy = null;
	    _entryPriceSell = null;
	    _signalTriggeredBuy = false;
	    _signalTriggeredSell = false;
	    _stopPrice = 0m;
	    _takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    _highest = new Highest { Length = SwingLength };
	    _lowest = new Lowest { Length = SwingLength };

	    var subscription = SubscribeCandles(CandleType);
	    subscription
	        .Bind(_highest, _lowest, ProcessCandle)
	        .Start();

	    var area = CreateChartArea();
	    if (area != null)
	    {
	        DrawCandles(area, subscription);
	        DrawIndicator(area, _highest);
	        DrawIndicator(area, _lowest);
	        DrawOwnTrades(area);
	    }

	    StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    if (!IsFormedAndOnlineAndAllowTrading())
	        return;

	    if (!_highest.IsFormed || !_lowest.IsFormed)
	        return;

	    if (!_lastTop.HasValue || highValue != _lastTop.Value)
	    {
	        _prevHigh = _lastTop;
	        _lastTop = highValue;
	        if (_prevHigh.HasValue && highValue < _prevHigh.Value)
	            _trendDir = -1;
	        _topWasStrong = _trendDir == -1;
	        _topCrossed = false;
	    }

	    if (!_lastBottom.HasValue || lowValue != _lastBottom.Value)
	    {
	        _prevLow = _lastBottom;
	        _lastBottom = lowValue;
	        if (_prevLow.HasValue && lowValue > _prevLow.Value)
	            _trendDir = 1;
	        _bottomWasStrong = _trendDir == 1;
	        _bottomCrossed = false;
	    }

	    var buySignal = !_topCrossed && _lastTop.HasValue && candle.ClosePrice > _lastTop.Value;
	    var sellSignal = !_bottomCrossed && _lastBottom.HasValue && candle.ClosePrice < _lastBottom.Value;

	    if (Position == 0)
	    {
	        _signalTriggeredBuy = false;
	        _signalTriggeredSell = false;
	        _entryPriceBuy = null;
	        _entryPriceSell = null;
	        _stopPrice = 0m;
	        _takeProfitPrice = 0m;
	    }

	    if (buySignal && !_signalTriggeredBuy && Position == 0)
	    {
	        _entryPriceBuy = candle.ClosePrice;
	        _signalTriggeredBuy = true;
	    }

	    if (sellSignal && !_signalTriggeredSell && Position == 0)
	    {
	        _entryPriceSell = candle.ClosePrice;
	        _signalTriggeredSell = true;
	    }

	    var pip = (Security.PriceStep ?? 0m) * 10m;
	    var buffer = SlBufferPoints * pip;

	    if (_signalTriggeredBuy && Position == 0 && _entryPriceBuy.HasValue)
	    {
	        var sl = candle.LowPrice - buffer;
	        var tp = _entryPriceBuy.Value + (_entryPriceBuy.Value - sl) * RiskReward;
	        var qty = CalculateQty(_entryPriceBuy.Value, sl, pip);
	        if (qty > 0m)
	        {
	            BuyMarket(qty);
	            _stopPrice = sl;
	            _takeProfitPrice = tp;
	            _topCrossed = true;
	        }
	    }
	    else if (_signalTriggeredSell && Position == 0 && _entryPriceSell.HasValue)
	    {
	        var sl = candle.HighPrice + buffer;
	        var tp = _entryPriceSell.Value - (sl - _entryPriceSell.Value) * RiskReward;
	        var qty = CalculateQty(_entryPriceSell.Value, sl, pip);
	        if (qty > 0m)
	        {
	            SellMarket(qty);
	            _stopPrice = sl;
	            _takeProfitPrice = tp;
	            _bottomCrossed = true;
	        }
	    }

	    if (Position > 0 && (_stopPrice > 0m || _takeProfitPrice > 0m))
	    {
	        if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
	            SellMarket(Math.Abs(Position));
	    }
	    else if (Position < 0 && (_stopPrice > 0m || _takeProfitPrice > 0m))
	    {
	        if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
	            BuyMarket(Math.Abs(Position));
	    }
	}

	private decimal CalculateQty(decimal entry, decimal sl, decimal pip)
	{
	    var equity = Portfolio?.CurrentValue ?? 0m;
	    var riskUsd = equity * RiskPercent / 100m;
	    var slPips = Math.Abs(entry - sl) / (pip == 0m ? 1m : pip);
	    return slPips > 0m ? riskUsd / (slPips * PipValueUsd) : 0m;
	}
}
