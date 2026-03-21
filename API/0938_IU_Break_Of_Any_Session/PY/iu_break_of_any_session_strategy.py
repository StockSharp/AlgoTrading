import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class iu_break_of_any_session_strategy(Strategy):
    """
    Breakout from custom session high/low.
    Enters when price breaks session range with risk/reward management.
    """

    def __init__(self):
        super(iu_break_of_any_session_strategy, self).__init__()
        self._session_bars = self.Param("SessionBars", 24) \
            .SetDisplay("Session Bars", "Number of bars to form session range", "Session")
        self._profit_factor = self.Param("ProfitFactor", 2.0) \
            .SetDisplay("Profit Factor", "Risk to reward ratio", "Risk")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum number of entries per test", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._session_high = 0.0
        self._session_low = 0.0
        self._bar_count = 0
        self._entries_executed = 0
        self._cooldown = 0
        self._stop_price = 0.0
        self._target_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(iu_break_of_any_session_strategy, self).OnReseted()
        self._session_high = 0.0
        self._session_low = 0.0
        self._bar_count = 0
        self._entries_executed = 0
        self._cooldown = 0
        self._stop_price = 0.0
        self._target_price = 0.0

    def OnStarted(self, time):
        super(iu_break_of_any_session_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._bar_count += 1
        self._cooldown += 1

        if self._bar_count <= self._session_bars.Value:
            if self._session_low == 0.0:
                self._session_high = high
                self._session_low = low
            else:
                self._session_high = max(self._session_high, high)
                self._session_low = min(self._session_low, low)
            return

        if self.Position > 0:
            if low <= self._stop_price or high >= self._target_price:
                self.SellMarket()
                self._session_high = high
                self._session_low = low
                self._bar_count = 1
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._target_price:
                self.BuyMarket()
                self._session_high = high
                self._session_low = low
                self._bar_count = 1
        elif self._entries_executed < self._max_entries.Value and self._cooldown >= self._cooldown_bars.Value:
            if close > self._session_high:
                self._stop_price = self._session_low
                risk = close - self._stop_price
                self._target_price = close + risk * self._profit_factor.Value
                self.BuyMarket()
                self._entries_executed += 1
                self._cooldown = 0
            elif close < self._session_low:
                self._stop_price = self._session_high
                risk = self._stop_price - close
                self._target_price = close - risk * self._profit_factor.Value
                self.SellMarket()
                self._entries_executed += 1
                self._cooldown = 0

    def CreateClone(self):
        return iu_break_of_any_session_strategy()
