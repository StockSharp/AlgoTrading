import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Strategies import Strategy


class liquidex_strategy(Strategy):
    def __init__(self):
        super(liquidex_strategy, self).__init__()
        self._kc_period = self.Param("KcPeriod", 10) \
            .SetDisplay("KC Period", "Keltner Channels period", "Parameters")
        self._use_kc_filter = self.Param("UseKcFilter", True) \
            .SetDisplay("Use KC Filter", "Enable Keltner Channels breakout filter", "Parameters")
        self._stop_loss = self.Param("StopLoss", 60.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 120.0) \
            .SetDisplay("Take Profit", "Take profit in price units, 0 disables", "Risk")
        self._move_to_be = self.Param("MoveToBe", 30.0) \
            .SetDisplay("Move To BE", "Profit to move stop to break-even, 0 disables", "Risk")
        self._move_to_be_offset = self.Param("MoveToBeOffset", 4.0) \
            .SetDisplay("BE Offset", "Offset when moving stop to break-even", "Risk")
        self._trailing_distance = self.Param("TrailingDistance", 15.0) \
            .SetDisplay("Trailing", "Trailing stop distance, 0 disables", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle", "Candle type", "General")
        self._breakout_percent = self.Param("BreakoutPercent", 0.0025) \
            .SetDisplay("Breakout %", "Minimum breakout beyond Keltner boundary", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    @property
    def kc_period(self):
        return self._kc_period.Value
    @property
    def use_kc_filter(self):
        return self._use_kc_filter.Value
    @property
    def stop_loss(self):
        return self._stop_loss.Value
    @property
    def take_profit(self):
        return self._take_profit.Value
    @property
    def move_to_be(self):
        return self._move_to_be.Value
    @property
    def move_to_be_offset(self):
        return self._move_to_be_offset.Value
    @property
    def trailing_distance(self):
        return self._trailing_distance.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def breakout_percent(self):
        return self._breakout_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(liquidex_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(liquidex_strategy, self).OnStarted2(time)
        keltner = KeltnerChannels()
        keltner.Length = self.kc_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, keltner_value):
        if candle.State != CandleStates.Finished or not keltner_value.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        upper = keltner_value.Upper
        lower = keltner_value.Lower
        middle = keltner_value.Middle
        if upper is None or lower is None or middle is None:
            return
        upper = float(upper)
        lower = float(lower)
        price = float(candle.ClosePrice)
        bp = float(self.breakout_percent)
        long_breakout = price > upper and price >= upper * (1.0 + bp)
        short_breakout = price < lower and price <= lower * (1.0 - bp)
        sl = float(self.stop_loss)
        tp = float(self.take_profit)
        mtb = float(self.move_to_be)
        mtbo = float(self.move_to_be_offset)
        td = float(self.trailing_distance)
        if self.Position == 0 and self._cooldown_remaining == 0:
            if not self.use_kc_filter or long_breakout:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = price - sl
                self._cooldown_remaining = self.cooldown_bars
            elif not self.use_kc_filter or short_breakout:
                self.SellMarket()
                self._entry_price = price
                self._stop_price = price + sl
                self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0:
            if tp > 0 and price >= self._entry_price + tp:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif price <= self._stop_price:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            else:
                if mtb > 0 and price - self._entry_price >= mtb:
                    self._stop_price = max(self._stop_price, self._entry_price + mtbo)
                if td > 0:
                    self._stop_price = max(self._stop_price, price - td)
        elif self.Position < 0:
            if tp > 0 and price <= self._entry_price - tp:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif price >= self._stop_price:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            else:
                if mtb > 0 and self._entry_price - price >= mtb:
                    self._stop_price = min(self._stop_price, self._entry_price - mtbo)
                if td > 0:
                    self._stop_price = min(self._stop_price, price + td)

    def CreateClone(self):
        return liquidex_strategy()
