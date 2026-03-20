import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_average_with_frames_strategy(Strategy):
    def __init__(self):
        super(moving_average_with_frames_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._sma_length = self.Param("SmaLength", 12) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._entry_price = 0.0
        self._prev_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_with_frames_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0

    def OnStarted(self, time):
        super(moving_average_with_frames_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_length
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return moving_average_with_frames_strategy()
