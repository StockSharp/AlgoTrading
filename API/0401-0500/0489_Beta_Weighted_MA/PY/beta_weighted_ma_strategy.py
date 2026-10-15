import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class beta_weighted_ma_strategy(Strategy):
    """Beta Weighted MA Strategy."""

    def __init__(self):
        super(beta_weighted_ma_strategy, self).__init__()

        self._length = self.Param("Length", 50) \
            .SetDisplay("BWMA Length", "Number of periods for Beta Weighted MA", "Parameters")
        self._alpha = self.Param("Alpha", 3.0) \
            .SetDisplay("Alpha (+Lag)", "Alpha parameter for Beta weighting", "Parameters")
        self._beta_param = self.Param("Beta", 3.0) \
            .SetDisplay("Beta (-Lag)", "Beta parameter for Beta weighting", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prices = []
        self._weights = []
        self._denominator = 0.0
        self._prev_ma = 0.0
        self._prev_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(beta_weighted_ma_strategy, self).OnReseted()
        self._prices = []
        self._weights = []
        self._denominator = 0.0
        self._prev_ma = 0.0
        self._prev_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(beta_weighted_ma_strategy, self).OnStarted2(time)

        length = int(self._length.Value)
        alpha = float(self._alpha.Value)
        beta = float(self._beta_param.Value)

        self._weights = []
        self._denominator = 0.0

        for i in range(length):
            x = float(i) / (length - 1)
            w = math.pow(x, alpha - 1) * math.pow(1 - x, beta - 1)
            self._weights.append(w)
            self._denominator += w

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        length = int(self._length.Value)

        self._prices.insert(0, float(candle.ClosePrice))
        if len(self._prices) > length:
            self._prices.pop()

        if len(self._prices) < length:
            return

        total = 0.0
        for i in range(length):
            total += self._prices[i] * self._weights[i]

        ma = total / self._denominator if self._denominator != 0 else 0.0

        if self._prev_ma == 0.0:
            self._prev_ma = ma
            self._prev_price = float(candle.ClosePrice)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_price = float(candle.ClosePrice)
            self._prev_ma = ma
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        crossed_above = close > ma and self._prev_price <= self._prev_ma
        crossed_below = close < ma and self._prev_price >= self._prev_ma

        if crossed_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif crossed_below and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_price = close
        self._prev_ma = ma

    def CreateClone(self):
        return beta_weighted_ma_strategy()
