import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class bill_williams_strategy(Strategy):
    def __init__(self):
        super(bill_williams_strategy, self).__init__()
        self._filter_pct = self.Param("FilterPct", 0.05) \
            .SetDisplay("Filter %", "Minimal price offset as percentage", "General")
        self._gator_div_slow_pct = self.Param("GatorDivSlowPct", 0.3) \
            .SetDisplay("Jaw-Teeth %", "Required jaw-teeth distance as % of price", "Alligator")
        self._gator_div_fast_pct = self.Param("GatorDivFastPct", 0.15) \
            .SetDisplay("Lips-Teeth %", "Required lips-teeth distance as % of price", "Alligator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = []
        self._lows = []
        self._fractal_up = None
        self._fractal_down = None
        self._jaw = None
        self._teeth = None
        self._lips = None

    @property
    def filter_pct(self):
        return self._filter_pct.Value

    @property
    def gator_div_slow_pct(self):
        return self._gator_div_slow_pct.Value

    @property
    def gator_div_fast_pct(self):
        return self._gator_div_fast_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bill_williams_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._fractal_up = None
        self._fractal_down = None
        self._jaw = None
        self._teeth = None
        self._lips = None

    def OnStarted2(self, time):
        super(bill_williams_strategy, self).OnStarted2(time)
        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5
        self.Indicators.Add(self._jaw)
        self.Indicators.Add(self._teeth)
        self.Indicators.Add(self._lips)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > 5:
            self._highs.pop(0)
        if len(self._lows) > 5:
            self._lows.pop(0)

        if len(self._highs) == 5:
            hs = self._highs
            if hs[2] > hs[0] and hs[2] > hs[1] and hs[2] > hs[3] and hs[2] > hs[4]:
                self._fractal_up = hs[2]

        if len(self._lows) == 5:
            ls = self._lows
            if ls[2] < ls[0] and ls[2] < ls[1] and ls[2] < ls[3] and ls[2] < ls[4]:
                self._fractal_down = ls[2]

        median = (candle.HighPrice + candle.LowPrice) / 2
        t = candle.OpenTime

        jaw_val = process_float(self._jaw, median, t, True)

        teeth_val = process_float(self._teeth, median, t, True)

        lips_val = process_float(self._lips, median, t, True)

        if not jaw_val.IsFormed or not teeth_val.IsFormed or not lips_val.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        jaw = float(jaw_val)
        teeth = float(teeth_val)
        lips = float(lips_val)

        price = float(candle.ClosePrice)
        filt = float(self.filter_pct) / 100.0 * price
        slow_threshold = float(self.gator_div_slow_pct) / 100.0 * price
        fast_threshold = float(self.gator_div_fast_pct) / 100.0 * price

        slow_diff = abs(jaw - teeth)
        fast_diff = abs(lips - teeth)
        alligator_open = slow_diff >= slow_threshold and fast_diff >= fast_threshold

        if (self.Position <= 0 and alligator_open and self._fractal_up is not None and
                float(candle.HighPrice) >= self._fractal_up + filt and
                float(candle.ClosePrice) > float(candle.OpenPrice) and self._fractal_up >= teeth):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif (self.Position >= 0 and alligator_open and self._fractal_down is not None and
                float(candle.LowPrice) <= self._fractal_down - filt and
                float(candle.ClosePrice) < float(candle.OpenPrice) and self._fractal_down <= teeth):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        if self.Position > 0 and self._fractal_down is not None and float(candle.ClosePrice) <= self._fractal_down - filt:
            self.SellMarket()
        elif self.Position < 0 and self._fractal_up is not None and float(candle.ClosePrice) >= self._fractal_up + filt:
            self.BuyMarket()

    def CreateClone(self):
        return bill_williams_strategy()
