import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class nrtr_extr_strategy(Strategy):
    def __init__(self):
        super(nrtr_extr_strategy, self).__init__()
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "ATR period for NRTR", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")
        self._price = 0.0
        self._value = 0.0
        self._trend = 0
        self._trend_prev = 0
        self._initialized = False

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(nrtr_extr_strategy, self).OnReseted()
        self._price = 0.0
        self._value = 0.0
        self._trend = 0
        self._trend_prev = 0
        self._initialized = False

    def OnStarted(self, time):
        super(nrtr_extr_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not atr_value.IsFormed:
            return
        atr = float(atr_value)
        if atr <= 0:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if not self._initialized:
            self._price = close
            self._value = close
            self._trend = 1
            self._trend_prev = 1
            self._initialized = True
            return
        dk = atr / self.period
        if self._trend >= 0:
            self._price = max(self._price, high)
            self._value = max(self._value, self._price * (1.0 - dk))
            if close < self._value:
                self._price = low
                self._value = self._price * (1.0 + dk)
                self._trend = -1
        else:
            self._price = min(self._price, low)
            self._value = min(self._value, self._price * (1.0 + dk))
            if close > self._value:
                self._price = high
                self._value = self._price * (1.0 - dk)
                self._trend = 1
        buy_signal = self._trend_prev <= 0 and self._trend > 0
        sell_signal = self._trend_prev >= 0 and self._trend < 0
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._trend_prev = self._trend

    def CreateClone(self):
        return nrtr_extr_strategy()
