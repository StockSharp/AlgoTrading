import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class elder_impulse_strategy(Strategy):
    """
    Strategy based on Elder's Impulse System.
    Uses EMA direction and MACD histogram to determine impulse.
    Green (bullish): EMA rising + MACD histogram rising -> buy
    Red (bearish): EMA falling + MACD histogram falling -> sell
    """

    def __init__(self):
        super(elder_impulse_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_ema = 0.0
        self._prev_histogram = 0.0
        self._has_prev_values = False
        self._prev_impulse = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elder_impulse_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_histogram = 0.0
        self._has_prev_values = False
        self._prev_impulse = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(elder_impulse_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        macd_signal = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, macd_signal, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, macd_signal)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if ema_value.IsEmpty:
            return

        ema_dec = float(ema_value)
        if ema_dec == 0.0:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        macd_f = float(macd_line)
        signal_f = float(signal_line)
        histogram = macd_f - signal_f

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_ema = ema_dec
            self._prev_histogram = histogram
            return

        ema_rising = ema_dec > self._prev_ema
        histogram_rising = histogram > self._prev_histogram

        if ema_rising and histogram_rising:
            impulse = 1
        elif not ema_rising and not histogram_rising and ema_dec != self._prev_ema:
            impulse = -1
        else:
            impulse = 0

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ema = ema_dec
            self._prev_histogram = histogram
            self._prev_impulse = impulse
            return

        if impulse == 1 and self._prev_impulse != 1 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 65
        elif impulse == -1 and self._prev_impulse != -1 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 65

        self._prev_ema = ema_dec
        self._prev_histogram = histogram
        self._prev_impulse = impulse

    def CreateClone(self):
        return elder_impulse_strategy()
