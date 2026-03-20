import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class up3x1_premium_strategy(Strategy):
    def __init__(self):
        super(up3x1_premium_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0)
        self._fast_ema_length = self.Param("FastEmaLength", 12)
        self._slow_ema_length = self.Param("SlowEmaLength", 26)
        self._take_profit = self.Param("TakeProfit", 0.015)
        self._stop_loss = self.Param("StopLoss", 0.01)
        self._trailing_stop = self.Param("TrailingStop", 0.001)
        self._range_threshold = self.Param("RangeThreshold", 0.006)
        self._body_threshold = self.Param("BodyThreshold", 0.005)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._fast_prev = None
        self._fast_prev2 = None
        self._slow_prev = None
        self._slow_prev2 = None
        self._prev_candle_open = None
        self._prev_candle_close = None
        self._prev_candle_high = None
        self._prev_candle_low = None
        self._prev_prev_candle_open = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def FastEmaLength(self):
        return self._fast_ema_length.Value

    @FastEmaLength.setter
    def FastEmaLength(self, value):
        self._fast_ema_length.Value = value

    @property
    def SlowEmaLength(self):
        return self._slow_ema_length.Value

    @SlowEmaLength.setter
    def SlowEmaLength(self, value):
        self._slow_ema_length.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def RangeThreshold(self):
        return self._range_threshold.Value

    @RangeThreshold.setter
    def RangeThreshold(self, value):
        self._range_threshold.Value = value

    @property
    def BodyThreshold(self):
        return self._body_threshold.Value

    @BodyThreshold.setter
    def BodyThreshold(self, value):
        self._body_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(up3x1_premium_strategy, self).OnStarted(time)

        self._fast_prev = None
        self._fast_prev2 = None
        self._slow_prev = None
        self._slow_prev2 = None
        self._prev_candle_open = None
        self._prev_candle_close = None
        self._prev_candle_high = None
        self._prev_candle_low = None
        self._prev_prev_candle_open = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast_ema_val, slow_ema_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_ema_val)
        slow_val = float(slow_ema_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)

        self._manage_open_position(candle)

        have_history = (self._prev_candle_open is not None and
                        self._prev_prev_candle_open is not None and
                        self._fast_prev is not None and self._fast_prev2 is not None and
                        self._slow_prev is not None and self._slow_prev2 is not None)

        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        trail = float(self.TrailingStop)
        range_th = float(self.RangeThreshold)
        body_th = float(self.BodyThreshold)

        if self.Position == 0 and have_history:
            bullish_cross = (self._fast_prev2 < self._slow_prev2 and
                             self._fast_prev > self._slow_prev and
                             self._prev_prev_candle_open < self._prev_candle_open)

            wide_bullish = (self._prev_candle_high is not None and
                            self._prev_candle_low is not None and
                            (self._prev_candle_high - self._prev_candle_low) > range_th and
                            self._prev_candle_close > self._prev_candle_open and
                            (self._prev_candle_close - self._prev_candle_open) > body_th)

            long_signal = bullish_cross or wide_bullish

            bearish_cross = (self._fast_prev2 > self._slow_prev2 and
                             self._fast_prev < self._slow_prev and
                             self._prev_prev_candle_open > self._prev_candle_open)

            wide_bearish = (self._prev_candle_high is not None and
                            self._prev_candle_low is not None and
                            (self._prev_candle_high - self._prev_candle_low) > range_th and
                            self._prev_candle_open > self._prev_candle_close and
                            (self._prev_candle_open - self._prev_candle_close) > body_th)

            short_signal = bearish_cross or wide_bearish

            if long_signal and short_signal:
                if self._fast_prev >= self._slow_prev:
                    short_signal = False
                else:
                    long_signal = False

            if long_signal:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - sl if sl > 0.0 else None
                self._take_profit_price = close + tp if tp > 0.0 else None
                self._trailing_stop_price = close - trail if trail > 0.0 else None

            elif short_signal:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + sl if sl > 0.0 else None
                self._take_profit_price = close - tp if tp > 0.0 else None
                self._trailing_stop_price = close + trail if trail > 0.0 else None

        self._prev_prev_candle_open = self._prev_candle_open
        self._prev_candle_open = open_price
        self._prev_candle_close = close
        self._prev_candle_high = high
        self._prev_candle_low = low
        self._fast_prev2 = self._fast_prev
        self._fast_prev = fast_val
        self._slow_prev2 = self._slow_prev
        self._slow_prev = slow_val

    def _manage_open_position(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail = float(self.TrailingStop)

        if self.Position > 0:
            if trail > 0.0 and self._entry_price is not None:
                move = high - self._entry_price
                if move >= trail:
                    new_stop = high - trail
                    if self._trailing_stop_price is None or new_stop > self._trailing_stop_price:
                        self._trailing_stop_price = new_stop

            do_exit = False
            if self._take_profit_price is not None and high >= self._take_profit_price:
                do_exit = True
            if not do_exit and self._stop_price is not None and low <= self._stop_price:
                do_exit = True
            if not do_exit and self._trailing_stop_price is not None and low <= self._trailing_stop_price:
                do_exit = True
            if not do_exit and self._are_ema_near():
                do_exit = True

            if do_exit:
                self.SellMarket()
                self._clear_trade_levels()

        elif self.Position < 0:
            if trail > 0.0 and self._entry_price is not None:
                move = self._entry_price - low
                if move >= trail:
                    new_stop = low + trail
                    if self._trailing_stop_price is None or new_stop < self._trailing_stop_price:
                        self._trailing_stop_price = new_stop

            do_exit = False
            if self._take_profit_price is not None and low <= self._take_profit_price:
                do_exit = True
            if not do_exit and self._stop_price is not None and high >= self._stop_price:
                do_exit = True
            if not do_exit and self._trailing_stop_price is not None and high >= self._trailing_stop_price:
                do_exit = True
            if not do_exit and self._are_ema_near():
                do_exit = True

            if do_exit:
                self.BuyMarket()
                self._clear_trade_levels()

    def _are_ema_near(self):
        if self._fast_prev is None or self._slow_prev is None:
            return False
        if self._slow_prev == 0.0:
            return False
        diff = abs(self._fast_prev - self._slow_prev)
        return diff <= abs(self._slow_prev) * 0.001

    def _clear_trade_levels(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None

    def OnReseted(self):
        super(up3x1_premium_strategy, self).OnReseted()
        self._fast_prev = None
        self._fast_prev2 = None
        self._slow_prev = None
        self._slow_prev2 = None
        self._prev_candle_open = None
        self._prev_candle_close = None
        self._prev_candle_high = None
        self._prev_candle_low = None
        self._prev_prev_candle_open = None
        self._clear_trade_levels()

    def CreateClone(self):
        return up3x1_premium_strategy()
