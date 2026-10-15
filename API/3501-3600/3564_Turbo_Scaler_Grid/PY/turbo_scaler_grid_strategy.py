import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class turbo_scaler_grid_strategy(Strategy):
    def __init__(self):
        super(turbo_scaler_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 14)
        self._slow_period = self.Param("SlowPeriod", 34)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_upper = self.Param("RsiUpper", 60.0)
        self._rsi_lower = self.Param("RsiLower", 40.0)

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @RsiUpper.setter
    def RsiUpper(self, value):
        self._rsi_upper.Value = value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    @RsiLower.setter
    def RsiLower(self, value):
        self._rsi_lower.Value = value

    def OnReseted(self):
        super(turbo_scaler_grid_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(turbo_scaler_grid_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, rsi, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        rsi_val = float(rsi_value)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and rsi_val > float(self.RsiUpper):
            if self.Position <= 0:
                self.BuyMarket()
        elif cross_down and rsi_val < float(self.RsiLower):
            if self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return turbo_scaler_grid_strategy()
