using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on two T3-style moving averages for BTC scalping.
/// </summary>
public class UltimateT3FibonacciBtcScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _t3Length;
	private readonly StrategyParam<int> _t3FiboLength;
	private readonly StrategyParam<bool> _useOpposite;
	private readonly StrategyParam<bool> _useTradeManagement;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevT3;
	private decimal _prevT3Fibo;

	public int T3Length { get => _t3Length.Value; set => _t3Length.Value = value; }
	public int T3FiboLength { get => _t3FiboLength.Value; set => _t3FiboLength.Value = value; }
	public bool UseOpposite { get => _useOpposite.Value; set => _useOpposite.Value = value; }
	public bool UseTradeManagement { get => _useTradeManagement.Value; set => _useTradeManagement.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateT3FibonacciBtcScalpingStrategy()
	{
	    _t3Length = Param(nameof(T3Length), 33)
	        .SetGreaterThanZero()
	        .SetDisplay("T3 Length", "Main T3 length", "General");

	    _t3FiboLength = Param(nameof(T3FiboLength), 19)
	        .SetGreaterThanZero()
	        .SetDisplay("T3 Fibo Length", "Fibonacci T3 length", "General");

	    _useOpposite = Param(nameof(UseOpposite), true)
	        .SetDisplay("Use Opposite", "Close on opposite signal", "General");

	    _useTradeManagement = Param(nameof(UseTradeManagement), true)
	        .SetDisplay("Use Trade Management", "Enable TP/SL", "General");

	    _takeProfit = Param(nameof(TakeProfit), 15m)
	        .SetGreaterThanZero()
	        .SetDisplay("Take Profit %", "Take profit percentage", "Risk");

	    _stopLoss = Param(nameof(StopLoss), 2m)
	        .SetGreaterThanZero()
	        .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	        .SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	    return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var t3 = new EMA { Length = T3Length };
	    var t3Fibo = new EMA { Length = T3FiboLength };

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(t3, t3Fibo, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal t3, decimal t3Fibo)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var crossUp = _prevT3Fibo <= _prevT3 && t3Fibo > t3;
	    var crossDown = _prevT3Fibo >= _prevT3 && t3Fibo < t3;
	    _prevT3 = t3;
	    _prevT3Fibo = t3Fibo;

	    if (crossUp && Position <= 0)
	    {
	        BuyMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else if (crossDown && Position >= 0)
	    {
	        SellMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else
	    {
	        if (UseOpposite)
	        {
	            if (Position > 0 && crossDown)
	                SellMarket();
	            else if (Position < 0 && crossUp)
	                BuyMarket();
	        }
	    }

	    if (UseTradeManagement && Position != 0)
	    {
	        var tp = _entryPrice * (1 + (Position > 0 ? TakeProfit : -TakeProfit) / 100m);
	        var sl = _entryPrice * (1 - (Position > 0 ? StopLoss : -StopLoss) / 100m);

	        if (Position > 0)
	        {
	            if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
	                SellMarket();
	        }
	        else if (Position < 0)
	        {
	            if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
	                BuyMarket();
	        }
	    }
	}
}
