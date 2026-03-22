import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class litecoin_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(litecoin_trailing_stop_strategy, self).__init__()
        self._kama_length = self.Param("KamaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("KAMA Length", "Period for KAMA indicator", "General")
        self._bars_between_entries = self.Param("BarsBetweenEntries", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Bars Between Entries", "Minimum bars between new positions", "General")
        self._trailing_stop_percent = self.Param("TrailingStopPercent", 15.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Stop %", "Percent for trailing stop", "Risk")
        self._delay_bars = self.Param("DelayBars", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Delay Bars", "Bars before trailing starts", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_kama = 0.0
        self._bars_since_entry = 0
        self._bars_since_last_trade = 1000
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(litecoin_trailing_stop_strategy, self).OnReseted()
        self._prev_kama = 0.0
        self._bars_since_entry = 0
        self._bars_since_last_trade = 1000
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def OnStarted(self, time):
        super(litecoin_trailing_stop_strategy, self).OnStarted(time)
        self._prev_kama = 0.0
        self._bars_since_entry = 0
        self._bars_since_last_trade = 1000
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._kama = KaufmanAdaptiveMovingAverage()
        self._kama.Length = self._kama_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._kama, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._kama)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, kama_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._kama.IsFormed:
            return
        kv = float(kama_value)
        bullish = self._prev_kama != 0.0 and kv > self._prev_kama
        bearish = self._prev_kama != 0.0 and kv < self._prev_kama
        if self._bars_since_last_trade < 10000:
            self._bars_since_last_trade += 1
        if self.Position != 0:
            self._bars_since_entry += 1
        else:
            self._bars_since_entry = 0
        can_enter = self._bars_since_last_trade >= self._bars_between_entries.Value
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if bullish and can_enter and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_last_trade = 0
            self._bars_since_entry = 0
            self._entry_price = close
            self._highest_price = self._entry_price
        elif bearish and can_enter and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_last_trade = 0
            self._bars_since_entry = 0
            self._entry_price = close
            self._lowest_price = self._entry_price
        if self.Position > 0:
            self._highest_price = max(self._highest_price, high)
            if self._bars_since_entry >= self._delay_bars.Value:
                trail_pct = float(self._trailing_stop_percent.Value) / 100.0
                stop_price = self._highest_price * (1.0 - trail_pct)
                if low <= stop_price:
                    self.SellMarket(abs(self.Position))
                    self._highest_price = 0.0
        elif self.Position < 0:
            self._lowest_price = min(self._lowest_price, low)
            if self._bars_since_entry >= self._delay_bars.Value:
                trail_pct = float(self._trailing_stop_percent.Value) / 100.0
                stop_price = self._lowest_price * (1.0 + trail_pct)
                if high >= stop_price:
                    self.BuyMarket(abs(self.Position))
                    self._lowest_price = 0.0
        self._prev_kama = kv

    def CreateClone(self):
        return litecoin_trailing_stop_strategy()
