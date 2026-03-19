import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ard_order_management_command_strategy(Strategy):
    """
    ARD Order Management Command: EMA trend following with ATR-based stops.
    """

    def __init__(self):
        super(ard_order_management_command_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_close = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def AtrLength(self): return self._atr_length.Value
    @AtrLength.setter
    def AtrLength(self, v): self._atr_length.Value = v

    def OnReseted(self):
        super(ard_order_management_command_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(ard_order_management_command_strategy, self).OnStarted(time)

        self._prev_close = 0.0
        self._entry_price = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._prev_close == 0 or atr_val <= 0:
            self._prev_close = close
            return

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 2.5 or close <= self._entry_price - atr_val * 1.5 or close < ema_val:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 2.5 or close >= self._entry_price + atr_val * 1.5 or close > ema_val:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close > ema_val and self._prev_close <= ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif close < ema_val and self._prev_close >= ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_close = close

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ard_order_management_command_strategy()
