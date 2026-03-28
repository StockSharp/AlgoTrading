import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class gonna_scalp_strategy(Strategy):
    def __init__(self):
        super(gonna_scalp_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "CCI period", "Indicators")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gonna_scalp_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(gonna_scalp_strategy, self).OnStarted(time)

        self._fast_ind = WeightedMovingAverage()
        self._fast_ind.Length = self._fast_period.Value
        self._slow_ind = WeightedMovingAverage()
        self._slow_ind.Length = self._slow_period.Value
        self._cci_ind = CommodityChannelIndex()
        self._cci_ind.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ind, self._slow_ind, self._cci_ind, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        cci_val = float(cci_value)

        if self._prev_fast is not None and self._prev_slow is not None:
            cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
            cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

            if cross_up and cci_val > -100.0 and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and cci_val < 100.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return gonna_scalp_strategy()
