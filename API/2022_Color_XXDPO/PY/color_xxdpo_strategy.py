import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_xxdpo_strategy(Strategy):

    def __init__(self):
        super(color_xxdpo_strategy, self).__init__()

        self._first_length = self.Param("FirstLength", 21) \
            .SetDisplay("First MA Length", "Length for the first smoothing stage.", "Indicators")
        self._second_length = self.Param("SecondLength", 5) \
            .SetDisplay("Second MA Length", "Length for the second smoothing stage.", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type for strategy calculation.", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 3) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new direction change.", "General")

        self._ma1 = None
        self._ma2 = None
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._is_initialized = False
        self._cooldown_remaining = 0

    @property
    def FirstLength(self):
        return self._first_length.Value

    @FirstLength.setter
    def FirstLength(self, value):
        self._first_length.Value = value

    @property
    def SecondLength(self):
        return self._second_length.Value

    @SecondLength.setter
    def SecondLength(self, value):
        self._second_length.Value = value

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

    def OnStarted2(self, time):
        super(color_xxdpo_strategy, self).OnStarted2(time)

        self._ma1 = SimpleMovingAverage()
        self._ma1.Length = self.FirstLength
        self._ma2 = SimpleMovingAverage()
        self._ma2.Length = self.SecondLength
        self._is_initialized = False
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close = float(candle.ClosePrice)
        t = candle.OpenTime

        ma1_input = DecimalIndicatorValue(self._ma1, close, t)
        ma1_input.IsFinal = True
        ma1_result = self._ma1.Process(ma1_input)
        if not self._ma1.IsFormed or ma1_result.IsEmpty:
            return

        ma1_val = float(ma1_result)
        dpo = close - ma1_val

        ma2_input = DecimalIndicatorValue(self._ma2, dpo, t)
        ma2_input.IsFinal = True
        xxdpo_result = self._ma2.Process(ma2_input)
        if not self._ma2.IsFormed or xxdpo_result.IsEmpty:
            return

        xxdpo = float(xxdpo_result)

        if not self._is_initialized:
            self._prev2 = xxdpo
            self._prev1 = xxdpo
            self._is_initialized = True
            return

        turned_up = self._prev2 >= self._prev1 and xxdpo > self._prev1
        turned_down = self._prev2 <= self._prev1 and xxdpo < self._prev1

        if self._cooldown_remaining == 0 and turned_up and self.Position <= 0:
            volume = self.Volume + (-self.Position if self.Position < 0 else 0)
            self.BuyMarket(volume)
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and turned_down and self.Position >= 0:
            volume = self.Volume + (self.Position if self.Position > 0 else 0)
            self.SellMarket(volume)
            self._cooldown_remaining = self.SignalCooldownBars

        self._prev2 = self._prev1
        self._prev1 = xxdpo

    def OnReseted(self):
        super(color_xxdpo_strategy, self).OnReseted()
        self._ma1 = None
        self._ma2 = None
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._is_initialized = False
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_xxdpo_strategy()
