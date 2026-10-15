import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_calculator_strategy(Strategy):
    def __init__(self):
        super(volume_calculator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)

        self._prev_volume = 0.0
        self._was_bullish_signal = False
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
        super(volume_calculator_strategy, self).OnReseted()
        self._prev_volume = 0.0
        self._was_bullish_signal = False
        self._has_prev = False

    def OnStarted2(self, time):
        super(volume_calculator_strategy, self).OnStarted2(time)
        self._prev_volume = 0.0
        self._was_bullish_signal = False
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
            bullish_signal = close > ema_val and volume_up
            bearish_signal = close < ema_val and volume_up
            crossed_up = bullish_signal and not self._was_bullish_signal
            crossed_down = bearish_signal and self._was_bullish_signal

            if crossed_up and self.Position <= 0:
                self.BuyMarket()
            elif crossed_down and self.Position >= 0:
                self.SellMarket()

            if bullish_signal or bearish_signal:
                self._was_bullish_signal = bullish_signal

        self._prev_volume = volume
        self._has_prev = True

    def CreateClone(self):
        return volume_calculator_strategy()
