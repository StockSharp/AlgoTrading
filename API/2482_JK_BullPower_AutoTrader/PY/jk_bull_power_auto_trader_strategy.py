import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BullPower
from StockSharp.Algo.Strategies import Strategy


class jk_bull_power_auto_trader_strategy(Strategy):
    def __init__(self):
        super(jk_bull_power_auto_trader_strategy, self).__init__()
        self._bulls_period = self.Param("BullsPeriod", 13)
        self._tp_points = self.Param("TakeProfitPoints", 350.0)
        self._sl_points = self.Param("StopLossPoints", 100.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 100.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 40.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_bulls = None
        self._prev_prev_bulls = None
        self._entry_price = 0.0
        self._stop_price = None
        self._tp_price = None
        self._price_step = 1.0
        self._bulls_power = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(jk_bull_power_auto_trader_strategy, self).OnReseted()
        self._prev_bulls = None
        self._prev_prev_bulls = None
        self._stop_price = None
        self._tp_price = None
        self._entry_price = 0.0
        self._price_step = 1.0

    def OnStarted(self, time):
        super(jk_bull_power_auto_trader_strategy, self).OnStarted(time)

        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if self._price_step <= 0:
            self._price_step = 1.0

        self._prev_bulls = None
        self._prev_prev_bulls = None
        self._stop_price = None
        self._tp_price = None

        self._bulls_power = BullPower()
        self._bulls_power.Length = self._bulls_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._bulls_power, self._process_candle).Start()

    def _process_candle(self, candle, bulls_val):
        if candle.State != CandleStates.Finished:
            return

        bv = float(bulls_val)

        self._update_trailing(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_history(bv)
            return

        self._check_risk(candle)

        if not self._bulls_power.IsFormed:
            self._update_history(bv)
            return

        if self._prev_bulls is None or self._prev_prev_bulls is None:
            self._update_history(bv)
            return

        sell_signal = self._prev_prev_bulls > self._prev_bulls and self._prev_bulls > 0 and bv < self._prev_bulls
        buy_signal = self._prev_prev_bulls < self._prev_bulls and self._prev_bulls < 0 and bv > self._prev_bulls

        pos = float(self.Position)
        vol = float(self.Volume)

        if sell_signal and pos >= 0:
            volume = vol + (pos if pos > 0 else 0.0)
            if volume > 0:
                self.SellMarket(volume)
                self._init_targets(False, float(candle.ClosePrice))
        elif buy_signal and pos <= 0:
            volume = vol + (abs(pos) if pos < 0 else 0.0)
            if volume > 0:
                self.BuyMarket(volume)
                self._init_targets(True, float(candle.ClosePrice))

        self._update_history(bv)

    def _update_trailing(self, candle):
        pos = float(self.Position)
        if pos == 0 or float(self._trailing_stop_points.Value) <= 0:
            return

        trailing_distance = float(self._trailing_stop_points.Value) * self._price_step
        if trailing_distance <= 0:
            return

        trailing_step = float(self._trailing_step_points.Value) * self._price_step
        close = float(candle.ClosePrice)

        if pos > 0:
            profit = close - self._entry_price
            if profit <= trailing_distance:
                return
            candidate = close - trailing_distance
            if self._stop_price is None or (candidate > self._stop_price and (trailing_step <= 0 or candidate - self._stop_price >= trailing_step)):
                self._stop_price = candidate
        else:
            profit = self._entry_price - close
            if profit <= trailing_distance:
                return
            candidate = close + trailing_distance
            if self._stop_price is None or (candidate < self._stop_price and (trailing_step <= 0 or self._stop_price - candidate >= trailing_step)):
                self._stop_price = candidate

    def _check_risk(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_targets()
                return
            if self._tp_price is not None and float(candle.HighPrice) >= self._tp_price:
                self.SellMarket(pos)
                self._reset_targets()
        elif pos < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()
                return
            if self._tp_price is not None and float(candle.LowPrice) <= self._tp_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()
        else:
            self._reset_targets()

    def _init_targets(self, is_long, entry_price):
        self._entry_price = entry_price
        stop_dist = float(self._sl_points.Value) * self._price_step
        take_dist = float(self._tp_points.Value) * self._price_step

        if stop_dist > 0:
            self._stop_price = entry_price - stop_dist if is_long else entry_price + stop_dist
        else:
            self._stop_price = None

        if take_dist > 0:
            self._tp_price = entry_price + take_dist if is_long else entry_price - take_dist
        else:
            self._tp_price = None

    def _reset_targets(self):
        self._stop_price = None
        self._tp_price = None

    def _update_history(self, bv):
        self._prev_prev_bulls = self._prev_bulls
        self._prev_bulls = bv

    def CreateClone(self):
        return jk_bull_power_auto_trader_strategy()
