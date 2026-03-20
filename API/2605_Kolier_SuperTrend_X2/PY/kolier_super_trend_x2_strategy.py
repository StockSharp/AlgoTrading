import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

MODE_SUPERTREND = 0
MODE_NEWWAY = 1
MODE_VISUAL = 2
MODE_EXPERT = 3


class kolier_super_trend_x2_strategy(Strategy):
    def __init__(self):
        super(kolier_super_trend_x2_strategy, self).__init__()

        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._entry_candle_type = self.Param("EntryCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._trend_atr_period = self.Param("TrendAtrPeriod", 10)
        self._trend_atr_multiplier = self.Param("TrendAtrMultiplier", 3.0)
        self._entry_atr_period = self.Param("EntryAtrPeriod", 10)
        self._entry_atr_multiplier = self.Param("EntryAtrMultiplier", 3.0)
        self._trend_mode = self.Param("TrendMode", MODE_NEWWAY)
        self._entry_mode = self.Param("EntryMode", MODE_NEWWAY)
        self._trend_signal_shift = self.Param("TrendSignalShift", 1)
        self._entry_signal_shift = self.Param("EntrySignalShift", 1)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._close_buy_on_trend_flip = self.Param("CloseBuyOnTrendFlip", True)
        self._close_sell_on_trend_flip = self.Param("CloseSellOnTrendFlip", True)
        self._close_buy_on_entry_flip = self.Param("CloseBuyOnEntryFlip", False)
        self._close_sell_on_entry_flip = self.Param("CloseSellOnEntryFlip", False)
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0)

        self._trend_directions = []
        self._entry_directions = []
        self._trend_direction = 0
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @TrendCandleType.setter
    def TrendCandleType(self, value):
        self._trend_candle_type.Value = value

    @property
    def EntryCandleType(self):
        return self._entry_candle_type.Value

    @EntryCandleType.setter
    def EntryCandleType(self, value):
        self._entry_candle_type.Value = value

    @property
    def TrendAtrPeriod(self):
        return self._trend_atr_period.Value

    @TrendAtrPeriod.setter
    def TrendAtrPeriod(self, value):
        self._trend_atr_period.Value = value

    @property
    def TrendAtrMultiplier(self):
        return self._trend_atr_multiplier.Value

    @TrendAtrMultiplier.setter
    def TrendAtrMultiplier(self, value):
        self._trend_atr_multiplier.Value = value

    @property
    def EntryAtrPeriod(self):
        return self._entry_atr_period.Value

    @EntryAtrPeriod.setter
    def EntryAtrPeriod(self, value):
        self._entry_atr_period.Value = value

    @property
    def EntryAtrMultiplier(self):
        return self._entry_atr_multiplier.Value

    @EntryAtrMultiplier.setter
    def EntryAtrMultiplier(self, value):
        self._entry_atr_multiplier.Value = value

    @property
    def TrendMode(self):
        return self._trend_mode.Value

    @TrendMode.setter
    def TrendMode(self, value):
        self._trend_mode.Value = value

    @property
    def EntryMode(self):
        return self._entry_mode.Value

    @EntryMode.setter
    def EntryMode(self, value):
        self._entry_mode.Value = value

    @property
    def TrendSignalShift(self):
        return self._trend_signal_shift.Value

    @TrendSignalShift.setter
    def TrendSignalShift(self, value):
        self._trend_signal_shift.Value = value

    @property
    def EntrySignalShift(self):
        return self._entry_signal_shift.Value

    @EntrySignalShift.setter
    def EntrySignalShift(self, value):
        self._entry_signal_shift.Value = value

    @property
    def EnableBuyEntries(self):
        return self._enable_buy_entries.Value

    @EnableBuyEntries.setter
    def EnableBuyEntries(self, value):
        self._enable_buy_entries.Value = value

    @property
    def EnableSellEntries(self):
        return self._enable_sell_entries.Value

    @EnableSellEntries.setter
    def EnableSellEntries(self, value):
        self._enable_sell_entries.Value = value

    @property
    def CloseBuyOnTrendFlip(self):
        return self._close_buy_on_trend_flip.Value

    @CloseBuyOnTrendFlip.setter
    def CloseBuyOnTrendFlip(self, value):
        self._close_buy_on_trend_flip.Value = value

    @property
    def CloseSellOnTrendFlip(self):
        return self._close_sell_on_trend_flip.Value

    @CloseSellOnTrendFlip.setter
    def CloseSellOnTrendFlip(self, value):
        self._close_sell_on_trend_flip.Value = value

    @property
    def CloseBuyOnEntryFlip(self):
        return self._close_buy_on_entry_flip.Value

    @CloseBuyOnEntryFlip.setter
    def CloseBuyOnEntryFlip(self, value):
        self._close_buy_on_entry_flip.Value = value

    @property
    def CloseSellOnEntryFlip(self):
        return self._close_sell_on_entry_flip.Value

    @CloseSellOnEntryFlip.setter
    def CloseSellOnEntryFlip(self, value):
        self._close_sell_on_entry_flip.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    def OnStarted(self, time):
        super(kolier_super_trend_x2_strategy, self).OnStarted(time)

        self._trend_directions = []
        self._entry_directions = []
        self._trend_direction = 0
        self._stop_loss_price = None
        self._take_profit_price = None

        self._trend_st = SuperTrend()
        self._trend_st.Length = self.TrendAtrPeriod
        self._trend_st.Multiplier = self.TrendAtrMultiplier

        self._entry_st = SuperTrend()
        self._entry_st.Length = self.EntryAtrPeriod
        self._entry_st.Multiplier = self.EntryAtrMultiplier

        entry_sub = self.SubscribeCandles(self.EntryCandleType)

        if str(self.TrendCandleType) == str(self.EntryCandleType):
            entry_sub.BindEx(self._trend_st, self._process_trend_candle)
            entry_sub.BindEx(self._entry_st, self._process_entry_candle)
            entry_sub.Start()
        else:
            entry_sub.BindEx(self._entry_st, self._process_entry_candle)
            entry_sub.Start()

            trend_sub = self.SubscribeCandles(self.TrendCandleType)
            trend_sub.BindEx(self._trend_st, self._process_trend_candle)
            trend_sub.Start()

    def _process_trend_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if not value.IsFormed:
            return

        if not self._trend_st.IsFormed:
            return

        direction = 1 if value.IsUpTrend else -1

        trend_shift = int(self.TrendSignalShift)
        max_len = trend_shift + 3
        self._trend_directions.insert(0, direction)
        while len(self._trend_directions) > max_len:
            self._trend_directions.pop()

        current = self._get_direction(self._trend_directions, trend_shift)
        previous = self._get_direction(self._trend_directions, trend_shift + 1)

        if current is None or previous is None:
            return

        trend_mode = int(self.TrendMode)
        if trend_mode == MODE_NEWWAY:
            self._trend_direction = current
        else:
            if current == previous:
                self._trend_direction = current

    def _process_entry_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if not value.IsFormed:
            return

        if not self._entry_st.IsFormed:
            return

        direction = 1 if value.IsUpTrend else -1

        entry_shift = int(self.EntrySignalShift)
        max_len = entry_shift + 3
        self._entry_directions.insert(0, direction)
        while len(self._entry_directions) > max_len:
            self._entry_directions.pop()

        if self._handle_stops(candle):
            return

        current = self._get_direction(self._entry_directions, entry_shift)
        previous = self._get_direction(self._entry_directions, entry_shift + 1)

        if current is None or previous is None:
            return

        flip_to_up = self._is_flip_to(1, current, previous)
        flip_to_down = self._is_flip_to(-1, current, previous)

        close_long = self.Position > 0 and ((self.CloseBuyOnTrendFlip and self._trend_direction < 0) or
                                             (self.CloseBuyOnEntryFlip and current < 0))
        close_short = self.Position < 0 and ((self.CloseSellOnTrendFlip and self._trend_direction > 0) or
                                              (self.CloseSellOnEntryFlip and current > 0))

        if close_long:
            self.SellMarket()
            self._reset_stops()

        if close_short:
            self.BuyMarket()
            self._reset_stops()

        if self.EnableBuyEntries and self._trend_direction > 0 and flip_to_up and self.Position <= 0:
            self.BuyMarket()
            self._update_stops(float(candle.ClosePrice), True)
        elif self.EnableSellEntries and self._trend_direction < 0 and flip_to_down and self.Position >= 0:
            self.SellMarket()
            self._update_stops(float(candle.ClosePrice), False)

    def _handle_stops(self, candle):
        if self.Position > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket()
                self._reset_stops()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket()
                self._reset_stops()
                return True
        elif self.Position < 0:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket()
                self._reset_stops()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket()
                self._reset_stops()
                return True
        return False

    def _update_stops(self, entry_price, is_long):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0.0:
            self._stop_loss_price = None
            self._take_profit_price = None
            return

        sl_pts = float(self.StopLossPoints)
        tp_pts = float(self.TakeProfitPoints)

        if sl_pts > 0.0:
            self._stop_loss_price = entry_price - sl_pts * step if is_long else entry_price + sl_pts * step
        else:
            self._stop_loss_price = None

        if tp_pts > 0.0:
            self._take_profit_price = entry_price + tp_pts * step if is_long else entry_price - tp_pts * step
        else:
            self._take_profit_price = None

    def _reset_stops(self):
        self._stop_loss_price = None
        self._take_profit_price = None

    def _get_direction(self, history, offset):
        if len(history) > offset:
            return history[offset]
        return None

    def _is_flip_to(self, target_direction, current, previous):
        if current != target_direction:
            return False
        entry_mode = int(self.EntryMode)
        if entry_mode == MODE_NEWWAY:
            return previous != target_direction
        else:
            return previous == -target_direction

    def OnReseted(self):
        super(kolier_super_trend_x2_strategy, self).OnReseted()
        self._trend_directions = []
        self._entry_directions = []
        self._trend_direction = 0
        self._stop_loss_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return kolier_super_trend_x2_strategy()
