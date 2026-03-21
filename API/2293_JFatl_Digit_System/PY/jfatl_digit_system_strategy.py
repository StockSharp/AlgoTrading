import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class jfatl_digit_system_strategy(Strategy):
    def __init__(self):
        super(jfatl_digit_system_strategy, self).__init__()

        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "JMA period", "Parameters")
        self._jma_phase = self.Param("JmaPhase", -100) \
            .SetDisplay("JMA Length", "JMA period", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("JMA Length", "JMA period", "Parameters")

        self._prev_jma = None
        self._prev_slope = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jfatl_digit_system_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_slope = None

    def OnStarted(self, time):
        super(jfatl_digit_system_strategy, self).OnStarted(time)

        self._jma = JurikMovingAverage()
        self._jma.Length = self.jma_length
        self._jma.Phase = self.jma_phase

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._jma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return jfatl_digit_system_strategy()
