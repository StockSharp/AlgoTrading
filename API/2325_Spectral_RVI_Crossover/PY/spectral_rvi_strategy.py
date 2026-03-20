import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class spectral_rvi_strategy(Strategy):
    def __init__(self):
        super(spectral_rvi_strategy, self).__init__()

        self._rvi_length = self.Param("RviLength", 14) \
            .SetDisplay("RVI Length", "Length for RVI", "General")
        self._smooth_length = self.Param("SmoothLength", 10) \
            .SetDisplay("RVI Length", "Length for RVI", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("RVI Length", "Length for RVI", "General")

        self._smooth_rvi = None
        self._smooth_sig = None
        self._prev_sm_rvi = None
        self._prev_sm_sig = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spectral_rvi_strategy, self).OnReseted()
        self._smooth_rvi = None
        self._smooth_sig = None
        self._prev_sm_rvi = None
        self._prev_sm_sig = None

    def OnStarted(self, time):
        super(spectral_rvi_strategy, self).OnStarted(time)

        self.__smooth_rvi = SimpleMovingAverage()
        self.__smooth_rvi.Length = self.smooth_length
        self.__smooth_sig = SimpleMovingAverage()
        self.__smooth_sig.Length = self.smooth_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rvi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return spectral_rvi_strategy()
