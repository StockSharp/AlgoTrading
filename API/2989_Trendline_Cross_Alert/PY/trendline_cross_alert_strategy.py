import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trendline_cross_alert_strategy(Strategy):
    """Price crossing SMA as trendline proxy: buy on cross above, sell on cross below."""
    def __init__(self):
        super(trendline_cross_alert_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "SMA period as trendline", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trendline_cross_alert_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_ma = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(trendline_cross_alert_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._prev_ma = 0
        self._has_prev = False

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ma = ma
            self._has_prev = True
            return

        if self._prev_close <= self._prev_ma and close > ma and self.Position <= 0:


            self.BuyMarket()


        elif self._prev_close >= self._prev_ma and close < ma and self.Position >= 0:


            self.SellMarket()

        self._prev_close = close
        self._prev_ma = ma

    def CreateClone(self):
        return trendline_cross_alert_strategy()
