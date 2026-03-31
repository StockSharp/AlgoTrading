import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class tma_breakout_strategy(Strategy):
    def __init__(self):
        super(tma_breakout_strategy, self).__init__()
        self._length = self.Param("Length", 30) \
            .SetDisplay("TMA Length", "Period for the Triangular Moving Average", "Parameters")
        self._up_level = self.Param("UpLevel", 300.0) \
            .SetDisplay("Upper Level", "Offset above TMA in price units", "Parameters")
        self._down_level = self.Param("DownLevel", 300.0) \
            .SetDisplay("Lower Level", "Offset below TMA in price units", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_tma = None
        self._prev_close = None

    @property
    def length(self):
        return self._length.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tma_breakout_strategy, self).OnReseted()
        self._prev_tma = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(tma_breakout_strategy, self).OnStarted2(time)

        tma = ExponentialMovingAverage()
        tma.Length = self.length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(tma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, tma_value):
        if candle.State != CandleStates.Finished:
            return

        tma_val = float(tma_value)
        close = float(candle.ClosePrice)

        if self._prev_tma is None or self._prev_close is None:
            self._prev_tma = tma_val
            self._prev_close = close
            return

        signal_up = self._prev_close > self._prev_tma + float(self.up_level)
        signal_dn = self._prev_close < self._prev_tma - float(self.down_level)

        if signal_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif signal_dn and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_tma = tma_val
        self._prev_close = close

    def CreateClone(self):
        return tma_breakout_strategy()
