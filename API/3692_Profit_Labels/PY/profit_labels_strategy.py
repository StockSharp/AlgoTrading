import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class profit_labels_strategy(Strategy):
    def __init__(self):
        super(profit_labels_strategy, self).__init__()
        self._tema_period = self.Param("TemaPeriod", 6).SetGreaterThanZero().SetDisplay("TEMA Period", "Triple EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(profit_labels_strategy, self).OnReseted()
        self._tema0 = None
        self._tema1 = None
        self._tema2 = None
        self._tema3 = None
        self._prev_trade_buy = False
        self._prev_trade_sell = False

    def OnStarted(self, time):
        super(profit_labels_strategy, self).OnStarted(time)
        self._tema0 = None
        self._tema1 = None
        self._tema2 = None
        self._tema3 = None
        self._prev_trade_buy = False
        self._prev_trade_sell = False

        tema = TripleExponentialMovingAverage()
        tema.Length = self._tema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(tema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, tema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, tema_val):
        if candle.State != CandleStates.Finished:
            return

        self._tema3 = self._tema2
        self._tema2 = self._tema1
        self._tema1 = self._tema0
        self._tema0 = tema_val

        if self._tema3 is None or self._tema2 is None or self._tema1 is None or self._tema0 is None:
            return

        trend_up = self._tema2 < self._tema3 and self._tema0 > self._tema1
        trend_down = self._tema2 > self._tema3 and self._tema0 < self._tema1

        if trend_up:
            if self.Position < 0:
                self.BuyMarket()
                self._prev_trade_buy = False
                self._prev_trade_sell = False
                return
            if self.Position != 0 or self._prev_trade_buy:
                return
            self.BuyMarket()
            self._prev_trade_buy = True
            self._prev_trade_sell = False
        elif trend_down:
            if self.Position > 0:
                self.SellMarket()
                self._prev_trade_buy = False
                self._prev_trade_sell = False
                return
            if self.Position != 0 or self._prev_trade_sell:
                return
            self.SellMarket()
            self._prev_trade_buy = False
            self._prev_trade_sell = True

    def CreateClone(self):
        return profit_labels_strategy()
