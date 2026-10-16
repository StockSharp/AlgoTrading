import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, DayOfWeek, Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Strategies import Strategy


class e_friday_session_strategy(Strategy):
    """One contrarian Friday trade based on the previous completed daily candle."""

    def __init__(self):
        super(e_friday_session_strategy, self).__init__()
        self.Volume = Decimal(0.1)
        self._hour_open = self.Param("HourOpen", 7).SetRange(0, 23)
        self._hour_close = self.Param("HourClose", 19).SetRange(0, 23)
        self._use_close_positions = self.Param("UseClosePositions", True)
        self._stop_loss_points = self.Param("StopLossPoints", 75.0).SetNotNegative()
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0).SetNotNegative()
        self._use_trailing = self.Param("UseTrailing", True)
        self._profit_trailing = self.Param("ProfitTrailing", True)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 60.0).SetNotNegative()
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0).SetNotNegative()
        self._intraday_candle_type = self.Param(
            "IntradayCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._daily_candle_type = self.Param(
            "DailyCandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._clear_state()

    def _clear_state(self):
        self._last_daily_date = None
        self._daily_open = 0.0
        self._daily_close = 0.0
        self._traded_date = None
        self._entry_price = None
        self._trailing_level = None
        self._best_price = 0.0
        self._entry_order = None
        self._exit_order = None

    def OnReseted(self):
        super(e_friday_session_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self._intraday_candle_type.Value),
                (self.Security, self._daily_candle_type.Value)]

    def OnStarted2(self, time):
        super(e_friday_session_strategy, self).OnStarted2(time)
        self._clear_state()
        if float(self.Security.PriceStep or 0) <= 0:
            raise ValueError("E-Friday requires a positive PriceStep.")
        self.SubscribeCandles(self._daily_candle_type.Value).Bind(self._process_daily).Start()
        self.SubscribeCandles(self._intraday_candle_type.Value).Bind(self._process_intraday).Start()

    def _process_daily(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._last_daily_date = candle.OpenTime.Date
        self._daily_open = float(candle.OpenPrice)
        self._daily_close = float(candle.ClosePrice)

    @staticmethod
    def _pending(order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_intraday(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0 and not self._pending(self._exit_order):
            if (self._use_close_positions.Value and
                    candle.OpenTime.Hour >= self._hour_close.Value):
                self._exit("EFriday session close")
                return
            if self._handle_risk(candle):
                return

        day = candle.OpenTime.Date
        if (candle.OpenTime.DayOfWeek != DayOfWeek.Friday or
                candle.OpenTime.Hour != self._hour_open.Value or
                self._traded_date == day or
                self._last_daily_date != day.AddDays(-1) or
                self.Position != 0 or
                self._pending(self._entry_order) or self._pending(self._exit_order)):
            return

        if self._daily_open == self._daily_close:
            return

        side = Sides.Buy if self._daily_close < self._daily_open else Sides.Sell
        volume = float(self.Volume)
        step = float(self.Security.VolumeStep or 0)
        if step > 0:
            volume = math.floor(volume / step) * step
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume or 0)
        if minimum > 0:
            volume = max(volume, minimum)
        if maximum > 0:
            volume = min(volume, maximum)
        if volume <= 0:
            return

        self._entry_order = self._market(side, volume, "EFriday entry")
        self._traded_date = day

    def _handle_risk(self, candle):
        if self._entry_price is None:
            return False

        entry = self._entry_price
        point = float(self.Security.PriceStep)
        long_pos = self.Position > 0
        stop = float(self._stop_loss_points.Value) * point
        take = float(self._take_profit_points.Value) * point

        if stop > 0 and (
                candle.LowPrice <= entry - stop if long_pos
                else candle.HighPrice >= entry + stop):
            self._exit("EFriday stop loss")
            return True
        if take > 0 and (
                candle.HighPrice >= entry + take if long_pos
                else candle.LowPrice <= entry - take):
            self._exit("EFriday take profit")
            return True

        old_stop = self._trailing_level
        if old_stop is not None and (
                candle.LowPrice <= old_stop if long_pos
                else candle.HighPrice >= old_stop):
            self._exit("EFriday trailing stop")
            return True

        if not self._use_trailing.Value or self._trailing_stop_points.Value <= 0:
            return False

        distance = float(self._trailing_stop_points.Value) * point
        improvement = float(self._trailing_step_points.Value) * point
        self._best_price = (max(self._best_price, float(candle.HighPrice)) if long_pos
                            else min(self._best_price, float(candle.LowPrice)))
        profit = (self._best_price - entry if long_pos else entry - self._best_price)
        if self._profit_trailing.Value and profit < distance:
            return False

        next_stop = (self._best_price - distance if long_pos else
                     self._best_price + distance)
        if old_stop is None or (
                next_stop >= old_stop + improvement and next_stop > old_stop if long_pos
                else next_stop <= old_stop - improvement and next_stop < old_stop):
            self._trailing_level = next_stop
        return False

    def _market(self, side, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = Decimal(volume)
        order.Comment = comment
        self.RegisterOrder(order)
        return order

    def _exit(self, comment):
        if self.Position == 0 or self._pending(self._exit_order):
            return
        self._exit_order = self._market(
            Sides.Sell if self.Position > 0 else Sides.Buy,
            abs(float(self.Position)), comment)

    def OnOwnTradeReceived(self, trade):
        super(e_friday_session_strategy, self).OnOwnTradeReceived(trade)
        if trade is not None and trade.Order == self._entry_order and trade.Trade is not None:
            self._entry_price = float(trade.Trade.Price)
            self._best_price = self._entry_price
            self._trailing_level = None
        elif self.Position == 0:
            self._entry_price = None
            self._trailing_level = None
            self._best_price = 0.0
            self._exit_order = None

    def CreateClone(self):
        return e_friday_session_strategy()
