import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest

class zig_zag_evge_trofi1_strategy(Strategy):
    PIVOT_NONE = 0
    PIVOT_HIGH = 1
    PIVOT_LOW = 2

    def __init__(self):
        super(zig_zag_evge_trofi1_strategy, self).__init__()

        self._depth = self.Param("Depth", 17) \
            .SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
        self._deviation = self.Param("Deviation", 7.0) \
            .SetDisplay("Deviation", "Minimum price movement in points", "ZigZag")
        self._backstep = self.Param("Backstep", 5) \
            .SetDisplay("Backstep", "Bars to wait before switching pivots", "ZigZag")
        self._urgency = self.Param("Urgency", 2) \
            .SetDisplay("Urgency", "Maximum bars to trade the latest pivot", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._volume_param = self.Param("VolumePerTrade", 1.0) \
            .SetDisplay("Volume", "Order volume per trade", "Trading")

        self._pivot_type = self.PIVOT_NONE
        self._pivot_price = 0.0
        self._bars_since_pivot = 999999
        self._signal_handled = True
        self._price_step = 0.0

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
    def CandleType(self):
        return self._candle_type.Value

    @property
    def VolumePerTrade(self):
        return self._volume_param.Value

    def OnStarted2(self, time):
        super(zig_zag_evge_trofi1_strategy, self).OnStarted2(time)

        self._price_step = self._get_effective_price_step()

        self._highest = Highest()
        self._highest.Length = self.Depth
        self._lowest = Lowest()
        self._lowest.Length = self.Depth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._highest, self._lowest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        highest_value = float(highest_value)
        lowest_value = float(lowest_value)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        if self._pivot_type != self.PIVOT_NONE and self._bars_since_pivot < 999999:
            self._bars_since_pivot += 1

        deviation_price = self._get_deviation_price()
        can_switch = self._pivot_type == self.PIVOT_NONE or self._bars_since_pivot >= self.Backstep

        if high_price >= highest_value and highest_value > 0:
            difference = high_price - self._pivot_price
            if (self._pivot_type != self.PIVOT_HIGH and can_switch) or \
               (self._pivot_type == self.PIVOT_HIGH and difference >= deviation_price):
                self._set_pivot(self.PIVOT_HIGH, high_price)
        elif low_price <= lowest_value and lowest_value > 0:
            difference = self._pivot_price - low_price
            if (self._pivot_type != self.PIVOT_LOW and can_switch) or \
               (self._pivot_type == self.PIVOT_LOW and difference >= deviation_price):
                self._set_pivot(self.PIVOT_LOW, low_price)

        if self._pivot_type == self.PIVOT_NONE:
            return

        if self._bars_since_pivot > self.Urgency:
            return

        if self._signal_handled:
            return

        volume = float(self.VolumePerTrade)
        if volume <= 0:
            self._signal_handled = True
            return

        is_buy_signal = self._pivot_type == self.PIVOT_HIGH

        if is_buy_signal:
            if self.Position > 0:
                self._signal_handled = True
                return
        else:
            if self.Position < 0:
                self._signal_handled = True
                return

        if is_buy_signal:
            if self.Position < 0:
                close_vol = abs(self.Position)
                if close_vol > 0:
                    self.BuyMarket(close_vol)
            self.BuyMarket(volume)
        else:
            if self.Position > 0:
                close_vol = abs(self.Position)
                if close_vol > 0:
                    self.SellMarket(close_vol)
            self.SellMarket(volume)

        self._signal_handled = True

    def _set_pivot(self, pivot_type, price):
        self._pivot_type = pivot_type
        self._pivot_price = price
        self._bars_since_pivot = 0
        self._signal_handled = False

    def _get_deviation_price(self):
        step = self._price_step if self._price_step > 0 else 1.0
        deviation = float(self.Deviation)
        if deviation <= 0:
            return step
        value = deviation * step
        return value if value >= step else step

    def _get_effective_price_step(self):
        if self.Security is not None:
            ps = self.Security.PriceStep
            if ps is not None and float(ps) > 0:
                return float(ps)
        return 1.0

    def OnReseted(self):
        super(zig_zag_evge_trofi1_strategy, self).OnReseted()
        self._pivot_type = self.PIVOT_NONE
        self._pivot_price = 0.0
        self._bars_since_pivot = 999999
        self._signal_handled = True
        self._price_step = 0.0

    def CreateClone(self):
        return zig_zag_evge_trofi1_strategy()
