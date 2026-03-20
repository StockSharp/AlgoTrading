import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class sample_detect_economic_calendar_strategy(Strategy):
    def __init__(self):
        super(sample_detect_economic_calendar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)

        self._prev_close = 0.0
        self._prev_sar = 0.0
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

    def OnReseted(self):
        super(sample_detect_economic_calendar_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(sample_detect_economic_calendar_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

        sar = ParabolicSar()
        sar.Acceleration = 0.01
        sar.AccelerationMax = 0.1
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sar, ema, self._process_candle).Start()

    def _process_candle(self, candle, sar_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sar_val = float(sar_value)
        ema_val = float(ema_value)

        if self._has_prev:
            above_sar = close > sar_val
            below_sar = close < sar_val
            above_ema = close > ema_val
            below_ema = close < ema_val

            was_above_both = self._prev_close > self._prev_sar and self._prev_close > self._prev_ema
            was_below_both = self._prev_close < self._prev_sar and self._prev_close < self._prev_ema

            if above_sar and above_ema and not was_above_both and self.Position <= 0:
                self.BuyMarket()
            elif below_sar and below_ema and not was_below_both and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sar = sar_val
        self._prev_ema = ema_val
        self._has_prev = True

    def CreateClone(self):
        return sample_detect_economic_calendar_strategy()
