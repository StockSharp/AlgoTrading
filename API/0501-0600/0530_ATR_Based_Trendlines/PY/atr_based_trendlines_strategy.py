import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_based_trendlines_strategy(Strategy):
    """
    Strategy that builds ATR based trendlines from pivot points and trades their breakouts.
    """

    def __init__(self):
        super(atr_based_trendlines_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._lookback_length = self.Param("LookbackLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Lookback length for pivots", "General")
        self._atr_percent = self.Param("AtrPercent", 1.0) \
            .SetDisplay("ATR Percent", "ATR target percentage", "General")
        self._use_wicks = self.Param("UseWicks", True) \
            .SetDisplay("Use Wicks", "Use candle wicks for pivots", "General")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_high = 0.0
        self._prev_prev_high = 0.0
        self._prev_low = 0.0
        self._prev_prev_low = 0.0
        self._last_pivot_high = 0.0
        self._last_pivot_low = 0.0
        self._slope_high = 0.0
        self._slope_low = 0.0
        self._bars_since_high = 0
        self._bars_since_low = 0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def LookbackLength(self): return self._lookback_length.Value
    @LookbackLength.setter
    def LookbackLength(self, v): self._lookback_length.Value = v
    @property
    def AtrPercent(self): return self._atr_percent.Value
    @AtrPercent.setter
    def AtrPercent(self, v): self._atr_percent.Value = v
    @property
    def UseWicks(self): return self._use_wicks.Value
    @UseWicks.setter
    def UseWicks(self, v): self._use_wicks.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v

    def OnReseted(self):
        super(atr_based_trendlines_strategy, self).OnReseted()
        self._prev_high = self._prev_prev_high = self._prev_low = self._prev_prev_low = 0.0
        self._last_pivot_high = self._last_pivot_low = 0.0
        self._slope_high = self._slope_low = 0.0
        self._bars_since_high = self._bars_since_low = 0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(atr_based_trendlines_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = max(1, self.LookbackLength // 2)

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
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        if self.UseWicks:
            high_source = float(candle.HighPrice)
            low_source = float(candle.LowPrice)
        else:
            high_source = max(float(candle.ClosePrice), float(candle.OpenPrice))
            low_source = min(float(candle.ClosePrice), float(candle.OpenPrice))

        close = float(candle.ClosePrice)

        if self._prev_prev_high != 0 and self._prev_high > self._prev_prev_high and self._prev_high > high_source:
            self._last_pivot_high = self._prev_high
            self._slope_high = self.AtrPercent * self.LookbackLength / 200.0 * atr_value
            self._bars_since_high = 1
        elif self._bars_since_high > 0:
            self._bars_since_high += 1
            line_value = self._last_pivot_high - self._slope_high * self._bars_since_high
            if close > line_value and self.Position <= 0 and cooldown_ok:
                self.BuyMarket()
                self._last_trade_bar = self._bar_index

        if self._prev_prev_low != 0 and self._prev_low < self._prev_prev_low and self._prev_low < low_source:
            self._last_pivot_low = self._prev_low
            self._slope_low = self.AtrPercent * self.LookbackLength / 200.0 * atr_value
            self._bars_since_low = 1
        elif self._bars_since_low > 0:
            self._bars_since_low += 1
            line_value = self._last_pivot_low + self._slope_low * self._bars_since_low
            if close < line_value and self.Position >= 0 and cooldown_ok:
                self.SellMarket()
                self._last_trade_bar = self._bar_index

        self._prev_prev_high = self._prev_high
        self._prev_high = high_source
        self._prev_prev_low = self._prev_low
        self._prev_low = low_source

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_based_trendlines_strategy()
