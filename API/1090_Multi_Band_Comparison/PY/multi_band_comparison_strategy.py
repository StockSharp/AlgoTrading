import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class multi_band_comparison_strategy(Strategy):
    def __init__(self):
        super(multi_band_comparison_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._length = self.Param("Length", 40) \
            .SetDisplay("Length", "SMA period", "Bands") \
            .SetGreaterThanZero()
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 1.1) \
            .SetDisplay("BB Mult", "Volatility multiplier for the breakout band", "Bands") \
            .SetGreaterThanZero()
        self._entry_confirm_bars = self.Param("EntryConfirmBars", 1) \
            .SetDisplay("Entry Confirm Bars", "Bars for entry confirmation", "Trading") \
            .SetGreaterThanZero()
        self._exit_confirm_bars = self.Param("ExitConfirmBars", 1) \
            .SetDisplay("Exit Confirm Bars", "Bars for exit confirmation", "Trading") \
            .SetGreaterThanZero()
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait before accepting a new signal", "Trading") \
            .SetGreaterThanZero()
        self._entry_counter = 0
        self._exit_counter = 0
        self._cooldown_remaining = 0
        self._was_above_entry_level = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_band_comparison_strategy, self).OnReseted()
        self._entry_counter = 0
        self._exit_counter = 0
        self._cooldown_remaining = 0
        self._was_above_entry_level = False

    def OnStarted2(self, time):
        super(multi_band_comparison_strategy, self).OnStarted2(time)
        self._entry_counter = 0
        self._exit_counter = 0
        self._cooldown_remaining = 0
        self._was_above_entry_level = False
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._length.Value
        self._std = StandardDeviation()
        self._std.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._std, self.OnProcess).Start()

    def OnProcess(self, candle, sma_value, std_value):
        if candle.State != CandleStates.Finished:
            return
        sv = float(sma_value)
        sdv = float(std_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if sdv <= 0.0:
            return
        bm = float(self._bollinger_multiplier.Value)
        entry_level = sv + sdv * bm
        exit_level = sv
        close = float(candle.ClosePrice)
        is_above_entry_level = close > entry_level
        crossed_up = not self._was_above_entry_level and is_above_entry_level
        crossed_down = self._was_above_entry_level and close < exit_level
        self._entry_counter = self._entry_counter + 1 if crossed_up else 0
        self._exit_counter = self._exit_counter + 1 if crossed_down else 0
        ecb = self._entry_confirm_bars.Value
        xcb = self._exit_confirm_bars.Value
        scb = self._signal_cooldown_bars.Value
        if self.Position <= 0 and self._cooldown_remaining == 0 and self._entry_counter >= ecb:
            self.BuyMarket()
            self._entry_counter = 0
            self._cooldown_remaining = scb
        elif self.Position > 0 and self._exit_counter >= xcb:
            self.SellMarket()
            self._exit_counter = 0
            self._cooldown_remaining = scb
        self._was_above_entry_level = is_above_entry_level

    def CreateClone(self):
        return multi_band_comparison_strategy()
