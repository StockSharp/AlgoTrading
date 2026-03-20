import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class nrtr_reversal_strategy(Strategy):
    """NRTR reversal strategy using ATR-based trailing stop.
    Maintains a trailing line based on ATR distance from price extremes.
    Reverses position when price crosses the trailing line."""

    def __init__(self):
        super(nrtr_reversal_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for trailing distance", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._trail_line = 0.0
        self._extreme = 0.0
        self._trend = 0
        self._is_initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    def OnReseted(self):
        super(nrtr_reversal_strategy, self).OnReseted()
        self._trail_line = 0.0
        self._extreme = 0.0
        self._trend = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(nrtr_reversal_strategy, self).OnStarted(time)

        self._is_initialized = False
        self._trend = 0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        atr_val = float(atr_value)
        offset = atr_val * float(self.AtrMultiplier)

        if not self._is_initialized:
            self._extreme = close
            self._trail_line = close - offset
            self._trend = 1
            self._is_initialized = True
            return

        if self._trend == 1:
            if close > self._extreme:
                self._extreme = close

            candidate = self._extreme - offset
            if candidate > self._trail_line:
                self._trail_line = candidate

            if close < self._trail_line:
                # Switch to downtrend
                self._trend = -1
                self._extreme = close
                self._trail_line = close + offset

                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
            elif self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
        else:
            if close < self._extreme:
                self._extreme = close

            candidate = self._extreme + offset
            if candidate < self._trail_line:
                self._trail_line = candidate

            if close > self._trail_line:
                # Switch to uptrend
                self._trend = 1
                self._extreme = close
                self._trail_line = close - offset

                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

    def CreateClone(self):
        return nrtr_reversal_strategy()
