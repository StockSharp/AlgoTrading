import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class color_jsatl_digit_strategy(Strategy):
    def __init__(self):
        super(color_jsatl_digit_strategy, self).__init__()

        self._jma_length = self.Param("JmaLength", 30) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")
        self._direct_mode = self.Param("DirectMode", True) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")
        self._take_profit = self.Param("TakeProfit", 2) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")

        self._prev_jma = None
        self._prev_prev_jma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_jsatl_digit_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_prev_jma = None

    def OnStarted(self, time):
        super(color_jsatl_digit_strategy, self).OnStarted(time)

        self._jma = JurikMovingAverage()
        self._jma.Length = self.jma_length

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._jma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return color_jsatl_digit_strategy()
