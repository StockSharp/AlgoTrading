import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, KaufmanAdaptiveMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_momentum_ama_strategy(Strategy):

    def __init__(self):
        super(color_momentum_ama_strategy, self).__init__()

        self._momentum_period = self.Param("MomentumPeriod", 8) \
            .SetDisplay("Momentum period", "Lookback period for momentum", "Indicator")
        self._ama_period = self.Param("AmaPeriod", 9) \
            .SetDisplay("AMA period", "Smoothing length for AMA", "Indicator")
        self._fast_period = self.Param("FastPeriod", 2) \
            .SetDisplay("Fast period", "Fast period of AMA", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow period", "Slow period of AMA", "Indicator")
        self._signal_bar = self.Param("SignalBar", 2) \
            .SetDisplay("Signal bar", "Bar index used for signals", "Strategy")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetDisplay("Signal cooldown", "Bars to wait between reversals", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Type of candles", "General")

        self._momentum = None
        self._ama = None
        self._buffer = None
        self._cooldown_remaining = 0

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def AmaPeriod(self):
        return self._ama_period.Value

    @AmaPeriod.setter
    def AmaPeriod(self, value):
        self._ama_period.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_momentum_ama_strategy, self).OnStarted(time)

        self._momentum = Momentum()
        self._momentum.Length = self.MomentumPeriod
        self._ama = KaufmanAdaptiveMovingAverage()
        self._ama.Length = self.AmaPeriod
        self._ama.FastSCPeriod = self.FastPeriod
        self._ama.SlowSCPeriod = self.SlowPeriod
        self._buffer = [None] * (self.SignalBar + 3)
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self._momentum, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        mom_val = float(momentum_value)
        ama_result = self._ama.Process(DecimalIndicatorValue(self._ama, mom_val, candle.OpenTime, True))
        if not self._ama.IsFormed or ama_result.IsEmpty:
            return
        ama_value = float(ama_result)

        for i in range(len(self._buffer) - 1, 0, -1):
            self._buffer[i] = self._buffer[i - 1]
        self._buffer[0] = ama_value

        sb = self.SignalBar
        if self._buffer[sb + 2] is None or self._buffer[sb + 1] is None:
            return

        v0 = self._buffer[sb]
        v1 = self._buffer[sb + 1]
        v2 = self._buffer[sb + 2]

        rising = v2 < v1 and v1 < v0
        falling = v2 > v1 and v1 > v0

        if self._cooldown_remaining == 0 and rising and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and falling and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

    def OnReseted(self):
        super(color_momentum_ama_strategy, self).OnReseted()
        self._buffer = [None] * (self.SignalBar + 3)
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_momentum_ama_strategy()
