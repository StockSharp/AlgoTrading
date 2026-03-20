import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class time_ea_strategy(Strategy):
    TYPE_BUY = 0
    TYPE_SELL = 1

    def __init__(self):
        super(time_ea_strategy, self).__init__()
        self._open_time = self.Param("OpenTime", TimeSpan(1, 0, 0))
        self._close_time = self.Param("CloseTime", TimeSpan.Zero)
        self._opened_type = self.Param("OpenedType", self.TYPE_BUY)
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._stop_loss_points = self.Param("StopLossPoints", 0)
        self._take_profit_points = self.Param("TakeProfitPoints", 0)
        self._min_spread_multiplier = self.Param("MinSpreadMultiplier", 2)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._last_entry_date = None
        self._last_close_date = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def OpenTime(self):
        return self._open_time.Value

    @property
    def CloseTime(self):
        return self._close_time.Value

    @property
    def OpenedType(self):
        return self._opened_type.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def MinSpreadMultiplier(self):
        return self._min_spread_multiplier.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(time_ea_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        candle_date = candle.CloseTime.Date

        if self._contains_time(candle, self.OpenTime) and self._last_entry_date != candle_date:
            self._last_entry_date = candle_date
            self._handle_open(candle)

        if self._contains_time(candle, self.CloseTime) and self._last_close_date != candle_date:
            self._last_close_date = candle_date
            pos = float(self.Position)
            if pos != 0:
                if pos > 0:
                    self.SellMarket(pos)
                elif pos < 0:
                    self.BuyMarket(abs(pos))
                self._reset_risk_levels()
            return

        self._manage_risk(candle)

    def _handle_open(self, candle):
        pos = float(self.Position)
        if self.OpenedType == self.TYPE_BUY:
            if pos < 0:
                self.BuyMarket(abs(pos))
                self._reset_risk_levels()
            if float(self.Position) == 0 and float(self.OrderVolume) > 0:
                self.BuyMarket(float(self.OrderVolume))
                self._set_risk_levels(float(candle.ClosePrice), True)
        else:
            if pos > 0:
                self.SellMarket(pos)
                self._reset_risk_levels()
            if float(self.Position) == 0 and float(self.OrderVolume) > 0:
                self.SellMarket(float(self.OrderVolume))
                self._set_risk_levels(float(candle.ClosePrice), False)

    def _manage_risk(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_risk_levels()
                return
            if self._take_profit_price > 0 and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(pos)
                self._reset_risk_levels()
        elif pos < 0:
            if self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._reset_risk_levels()
                return
            if self._take_profit_price > 0 and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(pos))
                self._reset_risk_levels()

    def _set_risk_levels(self, close_price, is_long):
        self._entry_price = close_price
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0
        min_distance = max(self.MinSpreadMultiplier, 0) * step
        stop_dist = max(self.StopLossPoints * step, min_distance) if self.StopLossPoints > 0 else 0.0
        take_dist = max(self.TakeProfitPoints * step, min_distance) if self.TakeProfitPoints > 0 else 0.0
        if is_long:
            self._stop_price = close_price - stop_dist if stop_dist > 0 else 0.0
            self._take_profit_price = close_price + take_dist if take_dist > 0 else 0.0
        else:
            self._stop_price = close_price + stop_dist if stop_dist > 0 else 0.0
            self._take_profit_price = close_price - take_dist if take_dist > 0 else 0.0

    def _reset_risk_levels(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def _contains_time(self, candle, target):
        open_time = candle.OpenTime
        close_time = candle.CloseTime
        open_span = open_time.TimeOfDay
        close_span = close_time.TimeOfDay
        crosses_midnight = close_time.Date > open_time.Date or close_span < open_span
        if not crosses_midnight:
            return target >= open_span and target <= close_span
        start_min = open_span.TotalMinutes
        end_min = close_span.TotalMinutes + 1440.0
        target_min = target.TotalMinutes
        if target_min < start_min:
            target_min += 1440.0
        return target_min >= start_min and target_min <= end_min

    def OnReseted(self):
        super(time_ea_strategy, self).OnReseted()
        self._last_entry_date = None
        self._last_close_date = None
        self._reset_risk_levels()

    def CreateClone(self):
        return time_ea_strategy()
