import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class move_cross_strategy(Strategy):
    def __init__(self):
        super(move_cross_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 24) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicators")
        self._threshold = self.Param("Threshold", 0.5) \
            .SetDisplay("Threshold", "RAVI threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ravi_prev1 = 0.0
        self._ravi_prev2 = 0.0
        self._ravi_prev3 = 0.0
        self._has_history = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(move_cross_strategy, self).OnReseted()
        self._ravi_prev1 = 0.0
        self._ravi_prev2 = 0.0
        self._ravi_prev3 = 0.0
        self._has_history = False

    def OnStarted2(self, time):
        super(move_cross_strategy, self).OnStarted2(time)
        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.fast_period
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if slow == 0:
            return
        ravi = (fast - slow) / slow * 100
        if self._has_history:
            # Buy: RAVI rising for 3 bars and above threshold
            ravi_rising = ravi > self._ravi_prev1 and self._ravi_prev1 > self._ravi_prev2 and self._ravi_prev2 > self._ravi_prev3
            # Sell: RAVI falling for 3 bars and below negative threshold
            ravi_falling = ravi < self._ravi_prev1 and self._ravi_prev1 < self._ravi_prev2 and self._ravi_prev2 < self._ravi_prev3
            if ravi_rising and ravi > self.threshold and self.Position <= 0:
                self.BuyMarket()
            elif ravi_falling and ravi < -self.threshold and self.Position >= 0:
                self.SellMarket()
        self._ravi_prev3 = self._ravi_prev2
        self._ravi_prev2 = self._ravi_prev1
        self._ravi_prev1 = ravi
        self._has_history = True

    def CreateClone(self):
        return move_cross_strategy()
