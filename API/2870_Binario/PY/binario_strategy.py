import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class binario_strategy(Strategy):
    def __init__(self):
        super(binario_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average length", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    def OnStarted(self, time):
        super(binario_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = self.MaPeriod

        lowest = Lowest()
        lowest.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(highest, lowest, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hv = float(high_value)
        lv = float(low_value)
        close = float(candle.ClosePrice)
        mid = (hv + lv) / 2.0
        channel_padding = (hv - lv) * 0.1

        if close > mid + channel_padding and self.Position <= 0:
            self.BuyMarket()
        elif close < mid - channel_padding and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return binario_strategy()
