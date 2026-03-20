import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class jma_slope_strategy(Strategy):
    def __init__(self):
        super(jma_slope_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 14) \
            .SetDisplay("JMA Length", "Period for Jurik Moving Average", "Indicators")
        self._jma_phase = self.Param("JmaPhase", 0) \
            .SetDisplay("JMA Phase", "Phase parameter", "Indicators")
        self._mode = self.Param("Mode", 0) \
            .SetDisplay("Mode", "Entry algorithm (0=Breakdown, 1=Twist)", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_jma = None
        self._prev_slope1 = None
        self._prev_slope2 = None
        self._prev_slope3 = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def jma_phase(self):
        return self._jma_phase.Value

    @property
    def mode(self):
        return self._mode.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jma_slope_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_slope1 = None
        self._prev_slope2 = None
        self._prev_slope3 = None

    def OnStarted(self, time):
        super(jma_slope_strategy, self).OnStarted(time)
        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        jma.Phase = self.jma_phase
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        slope = jma_value - self._prev_jma if self._prev_jma is not None else None
        if (self._prev_slope2 is not None and self._prev_slope1 is not None
                and self._prev_slope3 is not None):
            s1 = self._prev_slope1
            s2 = self._prev_slope2
            s3 = self._prev_slope3
            buy = False
            sell = False
            if self.mode == 0:
                buy = s2 > 0 and s1 <= 0
                sell = s2 < 0 and s1 >= 0
            else:
                buy = s2 < s3 and s1 > s2
                sell = s2 > s3 and s1 < s2
            if buy and self.Position <= 0:
                self.BuyMarket()
            elif sell and self.Position >= 0:
                self.SellMarket()
        self._prev_slope3 = self._prev_slope2
        self._prev_slope2 = self._prev_slope1
        self._prev_slope1 = slope
        self._prev_jma = jma_value

    def CreateClone(self):
        return jma_slope_strategy()
