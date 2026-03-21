import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_612_crossover_strategy(Strategy):
    """
    EMA 6/12 crossover strategy with trailing stop management.
    Enters on EMA crossover, manages position with take profit and trailing stop.
    """

    def __init__(self):
        super(ema_612_crossover_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle resolution", "General")
        self._fast_period = self.Param("FastPeriod", 6) \
            .SetDisplay("Fast Period", "Fast EMA length", "Moving Averages")
        self._slow_period = self.Param("SlowPeriod", 54) \
            .SetDisplay("Slow Period", "Slow EMA length", "Moving Averages")
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.001) \
            .SetDisplay("Take Profit", "Target distance in price units", "Risk")
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0.005) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step_offset = self.Param("TrailingStepOffset", 0.0005) \
            .SetDisplay("Trailing Step", "Additional profit required to tighten stop", "Risk")

        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_612_crossover_strategy, self).OnReseted()
        self._reset_position_state()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(ema_612_crossover_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)

        bullish_cross = False
        bearish_cross = False

        if self._prev_fast is not None and self._prev_slow is not None:
            bullish_cross = self._prev_slow > self._prev_fast and slow_val < fast_val
            bearish_cross = self._prev_slow < self._prev_fast and slow_val > fast_val

        self._handle_existing_position(candle, bullish_cross, bearish_cross)

        if self.Position == 0:
            if bullish_cross:
                self._enter_long(candle)
            elif bearish_cross:
                self._enter_short(candle)

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def _handle_existing_position(self, candle, bullish_cross, bearish_cross):
        if self.Position > 0:
            self._update_long_trailing(candle)
            do_exit = bearish_cross
            if not do_exit and self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                do_exit = True
            if not do_exit and self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                do_exit = True
            if do_exit:
                self.SellMarket()
                self._reset_position_state()
        elif self.Position < 0:
            self._update_short_trailing(candle)
            do_exit = bullish_cross
            if not do_exit and self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                do_exit = True
            if not do_exit and self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                do_exit = True
            if do_exit:
                self.BuyMarket()
                self._reset_position_state()

    def _enter_long(self, candle):
        self.BuyMarket()
        close = float(candle.ClosePrice)
        self._entry_price = close
        tp = self._take_profit_offset.Value
        self._take_profit_price = close + tp if tp > 0 else None
        self._stop_price = None

    def _enter_short(self, candle):
        self.SellMarket()
        close = float(candle.ClosePrice)
        self._entry_price = close
        tp = self._take_profit_offset.Value
        self._take_profit_price = close - tp if tp > 0 else None
        self._stop_price = None

    def _update_long_trailing(self, candle):
        trail = self._trailing_stop_offset.Value
        if trail <= 0 or self._entry_price is None:
            return
        close = float(candle.ClosePrice)
        gain = close - self._entry_price
        step = self._trailing_step_offset.Value
        trigger = trail + step
        if gain <= trigger:
            return
        candidate = close - trail
        min_advance = step if step > 0 else 0.0
        if self._stop_price is None or candidate - self._stop_price > min_advance:
            self._stop_price = candidate

    def _update_short_trailing(self, candle):
        trail = self._trailing_stop_offset.Value
        if trail <= 0 or self._entry_price is None:
            return
        close = float(candle.ClosePrice)
        gain = self._entry_price - close
        step = self._trailing_step_offset.Value
        trigger = trail + step
        if gain <= trigger:
            return
        candidate = close + trail
        min_advance = step if step > 0 else 0.0
        if self._stop_price is None or self._stop_price - candidate > min_advance:
            self._stop_price = candidate

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return ema_612_crossover_strategy()
