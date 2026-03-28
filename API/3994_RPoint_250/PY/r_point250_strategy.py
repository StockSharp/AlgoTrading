import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest

class r_point250_strategy(Strategy):
    def __init__(self):
        super(r_point250_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Base volume for market entries", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0) \
            .SetDisplay("Take Profit Points", "Take profit distance in price points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 999.0) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price points", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0.0) \
            .SetDisplay("Trailing Stop Points", "Optional trailing distance in price points", "Risk")
        self._reverse_point = self.Param("ReversePoint", 250) \
            .SetDisplay("Reverse Point Length", "Number of candles scanned for reversal levels", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle aggregation used for calculations", "General")

        self._last_high_level = 0.0
        self._last_low_level = 0.0
        self._executed_high_level = 0.0
        self._executed_low_level = 0.0
        self._last_signal_time = None
        self._price_step = 1.0
        self._trailing_distance = 0.0
        self._best_long_price = None
        self._best_short_price = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def ReversePoint(self):
        return self._reverse_point.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(r_point250_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = max(1, self.ReversePoint)
        lowest = Lowest()
        lowest.Length = max(1, self.ReversePoint)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._price_step = float(ps) if ps is not None else 1.0
        if self._price_step <= 0:
            self._price_step = 1.0

        tp_pts = float(self.TakeProfitPoints)
        sl_pts = float(self.StopLossPoints)
        trail_pts = float(self.TrailingStopPoints)

        take_dist = self._price_step * tp_pts if tp_pts > 0 else 0.0
        stop_dist = self._price_step * sl_pts if sl_pts > 0 else 0.0
        self._trailing_distance = self._price_step * trail_pts if trail_pts > 0 else 0.0

        tp = Unit(take_dist, UnitTypes.Absolute) if take_dist > 0 else None
        sl = Unit(stop_dist, UnitTypes.Absolute) if stop_dist > 0 else None
        if tp is not None or sl is not None:
            self.StartProtection(tp, sl)

        self._highest = highest
        self._lowest = lowest

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        highest_value = float(highest_value)
        lowest_value = float(lowest_value)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        close_price = float(candle.ClosePrice)

        if highest_value == high_price and highest_value != self._last_high_level:
            self._last_high_level = highest_value

        if lowest_value == low_price and lowest_value != self._last_low_level:
            self._last_low_level = lowest_value

        if self.Position > 0:
            if self._best_long_price is None or high_price > self._best_long_price:
                self._best_long_price = high_price

            if (self._trailing_distance > 0 and self._best_long_price is not None
                    and self._best_long_price - low_price >= self._trailing_distance):
                self.SellMarket(self.Position)
                self._best_long_price = None
                return

            if self._last_high_level != 0 and self._last_high_level != self._executed_high_level:
                self.SellMarket(self.Position)
                self._best_long_price = None
                return

        elif self.Position < 0:
            if self._best_short_price is None or low_price < self._best_short_price:
                self._best_short_price = low_price

            if (self._trailing_distance > 0 and self._best_short_price is not None
                    and high_price - self._best_short_price >= self._trailing_distance):
                self.BuyMarket(-self.Position)
                self._best_short_price = None
                return

            if self._last_low_level != 0 and self._last_low_level != self._executed_low_level:
                self.BuyMarket(-self.Position)
                self._best_short_price = None
                return
        else:
            self._best_long_price = None
            self._best_short_price = None

            ov = float(self.OrderVolume)
            if ov <= 0:
                return

            if self._last_signal_time == candle.OpenTime:
                return

            if self._last_high_level != 0 and self._last_high_level != self._executed_high_level:
                self.SellMarket(ov)
                self._executed_high_level = self._last_high_level
                self._last_signal_time = candle.OpenTime
                self._best_short_price = close_price
                return

            if self._last_low_level != 0 and self._last_low_level != self._executed_low_level:
                self.BuyMarket(ov)
                self._executed_low_level = self._last_low_level
                self._last_signal_time = candle.OpenTime
                self._best_long_price = close_price

    def OnReseted(self):
        super(r_point250_strategy, self).OnReseted()
        self._last_high_level = 0.0
        self._last_low_level = 0.0
        self._executed_high_level = 0.0
        self._executed_low_level = 0.0
        self._last_signal_time = None
        self._best_long_price = None
        self._best_short_price = None

    def CreateClone(self):
        return r_point250_strategy()
