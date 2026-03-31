import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex
from StockSharp.Algo.Strategies import Strategy


class rvi_diff_reversal_strategy(Strategy):
    def __init__(self):
        super(rvi_diff_reversal_strategy, self).__init__()
        self._rvi_length = self.Param("RviLength", 12) \
            .SetDisplay("RVI Length", "Length of RVI", "General")
        self._smoothing_length = self.Param("SmoothingLength", 13) \
            .SetDisplay("Smoothing Length", "Length of EMA smoothing", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_diff = None
        self._prev_prev_diff = None

    @property
    def rvi_length(self):
        return self._rvi_length.Value

    @property
    def smoothing_length(self):
        return self._smoothing_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rvi_diff_reversal_strategy, self).OnReseted()
        self._prev_diff = None
        self._prev_prev_diff = None

    def OnStarted2(self, time):
        super(rvi_diff_reversal_strategy, self).OnStarted2(time)
        self._prev_diff = None
        self._prev_prev_diff = None
        rvi = RelativeVigorIndex()
        rvi.Average.Length = int(self.rvi_length)
        rvi.Signal.Length = int(self.smoothing_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rvi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rvi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rvi_val):
        if candle.State != CandleStates.Finished:
            return
        if not rvi_val.IsFormed:
            return
        avg = rvi_val.Average
        sig = rvi_val.Signal
        if avg is None or sig is None:
            return
        avg = float(avg)
        sig = float(sig)
        current = avg - sig
        if self._prev_diff is not None and self._prev_prev_diff is not None:
            was_falling = self._prev_prev_diff > self._prev_diff
            was_rising = self._prev_prev_diff < self._prev_diff
            if was_falling and current > self._prev_diff and self.Position <= 0:
                self.BuyMarket()
            elif was_rising and current < self._prev_diff and self.Position >= 0:
                self.SellMarket()
        self._prev_prev_diff = self._prev_diff
        self._prev_diff = current

    def CreateClone(self):
        return rvi_diff_reversal_strategy()
