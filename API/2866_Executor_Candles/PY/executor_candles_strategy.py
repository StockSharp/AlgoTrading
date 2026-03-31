import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class executor_candles_strategy(Strategy):
    """
    Candle pattern strategy from Executor Candles MetaTrader expert.
    Detects hammer, engulfing, piercing, morning/evening star patterns.
    Manages positions with SL/TP and trailing stop.
    """

    def __init__(self):
        super(executor_candles_strategy, self).__init__()
        self._sl_buy = self.Param("StopLossBuyPips", 50) \
            .SetDisplay("Stop Loss Buy", "Stop loss for longs", "Risk")
        self._tp_buy = self.Param("TakeProfitBuyPips", 50) \
            .SetDisplay("Take Profit Buy", "Take profit for longs", "Risk")
        self._trail_buy = self.Param("TrailingStopBuyPips", 15) \
            .SetDisplay("Trailing Stop Buy", "Trailing stop for longs", "Risk")
        self._sl_sell = self.Param("StopLossSellPips", 50) \
            .SetDisplay("Stop Loss Sell", "Stop loss for shorts", "Risk")
        self._tp_sell = self.Param("TakeProfitSellPips", 50) \
            .SetDisplay("Take Profit Sell", "Take profit for shorts", "Risk")
        self._trail_sell = self.Param("TrailingStopSellPips", 15) \
            .SetDisplay("Trailing Stop Sell", "Trailing stop for shorts", "Risk")
        self._trail_step = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step", "Minimum trailing step", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Trading timeframe", "General")

        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._entry_price = None
        self._stop_level = None
        self._take_level = None
        self._price_step = 0.0001
        self._tolerance = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(executor_candles_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._entry_price = None
        self._stop_level = None
        self._take_level = None

    def OnStarted2(self, time):
        super(executor_candles_strategy, self).OnStarted2(time)

        ps = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 0.0001
        self._price_step = ps
        self._tolerance = ps / 2.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        c = {
            "o": float(candle.OpenPrice), "h": float(candle.HighPrice),
            "l": float(candle.LowPrice), "c": float(candle.ClosePrice)
        }

        self._prev3 = self._prev2
        self._prev2 = self._prev1
        self._prev1 = c

        if self.Position == 0:
            self._try_open()

        self._manage_position(c)

    def _try_open(self):
        if self._prev1 is None or self._prev2 is None:
            return

        p1 = self._prev1
        p2 = self._prev2

        if self._is_hammer(p1, p2) or self._is_bullish_engulfing(p1, p2) or self._is_piercing(p1, p2):
            self._open_long(p1["c"])
            return

        if self._prev3 is not None:
            if self._is_morning_star(p1, self._prev2, self._prev3) or self._is_morning_doji(p1, self._prev2, self._prev3):
                self._open_long(p1["c"])
                return

        if self._is_hanging_man(p1, p2) or self._is_bearish_engulfing(p1, p2) or self._is_dark_cloud(p1, p2):
            self._open_short(p1["c"])
            return

        if self._prev3 is not None:
            if self._is_evening_star(p1, self._prev2, self._prev3) or self._is_evening_doji(p1, self._prev2, self._prev3):
                self._open_short(p1["c"])

    def _open_long(self, price):
        self.BuyMarket()
        self._entry_price = price
        ps = self._price_step
        sl = self._sl_buy.Value
        tp = self._tp_buy.Value
        self._stop_level = price - sl * ps if sl > 0 else None
        self._take_level = price + tp * ps if tp > 0 else None

    def _open_short(self, price):
        self.SellMarket()
        self._entry_price = price
        ps = self._price_step
        sl = self._sl_sell.Value
        tp = self._tp_sell.Value
        self._stop_level = price + sl * ps if sl > 0 else None
        self._take_level = price - tp * ps if tp > 0 else None

    def _manage_position(self, c):
        if self.Position > 0:
            self._handle_long(c)
        elif self.Position < 0:
            self._handle_short(c)
        elif self._stop_level is not None or self._take_level is not None:
            self._reset_state()

    def _handle_long(self, c):
        if self._stop_level is not None and c["l"] <= self._stop_level:
            self.SellMarket()
            self._reset_state()
            return
        if self._take_level is not None and c["h"] >= self._take_level:
            self.SellMarket()
            self._reset_state()
            return
        if self._entry_price is None:
            return
        trail_pips = self._trail_buy.Value
        step_pips = self._trail_step.Value
        if trail_pips <= 0 or step_pips <= 0:
            return
        ps = self._price_step
        trail_dist = trail_pips * ps
        trail_step = step_pips * ps
        progress = c["c"] - self._entry_price
        if progress <= trail_dist + trail_step:
            return
        new_stop = c["c"] - trail_dist
        if self._stop_level is None or new_stop - self._stop_level >= trail_step:
            self._stop_level = new_stop

    def _handle_short(self, c):
        if self._stop_level is not None and c["h"] >= self._stop_level:
            self.BuyMarket()
            self._reset_state()
            return
        if self._take_level is not None and c["l"] <= self._take_level:
            self.BuyMarket()
            self._reset_state()
            return
        if self._entry_price is None:
            return
        trail_pips = self._trail_sell.Value
        step_pips = self._trail_step.Value
        if trail_pips <= 0 or step_pips <= 0:
            return
        ps = self._price_step
        trail_dist = trail_pips * ps
        trail_step = step_pips * ps
        progress = self._entry_price - c["c"]
        if progress <= trail_dist + trail_step:
            return
        new_stop = c["c"] + trail_dist
        if self._stop_level is None or self._stop_level - new_stop >= trail_step:
            self._stop_level = new_stop

    def _reset_state(self):
        if self.Position == 0:
            self._entry_price = None
            self._stop_level = None
            self._take_level = None

    def _eq(self, a, b):
        return abs(a - b) <= self._tolerance

    def _is_hammer(self, cur, prev):
        if self._eq(cur["c"], cur["o"]):
            return False
        if cur["c"] > cur["o"] and prev["o"] > prev["c"]:
            body = cur["c"] - cur["o"]
            if body <= 0:
                return False
            upper = (cur["h"] - cur["c"]) * 100 / body
            lower = (cur["o"] - cur["l"]) * 100 / body
            return upper > 200 and lower < 15
        return False

    def _is_bullish_engulfing(self, cur, prev):
        if self._eq(prev["o"], prev["c"]):
            return False
        if cur["c"] > cur["o"] and prev["o"] > prev["c"]:
            if cur["c"] < prev["o"] or cur["o"] > prev["c"]:
                return False
            prev_body = prev["o"] - prev["c"]
            cur_body = cur["c"] - cur["o"]
            return prev_body != 0 and cur_body / prev_body > 1.5
        return False

    def _is_piercing(self, cur, prev):
        if self._eq(prev["h"], prev["l"]):
            return False
        if cur["c"] > cur["o"] and prev["o"] > prev["c"]:
            body = prev["o"] - prev["c"]
            rng = prev["h"] - prev["l"]
            if rng == 0:
                return False
            ratio = body / rng
            mid = prev["c"] + body / 2
            return ratio > 0.6 and cur["o"] < prev["l"] and cur["c"] > mid
        return False

    def _is_morning_star(self, cur, mid, old):
        if self._eq(old["o"], old["c"]) or self._eq(old["h"], old["l"]):
            return False
        if self._eq(mid["h"], mid["l"]) or self._eq(cur["h"], cur["l"]):
            return False
        if old["o"] > old["c"] and mid["c"] > mid["o"] and cur["c"] > cur["o"]:
            if mid["c"] >= old["c"] or cur["o"] <= mid["c"]:
                return False
            denom = old["o"] - old["c"]
            if denom == 0:
                return False
            numer = abs(old["o"] - cur["c"]) + abs(cur["o"] - old["c"])
            or1 = denom / (old["h"] - old["l"])
            mr = (mid["c"] - mid["o"]) / (mid["h"] - mid["l"])
            cr = (cur["c"] - cur["o"]) / (cur["h"] - cur["l"])
            return numer / denom < 0.1 and or1 > 0.8 and mr < 0.3 and cr > 0.8
        return False

    def _is_morning_doji(self, cur, mid, old):
        if self._eq(old["o"], old["c"]) or old["o"] <= old["c"]:
            return False
        if not self._eq(mid["c"], mid["o"]) or cur["c"] <= cur["o"]:
            return False
        if mid["c"] > old["c"] or cur["o"] < mid["c"]:
            return False
        denom = old["o"] - old["c"]
        if denom == 0:
            return False
        numer = abs(old["o"] - cur["c"]) + abs(cur["o"] - old["c"])
        return numer / denom < 0.1

    def _is_hanging_man(self, cur, prev):
        if self._eq(cur["o"], cur["c"]):
            return False
        if cur["o"] > cur["c"] and prev["o"] < prev["c"]:
            body = cur["o"] - cur["c"]
            if body <= 0:
                return False
            upper = (cur["h"] - cur["o"]) * 100 / body
            lower = (cur["c"] - cur["l"]) * 100 / body
            return upper < 15 and lower > 200
        return False

    def _is_bearish_engulfing(self, cur, prev):
        if self._eq(prev["c"], prev["o"]):
            return False
        if cur["o"] > cur["c"] and prev["c"] > prev["o"]:
            if cur["o"] < prev["c"] or cur["c"] > prev["o"]:
                return False
            prev_body = prev["c"] - prev["o"]
            cur_body = cur["o"] - cur["c"]
            return prev_body != 0 and cur_body / prev_body > 1.5
        return False

    def _is_dark_cloud(self, cur, prev):
        if self._eq(prev["h"], prev["l"]):
            return False
        if cur["o"] > cur["c"] and prev["c"] > prev["o"]:
            body = prev["c"] - prev["o"]
            rng = prev["h"] - prev["l"]
            if rng == 0:
                return False
            ratio = body / rng
            mid = prev["o"] + body / 2
            return ratio > 0.6 and cur["o"] > prev["h"] and cur["c"] < mid
        return False

    def _is_evening_star(self, cur, mid, old):
        if self._eq(old["c"], old["o"]) or self._eq(old["h"], old["l"]):
            return False
        if self._eq(cur["h"], cur["l"]):
            return False
        if old["o"] < old["c"] and mid["c"] < mid["o"] and cur["c"] < cur["o"]:
            if mid["c"] <= old["c"] or cur["o"] >= mid["c"]:
                return False
            denom = old["c"] - old["o"]
            if denom == 0:
                return False
            numer = abs(old["o"] - cur["c"]) + abs(cur["o"] - old["c"])
            or1 = denom / (old["h"] - old["l"])
            mr = (mid["o"] - mid["c"]) / (mid["h"] - mid["l"])
            cr = (cur["o"] - cur["c"]) / (cur["h"] - cur["l"])
            return numer / denom < 0.1 and or1 > 0.8 and mr < 0.3 and cr > 0.8
        return False

    def _is_evening_doji(self, cur, mid, old):
        if self._eq(old["o"], old["c"]) or old["o"] >= old["c"]:
            return False
        if not self._eq(mid["c"], mid["o"]) or cur["c"] >= cur["o"]:
            return False
        if mid["c"] < old["c"] or cur["o"] > mid["c"]:
            return False
        denom = old["o"] - old["c"]
        if denom == 0:
            return False
        numer = abs(old["o"] - cur["c"]) + abs(cur["o"] - old["c"])
        return numer / denom < 0.1

    def CreateClone(self):
        return executor_candles_strategy()
