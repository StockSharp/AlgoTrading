import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class reduce_risks_strategy(Strategy):
    """Multi-timeframe trend-following strategy converted from Reduce Risks MQL5 EA."""

    def __init__(self):
        super(reduce_risks_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 30)
        self._take_profit_pips = self.Param("TakeProfitPips", 60)
        self._initial_deposit = self.Param("InitialDeposit", 1000.0)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        # Internal indicators (all on same timeframe since harness is single-tf)
        self._sma5 = None
        self._sma8 = None
        self._sma13 = None
        self._sma60 = None

        # Rolling SMA values: [current, prev1, prev2, prev3]
        self._sma5_vals = [None] * 4
        self._sma8_vals = [None] * 4
        self._sma13_vals = [None] * 4
        self._sma60_vals = [None] * 4

        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._pip_size = 0.0
        self._price_step = 0.0
        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0
        self._highest_since_entry = None
        self._lowest_since_entry = None
        self._long_bars_since_entry = 0
        self._short_bars_since_entry = 0
        self._previous_position = 0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def InitialDeposit(self):
        return self._initial_deposit.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    def OnStarted(self, time):
        super(reduce_risks_strategy, self).OnStarted(time)

        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        if self._price_step == 0:
            self._price_step = 0.01
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 2
        self._pip_size = self._price_step * 10.0 if (decimals == 3 or decimals == 5) else self._price_step
        if self._pip_size == 0:
            self._pip_size = 0.01

        self._risk_threshold = self.InitialDeposit * (100.0 - self.RiskPercent) / 100.0

        self._sma5 = SimpleMovingAverage()
        self._sma5.Length = 5
        self._sma8 = SimpleMovingAverage()
        self._sma8.Length = 8
        self._sma13 = SimpleMovingAverage()
        self._sma13.Length = 13
        self._sma60 = SimpleMovingAverage()
        self._sma60.Length = 60

        self._sma5_vals = [None] * 4
        self._sma8_vals = [None] * 4
        self._sma13_vals = [None] * 4
        self._sma60_vals = [None] * 4

        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._previous_position = 0
        self._entry_price = 0.0
        self._highest_since_entry = None
        self._lowest_since_entry = None
        self._long_bars_since_entry = 0
        self._short_bars_since_entry = 0
        self._risk_exceeded_counter = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        tp_unit = None
        sl_unit = None
        if self.TakeProfitPips > 0 and self._pip_size > 0:
            tp_unit = Unit(self.TakeProfitPips * self._pip_size, UnitTypes.Absolute)
        if self.StopLossPips > 0 and self._pip_size > 0:
            sl_unit = Unit(self.StopLossPips * self._pip_size, UnitTypes.Absolute)
        self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma5)
            self.DrawIndicator(area, self._sma8)
            self.DrawIndicator(area, self._sma13)
            self.DrawIndicator(area, self._sma60)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        typical = (h + l + c) / 3.0

        t = candle.OpenTime
        self._process_sma(self._sma5, self._sma5_vals, typical, t)
        self._process_sma(self._sma8, self._sma8_vals, typical, t)
        self._process_sma(self._sma13, self._sma13_vals, typical, t)
        self._process_sma(self._sma60, self._sma60_vals, typical, t)

        sma5 = self._sma5_vals[0]
        sma5_prev2 = self._sma5_vals[2]
        sma8 = self._sma8_vals[0]
        sma8_prev1 = self._sma8_vals[1]
        sma13 = self._sma13_vals[0]
        sma60 = self._sma60_vals[0]
        sma60_prev2 = self._sma60_vals[2]

        all_ready = (sma5 is not None and sma5_prev2 is not None and
                     sma8 is not None and sma8_prev1 is not None and
                     sma13 is not None and
                     sma60 is not None and sma60_prev2 is not None and
                     self._prev1 is not None and self._prev2 is not None and self._prev3 is not None)

        if not all_ready:
            self._handle_position_state()
            self._update_history(candle)
            self._previous_position = self.Position
            return

        pf = self.Portfolio
        equity = float(pf.CurrentValue) if pf is not None and pf.CurrentValue is not None else self.InitialDeposit
        risk_exceeded = equity <= self._risk_threshold and self.InitialDeposit > 0

        if risk_exceeded:
            if self._risk_exceeded_counter < 15:
                self._risk_exceeded_counter += 1
        else:
            self._risk_exceeded_counter = 0

        if self.Position == 0 and not risk_exceeded:
            close_price = float(candle.ClosePrice)

            ma_slope_up = sma5 > sma5_prev2 and sma60 > sma60_prev2
            ma_hierarchy_up = sma5 > sma8 and sma8 > sma13
            price_above = (close_price > sma5 and close_price > sma8 and
                           close_price > sma13 and close_price > sma60)

            prev1_up = float(self._prev1[3]) > float(self._prev1[0])
            prev2_up = float(self._prev2[3]) > float(self._prev2[0])

            long_cond = (ma_slope_up and ma_hierarchy_up and price_above and
                         prev1_up and prev2_up and
                         close_price > float(self._prev1[1]))

            if long_cond:
                self.BuyMarket()
                self._entry_price = close_price
            else:
                ma_slope_dn = sma5 < sma5_prev2 and sma60 < sma60_prev2
                ma_hierarchy_dn = sma5 < sma8 and sma8 < sma13
                price_below = (close_price < sma5 and close_price < sma8 and
                               close_price < sma13 and close_price < sma60)
                prev1_dn = float(self._prev1[3]) < float(self._prev1[0])
                prev2_dn = float(self._prev2[3]) < float(self._prev2[0])

                short_cond = (ma_slope_dn and ma_hierarchy_dn and price_below and
                              prev1_dn and prev2_dn and
                              close_price < float(self._prev1[2]))

                if short_cond:
                    self.SellMarket()
                    self._entry_price = close_price
        else:
            self._handle_active_position(candle, risk_exceeded)

        self._handle_position_state()
        self._update_history(candle)
        self._previous_position = self.Position

    def _handle_active_position(self, candle, risk_exceeded):
        close_price = float(candle.ClosePrice)
        pip = self._pip_size

        if self.Position > 0:
            if self._previous_position <= 0:
                self._highest_since_entry = float(candle.HighPrice)
                self._long_bars_since_entry = 0
            else:
                h = float(candle.HighPrice)
                if self._highest_since_entry is None or h > self._highest_since_entry:
                    self._highest_since_entry = h
                self._long_bars_since_entry += 1

            entry = self._entry_price
            collapse = close_price <= float(candle.OpenPrice) - 10.0 * pip
            profit_zone = close_price - entry >= 10.0 * pip
            trailing = (self._long_bars_since_entry >= 1 and
                        self._highest_since_entry is not None and
                        self._highest_since_entry > entry and
                        self._highest_since_entry - close_price >= 20.0 * pip)
            stop_hit = entry - close_price >= 20.0 * pip

            if collapse or profit_zone or trailing or stop_hit or risk_exceeded:
                self.SellMarket()

        elif self.Position < 0:
            if self._previous_position >= 0:
                self._lowest_since_entry = float(candle.LowPrice)
                self._short_bars_since_entry = 0
            else:
                lo = float(candle.LowPrice)
                if self._lowest_since_entry is None or lo < self._lowest_since_entry:
                    self._lowest_since_entry = lo
                self._short_bars_since_entry += 1

            entry = self._entry_price
            collapse = close_price >= float(candle.OpenPrice) + 10.0 * pip
            profit_zone = entry - close_price >= 10.0 * pip
            trailing = (self._short_bars_since_entry >= 1 and
                        self._lowest_since_entry is not None and
                        self._lowest_since_entry < entry and
                        close_price - self._lowest_since_entry >= 20.0 * pip)
            stop_hit = close_price - entry >= 20.0 * pip

            if collapse or profit_zone or trailing or stop_hit or risk_exceeded:
                self.BuyMarket()

    def _handle_position_state(self):
        if self.Position <= 0:
            self._highest_since_entry = None
            self._long_bars_since_entry = 0

        if self.Position >= 0:
            self._lowest_since_entry = None
            self._short_bars_since_entry = 0

        if self.Position == 0:
            self._entry_price = 0.0

    def _update_history(self, candle):
        self._prev3 = self._prev2
        self._prev2 = self._prev1
        self._prev1 = (float(candle.OpenPrice), float(candle.HighPrice),
                       float(candle.LowPrice), float(candle.ClosePrice))

    def _process_sma(self, sma, vals, input_val, time):
        iv = sma.Process(DecimalIndicatorValue(sma, input_val, time))
        if not sma.IsFormed:
            return
        v = float(iv.ToDecimal())
        vals[3] = vals[2]
        vals[2] = vals[1]
        vals[1] = vals[0]
        vals[0] = v

    def OnReseted(self):
        super(reduce_risks_strategy, self).OnReseted()
        self._sma5_vals = [None] * 4
        self._sma8_vals = [None] * 4
        self._sma13_vals = [None] * 4
        self._sma60_vals = [None] * 4
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None
        self._pip_size = 0.0
        self._price_step = 0.0
        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0
        self._highest_since_entry = None
        self._lowest_since_entry = None
        self._long_bars_since_entry = 0
        self._short_bars_since_entry = 0
        self._previous_position = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return reduce_risks_strategy()
