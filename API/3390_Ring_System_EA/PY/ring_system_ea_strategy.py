import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RateOfChange
from StockSharp.Algo.Strategies import Strategy


class ring_system_ea_strategy(Strategy):
    def __init__(self):
        super(ring_system_ea_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 30) \
            .SetDisplay("SMA Period", "SMA trend filter period", "Indicators")
        self._roc_period = self.Param("RocPeriod", 10) \
            .SetDisplay("ROC Period", "Rate of Change period", "Indicators")

        self._sma = None
        self._roc = None
        self._was_bullish = False
        self._has_prev_signal = False

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def roc_period(self):
        return self._roc_period.Value

    def OnReseted(self):
        super(ring_system_ea_strategy, self).OnReseted()
        self._sma = None
        self._roc = None
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted(self, time):
        super(ring_system_ea_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._roc = RateOfChange()
        self._roc.Length = self.roc_period
        self._has_prev_signal = False

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._sma, self._roc, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, roc_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._roc.IsFormed:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        roc_val = float(roc_value)
        is_bullish = roc_val > 0.0 and close > sma_val

        if self._has_prev_signal and is_bullish != self._was_bullish:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
            elif not is_bullish and roc_val < 0.0 and close < sma_val and self.Position >= 0:
                self.SellMarket()

        self._was_bullish = is_bullish
        self._has_prev_signal = True

    def CreateClone(self):
        return ring_system_ea_strategy()
