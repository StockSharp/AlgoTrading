import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class sophia11_strategy(Strategy):
    def __init__(self):
        super(sophia11_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA for exit target", "Indicators")
        self._atr_period = self.Param("AtrPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("ATR Period", "ATR for stops", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sophia11_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(sophia11_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma, atr):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if self._prev3 > 0:
            # 3-bar declining => counter-trend buy
            if self._prev1 < self._prev2 and self._prev2 < self._prev3 and self.Position <= 0:
                if self.Position < 0) BuyMarket(:
                    self.BuyMarket()
            # 3-bar rising => counter-trend sell
            elif self._prev1 > self._prev2 and self._prev2 > self._prev3 and self.Position >= 0:
                if self.Position > 0) SellMarket(:
                    self.SellMarket()
            # Exit long at SMA or ATR stop
            elif self.Position > 0 and (close >= sma or (atr > 0 and close < sma - atr * 3)):
                self.SellMarket()
            # Exit short at SMA or ATR stop
            elif self.Position < 0 and (close <= sma or (atr > 0 and close > sma + atr * 3)):
                self.BuyMarket()
        self._prev3 = self._prev2
        self._prev2 = self._prev1
        self._prev1 = close

    def CreateClone(self):
        return sophia11_strategy()
