import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vector_basket_trend_strategy(Strategy):
    """Smoothed MA trend strategy (single instrument simplification of multi-pair basket)."""
    def __init__(self):
        super(vector_basket_trend_strategy, self).__init__()
        self._tp = self.Param("TakeProfitPoints", 500).SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl = self.Param("StopLossPoints", 300).SetDisplay("Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vector_basket_trend_strategy, self).OnReseted()
        self._entry_price = 0

    def OnStarted2(self, time):
        super(vector_basket_trend_strategy, self).OnStarted2(time)
        self._entry_price = 0

        fast = SmoothedMovingAverage()
        fast.Length = 3
        slow = SmoothedMovingAverage()
        slow.Length = 7

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        self.StartProtection(None, None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        close = float(candle.ClosePrice)

        if fv > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif fv < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return vector_basket_trend_strategy()
