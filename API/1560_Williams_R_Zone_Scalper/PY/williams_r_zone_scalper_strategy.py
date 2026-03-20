import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class williams_r_zone_scalper_strategy(Strategy):
    def __init__(self):
        super(williams_r_zone_scalper_strategy, self).__init__()
        self._length = self.Param("Length", 14) \
            .SetDisplay("%R Length", "Williams %R period", "General")
        self._overbought = self.Param("Overbought", -20) \
            .SetDisplay("Overbought", "Overbought level", "General")
        self._oversold = self.Param("Oversold", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Oversold", "Oversold level", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def length(self):
        return self._length.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_r_zone_scalper_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(williams_r_zone_scalper_strategy, self).OnStarted(time)
        wr = WilliamsR()
        wr.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wr)
            self.DrawOwnTrades(area)

    def on_process(self, candle, wr):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_wr <= self.oversold and wr > self.oversold and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_wr >= self.overbought and wr < self.overbought and self.Position >= 0:
            self.SellMarket()
        self._prev_wr = wr

    def CreateClone(self):
        return williams_r_zone_scalper_strategy()
