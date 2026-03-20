import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class time_trader_strategy(Strategy):
    def __init__(self):
        super(time_trader_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(time_trader_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(time_trader_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_ema = ema_val
            self._has_prev = True
            return
        close = candle.ClosePrice
        # EMA rising and price above EMA -> buy
        if ema_val > self._prev_ema and close > ema_val and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # EMA falling and price below EMA -> sell
        elif ema_val < self._prev_ema and close < ema_val and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_ema = ema_val

    def CreateClone(self):
        return time_trader_strategy()
