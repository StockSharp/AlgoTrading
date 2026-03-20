import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class non_lag_dot_strategy(Strategy):
    def __init__(self):
        super(non_lag_dot_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetDisplay("Length", "Moving average period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percent based stop-loss", "Risk")
        self._prev_sma = None
        self._prev_trend = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    def OnReseted(self):
        super(non_lag_dot_strategy, self).OnReseted()
        self._prev_sma = None
        self._prev_trend = 0

    def OnStarted(self, time):
        super(non_lag_dot_strategy, self).OnStarted(time)
        sma = ExponentialMovingAverage()
        sma.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        sma_val = float(sma_val)
        if self._prev_sma is None:
            self._prev_sma = sma_val
            return
        if sma_val > self._prev_sma:
            trend = 1
        elif sma_val < self._prev_sma:
            trend = -1
        else:
            trend = self._prev_trend
        if trend > 0 and self._prev_trend < 0 and self.Position <= 0:
            self.BuyMarket()
        elif trend < 0 and self._prev_trend > 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_trend = trend
        self._prev_sma = sma_val

    def CreateClone(self):
        return non_lag_dot_strategy()
