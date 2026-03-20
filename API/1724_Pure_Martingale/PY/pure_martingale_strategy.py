import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class pure_martingale_strategy(Strategy):
    def __init__(self):
        super(pure_martingale_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles for trade timing", "General")
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._bar_count = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pure_martingale_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._bar_count = 0

    def OnStarted(self, time):
        super(pure_martingale_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        self._bar_count += 1
        if self._bar_count >= 3:
            # Two consecutive rising closes above EMA => buy
            if close > self._prev_close and self._prev_close > self._prev_prev_close and close > ema_value and self.Position <= 0:
                if self.Position < 0) BuyMarket(:
                    self.BuyMarket()
            # Two consecutive falling closes below EMA => sell
            elif close < self._prev_close and self._prev_close < self._prev_prev_close and close < ema_value and self.Position >= 0:
                if self.Position > 0) SellMarket(:
                    self.SellMarket()
        self._prev_prev_close = self._prev_close
        self._prev_close = close

    def CreateClone(self):
        return pure_martingale_strategy()
