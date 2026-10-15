import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class basic_martingale_ea3_strategy(Strategy):

    def __init__(self):
        super(basic_martingale_ea3_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

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

    def OnReseted(self):
        super(basic_martingale_ea3_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(basic_martingale_ea3_strategy, self).OnStarted2(time)
        self._has_prev = False

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, rsi, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)
        rv = float(rsi_value)

        if self._has_prev:
            if self._prev_fast <= self._prev_slow and fv > sv and rv < 45 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and fv < sv and rv > 55 and self.Position >= 0:
                self.SellMarket()
        else:
            if fv > sv and rv < 45 and self.Position <= 0:
                self.BuyMarket()
            elif fv < sv and rv > 55 and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv
        self._has_prev = True

    def CreateClone(self):
        return basic_martingale_ea3_strategy()
