import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class epsi_multi_set_strategy(Strategy):
    """
    Breakout strategy. Opens a position when price moves significantly from candle open.
    Uses StdDev as volatility measure for breakout confirmation and exit levels.
    """

    def __init__(self):
        super(epsi_multi_set_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._breakout_mult = self.Param("BreakoutMult", 0.5) \
            .SetDisplay("Breakout Mult", "ATR multiplier for breakout", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(epsi_multi_set_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(epsi_multi_set_strategy, self).OnStarted(time)

        atr = StandardDeviation()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_val)
        if atr_val <= 0.0:
            return

        min_dist = atr_val * self._breakout_mult.Value
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        if self.Position == 0:
            if high - open_p >= min_dist:
                self.BuyMarket()
                self._entry_price = close
            elif open_p - low >= min_dist:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0:
            if close <= self._entry_price - atr_val * 2.0 or close >= self._entry_price + atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + atr_val * 2.0 or close <= self._entry_price - atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0.0

    def CreateClone(self):
        return epsi_multi_set_strategy()
