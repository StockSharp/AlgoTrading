import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class triple_ema_qqe_trend_following_strategy(Strategy):
    def __init__(self):
        super(triple_ema_qqe_trend_following_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._trail_pct = self.Param("TrailPct", 4.0) \
            .SetDisplay("Trail %", "Trailing stop percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0
        self._candle_count = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def fast_ema_length(self):
        return self._fast_ema_length.Value

    @property
    def slow_ema_length(self):
        return self._slow_ema_length.Value

    @property
    def trail_pct(self):
        return self._trail_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(triple_ema_qqe_trend_following_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0
        self._candle_count = 0

    def OnStarted2(self, time):
        super(triple_ema_qqe_trend_following_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        self._candle_count += 1
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        rsi_val = float(rsi_val)
        if self._prev_fast == 0 or self._prev_slow == 0 or self._prev_rsi == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_rsi = rsi_val
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_rsi = rsi_val
            return
        price = float(candle.ClosePrice)
        trail = self.trail_pct / 100.0
        trend_up = fast_val > slow_val
        trend_down = fast_val < slow_val
        rsi_cross_up = self._prev_rsi <= 50 and rsi_val > 50
        rsi_cross_down = self._prev_rsi >= 50 and rsi_val < 50
        if self.Position > 0:
            if price < self._entry_price * (1 - trail) or rsi_cross_down:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
        elif self.Position < 0:
            if price > self._entry_price * (1 + trail) or rsi_cross_up:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
        if self.Position == 0:
            if rsi_cross_up and trend_up:
                self.BuyMarket()
                self._entry_price = price
                self._cooldown = 80
            elif rsi_cross_down and trend_down:
                self.SellMarket()
                self._entry_price = price
                self._cooldown = 80
        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return triple_ema_qqe_trend_following_strategy()
