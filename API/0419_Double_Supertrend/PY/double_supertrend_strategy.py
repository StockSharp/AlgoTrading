import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class double_supertrend_strategy(Strategy):
    """
    Double SuperTrend Strategy.
    Uses two SuperTrend indicators with different parameters.
    Enters long when both SuperTrends are bullish.
    Enters short when both SuperTrends are bearish.
    """

    def __init__(self):
        super(double_supertrend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")
        self._atr_period1 = self.Param("ATRPeriod1", 10) \
            .SetDisplay("ST1 Period", "First SuperTrend ATR period", "SuperTrend 1")
        self._factor1 = self.Param("Factor1", 2.0) \
            .SetDisplay("ST1 Factor", "First SuperTrend multiplier", "SuperTrend 1")
        self._atr_period2 = self.Param("ATRPeriod2", 20) \
            .SetDisplay("ST2 Period", "Second SuperTrend ATR period", "SuperTrend 2")
        self._factor2 = self.Param("Factor2", 4.0) \
            .SetDisplay("ST2 Factor", "Second SuperTrend multiplier", "SuperTrend 2")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._prev_up_trend1 = False
        self._prev_up_trend2 = False
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_supertrend_strategy, self).OnReseted()
        self._prev_up_trend1 = False
        self._prev_up_trend2 = False
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(double_supertrend_strategy, self).OnStarted(time)

        st1 = SuperTrend()
        st1.Length = self._atr_period1.Value
        st1.Multiplier = self._factor1.Value
        st2 = SuperTrend()
        st2.Length = self._atr_period2.Value
        st2.Multiplier = self._factor2.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st1, st2, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st1)
            self.DrawIndicator(area, st2)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, st1_value, st2_value):
        if candle.State != CandleStates.Finished:
            return

        if st1_value.IsEmpty or st2_value.IsEmpty:
            return

        up_trend1 = st1_value.IsUpTrend
        up_trend2 = st2_value.IsUpTrend

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_up_trend1 = up_trend1
            self._prev_up_trend2 = up_trend2
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_up_trend1 = up_trend1
            self._prev_up_trend2 = up_trend2
            self._has_prev = True
            return

        if not self._has_prev:
            self._prev_up_trend1 = up_trend1
            self._prev_up_trend2 = up_trend2
            self._has_prev = True
            return

        both_bullish = up_trend1 and up_trend2
        both_bearish = not up_trend1 and not up_trend2

        bullish_signal = both_bullish and (not self._prev_up_trend1 or not self._prev_up_trend2)
        bearish_signal = both_bearish and (self._prev_up_trend1 or self._prev_up_trend2)

        if bullish_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif bearish_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position > 0 and not both_bullish and (self._prev_up_trend1 and self._prev_up_trend2):
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0 and not both_bearish and (not self._prev_up_trend1 and not self._prev_up_trend2):
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_up_trend1 = up_trend1
        self._prev_up_trend2 = up_trend2

    def CreateClone(self):
        return double_supertrend_strategy()
