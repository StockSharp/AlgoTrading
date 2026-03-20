import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SuperTrend
from StockSharp.Algo.Strategies import Strategy


class three_kilos_btc_15m_strategy(Strategy):
    def __init__(self):
        super(three_kilos_btc_15m_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle type for strategy calculation", "General")
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators") \
            .SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators") \
            .SetGreaterThanZero()
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(three_kilos_btc_15m_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_kilos_btc_15m_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value
        st = SuperTrend()
        st.Length = 10
        st.Multiplier = 2.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, st, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, st_val):
        if candle.State != CandleStates.Finished:
            return
        if fast_val.IsEmpty or slow_val.IsEmpty or st_val.IsEmpty:
            return
        fast = float(fast_val.GetValue[float]())
        slow = float(slow_val.GetValue[float]())
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast
            self._prev_slow = slow
            return
        is_up_trend = st_val.IsUpTrend
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        bull_cross = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast > slow
        bear_cross = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast < slow
        if bull_cross and is_up_trend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif bear_cross and not is_up_trend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and not is_up_trend and bear_cross:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and is_up_trend and bull_cross:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return three_kilos_btc_15m_strategy()
