import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage

class awesome_fx_trader_strategy(Strategy):
    def __init__(self):
        super(awesome_fx_trader_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 8) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 13) \
            .SetDisplay("Slow EMA", "Period of the slow EMA driving the oscillator", "Awesome Oscillator")
        self._trend_lwma_period = self.Param("TrendLwmaPeriod", 34) \
            .SetDisplay("Trend LWMA", "Length of the linear weighted trend average", "Trend Filter")
        self._trend_smoothing_period = self.Param("TrendSmoothingPeriod", 6) \
            .SetDisplay("Trend Smoother", "Length of the SMA applied to the trend LWMA", "Trend Filter")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time-frame used for calculations", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._trend_lwma = None
        self._previous_ao = 0.0
        self._has_previous_ao = False
        self._is_ao_increasing = False
        self._previous_lwma = 0.0

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def TrendLwmaPeriod(self):
        return self._trend_lwma_period.Value

    @property
    def TrendSmoothingPeriod(self):
        return self._trend_smoothing_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(awesome_fx_trader_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastEmaPeriod
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowEmaPeriod
        self._trend_lwma = WeightedMovingAverage()
        self._trend_lwma.Length = self.TrendLwmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._trend_lwma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_ema, slow_ema, lwma):
        if candle.State != CandleStates.Finished:
            return

        fast_ema = float(fast_ema)
        slow_ema = float(slow_ema)
        lwma = float(lwma)

        ao = fast_ema - slow_ema

        if not self._has_previous_ao:
            self._previous_ao = ao
            self._has_previous_ao = True
            self._is_ao_increasing = ao >= 0
            self._previous_lwma = lwma
            return

        if ao > self._previous_ao:
            self._is_ao_increasing = True
        elif ao < self._previous_ao:
            self._is_ao_increasing = False

        is_trend_bullish = lwma > self._previous_lwma
        is_trend_bearish = lwma < self._previous_lwma
        bullish_signal = self._is_ao_increasing and ao > 0 and is_trend_bullish
        bearish_signal = not self._is_ao_increasing and ao < 0 and is_trend_bearish

        if bullish_signal and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            if volume <= 0:
                volume = 1
            self.BuyMarket(volume)

        elif bearish_signal and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            if volume <= 0:
                volume = 1
            self.SellMarket(volume)

        self._previous_ao = ao
        self._previous_lwma = lwma

    def OnReseted(self):
        super(awesome_fx_trader_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._trend_lwma = None
        self._previous_ao = 0.0
        self._has_previous_ao = False
        self._is_ao_increasing = False
        self._previous_lwma = 0.0

    def CreateClone(self):
        return awesome_fx_trader_strategy()
