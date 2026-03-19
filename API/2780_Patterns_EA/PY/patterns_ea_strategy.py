import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class patterns_ea_strategy(Strategy):
    def __init__(self):
        super(patterns_ea_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Type of candles for pattern search", "General")
        self._equality_pips = self.Param("EqualityPips", 1.0).SetNotNegative().SetDisplay("Equality Pips", "Max pip distance to treat prices as equal", "Detection")
        self.Volume = 1

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(patterns_ea_strategy, self).OnReseted()
        self._current = None
        self._previous = None
        self._previous2 = None

    def OnStarted(self, time):
        super(patterns_ea_strategy, self).OnStarted(time)
        self._current = None
        self._previous = None
        self._previous2 = None

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def _candle_info(self, candle):
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        body_top = max(o, c)
        body_bottom = min(o, c)
        body_size = body_top - body_bottom
        upper_shadow = h - body_top
        lower_shadow = body_bottom - l
        return {"o": o, "c": c, "h": h, "l": l, "bt": body_top, "bb": body_bottom, "bs": body_size, "us": upper_shadow, "ls": lower_shadow}

    def _compare(self, a, b, tol):
        diff = a - b
        if abs(diff) < tol:
            return 0
        return 1 if diff > 0 else -1

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._previous2 = self._previous
        self._previous = self._current
        self._current = self._candle_info(candle)

        self._evaluate_patterns()

    def _evaluate_patterns(self):
        if self._current is None:
            return

        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        eq = self._equality_pips.Value * step
        min_dev = max(eq, step)
        min_dev4 = max(eq * 4, step * 4)

        c0 = self._current
        cmp = self._compare

        # One-bar: Hammer -> Buy, ShootingStar -> Sell
        if c0["us"] <= min_dev and c0["ls"] > 2 * c0["bs"]:
            self.BuyMarket()
            return
        if c0["ls"] <= min_dev and c0["us"] > 2 * c0["bs"]:
            self.SellMarket()
            return

        if self._previous is None:
            return

        c1 = self._previous

        # Two-bar: Engulfing Bullish -> Buy
        if cmp(c1["c"], c1["o"], eq) < 0 and cmp(c0["c"], c0["o"], eq) > 0 and cmp(c0["o"], c1["c"], eq) < 0 and cmp(c0["c"], c1["o"], eq) > 0:
            self.BuyMarket()
            return

        # Two-bar: Engulfing Bearish -> Sell
        if cmp(c1["c"], c1["o"], eq) > 0 and cmp(c0["c"], c0["o"], eq) < 0 and cmp(c0["o"], c1["c"], eq) > 0 and cmp(c0["c"], c1["o"], eq) < 0:
            self.SellMarket()
            return

        if self._previous2 is None:
            return

        c2 = self._previous2

        # Three-bar: Morning Star -> Buy
        if cmp(c2["c"], c2["o"], eq) < 0 and cmp(c1["c"], c1["o"], eq) > 0 and cmp(c0["c"], c0["o"], eq) > 0:
            if cmp(c2["c"], c1["o"], eq) > 0 and cmp(c2["bs"], c1["bs"], eq) > 0 and cmp(c1["bs"], c0["bs"], eq) < 0:
                if cmp(c0["c"], c2["c"], eq) > 0 and cmp(c0["c"], c2["o"], eq) < 0:
                    self.BuyMarket()
                    return

        # Three-bar: Evening Star -> Sell
        if cmp(c2["c"], c2["o"], eq) > 0 and cmp(c1["c"], c1["o"], eq) > 0 and cmp(c0["c"], c0["o"], eq) < 0:
            if cmp(c2["c"], c1["o"], eq) < 0 and cmp(c2["bs"], c1["bs"], eq) > 0 and cmp(c1["bs"], c0["bs"], eq) < 0:
                if cmp(c0["c"], c2["o"], eq) > 0 and cmp(c0["c"], c2["c"], eq) < 0:
                    self.SellMarket()
                    return

    def CreateClone(self):
        return patterns_ea_strategy()
