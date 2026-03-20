import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class daily_break_point_strategy(Strategy):
    """Daily BreakPoint: daily open breakout with body size filter and trailing stop."""

    def __init__(self):
        super(daily_break_point_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Default order volume", "General")
        self._close_by_signal = self.Param("CloseBySignal", True) \
            .SetDisplay("Close By Signal", "Reverse existing position on opposite signal", "General")
        self._break_point_pips = self.Param("BreakPointPips", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Break Point (pips)", "Distance from the daily open", "Signals")
        self._last_bar_size_min_pips = self.Param("LastBarSizeMinPips", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Last Bar Min (pips)", "Minimum body size of the previous bar", "Signals")
        self._last_bar_size_max_pips = self.Param("LastBarSizeMaxPips", 5000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Last Bar Max (pips)", "Maximum body size of the previous bar", "Signals")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 2.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 2.0) \
            .SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0) \
            .SetDisplay("Stop Loss (pips)", "Fixed stop loss distance", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 30.0) \
            .SetDisplay("Take Profit (pips)", "Fixed take profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Intraday candle series", "Data")

        self._current_day_open = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = 0.0

    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)
    @property
    def CloseBySignal(self):
        return self._close_by_signal.Value
    @property
    def BreakPointPips(self):
        return float(self._break_point_pips.Value)
    @property
    def LastBarSizeMinPips(self):
        return float(self._last_bar_size_min_pips.Value)
    @property
    def LastBarSizeMaxPips(self):
        return float(self._last_bar_size_max_pips.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def TrailingStepPips(self):
        return float(self._trailing_step_pips.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 0.0001
        step = float(sec.PriceStep)
        if step <= 0:
            return 0.0001
        decimals = None
        if sec.Decimals is not None:
            decimals = int(sec.Decimals)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def _normalize_price(self, price):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return price
        step = float(sec.PriceStep)
        if step <= 0:
            return price
        return round(price / step) * step

    def OnStarted(self, time):
        super(daily_break_point_strategy, self).OnStarted(time)

        self._current_day_open = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = self._calc_pip_size()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        intraday_subscription = self.SubscribeCandles(self.CandleType)
        intraday_subscription.Bind(self.process_candle).Start()

        daily_subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        daily_subscription.Bind(self.process_daily_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, intraday_subscription)
            self.DrawOwnTrades(area)

    def process_daily_candle(self, candle):
        if candle.State == CandleStates.Finished or candle.State == CandleStates.Active:
            self._current_day_open = float(candle.OpenPrice)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._pip_size <= 0:
            self._pip_size = self._calc_pip_size()

        day_open = self._current_day_open
        if day_open is None:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        break_offset = self.BreakPointPips * self._pip_size
        min_body = self.LastBarSizeMinPips * self._pip_size
        max_body = self.LastBarSizeMaxPips * self._pip_size
        trailing_stop = self.TrailingStopPips * self._pip_size
        trailing_step = self.TrailingStepPips * self._pip_size
        stop_loss_offset = self.StopLossPips * self._pip_size if self.StopLossPips > 0 else 0.0
        take_profit_offset = self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else 0.0

        self._update_trailing(candle, trailing_stop, trailing_step)
        if self._handle_risk_exits(candle):
            return

        body_size = abs(close - open_p)

        bullish_body = close > open_p
        bearish_body = close < open_p

        bullish_signal = (bullish_body and break_offset > 0 and
                          close - day_open >= break_offset and
                          body_size >= min_body and
                          (max_body <= 0 or body_size <= max_body))

        bearish_signal = (bearish_body and break_offset > 0 and
                          day_open - close >= break_offset and
                          body_size >= min_body and
                          (max_body <= 0 or body_size <= max_body))

        if bullish_signal:
            self._execute_bullish_signal(close, stop_loss_offset, take_profit_offset)
        elif bearish_signal:
            self._execute_bearish_signal(close, stop_loss_offset, take_profit_offset)

    def _update_trailing(self, candle, trailing_stop, trailing_step):
        if trailing_stop <= 0:
            return

        close = float(candle.ClosePrice)

        if self.Position > 0 and self._long_entry_price is not None:
            profit = close - self._long_entry_price
            if profit > trailing_stop + trailing_step:
                threshold = close - (trailing_stop + trailing_step)
                if self._long_stop_price is None or self._long_stop_price < threshold:
                    self._long_stop_price = self._normalize_price(close - trailing_stop)
        elif self.Position < 0 and self._short_entry_price is not None:
            profit = self._short_entry_price - close
            if profit > trailing_stop + trailing_step:
                threshold = close + trailing_stop + trailing_step
                if (self._short_stop_price is None or
                        self._short_stop_price > threshold or self._short_stop_price == 0):
                    self._short_stop_price = self._normalize_price(close + trailing_stop)

    def _handle_risk_exits(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop_price is not None and lo <= self._long_stop_price:
                self.SellMarket()
                self._reset_long_state()
                return True
            if self._long_take_price is not None and h >= self._long_take_price:
                self.SellMarket()
                self._reset_long_state()
                return True
        elif self.Position < 0:
            if self._short_stop_price is not None and h >= self._short_stop_price:
                self.BuyMarket()
                self._reset_short_state()
                return True
            if self._short_take_price is not None and lo <= self._short_take_price:
                self.BuyMarket()
                self._reset_short_state()
                return True
        else:
            self._reset_long_state()
            self._reset_short_state()
        return False

    def _execute_bullish_signal(self, entry_price, stop_loss_offset, take_profit_offset):
        if self.CloseBySignal:
            if self.Position > 0:
                self.SellMarket()
            self._reset_long_state()

            self.SellMarket()

            self._short_entry_price = entry_price
            self._short_stop_price = self._normalize_price(entry_price + stop_loss_offset) if stop_loss_offset > 0 else None
            self._short_take_price = self._normalize_price(entry_price - take_profit_offset) if take_profit_offset > 0 else None
        else:
            self.BuyMarket()

            self._long_entry_price = entry_price
            self._long_stop_price = self._normalize_price(entry_price - stop_loss_offset) if stop_loss_offset > 0 else None
            self._long_take_price = self._normalize_price(entry_price + take_profit_offset) if take_profit_offset > 0 else None
            self._reset_short_state()

    def _execute_bearish_signal(self, entry_price, stop_loss_offset, take_profit_offset):
        if self.CloseBySignal:
            if self.Position < 0:
                self.BuyMarket()
            self._reset_short_state()

            self.BuyMarket()

            self._long_entry_price = entry_price
            self._long_stop_price = self._normalize_price(entry_price - stop_loss_offset) if stop_loss_offset > 0 else None
            self._long_take_price = self._normalize_price(entry_price + take_profit_offset) if take_profit_offset > 0 else None
        else:
            self.SellMarket()

            self._short_entry_price = entry_price
            self._short_stop_price = self._normalize_price(entry_price + stop_loss_offset) if stop_loss_offset > 0 else None
            self._short_take_price = self._normalize_price(entry_price - take_profit_offset) if take_profit_offset > 0 else None
            self._reset_long_state()

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def OnReseted(self):
        super(daily_break_point_strategy, self).OnReseted()
        self._current_day_open = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = 0.0

    def CreateClone(self):
        return daily_break_point_strategy()
