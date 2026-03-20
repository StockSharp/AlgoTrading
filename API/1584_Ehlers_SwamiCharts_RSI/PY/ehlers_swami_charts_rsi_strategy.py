import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ehlers_swami_charts_rsi_strategy(Strategy):
    def __init__(self):
        super(ehlers_swami_charts_rsi_strategy, self).__init__()
        self._long_color = self.Param("LongColor", 50) \
            .SetDisplay("LongColor", "Long color threshold", "General")
        self._short_color = self.Param("ShortColor", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("ShortColor", "Short color threshold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

    @property
    def long_color(self):
        return self._long_color.Value

    @property
    def short_color(self):
        return self._short_color.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ehlers_swami_charts_rsi_strategy, self).OnStarted(time)
        rsis = RelativeStrengthIndex()
        rsis = []
        for i in range(24):
            ind = RelativeStrengthIndex()
            ind.Length = i + 10
            rsis.append(ind)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsis, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def CreateClone(self):
        return ehlers_swami_charts_rsi_strategy()
