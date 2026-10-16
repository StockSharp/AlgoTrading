import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, T3MovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class color_step_xccx_strategy(Strategy):
    """Normalized stepped XCCX oscillator, never substituted by EMA crossover."""

    def __init__(self):
        super(color_step_xccx_strategy, self).__init__()
        self._d_period = self.Param("DPeriod", 30).SetDisplay("D Period", "JMA price smoothing period", "Indicators")
        self._m_period = self.Param("MPeriod", 7).SetDisplay("M Period", "T3 deviation smoothing period", "Indicators")
        self._step_fast = self.Param("StepSizeFast", 5).SetDisplay("Fast Step", "Normalized oscillator fast step", "Indicators")
        self._step_slow = self.Param("StepSizeSlow", 30).SetDisplay("Slow Step", "Normalized oscillator slow step", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._reset_signals()

    @property
    def DPeriod(self):
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    @property
    def MPeriod(self):
        return self._m_period.Value

    @MPeriod.setter
    def MPeriod(self, value):
        self._m_period.Value = value

    @property
    def StepSizeFast(self):
        return self._step_fast.Value

    @StepSizeFast.setter
    def StepSizeFast(self, value):
        self._step_fast.Value = value

    @property
    def StepSizeSlow(self):
        return self._step_slow.Value

    @StepSizeSlow.setter
    def StepSizeSlow(self, value):
        self._step_slow.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_signals(self):
        self._previous_fast = None
        self._previous_slow = None
        self._fast_min = self._fast_max = 0.0
        self._slow_min = self._slow_max = 0.0
        self._fast_trend = self._slow_trend = 0
        self._has_step_baseline = False

    def OnReseted(self):
        super(color_step_xccx_strategy, self).OnReseted()
        self._reset_signals()

    def OnStarted2(self, time):
        super(color_step_xccx_strategy, self).OnStarted2(time)
        self._reset_signals()
        self._price_average = JurikMovingAverage()
        self._price_average.Length = self.DPeriod
        self._price_average.Phase = 100
        self._signed_average = T3MovingAverage()
        self._signed_average.Length = self.MPeriod
        self._signed_average.VolumeFactor = 0.15
        self._absolute_average = T3MovingAverage()
        self._absolute_average.Length = self.MPeriod
        self._absolute_average.VolumeFactor = 0.15
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        typical = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        baseline = process_float(self._price_average, typical, candle.OpenTime, True)
        if not self._price_average.IsFormed:
            return
        deviation = typical - float(baseline)
        up = process_float(self._signed_average, deviation, candle.OpenTime, True)
        down = process_float(self._absolute_average, abs(deviation), candle.OpenTime, True)
        if not self._signed_average.IsFormed or not self._absolute_average.IsFormed:
            return
        numerator, denominator = float(up), float(down)
        oscillator = 100.0 * numerator / denominator if numerator != 0 and denominator != 0 else 0.0
        self._process_normalized(oscillator)

    def _process_normalized(self, oscillator):
        fast_step = float(self.StepSizeFast)
        slow_step = float(self.StepSizeSlow)
        if fast_step <= 0 or slow_step <= 0:
            raise ValueError('StepSizeFast and StepSizeSlow must be positive')
        fast_max, fast_min = oscillator + 2.0 * fast_step, oscillator - 2.0 * fast_step
        slow_max, slow_min = oscillator + 2.0 * slow_step, oscillator - 2.0 * slow_step

        if not self._has_step_baseline:
            self._fast_min, self._fast_max = fast_min, fast_max
            self._slow_min, self._slow_max = slow_min, slow_max
            self._previous_fast = self._previous_slow = oscillator
            self._has_step_baseline = True
            return

        if oscillator > self._fast_max:
            self._fast_trend = 1
        elif oscillator < self._fast_min:
            self._fast_trend = -1
        if self._fast_trend > 0 and fast_min < self._fast_min:
            fast_min = self._fast_min
        if self._fast_trend < 0 and fast_max > self._fast_max:
            fast_max = self._fast_max

        if oscillator > self._slow_max:
            self._slow_trend = 1
        elif oscillator < self._slow_min:
            self._slow_trend = -1
        if self._slow_trend > 0 and slow_min < self._slow_min:
            slow_min = self._slow_min
        if self._slow_trend < 0 and slow_max > self._slow_max:
            slow_max = self._slow_max

        fast = fast_min + fast_step if self._fast_trend > 0 else fast_max - fast_step
        slow = slow_min + slow_step if self._slow_trend > 0 else slow_max - slow_step
        previous_fast, previous_slow = self._previous_fast, self._previous_slow
        self._fast_min, self._fast_max = fast_min, fast_max
        self._slow_min, self._slow_max = slow_min, slow_max
        self._previous_fast, self._previous_slow = fast, slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        position = self.Position
        if previous_fast > previous_slow and fast < slow and position <= 0:
            if position < 0:
                self.BuyMarket(-position)
            self.BuyMarket(self.Volume)
        elif previous_fast < previous_slow and fast > slow and position >= 0:
            if position > 0:
                self.SellMarket(position)
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_step_xccx_strategy()
