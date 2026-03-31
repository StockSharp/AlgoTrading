import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class doji_arrows_breakout_strategy(Strategy):
    def __init__(self):
        super(doji_arrows_breakout_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for doji detection", "Indicators")
        self._doji_threshold = self.Param("DojiThreshold", 0.3) \
            .SetDisplay("Doji Threshold", "Max body/ATR ratio for doji", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def doji_threshold(self):
        return self._doji_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_arrows_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    def OnStarted2(self, time):
        super(doji_arrows_breakout_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._prev_was_doji = False
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.process_candle).Start()

    def process_candle(self, candle, atr):
        if candle.State != CandleStates.Finished:
            return
        atr_val = float(atr)
        if atr_val <= 0:
            return
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        is_doji = body / atr_val < self.doji_threshold
        if self._has_prev and self._prev_was_doji:
            if float(candle.ClosePrice) > self._prev_high and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif float(candle.ClosePrice) < self._prev_low and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_was_doji = is_doji
        self._has_prev = True

    def CreateClone(self):
        return doji_arrows_breakout_strategy()
