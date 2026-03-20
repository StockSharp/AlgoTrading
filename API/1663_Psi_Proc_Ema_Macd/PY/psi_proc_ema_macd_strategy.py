import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class psi_proc_ema_macd_strategy(Strategy):
    def __init__(self):
        super(psi_proc_ema_macd_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_ema200 = 0.0
        self._prev_ema50 = 0.0
        self._prev_ema10 = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(psi_proc_ema_macd_strategy, self).OnReseted()
        self._prev_ema200 = 0.0
        self._prev_ema50 = 0.0
        self._prev_ema10 = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(psi_proc_ema_macd_strategy, self).OnStarted(time)
        ema200 = ExponentialMovingAverage()
        ema200.Length = 50
        ema50 = ExponentialMovingAverage()
        ema50.Length = 20
        ema10 = ExponentialMovingAverage()
        ema10.Length = 10
        macd = MovingAverageConvergenceDivergence()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema200, ema50, ema10, macd, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema200, ema50, ema10, macd_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._initialized:
            self._prev_ema200 = ema200
            self._prev_ema50 = ema50
            self._prev_ema10 = ema10
            self._initialized = True
            return
        # Entry/reversal conditions - EMA alignment
        if ema10 > ema50 and self.Position <= 0:
            self.BuyMarket()
        elif ema10 < ema50 and self.Position >= 0:
            self.SellMarket()
        self._prev_ema200 = ema200
        self._prev_ema50 = ema50
        self._prev_ema10 = ema10

    def CreateClone(self):
        return psi_proc_ema_macd_strategy()
