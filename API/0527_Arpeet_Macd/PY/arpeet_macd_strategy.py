import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class arpeet_macd_strategy(Strategy):
    """
    Arpeet MACD strategy - trades MACD crossovers with zero-line filter.
    """

    def __init__(self):
        super(arpeet_macd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Length", "Signal line period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_diff = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def FastLength(self): return self._fast_length.Value
    @FastLength.setter
    def FastLength(self, v): self._fast_length.Value = v
    @property
    def SlowLength(self): return self._slow_length.Value
    @SlowLength.setter
    def SlowLength(self, v): self._slow_length.Value = v
    @property
    def SignalLength(self): return self._signal_length.Value
    @SignalLength.setter
    def SignalLength(self, v): self._signal_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v

    def OnReseted(self):
        super(arpeet_macd_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(arpeet_macd_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastLength
        macd.Macd.LongMa.Length = self.SlowLength
        macd.SignalMa.Length = self.SignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        macd_val = value.Macd
        signal_val = value.Signal

        if macd_val is None or signal_val is None:
            return

        macd_val = float(macd_val)
        signal_val = float(signal_val)

        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        diff = macd_val - signal_val
        crossed_up = diff > 0 and self._prev_diff <= 0
        crossed_down = diff < 0 and self._prev_diff >= 0

        if crossed_up and macd_val < 0 and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif crossed_down and macd_val > 0 and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_diff = diff

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return arpeet_macd_strategy()
