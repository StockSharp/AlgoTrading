import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class eugene_candle_pattern_strategy(Strategy):
    """
    Candle pattern strategy with breakout confirmation.
    Detects bullish/bearish setup patterns and trades breakouts with SL/TP management.
    """

    def __init__(self):
        super(eugene_candle_pattern_strategy, self).__init__()
        self._sl = self.Param("StopLossPoints", 500) \
            .SetDisplay("Stop Loss (points)", "Stop loss in price steps", "Risk")
        self._tp = self.Param("TakeProfitPoints", 800) \
            .SetDisplay("Take Profit (points)", "Take profit in price steps", "Risk")
        self._inv = self.Param("InvertSignals", False) \
            .SetDisplay("Invert Signals", "Swap buy and sell signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Bars to wait after position change", "Trading")
        self._min_body_percent = self.Param("MinBodyPercent", 0.0015) \
            .SetDisplay("Minimum Body %", "Min candle body size relative to close", "Filters")

        self._recent = [None, None, None, None]
        self._stop = 0.0
        self._take = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eugene_candle_pattern_strategy, self).OnReseted()
        self._recent = [None, None, None, None]
        self._stop = 0.0
        self._take = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(eugene_candle_pattern_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        self._check_stops(candle)

        self._recent[3] = self._recent[2]
        self._recent[2] = self._recent[1]
        self._recent[1] = self._recent[0]
        self._recent[0] = {
            "open": float(candle.OpenPrice),
            "close": float(candle.ClosePrice),
            "high": float(candle.HighPrice),
            "low": float(candle.LowPrice)
        }

        if self._recent[3] is None:
            return

        open_buy, open_sell, close_buy, close_sell = self._compute_signals()

        if self._inv.Value:
            open_buy, open_sell = open_sell, open_buy
            close_buy, close_sell = close_sell, close_buy

        if self.Position > 0 and close_buy:
            self._close_position()
        elif self.Position < 0 and close_sell:
            self._close_position()

        if self._cooldown_remaining > 0:
            return

        if self.Position <= 0 and open_buy and not open_sell:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._set_stops(self._recent[0]["close"], True)
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position >= 0 and open_sell and not open_buy:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._set_stops(self._recent[0]["close"], False)
            self._cooldown_remaining = self._cooldown_bars.Value

    def _compute_signals(self):
        current = self._recent[0]
        prev = self._recent[1]
        prev2 = self._recent[2]

        prev_body = abs(prev["close"] - prev["open"])
        current_body = abs(current["close"] - current["open"])
        prev_body_pct = prev_body / prev["close"] if prev["close"] != 0 else 0
        current_body_pct = current_body / current["close"] if current["close"] != 0 else 0

        min_body = float(self._min_body_percent.Value)

        bullish_setup = prev["close"] < prev["open"] and prev["low"] > prev2["low"]
        bearish_setup = prev["close"] > prev["open"] and prev["high"] < prev2["high"]
        bullish_breakout = current["close"] > prev["high"] and current["close"] > current["open"]
        bearish_breakout = current["close"] < prev["low"] and current["close"] < current["open"]

        open_buy = bullish_setup and bullish_breakout and prev_body_pct >= min_body and current_body_pct >= min_body
        open_sell = bearish_setup and bearish_breakout and prev_body_pct >= min_body and current_body_pct >= min_body
        close_buy = current["close"] < prev["low"]
        close_sell = current["close"] > prev["high"]

        return open_buy, open_sell, close_buy, close_sell

    def _set_stops(self, price, long_pos):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl_pts = self._sl.Value
        tp_pts = self._tp.Value

        if long_pos:
            self._stop = price - step * sl_pts if sl_pts > 0 else 0.0
            self._take = price + step * tp_pts if tp_pts > 0 else 0.0
        else:
            self._stop = price + step * sl_pts if sl_pts > 0 else 0.0
            self._take = price - step * tp_pts if tp_pts > 0 else 0.0

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._stop = 0.0
        self._take = 0.0
        self._cooldown_remaining = self._cooldown_bars.Value

    def _check_stops(self, candle):
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        if self.Position > 0:
            if (self._stop != 0 and low <= self._stop) or (self._take != 0 and high >= self._take):
                self._close_position()
        elif self.Position < 0:
            if (self._stop != 0 and high >= self._stop) or (self._take != 0 and low <= self._take):
                self._close_position()

    def CreateClone(self):
        return eugene_candle_pattern_strategy()
