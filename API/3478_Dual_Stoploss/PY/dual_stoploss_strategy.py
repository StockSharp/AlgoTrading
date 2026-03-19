import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class dual_stoploss_strategy(Strategy):
    """
    Dual Stoploss strategy: triple SMA crossover with confirmation.
    Buys when fast SMA crosses above mid SMA and mid is above slow.
    Sells on opposite crossover.
    """

    def __init__(self):
        super(dual_stoploss_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._mid_period = self.Param("MidPeriod", 10) \
            .SetDisplay("Mid SMA", "Mid SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")

        self._prev_fast = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dual_stoploss_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(dual_stoploss_strategy, self).OnStarted(time)
        self._has_prev = False

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        mid = SimpleMovingAverage()
        mid.Length = self._mid_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, mid, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, mid)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, mid_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast_val = float(fast_val)
        mid_val = float(mid_val)
        slow_val = float(slow_val)

        if self._has_prev:
            if self._prev_fast <= self._prev_mid and fast_val > mid_val and mid_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast >= self._prev_mid and fast_val < mid_val and mid_val < slow_val and self.Position >= 0:
                self.SellMarket()
        else:
            if fast_val > mid_val and mid_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif fast_val < mid_val and mid_val < slow_val and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_mid = mid_val
        self._has_prev = True

    def CreateClone(self):
        return dual_stoploss_strategy()
