import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sidus_strategy(Strategy):
    def __init__(self):
        super(sidus_strategy, self).__init__()
        self._fast_ema = self.Param("FastEma", 18) \
            .SetDisplay("Fast EMA", "Length of the fast EMA", "Sidus")
        self._slow_ema = self.Param("SlowEma", 28) \
            .SetDisplay("Slow EMA", "Length of the slow EMA", "Sidus")
        self._fast_lwma = self.Param("FastLwma", 5) \
            .SetDisplay("Fast LWMA", "Length of the fast LWMA", "Sidus")
        self._slow_lwma = self.Param("SlowLwma", 8) \
            .SetDisplay("Slow LWMA", "Length of the slow LWMA", "Sidus")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast_lwma = 0.0
        self._prev_slow_lwma = 0.0
        self._prev_slow_ema = 0.0
        self._is_initialized = False

    @property
    def fast_ema(self):
        return self._fast_ema.Value

    @property
    def slow_ema(self):
        return self._slow_ema.Value

    @property
    def fast_lwma(self):
        return self._fast_lwma.Value

    @property
    def slow_lwma(self):
        return self._slow_lwma.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sidus_strategy, self).OnReseted()
        self._prev_fast_lwma = 0.0
        self._prev_slow_lwma = 0.0
        self._prev_slow_ema = 0.0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(sidus_strategy, self).OnStarted2(time)
        fast_ema_ind = ExponentialMovingAverage()
        fast_ema_ind.Length = self.fast_ema
        slow_ema_ind = ExponentialMovingAverage()
        slow_ema_ind.Length = self.slow_ema
        fast_lwma_ind = WeightedMovingAverage()
        fast_lwma_ind.Length = self.fast_lwma
        slow_lwma_ind = WeightedMovingAverage()
        slow_lwma_ind.Length = self.slow_lwma
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema_ind, slow_ema_ind, fast_lwma_ind, slow_lwma_ind, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema_ind)
            self.DrawIndicator(area, slow_ema_ind)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_ema_val, slow_ema_val, fast_lwma_val, slow_lwma_val):
        if candle.State != CandleStates.Finished:
            return
        fast_ema_val = float(fast_ema_val)
        slow_ema_val = float(slow_ema_val)
        fast_lwma_val = float(fast_lwma_val)
        slow_lwma_val = float(slow_lwma_val)
        if not self._is_initialized:
            self._prev_fast_lwma = fast_lwma_val
            self._prev_slow_lwma = slow_lwma_val
            self._prev_slow_ema = slow_ema_val
            self._is_initialized = True
            return
        buy_signal = (
            (fast_lwma_val > slow_lwma_val and self._prev_fast_lwma <= self._prev_slow_lwma) or
            (slow_lwma_val > slow_ema_val and self._prev_slow_lwma <= self._prev_slow_ema)
        )
        sell_signal = (
            (fast_lwma_val < slow_lwma_val and self._prev_fast_lwma >= self._prev_slow_lwma) or
            (slow_lwma_val < slow_ema_val and self._prev_slow_lwma >= self._prev_slow_ema)
        )
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_fast_lwma = fast_lwma_val
        self._prev_slow_lwma = slow_lwma_val
        self._prev_slow_ema = slow_ema_val

    def CreateClone(self):
        return sidus_strategy()
