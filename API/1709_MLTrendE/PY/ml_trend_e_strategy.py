import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ml_trend_e_strategy(Strategy):
    def __init__(self):
        super(ml_trend_e_strategy, self).__init__()
        self._wma_period = self.Param("WmaPeriod", 34) \
            .SetDisplay("WMA Length", "Weighted moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._prev_close = 0.0
        self._prev_wma = 0.0
        self._has_prev = False

    @property
    def wma_period(self):
        return self._wma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ml_trend_e_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_wma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ml_trend_e_strategy, self).OnStarted(time)
        wma = WeightedMovingAverage()
        wma.Length = self.wma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, wma_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_close = close
            self._prev_wma = wma_value
            self._has_prev = True
            return
        # Cross above WMA
        if self._prev_close <= self._prev_wma and close > wma_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Cross below WMA
        elif self._prev_close >= self._prev_wma and close < wma_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_wma = wma_value

    def CreateClone(self):
        return ml_trend_e_strategy()
