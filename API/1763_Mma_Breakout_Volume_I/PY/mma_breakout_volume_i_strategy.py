import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mma_breakout_volume_i_strategy(Strategy):
    def __init__(self):
        super(mma_breakout_volume_i_strategy, self).__init__()
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow EMA Period", "Period for long moving average", "Indicators")
        self._exit_period = self.Param("ExitPeriod", 10) \
            .SetDisplay("Exit EMA Period", "Period for exit EMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_price = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def exit_period(self):
        return self._exit_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mma_breakout_volume_i_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(mma_breakout_volume_i_strategy, self).OnStarted(time)
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_period
        exit_ema = ExponentialMovingAverage()
        exit_ema.Length = self.exit_period
        self.SubscribeCandles(self.candle_type).Bind(slow_ema, exit_ema, self.process_candle).Start()

    def process_candle(self, candle, slow_value, exit_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sv = float(slow_value)
        exv = float(exit_value)

        if not self._has_prev:
            self._prev_price = close
            self._prev_slow = sv
            self._has_prev = True
            return

        is_cross_above = self._prev_price <= self._prev_slow and close > sv
        is_cross_below = self._prev_price >= self._prev_slow and close < sv

        if is_cross_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif is_cross_below and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and close < exv:
            self.SellMarket()
        elif self.Position < 0 and close > exv:
            self.BuyMarket()

        self._prev_price = close
        self._prev_slow = sv

    def CreateClone(self):
        return mma_breakout_volume_i_strategy()
