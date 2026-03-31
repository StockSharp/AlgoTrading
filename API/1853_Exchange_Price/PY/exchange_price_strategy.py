import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class exchange_price_strategy(Strategy):
    def __init__(self):
        super(exchange_price_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 12) \
            .SetDisplay("Short Period", "Bars for short lookback", "General")
        self._long_period = self.Param("LongPeriod", 48) \
            .SetDisplay("Long Period", "Bars for long lookback", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._min_diff_percent = self.Param("MinDiffPercent", 0.0025) \
            .SetDisplay("Minimum Difference %", "Minimum normalized difference between short and long deltas", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prices = []
        self._prev_up_diff = None
        self._prev_down_diff = None
        self._cooldown_remaining = 0

    @property
    def short_period(self):
        return self._short_period.Value
    @property
    def long_period(self):
        return self._long_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def min_diff_percent(self):
        return self._min_diff_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(exchange_price_strategy, self).OnReseted()
        self._prices = []
        self._prev_up_diff = None
        self._prev_down_diff = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(exchange_price_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        close = float(candle.ClosePrice)
        self._prices.append(close)
        if len(self._prices) > self.long_period + 1:
            self._prices.pop(0)
        if len(self._prices) <= self.long_period or len(self._prices) <= self.short_period:
            return
        price_short = self._prices[len(self._prices) - 1 - self.short_period]
        price_long = self._prices[len(self._prices) - 1 - self.long_period]
        up_diff = close - price_short
        down_diff = close - price_long
        diff_percent = abs(up_diff - down_diff) / close if close != 0 else 0.0
        min_dp = float(self.min_diff_percent)
        if self._prev_up_diff is not None and self._prev_down_diff is not None and self._cooldown_remaining == 0:
            cross_up = self._prev_up_diff <= self._prev_down_diff and up_diff > down_diff and diff_percent >= min_dp
            cross_down = self._prev_up_diff >= self._prev_down_diff and up_diff < down_diff and diff_percent >= min_dp
            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._prev_up_diff = up_diff
        self._prev_down_diff = down_diff

    def CreateClone(self):
        return exchange_price_strategy()
