import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage


class ema_crossover_trailing_strategy(Strategy):
    """EMA crossover strategy with stepped trailing stop."""

    def __init__(self):
        super(ema_crossover_trailing_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Period of the fast EMA", "Moving Averages")
        self._slow_period = self.Param("SlowPeriod", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Period of the slow EMA", "Moving Averages")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 20.0) \
            .SetDisplay("Trailing Stop (points)", "Distance from price to trailing stop in price steps", "Risk")
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0) \
            .SetDisplay("Trailing Step (points)", "Minimum advancement before moving stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Order volume for entries", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._prev_fast = None
        self._prev_slow = None
        self._long_stop = None
        self._short_stop = None
        self._stop_dist = 0.0
        self._step_dist = 0.0

    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value
    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value
    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    def _calc_distance(self, points):
        if points <= 0:
            return 0.0
        sec = self.Security
        step = 1.0
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            step = float(sec.PriceStep)
        return float(points) * step

    def OnStarted2(self, time):
        super(ema_crossover_trailing_strategy, self).OnStarted2(time)

        self.Volume = self.TradeVolume
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastPeriod
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowPeriod

        self._stop_dist = self._calc_distance(self.TrailingStopPoints)
        self._step_dist = self._calc_distance(self.TrailingStepPoints)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        self._stop_dist = self._calc_distance(self.TrailingStopPoints)
        self._step_dist = self._calc_distance(self.TrailingStepPoints)

        self._update_trailing(candle)

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        crossed_up = self._prev_fast <= self._prev_slow and fv > sv
        crossed_down = self._prev_fast >= self._prev_slow and fv < sv

        if crossed_up and self.Position <= 0:
            self.BuyMarket()
            self._init_long_trailing(float(candle.ClosePrice))

        elif crossed_down and self.Position >= 0:
            self.SellMarket()
            self._init_short_trailing(float(candle.ClosePrice))

        self._prev_fast = fv
        self._prev_slow = sv

    def _init_long_trailing(self, price):
        if self._stop_dist <= 0:
            self._long_stop = None
            return
        self._long_stop = price - self._stop_dist
        self._short_stop = None

    def _init_short_trailing(self, price):
        if self._stop_dist <= 0:
            self._short_stop = None
            return
        self._short_stop = price + self._stop_dist
        self._long_stop = None

    def _update_trailing(self, candle):
        if self._stop_dist <= 0:
            self._long_stop = None
            self._short_stop = None
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self._long_stop is None:
                self._init_long_trailing(close)
            else:
                new_stop = close - self._stop_dist
                if new_stop - self._long_stop >= self._step_dist:
                    self._long_stop = new_stop
                if float(candle.LowPrice) <= self._long_stop:
                    self.SellMarket()
                    self._long_stop = None
        elif self.Position < 0:
            if self._short_stop is None:
                self._init_short_trailing(close)
            else:
                new_stop = close + self._stop_dist
                if self._short_stop - new_stop >= self._step_dist:
                    self._short_stop = new_stop
                if float(candle.HighPrice) >= self._short_stop:
                    self.BuyMarket()
                    self._short_stop = None
        else:
            self._long_stop = None
            self._short_stop = None

    def OnReseted(self):
        super(ema_crossover_trailing_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
        self._long_stop = None
        self._short_stop = None
        self._fast_ema = None
        self._slow_ema = None

    def CreateClone(self):
        return ema_crossover_trailing_strategy()
