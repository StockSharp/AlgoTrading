import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rd_trend_trigger_strategy(Strategy):
    def __init__(self):
        super(rd_trend_trigger_strategy, self).__init__()
        self._regress = self.Param("Regress", 15) \
            .SetDisplay("Regress", "Length for high/low segments", "Indicator")
        self._t3_length = self.Param("T3Length", 5) \
            .SetDisplay("T3 Length", "Tillson T3 smoothing depth", "Indicator")
        self._t3_volume_factor = self.Param("T3VolumeFactor", 0.7) \
            .SetDisplay("T3 Volume Factor", "Tillson T3 volume factor", "Indicator")
        self._high_level = self.Param("HighLevel", 50.0) \
            .SetDisplay("High Level", "Upper threshold", "Signal")
        self._low_level = self.Param("LowLevel", -50.0) \
            .SetDisplay("Low Level", "Lower threshold", "Signal")
        self._mode = self.Param("Mode", 0) \
            .SetDisplay("Mode", "Trading mode (0=Twist, 1=Disposition)", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._highs = []
        self._lows = []
        self._prev1 = None
        self._prev2 = None
        self._e1 = None
        self._e2 = None
        self._e3 = None
        self._e4 = None
        self._e5 = None
        self._e6 = None

    @property
    def regress(self):
        return self._regress.Value
    @property
    def t3_length(self):
        return self._t3_length.Value
    @property
    def t3_volume_factor(self):
        return self._t3_volume_factor.Value
    @property
    def high_level(self):
        return self._high_level.Value
    @property
    def low_level(self):
        return self._low_level.Value
    @property
    def mode(self):
        return self._mode.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rd_trend_trigger_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev1 = None
        self._prev2 = None
        self._e1 = None
        self._e2 = None
        self._e3 = None
        self._e4 = None
        self._e5 = None
        self._e6 = None

    def _update_ema(self, prev, inp, length):
        alpha = 2.0 / (float(length) + 1.0)
        if prev is None:
            return inp
        return alpha * inp + (1.0 - alpha) * prev

    def OnStarted(self, time):
        super(rd_trend_trigger_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        reg = int(self.regress)
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        max_count = reg * 2
        if len(self._highs) > max_count:
            self._highs.pop(0)
            self._lows.pop(0)
        if len(self._highs) < max_count:
            return

        highest_recent = max(self._highs[reg:])
        highest_older = max(self._highs[:reg])
        lowest_recent = min(self._lows[reg:])
        lowest_older = min(self._lows[:reg])

        buy_power = highest_recent - lowest_older
        sell_power = highest_older - lowest_recent
        res = buy_power + sell_power
        ttf = 0.0 if res == 0 else (buy_power - sell_power) / (0.5 * res) * 100.0

        t3l = int(self.t3_length)
        self._e1 = self._update_ema(self._e1, ttf, t3l)
        self._e2 = self._update_ema(self._e2, self._e1, t3l)
        self._e3 = self._update_ema(self._e3, self._e2, t3l)
        self._e4 = self._update_ema(self._e4, self._e3, t3l)
        self._e5 = self._update_ema(self._e5, self._e4, t3l)
        self._e6 = self._update_ema(self._e6, self._e5, t3l)

        v = float(self.t3_volume_factor)
        c1 = -v * v * v
        c2 = 3 * v * v + 3 * v * v * v
        c3 = -6 * v * v - 3 * v - 3 * v * v * v
        c4 = 1 + 3 * v + v * v * v + 3 * v * v
        t3 = c1 * self._e6 + c2 * self._e5 + c3 * self._e4 + c4 * self._e3

        if int(self.mode) == 0:  # Twist
            if self._prev2 is not None and self._prev1 is not None:
                if t3 > self._prev1 and self._prev1 <= self._prev2 and self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
                elif t3 < self._prev1 and self._prev1 >= self._prev2 and self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
            self._prev2 = self._prev1
            self._prev1 = t3
        else:  # Disposition
            hl = float(self.high_level)
            ll = float(self.low_level)
            if self._prev1 is not None:
                if t3 > hl and self._prev1 <= hl and self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
                elif t3 < ll and self._prev1 >= ll and self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
                elif t3 > ll and self.Position < 0:
                    self.BuyMarket()
            self._prev1 = t3

    def CreateClone(self):
        return rd_trend_trigger_strategy()
