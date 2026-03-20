import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lot_scalp_strategy(Strategy):
    def __init__(self):
        super(lot_scalp_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle source", "General")
        self._prev_open = 0.0
        self._prev_prev_open = 0.0
        self._bar_count = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lot_scalp_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_prev_open = 0.0
        self._bar_count = 0

    def OnStarted(self, time):
        super(lot_scalp_strategy, self).OnStarted(time)
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
        self._bar_count += 1
        close = candle.ClosePrice
        open = candle.OpenPrice
        if self._bar_count >= 3:
            diff = self._prev_prev_open - self._prev_open
            # Open prices diverging downward + close above EMA => buy
            if diff > 0 and close > ema_value and self.Position <= 0:
                if self.Position < 0) BuyMarket(:
                    self.BuyMarket()
            # Open prices diverging upward + close below EMA => sell
            elif diff < 0 and close < ema_value and self.Position >= 0:
                if self.Position > 0) SellMarket(:
                    self.SellMarket()
        self._prev_prev_open = self._prev_open
        self._prev_open = open

    def CreateClone(self):
        return lot_scalp_strategy()
