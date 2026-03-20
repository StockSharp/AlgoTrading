import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SmoothedMovingAverage, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_ma_crossover_strategy(Strategy):
    def __init__(self):
        super(adx_ma_crossover_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 15)
        self._adx_period = self.Param("AdxPeriod", 12)
        self._adx_threshold = self.Param("AdxThreshold", 25.0)
        self._take_profit_buy = self.Param("TakeProfitBuy", 83.0)
        self._stop_loss_buy = self.Param("StopLossBuy", 55.0)
        self._trailing_stop_buy = self.Param("TrailingStopBuy", 27.0)
        self._take_profit_sell = self.Param("TakeProfitSell", 63.0)
        self._stop_loss_sell = self.Param("StopLossSell", 50.0)
        self._trailing_stop_sell = self.Param("TrailingStopSell", 27.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._pip_size = 1.0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_adx = 0.0
        self._has_prev = False
        self._has_prev_prev = False
        self._long_entry = 0.0
        self._long_stop = 0.0
        self._long_tp = 0.0
        self._short_entry = 0.0
        self._short_stop = 0.0
        self._short_tp = 0.0

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def TakeProfitBuy(self):
        return self._take_profit_buy.Value

    @TakeProfitBuy.setter
    def TakeProfitBuy(self, value):
        self._take_profit_buy.Value = value

    @property
    def StopLossBuy(self):
        return self._stop_loss_buy.Value

    @StopLossBuy.setter
    def StopLossBuy(self, value):
        self._stop_loss_buy.Value = value

    @property
    def TrailingStopBuy(self):
        return self._trailing_stop_buy.Value

    @TrailingStopBuy.setter
    def TrailingStopBuy(self, value):
        self._trailing_stop_buy.Value = value

    @property
    def TakeProfitSell(self):
        return self._take_profit_sell.Value

    @TakeProfitSell.setter
    def TakeProfitSell(self, value):
        self._take_profit_sell.Value = value

    @property
    def StopLossSell(self):
        return self._stop_loss_sell.Value

    @StopLossSell.setter
    def StopLossSell(self, value):
        self._stop_loss_sell.Value = value

    @property
    def TrailingStopSell(self):
        return self._trailing_stop_sell.Value

    @TrailingStopSell.setter
    def TrailingStopSell(self, value):
        self._trailing_stop_sell.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(adx_ma_crossover_strategy, self).OnStarted(time)

        self._pip_size = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._pip_size <= 0.0:
            self._pip_size = 1.0

        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_adx = 0.0
        self._has_prev = False
        self._has_prev_prev = False
        self._reset_long_targets()
        self._reset_short_targets()

        self._ma = SmoothedMovingAverage()
        self._ma.Length = self.MaPeriod
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFinal:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        median = (high + low) / 2.0

        ma_result = self._ma.Process(self._ma.CreateValue(candle.OpenTime, median))
        if not ma_result.IsFinal:
            return

        ma_val = float(ma_result)
        adx_typed = adx_value
        adx_ma = adx_typed.MovingAverage
        adx_val = float(adx_ma) if adx_ma is not None else 0.0

        if self._has_prev and self._has_prev_prev:
            self._manage_open_positions(close)

            long_signal = self._prev_close > self._prev_ma and self._prev_prev_close < self._prev_ma and self._prev_adx >= float(self.AdxThreshold)
            short_signal = self._prev_close < self._prev_ma and self._prev_prev_close > self._prev_ma and self._prev_adx >= float(self.AdxThreshold)

            if long_signal and self.Position <= 0:
                self.BuyMarket()
                self._init_long_targets(self._prev_close)
            elif short_signal and self.Position >= 0:
                self.SellMarket()
                self._init_short_targets(self._prev_close)

        self._update_history(close, ma_val, adx_val)

    def _manage_open_positions(self, current_close):
        if self.Position > 0:
            if self._prev_close < self._prev_ma:
                self.SellMarket()
                self._reset_long_targets()
                return

            self._update_long_trailing(current_close)

            if self._long_tp > 0.0 and current_close >= self._long_tp:
                self.SellMarket()
                self._reset_long_targets()
                return
            if self._long_stop > 0.0 and current_close <= self._long_stop:
                self.SellMarket()
                self._reset_long_targets()
                return

        elif self.Position < 0:
            if self._prev_close > self._prev_ma:
                self.BuyMarket()
                self._reset_short_targets()
                return

            self._update_short_trailing(current_close)

            if self._short_tp > 0.0 and current_close <= self._short_tp:
                self.BuyMarket()
                self._reset_short_targets()
                return
            if self._short_stop > 0.0 and current_close >= self._short_stop:
                self.BuyMarket()
                self._reset_short_targets()
                return
        else:
            self._reset_long_targets()
            self._reset_short_targets()

    def _update_long_trailing(self, current_close):
        trail_buy = float(self.TrailingStopBuy)
        if trail_buy <= 0.0 or self._long_entry <= 0.0:
            return
        trail_dist = trail_buy * self._pip_size
        if trail_dist <= 0.0:
            return
        profit = current_close - self._long_entry
        if profit <= trail_dist:
            return
        new_stop = current_close - trail_dist
        if new_stop > self._long_stop:
            self._long_stop = new_stop

    def _update_short_trailing(self, current_close):
        trail_sell = float(self.TrailingStopSell)
        if trail_sell <= 0.0 or self._short_entry <= 0.0:
            return
        trail_dist = trail_sell * self._pip_size
        if trail_dist <= 0.0:
            return
        profit = self._short_entry - current_close
        if profit <= trail_dist:
            return
        new_stop = current_close + trail_dist
        if self._short_stop == 0.0 or new_stop < self._short_stop:
            self._short_stop = new_stop

    def _init_long_targets(self, entry):
        self._long_entry = entry
        sl_buy = float(self.StopLossBuy)
        tp_buy = float(self.TakeProfitBuy)
        self._long_stop = entry - sl_buy * self._pip_size if sl_buy > 0.0 else 0.0
        self._long_tp = entry + tp_buy * self._pip_size if tp_buy > 0.0 else 0.0
        self._reset_short_targets()

    def _init_short_targets(self, entry):
        self._short_entry = entry
        sl_sell = float(self.StopLossSell)
        tp_sell = float(self.TakeProfitSell)
        self._short_stop = entry + sl_sell * self._pip_size if sl_sell > 0.0 else 0.0
        self._short_tp = entry - tp_sell * self._pip_size if tp_sell > 0.0 else 0.0
        self._reset_long_targets()

    def _reset_long_targets(self):
        self._long_entry = 0.0
        self._long_stop = 0.0
        self._long_tp = 0.0

    def _reset_short_targets(self):
        self._short_entry = 0.0
        self._short_stop = 0.0
        self._short_tp = 0.0

    def _update_history(self, close, ma, adx):
        if self._has_prev:
            self._prev_prev_close = self._prev_close
            self._has_prev_prev = True
        self._prev_close = close
        self._prev_ma = ma
        self._prev_adx = adx
        self._has_prev = True

    def OnReseted(self):
        super(adx_ma_crossover_strategy, self).OnReseted()
        self._pip_size = 1.0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_adx = 0.0
        self._has_prev = False
        self._has_prev_prev = False
        self._reset_long_targets()
        self._reset_short_targets()

    def CreateClone(self):
        return adx_ma_crossover_strategy()
