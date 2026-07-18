import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class channel_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(channel_trailing_stop_strategy, self).__init__()
        self._trail_period = self.Param("TrailPeriod", 10) \
            .SetDisplay("Channel Period", "Lookback for channel calculation", "Parameters")
        self._trail_stop = self.Param("TrailStop", 100.0) \
            .SetDisplay("Trail Stop", "Offset from channel boundaries", "Parameters")
        self._use_noose_trailing = self.Param("UseNooseTrailing", True) \
            .SetDisplay("Use Noose Trailing", "Mirror stop relative to take profit", "Parameters")
        self._use_channel_trailing = self.Param("UseChannelTrailing", True) \
            .SetDisplay("Use Channel Trailing", "Adjust stop to channel levels", "Parameters")
        self._delete_pending_orders = self.Param("DeletePendingOrders", True) \
            .SetDisplay("Delete Pending Orders", "Cancel pending orders after fill", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._highs = []
        self._lows = []
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._take_profit_price = None
        self._cooldown_remaining = 0

    @property
    def trail_period(self):
        return self._trail_period.Value
    @property
    def trail_stop(self):
        return self._trail_stop.Value
    @property
    def use_noose_trailing(self):
        return self._use_noose_trailing.Value
    @property
    def use_channel_trailing(self):
        return self._use_channel_trailing.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(channel_trailing_stop_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._take_profit_price = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(channel_trailing_stop_strategy, self).OnStarted2(time)
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
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        period = self.trail_period
        while len(self._highs) > period:
            self._highs.pop(0)
        while len(self._lows) > period:
            self._lows.pop(0)
        if len(self._highs) < period or len(self._lows) < period:
            return
        upper = max(self._highs)
        lower = min(self._lows)
        rng = upper - lower
        if rng <= 0:
            return
        threshold = rng * 0.05
        close = float(candle.ClosePrice)
        ts = float(self.trail_stop)
        if self._cooldown_remaining == 0:
            if close >= upper - threshold and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._long_stop = close - ts
                self._take_profit_price = close + ts
                self._cooldown_remaining = self.cooldown_bars
            elif close <= lower + threshold and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._short_stop = close + ts
                self._take_profit_price = close - ts
                self._cooldown_remaining = self.cooldown_bars
        if self.use_channel_trailing:
            if self.Position > 0:
                level = lower - ts
                if level > self._long_stop:
                    self._long_stop = level
            elif self.Position < 0:
                level = upper + ts
                if self._short_stop == 0 or level < self._short_stop:
                    self._short_stop = level
        if self.use_noose_trailing and self._take_profit_price is not None:
            if self.Position > 0:
                noose = close - (self._take_profit_price - close)
                if noose > self._long_stop:
                    self._long_stop = noose
            elif self.Position < 0:
                noose = close + (close - self._take_profit_price)
                if self._short_stop == 0 or noose < self._short_stop:
                    self._short_stop = noose
        if self.Position > 0 and self._long_stop > 0 and float(candle.LowPrice) <= self._long_stop:
            self.SellMarket()
            self._long_stop = 0.0
            self._take_profit_price = None
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and self._short_stop > 0 and float(candle.HighPrice) >= self._short_stop:
            self.BuyMarket()
            self._short_stop = 0.0
            self._take_profit_price = None
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return channel_trailing_stop_strategy()
