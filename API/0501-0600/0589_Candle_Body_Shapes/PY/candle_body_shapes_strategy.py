import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class candle_body_shapes_strategy(Strategy):
    """
    Candle Body Shapes strategy.
    "Near" means within BodyThreshold of the candle range. A candle that opens near its low and closes near its high goes long,
    one that opens near its high and closes near its low goes short; the opposite signal reverses the position.
    """

    def __init__(self):
        super(candle_body_shapes_strategy, self).__init__()
        self._body_threshold = self.Param("BodyThreshold", 0.2).SetRange(0.0, 1.0).SetDisplay("Body Threshold", "Fraction of the candle range that counts as near its high or low", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(candle_body_shapes_strategy, self).OnStarted2(time)

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

        price_range = candle.HighPrice - candle.LowPrice

        if price_range <= 0:
            return

        near = Decimal(self._body_threshold.Value) * price_range
        open_near_low = candle.OpenPrice - candle.LowPrice <= near
        open_near_high = candle.HighPrice - candle.OpenPrice <= near
        close_near_low = candle.ClosePrice - candle.LowPrice <= near
        close_near_high = candle.HighPrice - candle.ClosePrice <= near

        if open_near_low and close_near_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif open_near_high and close_near_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return candle_body_shapes_strategy()
