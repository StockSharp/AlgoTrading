import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class fourier_smoothed_vzo_strategy(Strategy):
    """
    Fourier Smoothed Volume Zone Oscillator strategy.
    The Volume Zone Oscillator is 100 times the EMA of volume signed by the close-to-close direction divided by the EMA of volume,
    both over VzoLength candles. It is smoothed by rebuilding its latest value from the mean and the fundamental harmonic of a
    discrete Fourier transform over the last SmoothLength values. Above Threshold the strategy is long, below -Threshold short,
    reversing an opposite position; between them the position is closed when CloseAllPositions is set.
    """

    def __init__(self):
        super(fourier_smoothed_vzo_strategy, self).__init__()
        self._vzo_length = self.Param("VzoLength", 2).SetGreaterThanZero().SetDisplay("VZO Length", "EMA length of the oscillator", "Indicators")
        self._smooth_length = self.Param("SmoothLength", 2).SetGreaterThanZero().SetDisplay("Smooth Length", "Window of the Fourier smoothing", "Indicators")
        self._threshold = self.Param("Threshold", 0.0).SetNotNegative().SetDisplay("Threshold", "Oscillator level for signals", "Signals")
        self._close_all_positions = self.Param("CloseAllPositions", True).SetDisplay("Close All Positions", "Close the position when there is no signal", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._signed_volume_ema = None
        self._volume_ema = None
        self._vzo_values = []
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fourier_smoothed_vzo_strategy, self).OnReseted()
        self._signed_volume_ema = None
        self._volume_ema = None
        self._vzo_values = []
        self._prev_close = None

    def OnStarted2(self, time):
        super(fourier_smoothed_vzo_strategy, self).OnStarted2(time)

        self._signed_volume_ema = ExponentialMovingAverage()
        self._signed_volume_ema.Length = self._vzo_length.Value
        self._volume_ema = ExponentialMovingAverage()
        self._volume_ema.Length = self._vzo_length.Value
        self._vzo_values = []
        self._prev_close = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        last_close = self._prev_close
        self._prev_close = close

        if last_close is None:
            return

        volume = float(candle.TotalVolume)
        direction = (close > last_close) - (close < last_close)

        signed_ema = process_float(self._signed_volume_ema, Decimal(direction * volume), candle.OpenTime, True)
        volume_ema = process_float(self._volume_ema, Decimal(volume), candle.OpenTime, True)

        if not self._signed_volume_ema.IsFormed or not self._volume_ema.IsFormed:
            return

        total = float(volume_ema)
        if total == 0:
            return

        smooth_length = self._smooth_length.Value
        self._vzo_values.append(100.0 * float(signed_ema) / total)
        if len(self._vzo_values) > smooth_length:
            del self._vzo_values[0]

        if len(self._vzo_values) < smooth_length:
            return

        oscillator = self._fourier_smooth(self._vzo_values)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        threshold = float(self._threshold.Value)

        if oscillator > threshold:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif oscillator < -threshold:
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        elif self._close_all_positions.Value:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)

    def _fourier_smooth(self, values):
        n = len(values)
        mean = 0.0
        cos_sum = 0.0
        sin_sum = 0.0

        for i, x in enumerate(values):
            angle = 2.0 * math.pi * i / n
            mean += x
            cos_sum += x * math.cos(angle)
            sin_sum += x * math.sin(angle)

        mean /= n

        if n < 2:
            return mean

        # The Nyquist harmonic of an even window has no sine part and counts once, not twice.
        scale = 1.0 / n if n == 2 else 2.0 / n
        last = 2.0 * math.pi * (n - 1) / n

        return mean + scale * (cos_sum * math.cos(last) + sin_sum * math.sin(last))

    def CreateClone(self):
        return fourier_smoothed_vzo_strategy()
