import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class virtual_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(virtual_trailing_stop_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._trailing_pct = self.Param("TrailingPct", 2.0) \
            .SetDisplay("Trailing %", "Trailing stop distance percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def trailing_pct(self):
        return self._trailing_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(virtual_trailing_stop_strategy, self).OnStarted(time)
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.trailing_pct, UnitTypes.Percent),
            isStopTrailing=True,
            useMarketOrders=True)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        if fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return virtual_trailing_stop_strategy()
