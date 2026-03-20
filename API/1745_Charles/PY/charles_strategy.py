import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class charles_strategy(Strategy):
    def __init__(self):
        super(charles_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 18) \
            .SetDisplay("Fast EMA", "Period of the fast EMA", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 60) \
            .SetDisplay("Slow EMA", "Period of the slow EMA", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._was_fast_below_slow = False
        self._is_initialized = False

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
        super(charles_strategy, self).OnReseted()
        self._was_fast_below_slow = False
        self._is_initialized = False

    def OnStarted(self, time):
        super(charles_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self.SubscribeCandles(self.candle_type).Bind(fast, slow, rsi, self.process_candle).Start()

    def process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)
        rv = float(rsi_value)

        if not self._is_initialized:
            self._was_fast_below_slow = fv < sv
            self._is_initialized = True
            return

        is_fast_below_slow = fv < sv

        if self._was_fast_below_slow and not is_fast_below_slow and rv > 55 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif not self._was_fast_below_slow and is_fast_below_slow and rv < 45 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._was_fast_below_slow = is_fast_below_slow

    def CreateClone(self):
        return charles_strategy()
