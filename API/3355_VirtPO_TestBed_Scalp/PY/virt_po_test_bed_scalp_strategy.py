import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class virt_po_test_bed_scalp_strategy(Strategy):
    def __init__(self):
        super(virt_po_test_bed_scalp_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for breakout offset", "Indicators")

        self._atr = None
        self._prev_high = None
        self._prev_low = None

    @property
    def atr_period(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(virt_po_test_bed_scalp_strategy, self).OnReseted()
        self._atr = None
        self._prev_high = None
        self._prev_low = None

    def OnStarted2(self, time):
        super(virt_po_test_bed_scalp_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        subscription.Bind(self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed:
            return

        close = float(candle.ClosePrice)
        atr_val = float(atr_value)

        if self._prev_high is not None and self._prev_low is not None:
            if close > self._prev_high + atr_val * 0.5 and self.Position <= 0:
                self.BuyMarket()
            elif close < self._prev_low - atr_val * 0.5 and self.Position >= 0:
                self.SellMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return virt_po_test_bed_scalp_strategy()
