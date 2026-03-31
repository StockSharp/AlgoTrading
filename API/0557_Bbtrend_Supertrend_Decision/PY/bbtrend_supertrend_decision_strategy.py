import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bbtrend_supertrend_decision_strategy(Strategy):
    def __init__(self):
        super(bbtrend_supertrend_decision_strategy, self).__init__()
        self._short_bb_length = self.Param("ShortBbLength", 20) \
            .SetDisplay("Short BB Length", "Short Bollinger Bands length", "BBTrend")
        self._long_bb_length = self.Param("LongBbLength", 50) \
            .SetDisplay("Long BB Length", "Long Bollinger Bands length", "BBTrend")
        self._std_dev = self.Param("StdDev", 2.0) \
            .SetDisplay("Std Dev", "Standard deviation", "BBTrend")
        self._supertrend_length = self.Param("SupertrendLength", 10) \
            .SetDisplay("ST Length", "SuperTrend ATR period", "SuperTrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 12.0) \
            .SetDisplay("ST Factor", "SuperTrend multiplier", "SuperTrend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_bb_trend = None
        self._prev_up = None
        self._prev_dn = None
        self._prev_atr = None
        self._prev_st = None

    @property
    def short_bb_length(self):
        return self._short_bb_length.Value
    @property
    def long_bb_length(self):
        return self._long_bb_length.Value
    @property
    def std_dev(self):
        return self._std_dev.Value
    @property
    def supertrend_length(self):
        return self._supertrend_length.Value
    @property
    def supertrend_multiplier(self):
        return self._supertrend_multiplier.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bbtrend_supertrend_decision_strategy, self).OnReseted()
        self._previous_bb_trend = None
        self._prev_up = None
        self._prev_dn = None
        self._prev_atr = None
        self._prev_st = None

    def OnStarted2(self, time):
        super(bbtrend_supertrend_decision_strategy, self).OnStarted2(time)
        short_bb = BollingerBands()
        short_bb.Length = self.short_bb_length
        short_bb.Width = self.std_dev
        long_bb = BollingerBands()
        long_bb.Length = self.long_bb_length
        long_bb.Width = self.std_dev
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_bb, long_bb, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_bb)
            self.DrawIndicator(area, long_bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, short_bb_val, long_bb_val):
        if candle.State != CandleStates.Finished:
            return

        short_upper = short_bb_val.UpBand
        short_lower = short_bb_val.LowBand
        short_middle = short_bb_val.MovingAverage
        if short_upper is None or short_lower is None or short_middle is None:
            return

        long_upper = long_bb_val.UpBand
        long_lower = long_bb_val.LowBand
        if long_upper is None or long_lower is None:
            return

        bb_trend = (abs(float(short_lower) - float(long_lower)) - abs(float(short_upper) - float(long_upper))) / float(short_middle) * 100.0

        if self._previous_bb_trend is None:
            self._previous_bb_trend = bb_trend
            return

        open_val = self._previous_bb_trend
        close_val = bb_trend
        high = max(open_val, close_val)
        low = min(open_val, close_val)

        tr = max(max(high - low, abs(high - open_val)), abs(low - open_val))
        atr = tr if self._prev_atr is None else self._prev_atr + (tr - self._prev_atr) / self.supertrend_length

        hl2 = (high + low) / 2.0
        up = hl2 + self.supertrend_multiplier * atr
        if self._prev_up is not None and not (up < self._prev_up or open_val > self._prev_up):
            up = self._prev_up
        dn = hl2 - self.supertrend_multiplier * atr
        if self._prev_dn is not None and not (dn > self._prev_dn or open_val < self._prev_dn):
            dn = self._prev_dn

        if self._prev_atr is None:
            direction = 1
        elif self._prev_st is not None and self._prev_up is not None and self._prev_st == self._prev_up:
            direction = -1 if close_val > up else 1
        else:
            direction = 1 if close_val < dn else -1

        st = dn if direction == -1 else up

        if direction < 0 and self.Position <= 0:
            self.BuyMarket()
        elif direction > 0 and self.Position >= 0:
            self.SellMarket()

        self._previous_bb_trend = close_val
        self._prev_atr = atr
        self._prev_up = up
        self._prev_dn = dn
        self._prev_st = st

    def CreateClone(self):
        return bbtrend_supertrend_decision_strategy()
