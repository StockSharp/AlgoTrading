import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class re_init_chart_strategy(Strategy):
    def __init__(self):
        super(re_init_chart_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 20).SetGreaterThanZero().SetDisplay("SMA Length", "Moving average length", "Reinitialization")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(re_init_chart_strategy, self).OnReseted()
        self._prev_relation = 0

    def OnStarted2(self, time):
        super(re_init_chart_strategy, self).OnStarted2(time)
        self._prev_relation = 0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        if close > sma_val:
            relation = 1
        elif close < sma_val:
            relation = -1
        else:
            relation = 0

        if relation == 0 or relation == self._prev_relation:
            self._prev_relation = relation
            return

        if relation > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif relation < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_relation = relation

    def CreateClone(self):
        return re_init_chart_strategy()
