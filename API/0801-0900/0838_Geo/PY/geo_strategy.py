import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class geo_strategy(Strategy):
    """
    Geo strategy.
    The candle is split at its close into the part above the low and the part below the high. When the lower part divided by the upper
    part is within Tolerance percent of the golden ratio the close sits at the upper golden section and the strategy goes long; when the
    upper part divided by the lower part is within tolerance it goes short. The opposite condition reverses the position.
    """

    PHI = Decimal(1.6180339887)

    def __init__(self):
        super(geo_strategy, self).__init__()
        self._tolerance = self.Param("Tolerance", 1.0).SetNotNegative().SetDisplay("Tolerance", "Allowed deviation from the golden ratio in percent", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(geo_strategy, self).OnStarted2(time)

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

        lower_part = candle.ClosePrice - candle.LowPrice
        upper_part = candle.HighPrice - candle.ClosePrice

        if lower_part <= 0 or upper_part <= 0:
            return

        allowed = self.PHI * Decimal(self._tolerance.Value) / Decimal(100)

        if abs(lower_part / upper_part - self.PHI) <= allowed and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif abs(upper_part / lower_part - self.PHI) <= allowed and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return geo_strategy()
