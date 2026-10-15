import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_break_impulse_buy_strategy(Strategy):
    def __init__(self):
        super(ma_break_impulse_buy_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_volume = 0.0
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
        super(ma_break_impulse_buy_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_volume = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ma_break_impulse_buy_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_volume = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        volume = float(candle.TotalVolume)

        if self._has_prev:
            volume_up = volume > self._prev_volume

            if self._prev_close <= self._prev_ema and close > ema_val and volume_up and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_close >= self._prev_ema and close < ema_val and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val
        self._prev_volume = volume
        self._has_prev = True

    def CreateClone(self):
        return ma_break_impulse_buy_strategy()
