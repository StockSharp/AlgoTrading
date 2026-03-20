import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class is_connected_strategy(Strategy):
    def __init__(self):
        super(is_connected_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._acceleration = self.Param("Acceleration", 0.01) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._acceleration_max = self.Param("AccelerationMax", 0.1) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(is_connected_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(is_connected_strategy, self).OnStarted(time)

        self._sar = ParabolicSar()
        self._sar.Acceleration = self.acceleration
        self._sar.AccelerationMax = self.accelerationMax

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sar, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return is_connected_strategy()
