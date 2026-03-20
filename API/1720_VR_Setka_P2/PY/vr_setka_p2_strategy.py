import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vr_setka_p2_strategy(Strategy):
    def __init__(self):
        super(vr_setka_p2_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vr_setka_p2_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(vr_setka_p2_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_open = candle.OpenPrice
            self._prev_close = close
            self._has_prev = True
            return
        # Previous candle bullish + close above EMA => buy
        if self._prev_close > self._prev_open and close > ema_value and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Previous candle bearish + close below EMA => sell
        elif self._prev_close < self._prev_open and close < ema_value and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_open = candle.OpenPrice
        self._prev_close = close

    def CreateClone(self):
        return vr_setka_p2_strategy()
