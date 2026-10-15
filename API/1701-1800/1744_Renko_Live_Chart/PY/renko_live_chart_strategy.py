import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class renko_live_chart_strategy(Strategy):
    def __init__(self):
        super(renko_live_chart_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")
        self._brick_size = self.Param("BrickSize", 500.0) \
            .SetDisplay("Brick Size", "Renko brick size", "General")
        self._renko_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def brick_size(self):
        return self._brick_size.Value

    def OnReseted(self):
        super(renko_live_chart_strategy, self).OnReseted()
        self._renko_price = 0.0

    def OnStarted2(self, time):
        super(renko_live_chart_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self.candle_type).Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        size = float(self.brick_size)

        if self._renko_price == 0.0:
            self._renko_price = close
            return

        diff = close - self._renko_price
        if abs(diff) < size:
            return

        direction = 1 if diff > 0 else -1
        self._renko_price += direction * size

        if direction > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif direction < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return renko_live_chart_strategy()
