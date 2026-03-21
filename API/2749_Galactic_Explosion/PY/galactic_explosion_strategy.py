import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class galactic_explosion_strategy(Strategy):
    """
    Galactic Explosion: grid strategy using MA bias with distance-based scaling.
    Buys when close < MA, sells when close > MA. After 8 entries applies
    indent and skip-candle logic for additional entries.
    """

    def __init__(self):
        super(galactic_explosion_strategy, self).__init__()
        self._start_hour = self.Param("StartHour", 8) \
            .SetDisplay("Start Hour", "Trading window start hour", "Time")
        self._end_hour = self.Param("EndHour", 20) \
            .SetDisplay("End Hour", "Trading window end hour", "Time")
        self._indent_after_eighth = self.Param("IndentAfterEighth", 100.0) \
            .SetDisplay("Indent After 8th", "Min distance after 8th entry (price steps)", "Grid")
        self._skip_three_candles_min = self.Param("SkipThreeCandlesMin", 300.0) \
            .SetDisplay("Skip3 Min", "Min distance for skip-3 logic", "Grid")
        self._skip_three_candles_max = self.Param("SkipThreeCandlesMax", 600.0) \
            .SetDisplay("Skip3 Max", "Max distance for skip-3 logic", "Grid")
        self._skip_six_candles_max = self.Param("SkipSixCandlesMax", 1200.0) \
            .SetDisplay("Skip6 Max", "Max distance for skip-6 logic", "Grid")
        self._ma_length = self.Param("MaLength", 14) \
            .SetDisplay("MA Length", "Moving average period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entries = 0
        self._first_price = 0.0
        self._last_price = 0.0
        self._missed_bars = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(galactic_explosion_strategy, self).OnReseted()
        self._entries = 0
        self._first_price = 0.0
        self._last_price = 0.0
        self._missed_bars = 0

    def OnStarted(self, time):
        super(galactic_explosion_strategy, self).OnStarted(time)

        ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        ma_val = float(ma_value)
        close = float(candle.ClosePrice)

        hour = candle.OpenTime.Hour
        if hour < self._start_hour.Value or hour >= self._end_hour.Value:
            return

        need_buy = close < ma_val
        need_sell = close > ma_val

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0

        if self._entries <= 8:
            if need_buy and self.Position <= 0:
                self._enter_long(close)
            elif need_sell and self.Position >= 0:
                self._enter_short(close)
            return

        indent = ps * self._indent_after_eighth.Value
        skip3_min = ps * self._skip_three_candles_min.Value
        skip3_max = ps * self._skip_three_candles_max.Value
        skip6_max = ps * self._skip_six_candles_max.Value

        if self.Position > 0:
            self._process_long_grid(close, need_buy, indent, skip3_min, skip3_max, skip6_max)
        elif self.Position < 0:
            self._process_short_grid(close, need_sell, indent, skip3_min, skip3_max, skip6_max)

    def _process_long_grid(self, price, need_buy, indent, skip3_min, skip3_max, skip6_max):
        if self._last_price <= 0 or self._first_price <= 0:
            return

        last_dist = abs(price - self._last_price)
        if last_dist <= indent:
            return

        first_dist = abs(price - self._first_price)

        if first_dist < skip3_min:
            self._missed_bars = 0
            if need_buy:
                self._enter_long(price)
        elif first_dist <= skip3_max:
            self._missed_bars += 1
            if self._missed_bars > 3:
                if need_buy:
                    self._enter_long(price)
                self._missed_bars = 0
        elif first_dist <= skip6_max:
            self._missed_bars += 1
            if self._missed_bars > 6:
                if need_buy:
                    self._enter_long(price)
                self._missed_bars = 0

    def _process_short_grid(self, price, need_sell, indent, skip3_min, skip3_max, skip6_max):
        if self._last_price <= 0 or self._first_price <= 0:
            return

        last_dist = abs(price - self._last_price)
        if last_dist <= indent:
            return

        first_dist = abs(price - self._first_price)

        if first_dist < skip3_min:
            self._missed_bars = 0
            if need_sell:
                self._enter_short(price)
        elif first_dist <= skip3_max:
            self._missed_bars += 1
            if self._missed_bars > 3:
                if need_sell:
                    self._enter_short(price)
                self._missed_bars = 0
        elif first_dist <= skip6_max:
            self._missed_bars += 1
            if self._missed_bars > 6:
                if need_sell:
                    self._enter_short(price)
                self._missed_bars = 0

    def _enter_long(self, price):
        if self._entries == 0:
            self._first_price = price
            self._missed_bars = 0
        self.BuyMarket()
        self._last_price = price
        self._entries += 1

    def _enter_short(self, price):
        if self._entries == 0:
            self._first_price = price
            self._missed_bars = 0
        self.SellMarket()
        self._last_price = price
        self._entries += 1

    def CreateClone(self):
        return galactic_explosion_strategy()
