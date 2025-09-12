using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA and EMA crossover strategy for VN301 index.
/// </summary>
public class DnseVn301SmaEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _sessionCloseHour;
	private readonly StrategyParam<int> _sessionCloseMinute;
	private readonly StrategyParam<int> _minutesBeforeClose;
	private readonly StrategyParam<decimal> _maxLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _prevEma15;
	private decimal _prevSma60;

	public int SessionCloseHour { get => _sessionCloseHour.Value; set => _sessionCloseHour.Value = value; }
	public int SessionCloseMinute { get => _sessionCloseMinute.Value; set => _sessionCloseMinute.Value = value; }
	public int MinutesBeforeClose { get => _minutesBeforeClose.Value; set => _minutesBeforeClose.Value = value; }
	public decimal MaxLossPercent { get => _maxLossPercent.Value; set => _maxLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DnseVn301SmaEmaCrossStrategy()
	{
	    _sessionCloseHour = Param(nameof(SessionCloseHour), 14)
	        .SetDisplay("Close Hour", "Session close hour", "General");

	    _sessionCloseMinute = Param(nameof(SessionCloseMinute), 30)
	        .SetDisplay("Close Minute", "Session close minute", "General");

	    _minutesBeforeClose = Param(nameof(MinutesBeforeClose), 5)
	        .SetDisplay("Minutes Before Close", "Exit minutes before close", "General");

	    _maxLossPercent = Param(nameof(MaxLossPercent), 2m)
	        .SetGreaterThanZero()
	        .SetDisplay("Max Loss %", "Stop loss percentage", "Risk");

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

	    var ema15 = new EMA { Length = 15 };
	    var sma60 = new SMA { Length = 60 };

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(ema15, sma60, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema15, decimal sma60)
	{
	    if (candle.State != CandleStates.Finished)
	        return;

	    var cutoffTime = new TimeSpan(SessionCloseHour, SessionCloseMinute, 0) - TimeSpan.FromMinutes(MinutesBeforeClose);
	    var cutoff = candle.OpenTime.TimeOfDay >= cutoffTime;

	    var crossUp = ema15 > sma60 && _prevEma15 <= _prevSma60;
	    var crossDown = ema15 < sma60 && _prevEma15 >= _prevSma60;
	    _prevEma15 = ema15;
	    _prevSma60 = sma60;

	    if (cutoff)
	    {
	        ClosePosition();
	        return;
	    }

	    if (crossUp && candle.ClosePrice >= ema15 && Position <= 0)
	    {
	        BuyMarket();
	        _entryPrice = candle.ClosePrice;
	    }
	    else if (crossDown && candle.ClosePrice <= ema15 && Position >= 0)
	    {
	        SellMarket();
	        _entryPrice = candle.ClosePrice;
	    }

	    if (Position > 0)
	    {
	        if (crossDown || candle.ClosePrice <= _entryPrice * (1 - MaxLossPercent / 100m))
	            SellMarket();
	    }
	    else if (Position < 0)
	    {
	        if (crossUp || candle.ClosePrice >= _entryPrice * (1 + MaxLossPercent / 100m))
	            BuyMarket();
	    }
	}

	private void ClosePosition()
	{
	    if (Position > 0)
	        SellMarket();
	    else if (Position < 0)
	        BuyMarket();
	}
}
