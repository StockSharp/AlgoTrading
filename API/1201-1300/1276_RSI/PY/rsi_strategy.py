import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_strategy(Strategy):
    def __init__(self):
        super(rsi_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero()
        self._over_sold = self.Param("OverSold", 25.0)
        self._over_bought = self.Param("OverBought", 75.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi = None
        self._previous_rsi = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(rsi_strategy, self).OnReseted()
        self._rsi = None
        self._previous_rsi = None

    def OnStarted2(self, time):
        super(rsi_strategy, self).OnStarted2(time)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        def on_candle(candle, value):
            if candle.State != CandleStates.Finished or not self._rsi.IsFormed:
                return
            current = float(value)
            if self._previous_rsi is not None:
                signal = self.get_signal(
                    self._previous_rsi, current,
                    float(self._over_sold.Value), float(self._over_bought.Value))
                if signal > 0 and self.Position <= 0:
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                elif signal < 0 and self.Position >= 0:
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._previous_rsi = current

        self.SubscribeCandles(self._candle_type.Value).Bind(self._rsi, on_candle).Start()

    @staticmethod
    def get_signal(previous, current, over_sold, over_bought):
        if previous <= over_sold and current > over_sold:
            return 1
        if previous >= over_bought and current < over_bought:
            return -1
        return 0

    def CreateClone(self):
        return rsi_strategy()
