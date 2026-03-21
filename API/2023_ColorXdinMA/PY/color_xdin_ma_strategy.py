import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_xdin_ma_strategy(Strategy):

    def __init__(self):
        super(color_xdin_ma_strategy, self).__init__()

        self._main_length = self.Param("MainLength", 10) \
            .SetDisplay("Main MA Length", "Period of the main moving average", "Indicator")
        self._plus_length = self.Param("PlusLength", 20) \
            .SetDisplay("Additional MA Length", "Period of the additional moving average", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 3) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new reversal", "General")

        self._is_initialized = False
        self._prev = 0.0
        self._prev_prev = 0.0
        self._cooldown_remaining = 0

    @property
    def MainLength(self):
        return self._main_length.Value

    @MainLength.setter
    def MainLength(self, value):
        self._main_length.Value = value

    @property
    def PlusLength(self):
        return self._plus_length.Value

    @PlusLength.setter
    def PlusLength(self, value):
        self._plus_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnStarted(self, time):
        super(color_xdin_ma_strategy, self).OnStarted(time)

        self._cooldown_remaining = 0

        main_ma = SimpleMovingAverage()
        main_ma.Length = self.MainLength
        plus_ma = SimpleMovingAverage()
        plus_ma.Length = self.PlusLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(main_ma, plus_ma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, main, plus):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        main_val = float(main)
        plus_val = float(plus)
        xdin = main_val * 2.0 - plus_val

        if not self._is_initialized:
            self._prev_prev = xdin
            self._prev = xdin
            self._is_initialized = True
            return

        if self._cooldown_remaining == 0 and self._prev < self._prev_prev and xdin > self._prev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and self._prev > self._prev_prev and xdin < self._prev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

        self._prev_prev = self._prev
        self._prev = xdin

    def OnReseted(self):
        super(color_xdin_ma_strategy, self).OnReseted()
        self._is_initialized = False
        self._prev = 0.0
        self._prev_prev = 0.0
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_xdin_ma_strategy()
