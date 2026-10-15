import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class simple_bars_strategy(Strategy):
    def __init__(self):
        super(simple_bars_strategy, self).__init__()
        self._period = self.Param("Period", 6) \
            .SetDisplay("Period", "Number of bars for trend check", "General")
        self._use_close = self.Param("UseClose", True) \
            .SetDisplay("Use Close", "Use close price instead of extremes", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._lows = []
        self._highs = []
        self._prev_min_low = 0.0
        self._prev_max_high = 0.0
        self._prev_trend = 0
        self._pending_signal = None
        self._is_initialized = False
        self._cooldown_remaining = 0

    @property
    def period(self):
        return self._period.Value
    @property
    def use_close(self):
        return self._use_close.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(simple_bars_strategy, self).OnReseted()
        self._lows = []
        self._highs = []
        self._prev_min_low = 0.0
        self._prev_max_high = 0.0
        self._prev_trend = 0
        self._pending_signal = None
        self._is_initialized = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(simple_bars_strategy, self).OnStarted2(time)
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
        if self._pending_signal is not None and self._cooldown_remaining == 0:
            if self._pending_signal == 1 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif self._pending_signal == -1 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._pending_signal = None
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        p = self.period
        while len(self._highs) > p:
            self._highs.pop(0)
        while len(self._lows) > p:
            self._lows.pop(0)
        if len(self._highs) < p or len(self._lows) < p:
            return
        min_low = min(self._lows)
        max_high = max(self._highs)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        buy_price = close if self.use_close else float(candle.LowPrice)
        sell_price = close if self.use_close else float(candle.HighPrice)
        trend = 0
        if not self._is_initialized:
            trend = 1 if close > open_p else -1
            self._is_initialized = True
        elif self._prev_trend >= 0:
            trend = 1 if buy_price > self._prev_min_low else -1
        else:
            trend = -1 if sell_price < self._prev_max_high else 1
        self._pending_signal = trend
        self._prev_trend = trend
        self._prev_min_low = min_low
        self._prev_max_high = max_high

    def CreateClone(self):
        return simple_bars_strategy()
