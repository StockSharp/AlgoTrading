import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class simple_engulfing_strategy(Strategy):
    def __init__(self):
        super(simple_engulfing_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._ema_period = self.Param("EmaPeriod", 50)

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    def OnReseted(self):
        super(simple_engulfing_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(simple_engulfing_strategy, self).OnStarted(time)
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        ema_val = float(ema_value)

        if self._has_prev:
            prev_bearish = self._prev_close < self._prev_open
            curr_bullish = close > open_price
            bullish_engulf = (prev_bearish and curr_bullish and
                              open_price <= self._prev_close and close >= self._prev_open)

            prev_bullish = self._prev_close > self._prev_open
            curr_bearish = close < open_price
            bearish_engulf = (prev_bullish and curr_bearish and
                              open_price >= self._prev_close and close <= self._prev_open)

            if bullish_engulf and close > ema_val and self.Position <= 0:
                self.BuyMarket()
            elif bearish_engulf and close < ema_val and self.Position >= 0:
                self.SellMarket()

        self._prev_open = open_price
        self._prev_close = close
        self._has_prev = True

    def CreateClone(self):
        return simple_engulfing_strategy()
