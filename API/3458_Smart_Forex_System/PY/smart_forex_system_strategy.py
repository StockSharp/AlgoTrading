import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class smart_forex_system_strategy(Strategy):
    def __init__(self):
        super(smart_forex_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 10)
        self._mid_period = self.Param("MidPeriod", 25)
        self._slow_period = self.Param("SlowPeriod", 50)

        self._was_bullish_alignment = False
        self._has_prev_alignment = False

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
    def MidPeriod(self):
        return self._mid_period.Value

    @MidPeriod.setter
    def MidPeriod(self, value):
        self._mid_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    def OnReseted(self):
        super(smart_forex_system_strategy, self).OnReseted()
        self._was_bullish_alignment = False
        self._has_prev_alignment = False

    def OnStarted2(self, time):
        super(smart_forex_system_strategy, self).OnStarted2(time)
        self._was_bullish_alignment = False
        self._has_prev_alignment = False

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        mid = ExponentialMovingAverage()
        mid.Length = self.MidPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, mid, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, mid_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        mid_val = float(mid_value)
        slow_val = float(slow_value)

        bullish_alignment = fast_val > mid_val and mid_val > slow_val
        bearish_alignment = fast_val < mid_val and mid_val < slow_val

        crossed_up = bullish_alignment and (not self._has_prev_alignment or not self._was_bullish_alignment)
        crossed_down = bearish_alignment and (not self._has_prev_alignment or self._was_bullish_alignment)

        if crossed_up and self.Position <= 0:
            self.BuyMarket()
        elif crossed_down and self.Position >= 0:
            self.SellMarket()

        if bullish_alignment or bearish_alignment:
            self._was_bullish_alignment = bullish_alignment
            self._has_prev_alignment = True

    def CreateClone(self):
        return smart_forex_system_strategy()
