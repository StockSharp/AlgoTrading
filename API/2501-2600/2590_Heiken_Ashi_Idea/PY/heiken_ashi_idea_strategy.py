import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange


class heiken_ashi_idea_strategy(Strategy):
    """Multi-timeframe Heikin Ashi strategy using pending limit orders and ATR filter."""

    def __init__(self):
        super(heiken_ashi_idea_strategy, self).__init__()

        self._distance_points = self.Param("DistancePoints", 8.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Pending Distance (pts)", "Distance for pending limit orders in price steps", "Trading")

        self._stop_loss_points = self.Param("StopLossPoints", 0.0) \
            .SetDisplay("Stop Loss (pts)", "Stop-loss distance in price steps (0 disables)", "Risk")

        self._take_profit_points = self.Param("TakeProfitPoints", 20.0) \
            .SetDisplay("Take Profit (pts)", "Take-profit distance in price steps (0 disables)", "Risk")

        self._use_close_all = self.Param("UseCloseAllOnNewBar", True) \
            .SetDisplay("Close On Higher Bar", "Flatten positions when close-all timeframe bar opens", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Primary Candle Type", "Primary timeframe for signals", "Data")

        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Higher Candle Type", "Confirmation timeframe for HA trend filter", "Data")

        self._close_all_candle_type = self.Param("CloseAllCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Close-All Candle Type", "Timeframe that triggers complete exit", "Data")

        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "First hour of trading window", "Session")

        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Last hour of trading window", "Session")

        self._use_atr_filter = self.Param("UseAtrFilter", False) \
            .SetDisplay("Use ATR Filter", "Require rising ATR to allow new orders", "Filters")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period used for ATR volatility filter", "Filters")

        self._has_atr_value = False
        self._has_prev_atr = False
        self._last_atr_value = 0.0
        self._prev_atr_value = 0.0

        self._base_ha_current = None
        self._base_ha_previous = None
        self._higher_ha_current = None
        self._higher_ha_previous = None

        self._buy_order = None
        self._sell_order = None
        self._last_close_all_time = None
        self._price_step = 0.0
        self._tolerance = 0.0

    @property
    def DistancePoints(self):
        return self._distance_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def UseCloseAllOnNewBar(self):
        return self._use_close_all.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def HigherCandleType(self):
        return self._higher_candle_type.Value

    @property
    def CloseAllCandleType(self):
        return self._close_all_candle_type.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def UseAtrFilter(self):
        return self._use_atr_filter.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    def OnStarted2(self, time):
        super(heiken_ashi_idea_strategy, self).OnStarted2(time)

        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._tolerance = self._price_step / 2.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        primary = self.SubscribeCandles(self.CandleType)
        primary \
            .Bind(atr, self.process_primary) \
            .Start()

        higher = self.SubscribeCandles(self.HigherCandleType)
        higher \
            .Bind(self.process_higher) \
            .Start()

        if self.UseCloseAllOnNewBar:
            close_all = self.SubscribeCandles(self.CloseAllCandleType)
            close_all \
                .Bind(self.process_close_all) \
                .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary)
            self.DrawOwnTrades(area)

        tp_val = float(self.TakeProfitPoints)
        sl_val = float(self.StopLossPoints)
        tp_unit = Unit(tp_val * self._price_step, UnitTypes.Absolute) if tp_val > 0 else None
        sl_unit = Unit(sl_val * self._price_step, UnitTypes.Absolute) if sl_val > 0 else None
        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

    def process_primary(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        self._update_atr(float(atr_val))
        self._update_ha(candle, True)
        self._try_place_orders(candle)

    def process_higher(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._update_ha(candle, False)

    def process_close_all(self, candle):
        if not self.UseCloseAllOnNewBar or candle.State != CandleStates.Finished:
            return
        if self._last_close_all_time == candle.OpenTime:
            return
        self._last_close_all_time = candle.OpenTime

        self._cancel_tracked()

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def _update_atr(self, val):
        if self._has_atr_value:
            self._prev_atr_value = self._last_atr_value
            self._has_prev_atr = True
        self._last_atr_value = val
        self._has_atr_value = True

    def _update_ha(self, candle, is_base):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        if is_base:
            prev = self._base_ha_current
        else:
            prev = self._higher_ha_current

        if prev is not None:
            ha_open = (prev[0] + prev[3]) / 2.0
        else:
            ha_open = (o + c) / 2.0

        ha_close = (o + h + l + c) / 4.0
        ha_high = max(h, ha_open, ha_close)
        ha_low = min(l, ha_open, ha_close)

        new_ha = (ha_open, ha_high, ha_low, ha_close)

        if is_base:
            self._base_ha_previous = self._base_ha_current
            self._base_ha_current = new_ha
        else:
            self._higher_ha_previous = self._higher_ha_current
            self._higher_ha_current = new_ha

    def _try_place_orders(self, candle):
        if float(self.DistancePoints) <= 0:
            return

        tod = candle.OpenTime.TimeOfDay
        if not self._in_hours(tod):
            return

        if self._base_ha_previous is None or self._higher_ha_previous is None:
            return

        if self.UseAtrFilter:
            if not self._has_prev_atr or self._last_atr_value <= self._prev_atr_value:
                return

        self._update_order_refs()

        long_sig = (self._ha_bull_break(self._base_ha_current, self._base_ha_previous) and
                    self._ha_bull_break(self._higher_ha_current, self._higher_ha_previous))
        short_sig = (self._ha_bear_break(self._base_ha_current, self._base_ha_previous) and
                     self._ha_bear_break(self._higher_ha_current, self._higher_ha_previous))

        offset = float(self.DistancePoints) * self._price_step

        if long_sig and self.Position <= 0:
            if self._sell_order is not None and self._sell_order.State == 1:
                self.CancelOrder(self._sell_order)
                self._sell_order = None

            if self._buy_order is None or self._buy_order.State != 1:
                price = float(candle.ClosePrice) - offset
                if price <= 0:
                    price = self._price_step
                vol = self.Volume + (abs(self.Position) if self.Position < 0 else 0)
                if vol > 0:
                    self._buy_order = self.BuyLimit(price, vol)

        elif short_sig and self.Position >= 0:
            if self._buy_order is not None and self._buy_order.State == 1:
                self.CancelOrder(self._buy_order)
                self._buy_order = None

            if self._sell_order is None or self._sell_order.State != 1:
                price = float(candle.ClosePrice) + offset
                vol = self.Volume + (abs(self.Position) if self.Position > 0 else 0)
                if vol > 0:
                    self._sell_order = self.SellLimit(price, vol)

    def _in_hours(self, tod):
        start = TimeSpan.FromHours(self.StartHour)
        end = TimeSpan.FromHours(self.EndHour)
        if end < start:
            return tod >= start or tod <= end
        return tod >= start and tod <= end

    def _update_order_refs(self):
        if self._buy_order is not None and self._buy_order.State != 1:
            self._buy_order = None
        if self._sell_order is not None and self._sell_order.State != 1:
            self._sell_order = None

    def _cancel_tracked(self):
        if self._buy_order is not None:
            if self._buy_order.State == 1:
                self.CancelOrder(self._buy_order)
            self._buy_order = None
        if self._sell_order is not None:
            if self._sell_order.State == 1:
                self.CancelOrder(self._sell_order)
            self._sell_order = None

    def _ha_bull_break(self, curr, prev):
        if curr is None or prev is None:
            return False
        return (self._is_bullish(curr) and self._no_lower_shadow(curr) and
                self._is_bullish(prev) and self._has_lower_shadow(prev))

    def _ha_bear_break(self, curr, prev):
        if curr is None or prev is None:
            return False
        return (self._is_bearish(curr) and self._no_upper_shadow(curr) and
                self._is_bearish(prev) and self._has_upper_shadow(prev))

    def _is_bullish(self, ha):
        return ha[3] > ha[0]

    def _is_bearish(self, ha):
        return ha[3] < ha[0]

    def _no_lower_shadow(self, ha):
        return abs(ha[0] - ha[2]) <= self._tolerance

    def _has_lower_shadow(self, ha):
        return abs(ha[0] - ha[2]) > self._tolerance

    def _no_upper_shadow(self, ha):
        return abs(ha[0] - ha[1]) <= self._tolerance

    def _has_upper_shadow(self, ha):
        return abs(ha[0] - ha[1]) > self._tolerance

    def OnReseted(self):
        super(heiken_ashi_idea_strategy, self).OnReseted()
        self._has_atr_value = False
        self._has_prev_atr = False
        self._last_atr_value = 0.0
        self._prev_atr_value = 0.0
        self._base_ha_current = None
        self._base_ha_previous = None
        self._higher_ha_current = None
        self._higher_ha_previous = None
        self._buy_order = None
        self._sell_order = None
        self._last_close_all_time = None
        self._price_step = 0.0
        self._tolerance = 0.0

    def CreateClone(self):
        return heiken_ashi_idea_strategy()
