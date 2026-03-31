import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

PRICE_CLOSE = 1
PRICE_OPEN = 2
PRICE_HIGH = 3
PRICE_LOW = 4
PRICE_MEDIAN = 5
PRICE_TYPICAL = 6
PRICE_WEIGHTED = 7
PRICE_SIMPLE = 8
PRICE_QUARTER = 9
PRICE_TRENDFOLLOW0 = 10
PRICE_TRENDFOLLOW1 = 11
PRICE_DEMARK = 12


class xo_signal_re_open_strategy(Strategy):
    def __init__(self):
        super(xo_signal_re_open_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 13)
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000)
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000)
        self._price_step_ticks = self.Param("PriceStepTicks", 1000)
        self._max_pyramiding_positions = self.Param("MaxPyramidingPositions", 1)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exits = self.Param("EnableBuyExits", True)
        self._enable_sell_exits = self.Param("EnableSellExits", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._range = self.Param("Range", 10)
        self._applied_price = self.Param("AppliedPrice", PRICE_CLOSE)
        self._signal_bar = self.Param("SignalBar", 1)

        self._signal_queue = []
        self._hi = 0.0
        self._lo = 0.0
        self._kr = 0
        self._no = 0
        self._trend = 0
        self._initialized = False
        self._last_buy_signal_time = None
        self._last_sell_signal_time = None
        self._last_executed_buy_signal_time = None
        self._last_executed_sell_signal_time = None
        self._long_order_count = 0
        self._short_order_count = 0
        self._last_long_entry_price = 0.0
        self._last_short_entry_price = 0.0
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def StopLossTicks(self):
        return self._stop_loss_ticks.Value

    @StopLossTicks.setter
    def StopLossTicks(self, value):
        self._stop_loss_ticks.Value = value

    @property
    def TakeProfitTicks(self):
        return self._take_profit_ticks.Value

    @TakeProfitTicks.setter
    def TakeProfitTicks(self, value):
        self._take_profit_ticks.Value = value

    @property
    def PriceStepTicks(self):
        return self._price_step_ticks.Value

    @PriceStepTicks.setter
    def PriceStepTicks(self, value):
        self._price_step_ticks.Value = value

    @property
    def MaxPyramidingPositions(self):
        return self._max_pyramiding_positions.Value

    @MaxPyramidingPositions.setter
    def MaxPyramidingPositions(self, value):
        self._max_pyramiding_positions.Value = value

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
    def EnableBuyExits(self):
        return self._enable_buy_exits.Value

    @EnableBuyExits.setter
    def EnableBuyExits(self, value):
        self._enable_buy_exits.Value = value

    @property
    def EnableSellExits(self):
        return self._enable_sell_exits.Value

    @EnableSellExits.setter
    def EnableSellExits(self, value):
        self._enable_sell_exits.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Range(self):
        return self._range.Value

    @Range.setter
    def Range(self, value):
        self._range.Value = value

    @property
    def AppliedPrice(self):
        return self._applied_price.Value

    @AppliedPrice.setter
    def AppliedPrice(self, value):
        self._applied_price.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    def OnStarted2(self, time):
        super(xo_signal_re_open_strategy, self).OnStarted2(time)

        self._signal_queue = []
        self._hi = 0.0
        self._lo = 0.0
        self._kr = 0
        self._no = 0
        self._trend = 0
        self._initialized = False
        self._last_buy_signal_time = None
        self._last_sell_signal_time = None
        self._last_executed_buy_signal_time = None
        self._last_executed_sell_signal_time = None
        self._long_order_count = 0
        self._short_order_count = 0
        self._last_long_entry_price = 0.0
        self._last_short_entry_price = 0.0
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr = float(atr_value)
        if atr <= 0.0:
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        range_step = max(1, int(self.Range)) * step
        price = self._get_applied_price(candle)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if not self._initialized:
            self._hi = price
            self._lo = price
            self._initialized = True

        if price > self._hi + range_step:
            self._hi = price
            self._lo = self._hi - range_step
            self._kr += 1
            self._no = 0
        elif price < self._lo - range_step:
            self._lo = price
            self._hi = self._lo + range_step
            self._no += 1
            self._kr = 0

        trend = self._trend
        if self._kr > 0:
            trend = 1
        if self._no > 0:
            trend = -1

        buy_signal = self._trend < 0 and trend > 0
        sell_signal = self._trend > 0 and trend < 0
        self._trend = trend

        close_time = candle.OpenTime + self.CandleType.Arg
        buy_time = close_time if buy_signal else (self._last_buy_signal_time if self._last_buy_signal_time is not None else close_time)
        sell_time = close_time if sell_signal else (self._last_sell_signal_time if self._last_sell_signal_time is not None else close_time)
        buy_level = low - atr * 3.0 / 8.0
        sell_level = high + atr * 3.0 / 8.0

        info = (buy_signal, sell_signal, sell_signal, buy_signal, buy_time, sell_time, buy_level, sell_level, close)
        self._signal_queue.append(info)

        sb = int(self.SignalBar)
        if len(self._signal_queue) <= sb:
            return

        active_signal = self._signal_queue.pop(0)

        self._handle_stops(candle)
        self._apply_signal(active_signal, candle)
        self._handle_reentries(candle)

    def _apply_signal(self, signal, candle):
        buy_entry, sell_entry, buy_exit, sell_exit, buy_time, sell_time, buy_level, sell_level, sig_close = signal
        close = float(candle.ClosePrice)

        if buy_entry or sell_exit:
            self._last_buy_signal_time = buy_time
        if sell_entry or buy_exit:
            self._last_sell_signal_time = sell_time

        if buy_exit and self.EnableBuyExits and self.Position > 0:
            self.SellMarket()
            self._reset_long_state()

        if sell_exit and self.EnableSellExits and self.Position < 0:
            self.BuyMarket()
            self._reset_short_state()

        if buy_entry and self.EnableBuyEntries:
            if self._last_executed_buy_signal_time != buy_time:
                if self.Position < 0:
                    self.BuyMarket()
                    self._reset_short_state()
                if self.Position <= 0:
                    self.BuyMarket()
                    self._last_executed_buy_signal_time = buy_time
                    self._long_order_count = 1
                    self._short_order_count = 0
                    self._last_long_entry_price = close
                    self._update_long_risk_levels(close)

        if sell_entry and self.EnableSellEntries:
            if self._last_executed_sell_signal_time != sell_time:
                if self.Position > 0:
                    self.SellMarket()
                    self._reset_long_state()
                if self.Position >= 0:
                    self.SellMarket()
                    self._last_executed_sell_signal_time = sell_time
                    self._short_order_count = 1
                    self._long_order_count = 0
                    self._last_short_entry_price = close
                    self._update_short_risk_levels(close)

    def _handle_stops(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop_price is not None and low <= self._long_stop_price:
                self.SellMarket()
                self._reset_long_state()
            elif self._long_take_price is not None and high >= self._long_take_price:
                self.SellMarket()
                self._reset_long_state()
        else:
            self._long_stop_price = None
            self._long_take_price = None

        if self.Position < 0:
            if self._short_stop_price is not None and high >= self._short_stop_price:
                self.BuyMarket()
                self._reset_short_state()
            elif self._short_take_price is not None and low <= self._short_take_price:
                self.BuyMarket()
                self._reset_short_state()
        else:
            self._short_stop_price = None
            self._short_take_price = None

    def _handle_reentries(self, candle):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        distance = int(self.PriceStepTicks) * step
        close = float(candle.ClosePrice)

        if distance <= 0.0:
            return

        if self.EnableBuyEntries and self.Position > 0 and self._long_order_count > 0 and self._long_order_count < int(self.MaxPyramidingPositions):
            if close >= self._last_long_entry_price + distance:
                self.BuyMarket()
                self._long_order_count += 1
                self._last_long_entry_price = close
                self._update_long_risk_levels(close)

        if self.EnableSellEntries and self.Position < 0 and self._short_order_count > 0 and self._short_order_count < int(self.MaxPyramidingPositions):
            if close <= self._last_short_entry_price - distance:
                self.SellMarket()
                self._short_order_count += 1
                self._last_short_entry_price = close
                self._update_short_risk_levels(close)

    def _update_long_risk_levels(self, entry_price):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        sl = int(self.StopLossTicks)
        tp = int(self.TakeProfitTicks)
        self._long_stop_price = entry_price - sl * step if sl > 0 else None
        self._long_take_price = entry_price + tp * step if tp > 0 else None

    def _update_short_risk_levels(self, entry_price):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        sl = int(self.StopLossTicks)
        tp = int(self.TakeProfitTicks)
        self._short_stop_price = entry_price + sl * step if sl > 0 else None
        self._short_take_price = entry_price - tp * step if tp > 0 else None

    def _reset_long_state(self):
        self._long_order_count = 0
        self._last_long_entry_price = 0.0
        self._long_stop_price = None
        self._long_take_price = None

    def _reset_short_state(self):
        self._short_order_count = 0
        self._last_short_entry_price = 0.0
        self._short_stop_price = None
        self._short_take_price = None

    def _get_applied_price(self, candle):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        ap = int(self.AppliedPrice)

        if ap == PRICE_OPEN:
            return o
        elif ap == PRICE_HIGH:
            return h
        elif ap == PRICE_LOW:
            return l
        elif ap == PRICE_MEDIAN:
            return (h + l) / 2.0
        elif ap == PRICE_TYPICAL:
            return (c + h + l) / 3.0
        elif ap == PRICE_WEIGHTED:
            return (2.0 * c + h + l) / 4.0
        elif ap == PRICE_SIMPLE:
            return (o + c) / 2.0
        elif ap == PRICE_QUARTER:
            return (o + c + h + l) / 4.0
        elif ap == PRICE_TRENDFOLLOW0:
            if c > o:
                return h
            elif c < o:
                return l
            else:
                return c
        elif ap == PRICE_TRENDFOLLOW1:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (l + c) / 2.0
            else:
                return c
        elif ap == PRICE_DEMARK:
            return self._calculate_demark_price(candle)
        else:
            return c

    def _calculate_demark_price(self, candle):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        res = h + l + c
        if c < o:
            res = (res + l) / 2.0
        elif c > o:
            res = (res + h) / 2.0
        else:
            res = (res + c) / 2.0
        return ((res - l) + (res - h)) / 2.0

    def OnReseted(self):
        super(xo_signal_re_open_strategy, self).OnReseted()
        self._signal_queue = []
        self._hi = 0.0
        self._lo = 0.0
        self._kr = 0
        self._no = 0
        self._trend = 0
        self._initialized = False
        self._last_buy_signal_time = None
        self._last_sell_signal_time = None
        self._last_executed_buy_signal_time = None
        self._last_executed_sell_signal_time = None
        self._long_order_count = 0
        self._short_order_count = 0
        self._last_long_entry_price = 0.0
        self._last_short_entry_price = 0.0
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def CreateClone(self):
        return xo_signal_re_open_strategy()
