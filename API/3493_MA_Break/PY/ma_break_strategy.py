import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_break_strategy(Strategy):
    def __init__(self):
        super(ma_break_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)
        self._body_ratio = self.Param("BodyRatio", 0.7)

        self._prev_close = 0.0
        self._prev_ema = 0.0
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

    @property
    def BodyRatio(self):
        return self._body_ratio.Value

    @BodyRatio.setter
    def BodyRatio(self, value):
        self._body_ratio.Value = value

    def OnReseted(self):
        super(ma_break_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ma_break_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_ema = 0.0
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
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ema_val = float(ema_value)

        range_val = high - low
        body = abs(close - open_price)

        if self._has_prev and range_val > 0:
            is_impulse = body >= range_val * self.BodyRatio

            if (self._prev_close <= self._prev_ema and close > ema_val and
                    close > open_price and is_impulse and self.Position <= 0):
                self.BuyMarket()
            elif (self._prev_close >= self._prev_ema and close < ema_val and
                    close < open_price and is_impulse and self.Position >= 0):
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val
        self._has_prev = True

    def CreateClone(self):
        return ma_break_strategy()
