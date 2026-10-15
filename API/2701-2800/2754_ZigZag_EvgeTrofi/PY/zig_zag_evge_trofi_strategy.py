import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


# Pivot type constants
PIVOT_NONE = 0
PIVOT_HIGH = 1
PIVOT_LOW = 2


class zig_zag_evge_trofi_strategy(Strategy):
    def __init__(self):
        super(zig_zag_evge_trofi_strategy, self).__init__()

        self._depth = self.Param("Depth", 17)
        self._deviation = self.Param("Deviation", 7.0)
        self._backstep = self.Param("Backstep", 5)
        self._urgency = self.Param("Urgency", 2)
        self._signal_reverse = self.Param("SignalReverse", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._highest = None
        self._lowest = None
        self._pivot_type = PIVOT_NONE
        self._pivot_price = 0.0
        self._bars_since_pivot = 999999
        self._price_step = 1.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Depth(self):
        return self._depth.Value

    @property
    def Deviation(self):
        return self._deviation.Value

    @property
    def Backstep(self):
        return self._backstep.Value

    @property
    def Urgency(self):
        return self._urgency.Value

    @property
    def SignalReverse(self):
        return self._signal_reverse.Value

    def OnStarted2(self, time):
        super(zig_zag_evge_trofi_strategy, self).OnStarted2(time)

        sec = self.Security
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            self._price_step = float(sec.PriceStep)
        else:
            self._price_step = 1.0

        self._highest = Highest()
        self._highest.Length = self.Depth
        self._lowest = Lowest()
        self._lowest.Length = self.Depth

        self._pivot_type = PIVOT_NONE
        self._pivot_price = 0.0
        self._bars_since_pivot = 999999

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._highest, self._lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_v, lowest_v):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        if self._pivot_type != PIVOT_NONE and self._bars_since_pivot < 999999:
            self._bars_since_pivot += 1

        hv = float(highest_v)
        lv = float(lowest_v)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        deviation_price = max(self.Deviation * self._price_step, self._price_step)
        can_switch = self._pivot_type == PIVOT_NONE or self._bars_since_pivot >= self.Backstep

        if high >= hv and hv > 0:
            difference = high - self._pivot_price
            if (self._pivot_type != PIVOT_HIGH and can_switch) or (self._pivot_type == PIVOT_HIGH and difference >= deviation_price):
                self._set_pivot(PIVOT_HIGH, high)
        elif low <= lv and lv > 0:
            difference = self._pivot_price - low
            if (self._pivot_type != PIVOT_LOW and can_switch) or (self._pivot_type == PIVOT_LOW and difference >= deviation_price):
                self._set_pivot(PIVOT_LOW, low)

        if self._pivot_type == PIVOT_NONE:
            return

        is_buy_signal = (self._pivot_type != PIVOT_HIGH) if self.SignalReverse else (self._pivot_type == PIVOT_HIGH)

        if is_buy_signal:
            if self.Position < 0:
                self.BuyMarket()
        else:
            if self.Position > 0:
                self.SellMarket()

        if self._bars_since_pivot > self.Urgency:
            return

        if is_buy_signal:
            self.BuyMarket()
        else:
            self.SellMarket()

    def _set_pivot(self, pivot_type, price):
        self._pivot_type = pivot_type
        self._pivot_price = price
        self._bars_since_pivot = 0

    def OnReseted(self):
        super(zig_zag_evge_trofi_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._pivot_type = PIVOT_NONE
        self._pivot_price = 0.0
        self._bars_since_pivot = 999999
        self._price_step = 1.0

    def CreateClone(self):
        return zig_zag_evge_trofi_strategy()
