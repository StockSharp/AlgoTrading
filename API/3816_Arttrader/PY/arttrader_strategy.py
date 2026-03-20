import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class arttrader_strategy(Strategy):
    def __init__(self):
        super(arttrader_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(arttrader_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(arttrader_strategy, self).OnStarted(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        if not self._has_prev:
            self._prev_prev_ema = ema_val
            self._prev_ema = ema_val
            self._prev_close = close
            self._has_prev = True
            return
        ema_rising = ema_val > self._prev_ema
        ema_falling = ema_val < self._prev_ema
        if ema_rising and self._prev_close <= self._prev_ema and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif ema_falling and self._prev_close >= self._prev_ema and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val
        self._prev_close = close

    def CreateClone(self):
        return arttrader_strategy()
