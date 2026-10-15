import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class fibo_stop_strategy(Strategy):
    def __init__(self):
        super(fibo_stop_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 500) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Bars to calculate high/low range", "General")
        self._entry_fibo_level = self.Param("EntryFiboLevel", 0.382) \
            .SetDisplay("Entry Fibo", "Fibonacci level for entry (0.236, 0.382, 0.5, 0.618)", "Fibonacci")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars between new entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest_high = -1e18
        self._lowest_low = 1e18
        self._bar_count = 0
        self._bars_since_trade = 0
        self._range_set = False
        self._entry_price = 0.0

    @property
    def lookback_period(self):
        return self._lookback_period.Value
    @property
    def entry_fibo_level(self):
        return self._entry_fibo_level.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibo_stop_strategy, self).OnReseted()
        self._highest_high = -1e18
        self._lowest_low = 1e18
        self._bar_count = 0
        self._bars_since_trade = self.cooldown_bars
        self._range_set = False
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(fibo_stop_strategy, self).OnStarted2(time)
        self._highest_high = -1e18
        self._lowest_low = 1e18
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.stop_loss_pct), UnitTypes.Percent),
            Unit(2, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        self._bars_since_trade += 1
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        if h > self._highest_high:
            self._highest_high = h
        if l < self._lowest_low:
            self._lowest_low = l
        if self._bar_count < self.lookback_period:
            return
        if not self._range_set:
            self._range_set = True
            return
        rng = self._highest_high - self._lowest_low
        if rng <= 0:
            return
        fibo = float(self.entry_fibo_level)
        fibo_level = self._highest_high - rng * fibo
        fibo_618 = self._highest_high - rng * 0.618
        close = float(candle.ClosePrice)
        if self.Position == 0:
            if self._bars_since_trade < self.cooldown_bars:
                return
            if close <= fibo_level and close > fibo_618:
                self.BuyMarket()
                self._entry_price = close
                self._bars_since_trade = 0
            elif close >= self._lowest_low + rng * (1.0 - fibo) and close < self._lowest_low + rng * 0.382:
                self.SellMarket()
                self._entry_price = close
                self._bars_since_trade = 0
        if self._bar_count > self.lookback_period * 2:
            self._highest_high = h
            self._lowest_low = l
            self._bar_count = self.lookback_period

    def CreateClone(self):
        return fibo_stop_strategy()
