import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SuperTrend, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class three_kilos_btc_15m_strategy(Strategy):
    """Triple EMA crossover with Supertrend filter strategy."""

    def __init__(self):
        super(three_kilos_btc_15m_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle type for strategy calculation", "General")
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._fast_ema = None
        self._slow_ema = None
        self._super_trend = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_kilos_btc_15m_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._super_trend = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_kilos_btc_15m_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_length.Value)
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_length.Value)
        self._super_trend = SuperTrend()
        self._super_trend.Length = 10
        self._super_trend.Multiplier = 2.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._fast_ema, self._slow_ema, self._super_trend, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, slow_val, st_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._super_trend.IsFormed:
            return

        if fast_val.IsEmpty or slow_val.IsEmpty or st_val.IsEmpty:
            return

        fast = float(IndicatorHelper.ToDecimal(fast_val))
        slow = float(IndicatorHelper.ToDecimal(slow_val))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast
            self._prev_slow = slow
            return

        is_up_trend = st_val.IsUpTrend
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return

        bull_cross = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast > slow
        bear_cross = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast < slow

        if bull_cross and is_up_trend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bear_cross and not is_up_trend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and not is_up_trend and bear_cross:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and is_up_trend and bull_cross:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return three_kilos_btc_15m_strategy()
