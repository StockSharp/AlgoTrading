import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hull_suite_1_risk_no_sl_tp_strategy(Strategy):
    """
    EMA crossover strategy. Buys when fast EMA crosses above slow EMA,
    sells when fast EMA crosses below slow EMA.
    """

    def __init__(self):
        super(hull_suite_1_risk_no_sl_tp_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 120) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_suite_1_risk_no_sl_tp_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(hull_suite_1_risk_no_sl_tp_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)

        if self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return hull_suite_1_risk_no_sl_tp_strategy()
