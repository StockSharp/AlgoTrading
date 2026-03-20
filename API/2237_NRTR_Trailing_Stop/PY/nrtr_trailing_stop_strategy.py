import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class nrtr_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(nrtr_trailing_stop_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("NRTR Length", "Number of bars for average range", "Indicator")
        self._digits_shift = self.Param("DigitsShift", 0) \
            .SetDisplay("Digits Shift", "Adjustment for price digits", "Indicator")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit (pts)", "Take profit level in points", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss (pts)", "Stop loss level in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._price = 0.0
        self._value = 0.0
        self._trend = 0
        self._is_initialized = False

    @property
    def length(self):
        return self._length.Value

    @property
    def digits_shift(self):
        return self._digits_shift.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(nrtr_trailing_stop_strategy, self).OnReseted()
        self._price = 0.0
        self._value = 0.0
        self._trend = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(nrtr_trailing_stop_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self.length
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
        dk = atr / self.length * (10.0 ** (-self.digits_shift))
        close = float(candle.ClosePrice)
        if not self._is_initialized:
            self._price = close
            self._value = close
            self._trend = 0
            self._is_initialized = True
            return
        if self._trend >= 0:
            self._price = max(self._price, close)
            self._value = max(self._value, self._price * (1.0 - dk))
            if close < self._value:
                self._price = close
                self._value = self._price * (1.0 + dk)
                self._trend = -1
                if self.Position >= 0:
                    self.SellMarket()
        else:
            self._price = min(self._price, close)
            self._value = min(self._value, self._price * (1.0 + dk))
            if close > self._value:
                self._price = close
                self._value = self._price * (1.0 - dk)
                self._trend = 1
                if self.Position <= 0:
                    self.BuyMarket()

    def CreateClone(self):
        return nrtr_trailing_stop_strategy()
