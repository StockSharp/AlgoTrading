import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange


class nrtr_atr_stop_strategy(Strategy):
    """NRTR ATR Stop: ATR-based trailing reversal levels determine trend changes."""

    def __init__(self):
        super(nrtr_atr_stop_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume used when opening positions", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss (points)", "Stop-loss distance in price steps", "Risk Management")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetDisplay("Take Profit (points)", "Take-profit distance in price steps", "Risk Management")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long Positions", "Allow long exits by indicator", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short Positions", "Allow short exits by indicator", "Trading")
        self._use_trading_window = self.Param("UseTradingWindow", True) \
            .SetDisplay("Use Trading Window", "Restrict trading to intraday window", "Session")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Hour when trading becomes available", "Session")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Minute when trading becomes available", "Session")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Hour when trading stops", "Session")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Minute when trading stops", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Number of bars used to calculate ATR", "Indicator")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier applied to the ATR value", "Indicator")
        self._signal_bar_delay = self.Param("SignalBarDelay", 1) \
            .SetDisplay("Signal Bar", "Number of closed bars to wait before acting", "Indicator")

        self._previous_up_line = None
        self._previous_down_line = None
        self._previous_trend = 0
        self._has_previous_candle = False
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._signal_queue = []

    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)
    @property
    def StopLossPoints(self):
        return float(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return float(self._take_profit_points.Value)
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
    @property
    def UseTradingWindow(self):
        return self._use_trading_window.Value
    @property
    def StartHour(self):
        return int(self._start_hour.Value)
    @property
    def StartMinute(self):
        return int(self._start_minute.Value)
    @property
    def EndHour(self):
        return int(self._end_hour.Value)
    @property
    def EndMinute(self):
        return int(self._end_minute.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def AtrPeriod(self):
        return int(self._atr_period.Value)
    @property
    def AtrMultiplier(self):
        return float(self._atr_multiplier.Value)
    @property
    def SignalBarDelay(self):
        return int(self._signal_bar_delay.Value)

    def OnStarted2(self, time):
        super(nrtr_atr_stop_strategy, self).OnStarted2(time)

        self._previous_up_line = None
        self._previous_down_line = None
        self._previous_trend = 0
        self._has_previous_candle = False
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._signal_queue = []

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.process_candle).Start()

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        self._handle_risk(candle)

        in_window = not self.UseTradingWindow or self._is_within_window(candle.CloseTime)

        if self.UseTradingWindow and not in_window and self.Position != 0:
            self._force_flat()

        if not self._atr.IsFormed:
            self._has_previous_candle = True
            self._prev_candle_high = float(candle.HighPrice)
            self._prev_candle_low = float(candle.LowPrice)
            return

        nrtr = self._calc_nrtr(candle, atr_val)
        if nrtr is None:
            return

        up_line, down_line, buy_signal, sell_signal = nrtr
        self._signal_queue.append(nrtr)

        if len(self._signal_queue) <= self.SignalBarDelay:
            return

        sig = self._signal_queue.pop(0)
        s_up, s_down, s_buy, s_sell = sig

        # Trail stops with NRTR levels
        if self.Position > 0 and s_up is not None:
            new_stop = s_up
            self._long_stop = max(self._long_stop, new_stop) if self._long_stop is not None else new_stop
        elif self.Position < 0 and s_down is not None:
            new_stop = s_down
            self._short_stop = min(self._short_stop, new_stop) if self._short_stop is not None else new_stop

        if self.UseTradingWindow and not in_window:
            return

        if s_buy:
            if self.SellPosClose:
                self._close_short()
            if self.BuyPosOpen and self.Position <= 0:
                self._open_long(float(candle.ClosePrice), s_up)
        elif s_sell:
            if self.BuyPosClose:
                self._close_long()
            if self.SellPosOpen and self.Position >= 0:
                self._open_short(float(candle.ClosePrice), s_down)

    def _calc_nrtr(self, candle, atr_val):
        if not self._has_previous_candle:
            self._has_previous_candle = True
            self._prev_candle_high = float(candle.HighPrice)
            self._prev_candle_low = float(candle.LowPrice)
            return None

        if atr_val <= 0:
            self._prev_candle_high = float(candle.HighPrice)
            self._prev_candle_low = float(candle.LowPrice)
            return None

        prev_low = self._prev_candle_low
        prev_high = self._prev_candle_high
        rez = atr_val * self.AtrMultiplier

        trend = self._previous_trend
        up_prev = self._previous_up_line if self._previous_up_line is not None and self._previous_up_line > 0 else None
        down_prev = self._previous_down_line if self._previous_down_line is not None and self._previous_down_line > 0 else None

        if trend <= 0:
            if down_prev is not None:
                if prev_low > down_prev:
                    up_prev = prev_low - rez
                    trend = 1
            else:
                up_prev = prev_low - rez
                trend = 1

        if trend >= 0:
            if up_prev is not None:
                if prev_high < up_prev:
                    down_prev = prev_high + rez
                    trend = -1
            else:
                down_prev = prev_high + rez
                trend = -1

        current_up = None
        if trend >= 0 and up_prev is not None:
            current_up = prev_low - rez if prev_low > up_prev + rez else up_prev

        current_down = None
        if trend <= 0 and down_prev is not None:
            current_down = prev_high + rez if prev_high < down_prev - rez else down_prev

        buy_signal = trend > 0 and self._previous_trend <= 0 and current_up is not None
        sell_signal = trend < 0 and self._previous_trend >= 0 and current_down is not None

        self._previous_trend = trend
        self._previous_up_line = current_up
        self._previous_down_line = current_down
        self._prev_candle_high = float(candle.HighPrice)
        self._prev_candle_low = float(candle.LowPrice)

        return (current_up, current_down, buy_signal, sell_signal)

    def _handle_risk(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop is not None and lo <= self._long_stop:
                self._close_long()
            elif self._long_target is not None and h >= self._long_target:
                self._close_long()
        elif self.Position < 0:
            if self._short_stop is not None and h >= self._short_stop:
                self._close_short()
            elif self._short_target is not None and lo <= self._short_target:
                self._close_short()

    def _open_long(self, price, indicator_stop):
        self.BuyMarket()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0

        manual_stop = price - self.StopLossPoints * step if step > 0 and self.StopLossPoints > 0 else None
        if indicator_stop is not None and manual_stop is not None:
            self._long_stop = max(indicator_stop, manual_stop)
        else:
            self._long_stop = indicator_stop if indicator_stop is not None else manual_stop

        self._long_target = price + self.TakeProfitPoints * step if step > 0 and self.TakeProfitPoints > 0 else None
        self._short_stop = None
        self._short_target = None

    def _open_short(self, price, indicator_stop):
        self.SellMarket()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0

        manual_stop = price + self.StopLossPoints * step if step > 0 and self.StopLossPoints > 0 else None
        if indicator_stop is not None and manual_stop is not None:
            self._short_stop = min(indicator_stop, manual_stop)
        else:
            self._short_stop = indicator_stop if indicator_stop is not None else manual_stop

        self._short_target = price - self.TakeProfitPoints * step if step > 0 and self.TakeProfitPoints > 0 else None
        self._long_stop = None
        self._long_target = None

    def _close_long(self):
        if self.Position <= 0:
            return
        self.SellMarket()
        self._long_stop = None
        self._long_target = None

    def _close_short(self):
        if self.Position >= 0:
            return
        self.BuyMarket()
        self._short_stop = None
        self._short_target = None

    def _force_flat(self):
        if self.Position > 0:
            self._close_long()
        elif self.Position < 0:
            self._close_short()

    def _is_within_window(self, time):
        hour = time.Hour
        minute = time.Minute
        sh = self.StartHour
        sm = self.StartMinute
        eh = self.EndHour
        em = self.EndMinute

        if sh < eh:
            if hour == sh and minute >= sm:
                return True
            if hour > sh and hour < eh:
                return True
            if hour > sh and hour == eh and minute < em:
                return True
        elif sh == eh:
            if hour == sh and minute >= sm and minute < em:
                return True
        else:
            if hour > sh or (hour == sh and minute >= sm):
                return True
            if hour < eh:
                return True
            if hour == eh and minute < em:
                return True

        return False

    def OnReseted(self):
        super(nrtr_atr_stop_strategy, self).OnReseted()
        self._previous_up_line = None
        self._previous_down_line = None
        self._previous_trend = 0
        self._has_previous_candle = False
        self._prev_candle_high = 0.0
        self._prev_candle_low = 0.0
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._signal_queue = []

    def CreateClone(self):
        return nrtr_atr_stop_strategy()
