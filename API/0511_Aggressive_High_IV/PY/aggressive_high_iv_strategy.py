import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class aggressive_high_iv_strategy(Strategy):
    def __init__(self):
        super(aggressive_high_iv_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Period for fast EMA", "Parameters")
        self._slow_ema_length = self.Param("SlowEmaLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Period for slow EMA", "Parameters")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR calculation period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
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
        super(aggressive_high_iv_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(aggressive_high_iv_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = float(fast_val)
            self._prev_slow = float(slow_val)
            return
        fast = float(fast_val)
        slow = float(slow_val)
        atr_v = float(atr_val)
        close = float(candle.ClosePrice)
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self.Position > 0 and self._entry_price > 0 and atr_v > 0:
            if close <= self._entry_price - 2.0 * atr_v or close >= self._entry_price + 4.0 * atr_v:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0 and atr_v > 0:
            if close >= self._entry_price + 2.0 * atr_v or close <= self._entry_price - 4.0 * atr_v:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_fast = fast
                self._prev_slow = slow
                return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        long_cross = self._prev_fast <= self._prev_slow and fast > slow
        short_cross = self._prev_fast >= self._prev_slow and fast < slow
        if long_cross and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown_remaining = self.cooldown_bars
        elif short_cross and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown_remaining = self.cooldown_bars
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return aggressive_high_iv_strategy()
