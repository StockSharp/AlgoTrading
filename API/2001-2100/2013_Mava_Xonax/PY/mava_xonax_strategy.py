import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class mava_xonax_strategy(Strategy):
    """Long: open-price EMA crosses ABOVE close-price EMA; short: crosses BELOW."""

    def __init__(self):
        super(mava_xonax_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 6).SetDisplay("EMA Period", "EMA period", "General")
        self._cooldown_bars = self.Param("SignalCooldownBars", 1).SetDisplay(
            "Signal Cooldown", "Bars to wait after entry or exit", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay(
            "Candle Type", "Candles", "General")
        self._ema_close = self._ema_open = self._ema_high = self._ema_low = None
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._have_previous = False
        self._long_stop = self._long_take = 0.0
        self._short_stop = self._short_take = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mava_xonax_strategy, self).OnReseted()
        self._ema_close = self._ema_open = self._ema_high = self._ema_low = None
        self._previous_open = self._previous_close = 0.0
        self._have_previous = False
        self._long_stop = self._long_take = 0.0
        self._short_stop = self._short_take = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(mava_xonax_strategy, self).OnStarted2(time)
        self._ema_close = ExponentialMovingAverage()
        self._ema_open = ExponentialMovingAverage()
        self._ema_high = ExponentialMovingAverage()
        self._ema_low = ExponentialMovingAverage()
        for indicator in (self._ema_close, self._ema_open, self._ema_high, self._ema_low):
            indicator.Length = self._ema_period.Value
        self._have_previous = False
        self._cooldown_remaining = 0
        self.SubscribeCandles(self.candle_type).Bind(self._ema_close, self._process_candle).Start()

    def _process_candle(self, candle, close_ema_val):
        if candle.State != CandleStates.Finished:
            return
        cooling_down = self._cooldown_remaining > 0
        if cooling_down:
            self._cooldown_remaining -= 1

        open_value = process_float(self._ema_open, candle.OpenPrice, candle.ServerTime, True)
        high_value = process_float(self._ema_high, candle.HighPrice, candle.ServerTime, True)
        low_value = process_float(self._ema_low, candle.LowPrice, candle.ServerTime, True)
        if not open_value.IsFinal or not high_value.IsFinal or not low_value.IsFinal:
            return
        open_ema = float(open_value)
        high_ema = float(high_value)
        low_ema = float(low_value)
        close_ema = float(close_ema_val)
        close = float(candle.ClosePrice)
        formed = all(ind.IsFormed for ind in (self._ema_open, self._ema_close, self._ema_high, self._ema_low))
        closed = False

        # Provisional candle-close risk exits; actual quote/fill behavior needs native replay.
        if self.Position > 0 and (close <= self._long_stop or close >= self._long_take):
            self.SellMarket(self.Position)
            self._cooldown_remaining = self._cooldown_bars.Value
            closed = True
        elif self.Position < 0 and (close >= self._short_stop or close <= self._short_take):
            self.BuyMarket(-self.Position)
            self._cooldown_remaining = self._cooldown_bars.Value
            closed = True

        if formed and self._have_previous and not closed and not cooling_down and self._cooldown_remaining == 0 and self.Position == 0 and self.IsFormedAndOnlineAndAllowTrading():
            cross_up = self._previous_open <= self._previous_close and open_ema > close_ema
            cross_down = self._previous_open >= self._previous_close and open_ema < close_ema
            if cross_up or cross_down:
                raw_step = getattr(self.Security, 'PriceStep', None)
                step = float(raw_step) if raw_step is not None and float(raw_step) > 0 else 1.0
                take_distance = max(high_ema - low_ema, 600.0 * step)
                stop_raw = 2.0 * (open_ema - low_ema) if cross_up else 2.0 * (high_ema - close_ema)
                stop_distance = max(step, min(stop_raw, 400.0 * step))
                if cross_up:
                    self._long_stop = close - stop_distance
                    self._long_take = close + take_distance
                    self.BuyMarket()
                else:
                    self._short_stop = close + stop_distance
                    self._short_take = close - take_distance
                    self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        self._previous_open = open_ema
        self._previous_close = close_ema
        self._have_previous = formed

    def CreateClone(self):
        return mava_xonax_strategy()
