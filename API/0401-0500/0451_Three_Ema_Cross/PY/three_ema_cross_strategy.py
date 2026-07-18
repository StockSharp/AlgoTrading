import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_ema_cross_strategy(Strategy):
    """Three EMA Cross Strategy."""

    def __init__(self):
        super(three_ema_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Moving Averages")
        self._slow_ema_length = self.Param("SlowEmaLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Moving Averages")
        self._trend_ema_length = self.Param("TrendEmaLength", 100) \
            .SetDisplay("Trend EMA", "Trend EMA length", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._fast_ema = None
        self._slow_ema = None
        self._trend_ema = None
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_ema_cross_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._trend_ema = None
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_ema_cross_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_ema_length.Value)

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_ema_length.Value)

        self._trend_ema = ExponentialMovingAverage()
        self._trend_ema.Length = int(self._trend_ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._trend_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawIndicator(area, self._trend_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_ema, slow_ema, trend_ema):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._trend_ema.IsFormed:
            self._prev_fast_ema = float(fast_ema)
            self._prev_slow_ema = float(slow_ema)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast_ema = float(fast_ema)
            self._prev_slow_ema = float(slow_ema)
            return

        fe = float(fast_ema)
        se = float(slow_ema)
        te = float(trend_ema)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast_ema = fe
            self._prev_slow_ema = se
            return

        if self._prev_fast_ema == 0.0 or self._prev_slow_ema == 0.0:
            self._prev_fast_ema = fe
            self._prev_slow_ema = se
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        cross_up = fe > se and self._prev_fast_ema <= self._prev_slow_ema
        cross_down = fe < se and self._prev_fast_ema >= self._prev_slow_ema

        if cross_up and close > te and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif cross_down and close < te and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and cross_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and cross_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_fast_ema = fe
        self._prev_slow_ema = se

    def CreateClone(self):
        return three_ema_cross_strategy()
