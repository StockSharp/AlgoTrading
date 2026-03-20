import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class virtual_robot_strategy(Strategy):
    def __init__(self):
        super(virtual_robot_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._rsi = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(virtual_robot_strategy, self).OnReseted()
        self._rsi = None

    def OnStarted(self, time):
        super(virtual_robot_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        rsi = float(rsi_value)

        if rsi < 25.0 and self.Position <= 0:
            self.BuyMarket()
        elif rsi > 75.0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return virtual_robot_strategy()
