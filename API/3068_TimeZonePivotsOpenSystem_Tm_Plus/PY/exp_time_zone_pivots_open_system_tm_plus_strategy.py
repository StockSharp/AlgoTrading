import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class exp_time_zone_pivots_open_system_tm_plus_strategy(Strategy):
    ZONE_INSIDE = 0
    ZONE_ABOVE = 1
    ZONE_BELOW = 2

    def __init__(self):
        super(exp_time_zone_pivots_open_system_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss (points)", "Distance from entry to stop loss", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetDisplay("Take Profit (points)", "Distance from entry to take profit", "Risk")
        self._offset_points = self.Param("OffsetPoints", 200.0) \
            .SetDisplay("Offset (points)", "Distance from session open that defines pivot zones", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Number of bars to delay signal evaluation", "Indicator")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Session Start Hour", "Hour of day used to anchor session open price", "Indicator")
        self._use_time_exit = self.Param("TimeTrade", True) \
            .SetDisplay("Use Time Exit", "Close positions after a fixed holding time", "Risk")
        self._holding_minutes = self.Param("HoldingMinutes", 720) \
            .SetDisplay("Holding Minutes", "Maximum position lifetime in minutes", "Risk")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Enable Long Exits", "Allow closing long positions on opposite signals", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Enable Short Exits", "Allow closing short positions on opposite signals", "Trading")

        self._zone_history = []
        self._last_session_date = None
        self._session_open_price = None
        self._upper_band = None
        self._lower_band = None
        self._session_trade_taken = False
        self._long_entry_time = None
        self._short_entry_time = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value
    @property
    def OffsetPoints(self):
        return self._offset_points.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def StartHour(self):
        return self._start_hour.Value
    @property
    def UseTimeExit(self):
        return self._use_time_exit.Value
    @property
    def HoldingMinutes(self):
        return self._holding_minutes.Value
    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value
    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value
    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value
    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(exp_time_zone_pivots_open_system_tm_plus_strategy, self).OnReseted()
        self._zone_history = []
        self._last_session_date = None
        self._session_open_price = None
        self._upper_band = None
        self._lower_band = None
        self._session_trade_taken = False
        self._long_entry_time = None
        self._short_entry_time = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def OnStarted(self, time):
        super(exp_time_zone_pivots_open_system_tm_plus_strategy, self).OnStarted(time)
        self._zone_history = []
        self._last_session_date = None
        self._session_open_price = None
        self._upper_band = None
        self._lower_band = None
        self._session_trade_taken = False
        self._long_entry_time = None
        self._short_entry_time = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_session_reference(candle)

        state = self._determine_state(candle)
        self._zone_history.insert(0, state)

        max_history = max(5, self.SignalBar + 3)
        while len(self._zone_history) > max_history:
            self._zone_history.pop()

        if len(self._zone_history) <= self.SignalBar + 1:
            self._manage_stops(candle)
            self._handle_time_exit(candle.CloseTime)
            return

        signal_snapshot = self._zone_history[self.SignalBar]
        confirm_snapshot = self._zone_history[self.SignalBar + 1]

        close_long = False
        close_short = False

        if confirm_snapshot == self.ZONE_ABOVE:
            if self.SellPosClose:
                close_short = True
        elif confirm_snapshot == self.ZONE_BELOW:
            if self.BuyPosClose:
                close_long = True

        if close_long and self.Position > 0:
            self.SellMarket()
            self._long_entry_time = None
            self._long_entry_price = None
            self._long_stop_price = None
            self._long_take_price = None

        if close_short and self.Position < 0:
            self.BuyMarket()
            self._short_entry_time = None
            self._short_entry_price = None
            self._short_stop_price = None
            self._short_take_price = None

        self._manage_stops(candle)
        self._handle_time_exit(candle.CloseTime)

        if self._session_trade_taken:
            return

        if self.Position != 0:
            return

        if confirm_snapshot == self.ZONE_ABOVE and signal_snapshot != self.ZONE_ABOVE and self.BuyPosOpen:
            self.BuyMarket()
            self._session_trade_taken = True
            self._long_entry_time = candle.CloseTime
            self._long_entry_price = float(candle.ClosePrice)
            step = self._get_price_step()
            self._long_stop_price = self._long_entry_price - float(self.StopLossPoints) * step if float(self.StopLossPoints) > 0.0 else None
            self._long_take_price = self._long_entry_price + float(self.TakeProfitPoints) * step if float(self.TakeProfitPoints) > 0.0 else None
        elif confirm_snapshot == self.ZONE_BELOW and signal_snapshot != self.ZONE_BELOW and self.SellPosOpen:
            self.SellMarket()
            self._session_trade_taken = True
            self._short_entry_time = candle.CloseTime
            self._short_entry_price = float(candle.ClosePrice)
            step = self._get_price_step()
            self._short_stop_price = self._short_entry_price + float(self.StopLossPoints) * step if float(self.StopLossPoints) > 0.0 else None
            self._short_take_price = self._short_entry_price - float(self.TakeProfitPoints) * step if float(self.TakeProfitPoints) > 0.0 else None

    def _manage_stops(self, candle):
        if self.Position > 0:
            if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
                self.SellMarket()
                self._long_entry_time = None
                self._long_entry_price = None
                self._long_stop_price = None
                self._long_take_price = None
            elif self._long_take_price is not None and float(candle.HighPrice) >= self._long_take_price:
                self.SellMarket()
                self._long_entry_time = None
                self._long_entry_price = None
                self._long_stop_price = None
                self._long_take_price = None
        elif self.Position < 0:
            if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
                self.BuyMarket()
                self._short_entry_time = None
                self._short_entry_price = None
                self._short_stop_price = None
                self._short_take_price = None
            elif self._short_take_price is not None and float(candle.LowPrice) <= self._short_take_price:
                self.BuyMarket()
                self._short_entry_time = None
                self._short_entry_price = None
                self._short_stop_price = None
                self._short_take_price = None

    def _handle_time_exit(self, time):
        if not self.UseTimeExit:
            return
        if self.HoldingMinutes <= 0:
            return

        if self.Position > 0 and self._long_entry_time is not None:
            if (time - self._long_entry_time).TotalMinutes >= self.HoldingMinutes:
                self.SellMarket()
                self._long_entry_time = None
                self._long_entry_price = None
                self._long_stop_price = None
                self._long_take_price = None
        elif self.Position < 0 and self._short_entry_time is not None:
            if (time - self._short_entry_time).TotalMinutes >= self.HoldingMinutes:
                self.BuyMarket()
                self._short_entry_time = None
                self._short_entry_price = None
                self._short_stop_price = None
                self._short_take_price = None

    def _update_session_reference(self, candle):
        open_time = candle.OpenTime
        current_date = open_time.Date

        if (self._last_session_date is None or self._last_session_date != current_date) and open_time.Hour == self.StartHour:
            self._session_open_price = float(candle.OpenPrice)
            self._last_session_date = current_date
            self._zone_history = []
            self._session_trade_taken = False

        if self._session_open_price is not None:
            step = self._get_price_step()
            offset = float(self.OffsetPoints) * step
            self._upper_band = self._session_open_price + offset
            self._lower_band = self._session_open_price - offset
        else:
            self._upper_band = None
            self._lower_band = None

    def _determine_state(self, candle):
        if self._session_open_price is None or self._upper_band is None or self._lower_band is None:
            return self.ZONE_INSIDE
        close = float(candle.ClosePrice)
        if close > self._upper_band:
            return self.ZONE_ABOVE
        if close < self._lower_band:
            return self.ZONE_BELOW
        return self.ZONE_INSIDE

    def _get_price_step(self):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None:
            step = float(sec.PriceStep)
            if step > 0.0:
                return step
        return 0.0001

    def CreateClone(self):
        return exp_time_zone_pivots_open_system_tm_plus_strategy()
