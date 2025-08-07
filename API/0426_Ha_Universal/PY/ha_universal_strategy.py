import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import HeikinAshi
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class ha_universal_strategy(Strategy):
    """Heikin Ashi universal strategy.

    Converts standard candles to Heikin Ashi and trades in the direction of
    the transformed body. Can be extended with additional filters.
    """

    def __init__(self):
        super(ha_universal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ha = HeikinAshi()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ha_universal_strategy, self).OnReseted()
        self._ha = HeikinAshi()

    def OnStarted(self, time):
        super(ha_universal_strategy, self).OnStarted(time)
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ha, self._on_process).Start()

    def _on_process(self, candle, ha_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_ = ha_value.Open
        close = ha_value.Close
        if close > open_ and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif close < open_ and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return ha_universal_strategy()
