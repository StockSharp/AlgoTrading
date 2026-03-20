import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class sample_detect_economic_calendar_strategy(Strategy):
    def __init__(self):
        super(sample_detect_economic_calendar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sample_detect_economic_calendar_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(sample_detect_economic_calendar_strategy, self).OnStarted(time)

        self._sar = ParabolicSar()
        self._sar.Acceleration = 0.01
        self._sar.AccelerationMax = 0.1
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sar, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return sample_detect_economic_calendar_strategy()
