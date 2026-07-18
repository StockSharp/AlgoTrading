import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_coppock_strategy(Strategy):
    def __init__(self):
        super(color_coppock_strategy, self).__init__()
        self._roc1_period = self.Param("Roc1Period", 14) \
            .SetDisplay("ROC1 Period", "First ROC calculation period", "Parameters")
        self._roc2_period = self.Param("Roc2Period", 10) \
            .SetDisplay("ROC2 Period", "Second ROC calculation period", "Parameters")
        self._smoothing_period = self.Param("SmoothingPeriod", 10) \
            .SetDisplay("Smoothing Period", "SMA period for ROC sum", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for processing", "General")
        self._closes = []
        self._coppock_values = []
        self._prev_coppock = None
        self._prev_prev_coppock = None

    @property
    def roc1_period(self):
        return self._roc1_period.Value

    @property
    def roc2_period(self):
        return self._roc2_period.Value

    @property
    def smoothing_period(self):
        return self._smoothing_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_coppock_strategy, self).OnReseted()
        self._closes = []
        self._coppock_values = []
        self._prev_coppock = None
        self._prev_prev_coppock = None

    def OnStarted2(self, time):
        super(color_coppock_strategy, self).OnStarted2(time)

        sma = ExponentialMovingAverage()
        sma.Length = int(self.roc1_period)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._closes.append(close)
        r1p = int(self.roc1_period)
        r2p = int(self.roc2_period)
        sp = int(self.smoothing_period)
        max_period = max(r1p, r2p)

        if len(self._closes) > max_period + sp + 5:
            self._closes.pop(0)

        if len(self._closes) <= max_period:
            return

        idx = len(self._closes) - 1
        roc1 = 0.0
        roc2 = 0.0

        if idx >= r1p and self._closes[idx - r1p] != 0.0:
            roc1 = (self._closes[idx] - self._closes[idx - r1p]) / self._closes[idx - r1p] * 100.0
        if idx >= r2p and self._closes[idx - r2p] != 0.0:
            roc2 = (self._closes[idx] - self._closes[idx - r2p]) / self._closes[idx - r2p] * 100.0

        self._coppock_values.append(roc1 + roc2)
        if len(self._coppock_values) > sp + 5:
            self._coppock_values.pop(0)

        if len(self._coppock_values) < sp:
            return

        # SMA of ROC sum
        coppock = sum(self._coppock_values[-sp:]) / sp

        if self._prev_coppock is not None and self._prev_prev_coppock is not None:
            # Buy when Coppock turns up from bottom
            if self._prev_coppock < self._prev_prev_coppock and coppock > self._prev_coppock and self.Position <= 0:
                self.BuyMarket()
            # Sell when Coppock turns down from top
            elif self._prev_coppock > self._prev_prev_coppock and coppock < self._prev_coppock and self.Position >= 0:
                self.SellMarket()

        self._prev_prev_coppock = self._prev_coppock
        self._prev_coppock = coppock

    def CreateClone(self):
        return color_coppock_strategy()
