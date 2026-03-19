import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class average_candle_cross_strategy(Strategy):
    def __init__(self):
        super(average_candle_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._period = self.Param("Period", 20) \
            .SetDisplay("Period", "SMA period", "Indicators")
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def period(self):
        return self._period.Value
    @period.setter
    def period(self, value):
        self._period.Value = value

    def OnReseted(self):
        super(average_candle_cross_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(average_candle_cross_strategy, self).OnStarted(time)
        self._has_prev = False
        sma = SimpleMovingAverage()
        sma.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._has_prev:
            if self._prev_close <= self._prev_sma and candle.ClosePrice > sma_value and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_close >= self._prev_sma and candle.ClosePrice < sma_value and self.Position >= 0:
                self.SellMarket()

        self._prev_close = float(candle.ClosePrice)
        self._prev_sma = float(sma_value)
        self._has_prev = True

    def CreateClone(self):
        return average_candle_cross_strategy()
