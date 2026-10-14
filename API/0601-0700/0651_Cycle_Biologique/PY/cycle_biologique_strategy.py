import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class cycle_biologique_strategy(Strategy):
    """
    Cycle Biologique strategy.
    The cycle is Amplitude * sin(2 * pi * (bar index + Offset) / CycleLength).
    A cross above zero opens a long and a cross below zero closes it.
    """

    def __init__(self):
        super(cycle_biologique_strategy, self).__init__()
        self._cycle_length = self.Param("CycleLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Cycle Length", "Bars in one full cycle", "Cycle")
        self._amplitude = self.Param("Amplitude", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Amplitude", "Cycle amplitude", "Cycle")
        self._offset = self.Param("Offset", 0) \
            .SetDisplay("Offset", "Phase shift of the cycle in bars", "Cycle")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bar_index = 0
        self._prev_cycle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(cycle_biologique_strategy, self).OnReseted()
        self._bar_index = 0
        self._prev_cycle = None

    def OnStarted2(self, time):
        super(cycle_biologique_strategy, self).OnStarted2(time)

        self._bar_index = 0
        self._prev_cycle = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        cycle = float(self._amplitude.Value) * math.sin(
            2 * math.pi * (self._bar_index + self._offset.Value) / self._cycle_length.Value)
        self._bar_index += 1

        prev = self._prev_cycle
        self._prev_cycle = cycle

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev <= 0 and cycle > 0 and self.Position == 0:
            self.BuyMarket()
        elif prev >= 0 and cycle < 0 and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return cycle_biologique_strategy()
