import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp2_xma_ichimoku_oscillator_strategy(Strategy):
    def __init__(self):
        super(exp2_xma_ichimoku_oscillator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Fast moving average length", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow Period", "Slow moving average length", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(exp2_xma_ichimoku_oscillator_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_initialized = False

    def OnStarted(self, time):
        super(exp2_xma_ichimoku_oscillator_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_initialized = False

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastPeriod

        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(fast_ma, slow_ma, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fv
            self._prev_slow = sv
            self._prev_initialized = True
            return

        if not self._prev_initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._prev_initialized = True
            return

        if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return exp2_xma_ichimoku_oscillator_strategy()
