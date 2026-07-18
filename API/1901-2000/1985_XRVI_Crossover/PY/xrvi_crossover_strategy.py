import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex
from StockSharp.Algo.Strategies import Strategy


class xrvi_crossover_strategy(Strategy):

    def __init__(self):
        super(xrvi_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_avg = None
        self._prev_sig = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(xrvi_crossover_strategy, self).OnStarted2(time)

        rvi = RelativeVigorIndex()

        self.SubscribeCandles(self.CandleType) \
            .BindEx(rvi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, rvi_value):
        if candle.State != CandleStates.Finished:
            return

        avg_raw = rvi_value.Average
        sig_raw = rvi_value.Signal
        if avg_raw is None or sig_raw is None:
            return

        avg = float(avg_raw)
        sig = float(sig_raw)

        if self._prev_avg is not None and self._prev_sig is not None:
            cross_up = self._prev_avg <= self._prev_sig and avg > sig
            cross_down = self._prev_avg >= self._prev_sig and avg < sig

            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_avg = avg
        self._prev_sig = sig

    def OnReseted(self):
        super(xrvi_crossover_strategy, self).OnReseted()
        self._prev_avg = None
        self._prev_sig = None

    def CreateClone(self):
        return xrvi_crossover_strategy()
