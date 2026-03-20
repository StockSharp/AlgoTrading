import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ten_points3_strategy(Strategy):
    def __init__(self):
        super(ten_points3_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 25) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ten_points3_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(ten_points3_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ten_points3_strategy()
