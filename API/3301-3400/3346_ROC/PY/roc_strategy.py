import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class roc_strategy(Strategy):
    def __init__(self):
        super(roc_strategy, self).__init__()

        self._roc_period = self.Param("RocPeriod", 12) \
            .SetDisplay("ROC Period", "Rate of Change period", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetDisplay("Slow WMA", "Slow WMA period", "Indicators")

        self._roc = None
        self._fast_ma = None
        self._slow_ma = None
        self._prev_roc = None

    @property
    def roc_period(self):
        return self._roc_period.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    def OnReseted(self):
        super(roc_strategy, self).OnReseted()
        self._roc = None
        self._fast_ma = None
        self._slow_ma = None
        self._prev_roc = None

    def OnStarted2(self, time):
        super(roc_strategy, self).OnStarted2(time)

        self._roc = RateOfChange()
        self._roc.Length = self.roc_period
        self._fast_ma = WeightedMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = WeightedMovingAverage()
        self._slow_ma.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._roc, self._fast_ma, self._slow_ma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, roc_value, fast_ma_value, slow_ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._roc.IsFormed or not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        roc_val = float(roc_value)
        fast_val = float(fast_ma_value)
        slow_val = float(slow_ma_value)

        if self._prev_roc is not None:
            if self._prev_roc <= 0.0 and roc_val > 0.0 and fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_roc >= 0.0 and roc_val < 0.0 and fast_val < slow_val and self.Position >= 0:
                self.SellMarket()

        self._prev_roc = roc_val

    def CreateClone(self):
        return roc_strategy()
