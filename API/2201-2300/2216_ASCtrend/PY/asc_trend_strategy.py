import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class asc_trend_strategy(Strategy):
    def __init__(self):
        super(asc_trend_strategy, self).__init__()
        self._risk = self.Param("Risk", 4) \
            .SetDisplay("Risk", "Risk parameter", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._was_oversold = False
        self._was_overbought = False

    @property
    def risk(self):
        return self._risk.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(asc_trend_strategy, self).OnReseted()
        self._was_oversold = False
        self._was_overbought = False

    def OnStarted2(self, time):
        super(asc_trend_strategy, self).OnStarted2(time)
        wpr = WilliamsR()
        wpr.Length = 3 + self.risk * 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_val):
        if candle.State != CandleStates.Finished:
            return
        wpr_val = float(wpr_val)
        value2 = 100.0 - abs(wpr_val)
        x1 = 67.0 + float(self.risk)
        x2 = 33.0 - float(self.risk)
        if value2 < x2:
            self._was_oversold = True
        elif value2 > x1:
            self._was_overbought = True
        if self._was_oversold and value2 > x1 and self.Position >= 0:
            self.SellMarket()
            self._was_oversold = False
            self._was_overbought = False
            return
        if self._was_overbought and value2 < x2 and self.Position <= 0:
            self.BuyMarket()
            self._was_oversold = False
            self._was_overbought = False

    def CreateClone(self):
        return asc_trend_strategy()
