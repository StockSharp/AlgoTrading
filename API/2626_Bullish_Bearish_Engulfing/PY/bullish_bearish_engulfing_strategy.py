import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class bullish_bearish_engulfing_strategy(Strategy):
    """Engulfing pattern strategy that reacts to bullish and bearish engulfing candles."""

    def __init__(self):
        super(bullish_bearish_engulfing_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")
        self._shift = self.Param("Shift", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift", "Number of completed candles to skip", "Pattern")
        self._distance_pips = self.Param("DistanceInPips", 0.0) \
            .SetDisplay("Distance (pips)", "Additional filter in pips", "Pattern")
        self._close_opposite = self.Param("CloseOpposite", True) \
            .SetDisplay("Close Opposite", "Close opposite position before entering", "Risk")

        self._candles = []

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def Shift(self):
        return self._shift.Value
    @property
    def DistanceInPips(self):
        return self._distance_pips.Value
    @property
    def CloseOpposite(self):
        return self._close_opposite.Value

    def OnStarted(self, time):
        super(bullish_bearish_engulfing_strategy, self).OnStarted(time)

        self._candles = []
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        self._candles.append((o, h, lo, c))

        max_count = max(self.Shift + 2, 3)
        while len(self._candles) > max_count:
            self._candles.pop(0)

        if len(self._candles) < self.Shift + 1:
            return

        idx = len(self._candles) - self.Shift
        if idx <= 0:
            return
        prev_idx = idx - 1
        if prev_idx < 0:
            return

        cur = self._candles[idx]
        prev = self._candles[prev_idx]
        dist = self._calc_distance()

        # Bullish engulfing
        is_bullish = (cur[3] > cur[0] and prev[0] > prev[3] and
                      cur[1] > prev[1] + dist and
                      cur[3] > prev[0] + dist and
                      cur[0] < prev[3] - dist and
                      cur[2] < prev[2] - dist)

        if is_bullish:
            self._enter_long()
            return

        # Bearish engulfing
        is_bearish = (cur[0] > cur[3] and prev[0] < prev[3] and
                      cur[1] > prev[1] + dist and
                      cur[0] > prev[3] + dist and
                      cur[3] < prev[0] - dist and
                      cur[2] < prev[2] - dist)

        if is_bearish:
            self._enter_short()

    def _enter_long(self):
        if self.Position > 0:
            return
        if self.Position < 0:
            if not self.CloseOpposite:
                return
            self.BuyMarket()
        self.BuyMarket()

    def _enter_short(self):
        if self.Position < 0:
            return
        if self.Position > 0:
            if not self.CloseOpposite:
                return
            self.SellMarket()
        self.SellMarket()

    def _calc_distance(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 0.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 0
        mult = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return float(self.DistanceInPips) * step * mult

    def OnReseted(self):
        super(bullish_bearish_engulfing_strategy, self).OnReseted()
        self._candles = []

    def CreateClone(self):
        return bullish_bearish_engulfing_strategy()
