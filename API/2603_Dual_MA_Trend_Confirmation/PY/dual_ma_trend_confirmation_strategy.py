import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dual_ma_trend_confirmation_strategy(Strategy):
    def __init__(self):
        super(dual_ma_trend_confirmation_strategy, self).__init__()

        self._slow_ma_length = self.Param("SlowMaLength", 57)
        self._fast_ma_length = self.Param("FastMaLength", 3)
        self._stop_loss_points = self.Param("StopLossPoints", 100.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 100.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._previous_close = 0.0
        self._slow_previous = 0.0
        self._slow_previous2 = 0.0
        self._fast_previous = 0.0
        self._fast_previous2 = 0.0
        self._history_count = 0

    @property
    def SlowMaLength(self):
        return self._slow_ma_length.Value

    @SlowMaLength.setter
    def SlowMaLength(self, value):
        self._slow_ma_length.Value = value

    @property
    def FastMaLength(self):
        return self._fast_ma_length.Value

    @FastMaLength.setter
    def FastMaLength(self, value):
        self._fast_ma_length.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(dual_ma_trend_confirmation_strategy, self).OnStarted(time)

        self._previous_close = 0.0
        self._slow_previous = 0.0
        self._slow_previous2 = 0.0
        self._fast_previous = 0.0
        self._fast_previous2 = 0.0
        self._history_count = 0

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowMaLength

        self._fast_lwma = WeightedMovingAverage()
        self._fast_lwma.Length = self.FastMaLength

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        self.StartProtection(
            Unit(float(self.TakeProfitPoints) * step, UnitTypes.Absolute),
            Unit(float(self.StopLossPoints) * step, UnitTypes.Absolute),
            False, None, None, True)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._slow_ema, self._fast_lwma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, slow_value, fast_value):
        if candle.State != CandleStates.Finished:
            return

        sv = float(slow_value)
        fv = float(fast_value)
        close = float(candle.ClosePrice)

        if not self._slow_ema.IsFormed or not self._fast_lwma.IsFormed:
            self._update_history(sv, fv, close)
            return

        if self._history_count < 2:
            self._update_history(sv, fv, close)
            return

        slow_rising = sv > self._slow_previous and self._slow_previous > self._slow_previous2
        fast_rising = fv > self._fast_previous and self._fast_previous > self._fast_previous2
        slow_falling = sv < self._slow_previous and self._slow_previous < self._slow_previous2
        fast_falling = fv < self._fast_previous and self._fast_previous < self._fast_previous2
        price_above_slow = self._previous_close > self._slow_previous
        price_below_slow = self._previous_close < self._slow_previous
        slow_above_fast = sv > fv
        slow_below_fast = sv < fv

        if slow_rising and fast_rising and price_above_slow and slow_above_fast and self.Position <= 0:
            self.BuyMarket()
        elif slow_falling and fast_falling and price_below_slow and slow_below_fast and self.Position >= 0:
            self.SellMarket()

        self._update_history(sv, fv, close)

    def _update_history(self, slow_value, fast_value, close):
        self._slow_previous2 = self._slow_previous
        self._slow_previous = slow_value
        self._fast_previous2 = self._fast_previous
        self._fast_previous = fast_value
        self._previous_close = close
        if self._history_count < 2:
            self._history_count += 1

    def OnReseted(self):
        super(dual_ma_trend_confirmation_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._slow_previous = 0.0
        self._slow_previous2 = 0.0
        self._fast_previous = 0.0
        self._fast_previous2 = 0.0
        self._history_count = 0

    def CreateClone(self):
        return dual_ma_trend_confirmation_strategy()
