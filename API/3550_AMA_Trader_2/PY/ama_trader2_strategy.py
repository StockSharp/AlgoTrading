import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ama_trader2_strategy(Strategy):
    def __init__(self):
        super(ama_trader2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._rsi_level_up = self.Param("RsiLevelUp", 60) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._rsi_level_down = self.Param("RsiLevelDown", 40) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._rsi = None
        self._ema = None
        self._prev_price = None
        self._prev_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ama_trader2_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._prev_price = None
        self._prev_ema = None

    def OnStarted(self, time):
        super(ama_trader2_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_length
        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self.__ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ama_trader2_strategy()
