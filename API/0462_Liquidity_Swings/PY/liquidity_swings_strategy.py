import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class liquidity_swings_strategy(Strategy):
    def __init__(self):
        super(liquidity_swings_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._lookback = self.Param("Lookback", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Lookback", "Pivot detection lookback", "Parameters")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._high_buffer = []
        self._low_buffer = []
        self._resistance = 0.0
        self._support = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def lookback(self):
        return self._lookback.Value
    @property
    def ema_length(self):
        return self._ema_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(liquidity_swings_strategy, self).OnReseted()
        self._high_buffer = []
        self._low_buffer = []
        self._resistance = 0.0
        self._support = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(liquidity_swings_strategy, self).OnStarted(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._ema, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return

        self._update_pivot_levels(candle)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if self._resistance == 0.0 or self._support == 0.0:
            return

        price = float(candle.ClosePrice)
        ema_v = float(ema_val)
        rng = self._resistance - self._support

        if price > self._support and price < (self._support + rng * 0.3) and price > ema_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif price < self._resistance and price > (self._resistance - rng * 0.3) and price < ema_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._entry_price = price
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price >= self._resistance:
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price <= self._support:
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < self._support:
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > self._resistance:
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars

    def _update_pivot_levels(self, candle):
        size = self.lookback * 2 + 1
        self._high_buffer.append(float(candle.HighPrice))
        self._low_buffer.append(float(candle.LowPrice))

        if len(self._high_buffer) > size:
            self._high_buffer.pop(0)
        if len(self._low_buffer) > size:
            self._low_buffer.pop(0)

        if len(self._high_buffer) == size:
            center = self.lookback
            candidate = self._high_buffer[center]
            is_pivot = True
            for i in range(size):
                if i == center:
                    continue
                if self._high_buffer[i] >= candidate:
                    is_pivot = False
                    break
            if is_pivot:
                self._resistance = candidate

        if len(self._low_buffer) == size:
            center = self.lookback
            candidate = self._low_buffer[center]
            is_pivot = True
            for i in range(size):
                if i == center:
                    continue
                if self._low_buffer[i] <= candidate:
                    is_pivot = False
                    break
            if is_pivot:
                self._support = candidate

    def CreateClone(self):
        return liquidity_swings_strategy()
