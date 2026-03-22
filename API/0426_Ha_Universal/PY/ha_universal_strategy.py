import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ha_universal_strategy(Strategy):
    """Heikin Ashi Universal Strategy. Fast/slow EMA crossover."""

    def __init__(self):
        super(ha_universal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Strategy")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._fast_ema = None
        self._slow_ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ha_universal_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ha_universal_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_length.Value)

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            self._prev_fast = float(fast)
            self._prev_slow = float(slow)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = float(fast)
            self._prev_slow = float(slow)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = float(fast)
            self._prev_slow = float(slow)
            return

        f = float(fast)
        s = float(slow)
        cooldown = int(self._cooldown_bars.Value)

        if self._prev_fast == 0.0:
            self._prev_fast = f
            self._prev_slow = s
            return

        bullish_cross = f > s and self._prev_fast <= self._prev_slow
        bearish_cross = f < s and self._prev_fast >= self._prev_slow

        if bullish_cross and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bearish_cross and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return ha_universal_strategy()
