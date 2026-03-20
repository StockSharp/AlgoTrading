import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class leman_trend_hist_strategy(Strategy):
    def __init__(self):
        super(leman_trend_hist_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_period = self.Param("EmaPeriod", 3) \
            .SetDisplay("EMA Period", "EMA length", "Parameters")
        self._value1 = None
        self._value2 = None
        self._value3 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(leman_trend_hist_strategy, self).OnReseted()
        self._value1 = None
        self._value2 = None
        self._value3 = None

    def OnStarted(self, time):
        super(leman_trend_hist_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        self._value3 = self._value2
        self._value2 = self._value1
        self._value1 = ema_value
        if self._value1 is None or self._value2 is None or self._value3 is None:
            return
        # EMA turned up
        if self._value2 < self._value3 and self._value1 > self._value2:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # EMA turned down
        elif self._value2 > self._value3 and self._value1 < self._value2:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return leman_trend_hist_strategy()
