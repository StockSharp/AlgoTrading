import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_sticker_strategy(Strategy):
    def __init__(self):
        super(ema_sticker_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("EMA Period", "Length of the EMA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._was_above = False
        self._has_prev = False

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_sticker_strategy, self).OnReseted()
        self._was_above = False
        self._has_prev = False

    def OnStarted(self, time):
        super(ema_sticker_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        is_above = candle.ClosePrice > ema_value
        if not self._has_prev:
            self._was_above = is_above
            self._has_prev = True
            return
        # Cross above EMA -> buy
        if is_above and not self._was_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Cross below EMA -> sell
        elif not is_above and self._was_above:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._was_above = is_above

    def CreateClone(self):
        return ema_sticker_strategy()
