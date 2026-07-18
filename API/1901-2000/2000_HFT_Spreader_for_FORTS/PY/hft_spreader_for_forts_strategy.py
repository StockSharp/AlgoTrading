import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class hft_spreader_for_forts_strategy(Strategy):
    """
    HFT Spreader for FORTS: trades when candle spread is wide enough.
    Opens and closes based on high-low range vs spread multiplier.
    """

    def __init__(self):
        super(hft_spreader_for_forts_strategy, self).__init__()
        self._spread_multiplier = self.Param("SpreadMultiplier", 4) \
            .SetDisplay("Spread Multiplier", "Spread ticks required to place orders", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle type for price feed", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hft_spreader_for_forts_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        spread = float(candle.HighPrice) - float(candle.LowPrice)

        if spread >= self._spread_multiplier.Value * step:
            if self.Position == 0:
                self.BuyMarket()
            elif self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

    def CreateClone(self):
        return hft_spreader_for_forts_strategy()
