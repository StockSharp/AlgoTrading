import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class mava_xonax_strategy(Strategy):
    """
    Mava Xonax: EMA cross of open/close prices with SL/TP.
    """

    def __init__(self):
        super(mava_xonax_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 6).SetDisplay("EMA Period", "EMA period", "General")
        self._cooldown_bars = self.Param("SignalCooldownBars", 1).SetDisplay("Signal Cooldown", "Bars to wait after an entry or exit", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_open1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close1 = 0.0
        self._prev_close2 = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._history = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mava_xonax_strategy, self).OnReseted()
        self._prev_open1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close1 = 0.0
        self._prev_close2 = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._history = 0
        self._cooldown_remaining = 0
        self._ema_close = None
        self._ema_open = None
        self._ema_high = None
        self._ema_low = None

    def OnStarted2(self, time):
        super(mava_xonax_strategy, self).OnStarted2(time)
        self._ema_close = ExponentialMovingAverage()
        self._ema_close.Length = self._ema_period.Value
        self._ema_open = ExponentialMovingAverage()
        self._ema_open.Length = self._ema_period.Value
        self._ema_high = ExponentialMovingAverage()
        self._ema_high.Length = self._ema_period.Value
        self._ema_low = ExponentialMovingAverage()
        self._ema_low.Length = self._ema_period.Value
        self._cooldown_remaining = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema_close, self._process_candle).Start()

    def _process_candle(self, candle, close_ema_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        close_ema = float(close_ema_val)

        open_result = process_float(self._ema_open, candle.OpenPrice, candle.ServerTime, True)
        high_result = process_float(self._ema_high, candle.HighPrice, candle.ServerTime, True)
        low_result = process_float(self._ema_low, candle.LowPrice, candle.ServerTime, True)

        if not open_result.IsFinal or not high_result.IsFinal or not low_result.IsFinal:
            return

        open_ema = float(open_result)
        high_ema = float(high_result)
        low_ema = float(low_result)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close <= self._long_stop or close >= self._long_take:
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0:
            if close >= self._short_stop or close <= self._short_take:
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        if self._history >= 2 and self._cooldown_remaining == 0 and self.IsFormedAndOnlineAndAllowTrading():
            step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
            buy_signal = self._prev_open2 > self._prev_close2 and self._prev_open1 < self._prev_close1
            sell_signal = self._prev_open2 < self._prev_close2 and self._prev_open1 > self._prev_close1
            if buy_signal and self.Position == 0:
                take_pr = self._prev_high - self._prev_low
                if take_pr < 600.0 * step:
                    take_pr = 600.0 * step
                stop_l = 2.0 * (self._prev_open1 - self._prev_low)
                if stop_l > 400.0 * step:
                    stop_l = 400.0 * step
                self._long_stop = close - stop_l
                self._long_take = close + take_pr
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
            elif sell_signal and self.Position == 0:
                take_pr = self._prev_high - self._prev_low
                if take_pr < 600.0 * step:
                    take_pr = 600.0 * step
                stop_l = 2.0 * (self._prev_high - self._prev_close1)
                if stop_l > 400.0 * step:
                    stop_l = 400.0 * step
                self._short_stop = close + stop_l
                self._short_take = close - take_pr
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_open2 = self._prev_open1
        self._prev_open1 = open_ema
        self._prev_close2 = self._prev_close1
        self._prev_close1 = close_ema
        self._prev_high = high_ema
        self._prev_low = low_ema
        if self._history < 2:
            self._history += 1

    def CreateClone(self):
        return mava_xonax_strategy()
