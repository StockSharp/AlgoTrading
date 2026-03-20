import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class khaled_tamims_avellaneda_stoikov_strategy(Strategy):
    def __init__(self):
        super(khaled_tamims_avellaneda_stoikov_strategy, self).__init__()
        self._gamma = self.Param("Gamma", 2.0) \
            .SetDisplay("Gamma", "Gamma", "General")
        self._sigma = self.Param("Sigma", 8.0) \
            .SetDisplay("Sigma", "Sigma", "General")
        self._t = self.Param("T", 0.0833) \
            .SetDisplay("T", "T", "General")
        self._k = self.Param("K", 5.0) \
            .SetDisplay("K", "K", "General")
        self._m = self.Param("M", 0.5) \
            .SetDisplay("M", "M", "General")
        self._fee = self.Param("Fee", 0.0) \
            .SetDisplay("Fee", "Fee", "General")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 12000) \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_close = 0.0
        self._is_first = True
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(khaled_tamims_avellaneda_stoikov_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._is_first = True
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(khaled_tamims_avellaneda_stoikov_strategy, self).OnStarted(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        close = float(candle.ClosePrice)
        if self._is_first:
            self._prev_close = close
            self._is_first = False
            return
        mid_price = (close + self._prev_close) / 2.0
        self._prev_close = close
        gamma = float(self._gamma.Value)
        sigma = float(self._sigma.Value)
        t = float(self._t.Value)
        k = float(self._k.Value)
        m = float(self._m.Value)
        fee = float(self._fee.Value)
        sqrt_term = gamma * sigma * sigma * t
        bid_quote = mid_price - k * sqrt_term - mid_price * fee
        ask_quote = mid_price + k * sqrt_term + mid_price * fee
        long_cond = close < bid_quote - m
        short_cond = close > ask_quote + m
        if self._entries_executed >= self._max_entries.Value or self._bars_since_signal < self._cooldown_bars.Value:
            return
        if long_cond and self.Position <= 0:
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif short_cond and self.Position >= 0:
            self.SellMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0

    def CreateClone(self):
        return khaled_tamims_avellaneda_stoikov_strategy()
