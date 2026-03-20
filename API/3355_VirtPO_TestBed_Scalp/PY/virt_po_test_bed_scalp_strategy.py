import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class virt_po_test_bed_scalp_strategy(Strategy):
    def __init__(self):
        super(virt_po_test_bed_scalp_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(virt_po_test_bed_scalp_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(virt_po_test_bed_scalp_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return virt_po_test_bed_scalp_strategy()
