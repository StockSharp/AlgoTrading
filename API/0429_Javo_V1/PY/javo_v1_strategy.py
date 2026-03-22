import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class javo_v1_strategy(Strategy):
    """Javo v1 Strategy. Fast/slow EMA crossover with HA color confirmation."""

    def __init__(self):
        super(javo_v1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Moving Averages")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._fast_ema = None
        self._slow_ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(javo_v1_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(javo_v1_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_period.Value)

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_period.Value)

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

        # Calculate Heikin-Ashi
        if self._prev_ha_open == 0.0:
            ha_open = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
            ha_close = (float(candle.OpenPrice) + float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 4.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (float(candle.OpenPrice) + float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 4.0

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

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

        ha_green = ha_close > ha_open
        ha_red = ha_close < ha_open

        go_long = f > s and self._prev_fast <= self._prev_slow and ha_green
        go_short = f < s and self._prev_fast >= self._prev_slow and ha_red

        if go_long and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif go_short and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return javo_v1_strategy()
