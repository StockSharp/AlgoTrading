import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class e_rp250_strategy(Strategy):
    def __init__(self):
        super(e_rp250_strategy, self).__init__()

        self._take_profit_points = self.Param("TakeProfitPoints", 15.0)
        self._stop_loss_points = self.Param("StopLossPoints", 999.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0.0)
        self._reverse_point = self.Param("ReversePoint", 400)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._latest_high_signal = 0.0
        self._latest_low_signal = 0.0
        self._last_executed_high = 0.0
        self._last_executed_low = 0.0
        self._last_signal_time = None
        self._best_long_price = None
        self._best_short_price = None
        self._trailing_distance = 0.0

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def ReversePoint(self):
        return self._reverse_point.Value

    @ReversePoint.setter
    def ReversePoint(self, value):
        self._reverse_point.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(e_rp250_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self.ReversePoint
        self._lowest = Lowest()
        self._lowest.Length = self.ReversePoint

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        take_distance = step * float(self.TakeProfitPoints) if float(self.TakeProfitPoints) > 0.0 else 0.0
        stop_distance = step * float(self.StopLossPoints) if float(self.StopLossPoints) > 0.0 else 0.0
        self._trailing_distance = step * float(self.TrailingStopPoints) if float(self.TrailingStopPoints) > 0.0 else 0.0

        tp_unit = Unit(take_distance, UnitTypes.Absolute) if take_distance > 0.0 else Unit()
        sl_unit = Unit(stop_distance, UnitTypes.Absolute) if stop_distance > 0.0 else Unit()
        self.StartProtection(tp_unit, sl_unit)

        self._latest_high_signal = 0.0
        self._latest_low_signal = 0.0
        self._last_executed_high = 0.0
        self._last_executed_low = 0.0
        self._last_signal_time = None
        self._best_long_price = None
        self._best_short_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        high_result = process_float(self._highest, candle.HighPrice, candle.OpenTime, True)

        low_result = process_float(self._lowest, candle.LowPrice, candle.OpenTime, True)

        if high_result.IsEmpty or low_result.IsEmpty:
            return

        high_value = float(high_result)
        low_value = float(low_result)

        if abs(high_value - high) < 1e-10:
            self._latest_high_signal = high

        if abs(low_value - low) < 1e-10:
            self._latest_low_signal = low

        # Manage existing long position
        if self.Position > 0:
            if self._best_long_price is None or high > self._best_long_price:
                self._best_long_price = high

            if self._trailing_distance > 0.0 and self._best_long_price is not None and self._best_long_price - close >= self._trailing_distance:
                self.SellMarket()
                self._best_long_price = None
                return

            if self._latest_high_signal != 0.0 and self._latest_high_signal != self._last_executed_high:
                self.SellMarket()
                self._best_long_price = None
                return

        elif self.Position < 0:
            if self._best_short_price is None or low < self._best_short_price:
                self._best_short_price = low

            if self._trailing_distance > 0.0 and self._best_short_price is not None and close - self._best_short_price >= self._trailing_distance:
                self.BuyMarket()
                self._best_short_price = None
                return

            if self._latest_low_signal != 0.0 and self._latest_low_signal != self._last_executed_low:
                self.BuyMarket()
                self._best_short_price = None
                return
        else:
            self._best_long_price = None
            self._best_short_price = None

        if self.Position != 0:
            return

        # Avoid placing more than one order within the same candle
        if self._last_signal_time is not None and self._last_signal_time == candle.OpenTime:
            return

        # Short on fresh reversal high
        if self._latest_high_signal != 0.0 and self._latest_high_signal != self._last_executed_high:
            self.SellMarket()
            self._last_executed_high = self._latest_high_signal
            self._last_signal_time = candle.OpenTime
            self._best_short_price = close
            self._best_long_price = None
            return

        # Long on fresh reversal low
        if self._latest_low_signal != 0.0 and self._latest_low_signal != self._last_executed_low:
            self.BuyMarket()
            self._last_executed_low = self._latest_low_signal
            self._last_signal_time = candle.OpenTime
            self._best_long_price = close
            self._best_short_price = None

    def OnReseted(self):
        super(e_rp250_strategy, self).OnReseted()
        self._latest_high_signal = 0.0
        self._latest_low_signal = 0.0
        self._last_executed_high = 0.0
        self._last_executed_low = 0.0
        self._last_signal_time = None
        self._best_long_price = None
        self._best_short_price = None
        self._trailing_distance = 0.0

    def CreateClone(self):
        return e_rp250_strategy()
