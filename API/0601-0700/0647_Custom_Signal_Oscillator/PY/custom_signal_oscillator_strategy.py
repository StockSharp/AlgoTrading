import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class custom_signal_oscillator_strategy(Strategy):
    """
    Custom Signal Oscillator strategy.
    The oscillator is the difference between two price signals of the candle, its close and its open.
    A cross above zero goes long and a cross below zero goes short, reversing an opposite position.
    In long-only mode a cross below zero only closes the long.
    """

    def __init__(self):
        super(custom_signal_oscillator_strategy, self).__init__()
        self._long_only = self.Param("LongOnly", False) \
            .SetDisplay("Long Only", "Trade only long positions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_oscillator = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(custom_signal_oscillator_strategy, self).OnReseted()
        self._prev_oscillator = None

    def OnStarted2(self, time):
        super(custom_signal_oscillator_strategy, self).OnStarted2(time)

        self._prev_oscillator = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        oscillator = candle.ClosePrice - candle.OpenPrice
        prev = self._prev_oscillator
        self._prev_oscillator = oscillator

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev <= 0 and oscillator > 0
        cross_down = prev >= 0 and oscillator < 0

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down:
            if self._long_only.Value:
                if self.Position > 0:
                    self.SellMarket(self.Position)
            elif self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return custom_signal_oscillator_strategy()
