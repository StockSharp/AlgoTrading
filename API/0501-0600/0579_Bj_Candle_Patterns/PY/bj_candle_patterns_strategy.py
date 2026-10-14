import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bj_candle_patterns_strategy(Strategy):
    """
    Bj Candle Patterns strategy.
    A doji has a body of at most DojiThreshold of its high-low range. A Dragonfly Doji also has an upper wick of at most
    DojiThreshold of the range (a long lower wick) and goes long; a Gravestone Doji has a lower wick of at most DojiThreshold of the
    range (a long upper wick) and goes short. The opposite pattern reverses the position.
    """

    def __init__(self):
        super(bj_candle_patterns_strategy, self).__init__()
        self._doji_threshold = self.Param("DojiThreshold", 0.1).SetRange(0.0, 1.0).SetDisplay("Doji Threshold", "Maximum body and short-wick size as a fraction of the range", "Patterns")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bj_candle_patterns_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rng = candle.HighPrice - candle.LowPrice
        if rng <= 0:
            return

        body_top = max(candle.OpenPrice, candle.ClosePrice)
        body_bottom = min(candle.OpenPrice, candle.ClosePrice)
        limit = Decimal(self._doji_threshold.Value) * rng

        if body_top - body_bottom > limit:
            return

        dragonfly = candle.HighPrice - body_top <= limit
        gravestone = body_bottom - candle.LowPrice <= limit

        if dragonfly and not gravestone and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif gravestone and not dragonfly and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return bj_candle_patterns_strategy()
