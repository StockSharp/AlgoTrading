import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class close_orders_strategy(Strategy):
    def __init__(self):
        super(close_orders_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 40) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(close_orders_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(close_orders_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
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
        # EMA rising -> buy
        if ema_val > self._prev_ema and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # EMA falling -> sell
        elif ema_val < self._prev_ema and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_ema = ema_val

    def CreateClone(self):
        return close_orders_strategy()
