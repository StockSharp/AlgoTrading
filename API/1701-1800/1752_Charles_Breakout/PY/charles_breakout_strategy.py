import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class charles_breakout_strategy(Strategy):
    def __init__(self):
        super(charles_breakout_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 18) \
            .SetDisplay("Fast EMA Period", "Fast EMA length", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 60) \
            .SetDisplay("Slow EMA Period", "Slow EMA length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(charles_breakout_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(charles_breakout_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self.SubscribeCandles(self.candle_type).Bind(fast_ema, slow_ema, rsi, self.process_candle).Start()

    def process_candle(self, candle, fast_ema, slow_ema, rsi):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_ema)
        sv = float(slow_ema)
        rv = float(rsi)

        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv

        if cross_up and rv > 50 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and rv < 50 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return charles_breakout_strategy()
