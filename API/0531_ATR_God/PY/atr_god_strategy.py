import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_god_strategy(Strategy):
    """
    Strategy based on Supertrend indicator with ATR-based risk management.
    Trades supertrend direction changes with cooldown.
    """

    def __init__(self):
        super(atr_god_strategy, self).__init__()

        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "ATR period for Supertrend", "Indicators")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_is_above = False
        self._prev_supertrend = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def Period(self): return self._period.Value
    @Period.setter
    def Period(self, v): self._period.Value = v
    @property
    def Multiplier(self): return self._multiplier.Value
    @Multiplier.setter
    def Multiplier(self, v): self._multiplier.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(atr_god_strategy, self).OnReseted()
        self._prev_is_above = False
        self._prev_supertrend = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(atr_god_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        median = (high + low) / 2.0
        basic_upper = median + self.Multiplier * atr_value
        basic_lower = median - self.Multiplier * atr_value

        if self._prev_supertrend == 0.0:
            supertrend = basic_lower if close > median else basic_upper
            self._prev_supertrend = supertrend
            self._prev_is_above = close > supertrend
            return

        if self._prev_supertrend <= high:
            supertrend = max(basic_lower, self._prev_supertrend)
        elif self._prev_supertrend >= low:
            supertrend = min(basic_upper, self._prev_supertrend)
        else:
            supertrend = basic_lower if close > self._prev_supertrend else basic_upper

        is_above = close > supertrend
        crossed_above = is_above and not self._prev_is_above
        crossed_below = not is_above and self._prev_is_above

        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        if crossed_above and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif crossed_below and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_supertrend = supertrend
        self._prev_is_above = is_above

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_god_strategy()
