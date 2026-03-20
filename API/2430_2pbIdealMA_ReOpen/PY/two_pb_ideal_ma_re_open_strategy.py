import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class two_pb_ideal_ma_re_open_strategy(Strategy):
    def __init__(self):
        super(two_pb_ideal_ma_re_open_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._position_volume = self.Param("PositionVolume", 0.1)
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000)
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000)
        self._price_step_ticks = self.Param("PriceStepTicks", 300)
        self._max_re_entries = self.Param("MaxReEntries", 10)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exits = self.Param("EnableBuyExits", True)
        self._enable_sell_exits = self.Param("EnableSellExits", True)
        self._signal_bar_shift = self.Param("SignalBarShift", 1)
        self._period1 = self.Param("Period1", 10)
        self._period2 = self.Param("Period2", 10)
        self._period_x1 = self.Param("PeriodX1", 10)
        self._period_x2 = self.Param("PeriodX2", 10)
        self._period_y1 = self.Param("PeriodY1", 10)
        self._period_y2 = self.Param("PeriodY2", 10)
        self._period_z1 = self.Param("PeriodZ1", 10)
        self._period_z2 = self.Param("PeriodZ2", 10)

        self._fast_history = []
        self._slow_history = []
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_re_entries = 0
        self._sell_re_entries = 0

        self._fast_initialized = False
        self._fast_prev_price = 0.0
        self._fast_prev_value = 0.0

        self._slow_initialized = False
        self._slow_prev_price = 0.0
        self._slow_prev_x = 0.0
        self._slow_prev_y = 0.0
        self._slow_prev_z = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PositionVolume(self):
        return self._position_volume.Value

    @PositionVolume.setter
    def PositionVolume(self, value):
        self._position_volume.Value = value

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
    def MaxReEntries(self):
        return self._max_re_entries.Value

    @MaxReEntries.setter
    def MaxReEntries(self, value):
        self._max_re_entries.Value = value

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
    def SignalBarShift(self):
        return self._signal_bar_shift.Value

    @SignalBarShift.setter
    def SignalBarShift(self, value):
        self._signal_bar_shift.Value = value

    @property
    def Period1(self):
        return self._period1.Value

    @Period1.setter
    def Period1(self, value):
        self._period1.Value = value

    @property
    def Period2(self):
        return self._period2.Value

    @Period2.setter
    def Period2(self, value):
        self._period2.Value = value

    @property
    def PeriodX1(self):
        return self._period_x1.Value

    @PeriodX1.setter
    def PeriodX1(self, value):
        self._period_x1.Value = value

    @property
    def PeriodX2(self):
        return self._period_x2.Value

    @PeriodX2.setter
    def PeriodX2(self, value):
        self._period_x2.Value = value

    @property
    def PeriodY1(self):
        return self._period_y1.Value

    @PeriodY1.setter
    def PeriodY1(self, value):
        self._period_y1.Value = value

    @property
    def PeriodY2(self):
        return self._period_y2.Value

    @PeriodY2.setter
    def PeriodY2(self, value):
        self._period_y2.Value = value

    @property
    def PeriodZ1(self):
        return self._period_z1.Value

    @PeriodZ1.setter
    def PeriodZ1(self, value):
        self._period_z1.Value = value

    @property
    def PeriodZ2(self):
        return self._period_z2.Value

    @PeriodZ2.setter
    def PeriodZ2(self, value):
        self._period_z2.Value = value

    def _calc_ideal(self, w1, w2, prev_input, curr_input, prev_value):
        diff = curr_input - prev_input
        dsm1 = diff * diff - 1.0
        denom = 1.0 + w2 * dsm1
        if denom == 0.0:
            return curr_input
        num = w1 * (curr_input - prev_value) + prev_value + w2 * prev_value * dsm1
        return num / denom

    def _process_fast(self, price):
        if not self._fast_initialized:
            self._fast_initialized = True
            self._fast_prev_price = price
            self._fast_prev_value = price
            return price
        w1 = 1.0 / max(int(self.Period1), 1)
        w2 = 1.0 / max(int(self.Period2), 1)
        result = self._calc_ideal(w1, w2, self._fast_prev_price, price, self._fast_prev_value)
        self._fast_prev_price = price
        self._fast_prev_value = result
        return result

    def _process_slow(self, price):
        if not self._slow_initialized:
            self._slow_initialized = True
            self._slow_prev_price = price
            self._slow_prev_x = price
            self._slow_prev_y = price
            self._slow_prev_z = price
            return price
        wx1 = 1.0 / max(int(self.PeriodX1), 1)
        wx2 = 1.0 / max(int(self.PeriodX2), 1)
        wy1 = 1.0 / max(int(self.PeriodY1), 1)
        wy2 = 1.0 / max(int(self.PeriodY2), 1)
        wz1 = 1.0 / max(int(self.PeriodZ1), 1)
        wz2 = 1.0 / max(int(self.PeriodZ2), 1)
        x = self._calc_ideal(wx1, wx2, self._slow_prev_price, price, self._slow_prev_x)
        y = self._calc_ideal(wy1, wy2, self._slow_prev_x, x, self._slow_prev_y)
        z = self._calc_ideal(wz1, wz2, self._slow_prev_y, y, self._slow_prev_z)
        self._slow_prev_price = price
        self._slow_prev_x = x
        self._slow_prev_y = y
        self._slow_prev_z = z
        return z

    def OnStarted(self, time):
        super(two_pb_ideal_ma_re_open_strategy, self).OnStarted(time)

        self._fast_history = []
        self._slow_history = []
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_re_entries = 0
        self._sell_re_entries = 0
        self._fast_initialized = False
        self._slow_initialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

        if float(self.PositionVolume) > 0.0:
            self.Volume = self.PositionVolume

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        fast = self._process_fast(price)
        slow = self._process_slow(price)

        self._fast_history.append(fast)
        self._slow_history.append(slow)

        shift = int(self.SignalBarShift)
        max_count = max(shift + 2, 3)
        while len(self._fast_history) > max_count:
            self._fast_history.pop(0)
        while len(self._slow_history) > max_count:
            self._slow_history.pop(0)

        if len(self._fast_history) < shift + 2 or len(self._slow_history) < shift + 2:
            return

        ci = len(self._fast_history) - 1 - shift
        pi = ci - 1
        if pi < 0:
            return

        fast_curr = self._fast_history[ci]
        fast_prev = self._fast_history[pi]
        slow_curr = self._slow_history[ci]
        slow_prev = self._slow_history[pi]

        bearish_cross = fast_prev > slow_prev and fast_curr < slow_curr
        bullish_cross = fast_prev < slow_prev and fast_curr > slow_curr

        if self.EnableBuyExits and bullish_cross and self.Position > 0:
            self.SellMarket()
            self._last_buy_price = 0.0
            self._buy_re_entries = 0

        if self.EnableSellExits and bearish_cross and self.Position < 0:
            self.BuyMarket()
            self._last_sell_price = 0.0
            self._sell_re_entries = 0

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        re_entry_dist = float(self.PriceStepTicks) * step

        if int(self.PriceStepTicks) > 0 and re_entry_dist > 0.0:
            if self.Position > 0 and self._buy_re_entries < int(self.MaxReEntries):
                advance = price - self._last_buy_price
                if advance >= re_entry_dist:
                    self.BuyMarket()
                    self._buy_re_entries += 1
                    self._last_buy_price = price
            elif self.Position < 0 and self._sell_re_entries < int(self.MaxReEntries):
                advance = self._last_sell_price - price
                if advance >= re_entry_dist:
                    self.SellMarket()
                    self._sell_re_entries += 1
                    self._last_sell_price = price

        if bearish_cross and self.EnableBuyEntries and self.Position == 0:
            self.BuyMarket()
            self._last_buy_price = price
            self._buy_re_entries = 0
            self._sell_re_entries = 0
        elif bullish_cross and self.EnableSellEntries and self.Position == 0:
            self.SellMarket()
            self._last_sell_price = price
            self._sell_re_entries = 0
            self._buy_re_entries = 0

    def OnReseted(self):
        super(two_pb_ideal_ma_re_open_strategy, self).OnReseted()
        self._fast_history = []
        self._slow_history = []
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_re_entries = 0
        self._sell_re_entries = 0

    def CreateClone(self):
        return two_pb_ideal_ma_re_open_strategy()
