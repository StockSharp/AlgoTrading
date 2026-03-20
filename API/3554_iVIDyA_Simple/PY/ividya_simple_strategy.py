import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ividya_simple_strategy(Strategy):
    def __init__(self):
        super(ividya_simple_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._cmo_period = self.Param("CmoPeriod", 20) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._ema_period = self.Param("EmaPeriod", 30) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self._cmo = None
        self._prev_vidya = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ividya_simple_strategy, self).OnReseted()
        self._cmo = None
        self._prev_vidya = None
        self._prev_close = None

    def OnStarted(self, time):
        super(ividya_simple_strategy, self).OnStarted(time)

        self.__cmo = ChandeMomentumOscillator()
        self.__cmo.Length = self.cmo_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__cmo, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ividya_simple_strategy()
