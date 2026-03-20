import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class my_line_order_strategy(Strategy):
    def __init__(self):
        super(my_line_order_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("SMA", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def sma_length(self):
        return self._sma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(my_line_order_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(my_line_order_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_close = close
            self._prev_sma = sma
            self._has_prev = True
            return
        # Cross above SMA
        if self._prev_close <= self._prev_sma and close > sma:
            if self.Position < 0) BuyMarket(:
                if self.Position <= 0) BuyMarket(:
            # Cross below SMA
        elif self._prev_close >= self._prev_sma and close < sma:
            if self.Position > 0) SellMarket(:
                if self.Position >= 0) SellMarket(:
            self._prev_close = close
        self._prev_sma = sma

    def CreateClone(self):
        return my_line_order_strategy()
