import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ama_trader_strategy(Strategy):
    """
    AMA Trader strategy. Uses Kaufman Adaptive MA with price crossover.
    """

    def __init__(self):
        super(ama_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(60)) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ama_period = self.Param("AmaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("AMA Period", "Kaufman AMA period", "Indicators")

        self._prev_close = None
        self._prev_ama = None

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def AmaPeriod(self): return self._ama_period.Value
    @AmaPeriod.setter
    def AmaPeriod(self, v): self._ama_period.Value = v

    def OnReseted(self):
        super(ama_trader_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ama = None

    def OnStarted(self, time):
        super(ama_trader_strategy, self).OnStarted(time)
        self._prev_close = None
        self._prev_ama = None

        ama = KaufmanAdaptiveMovingAverage()
        ama.Length = self.AmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ama, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ama)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ama_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            self._prev_ama = ama_val
            return

        if self._prev_close is None or self._prev_ama is None:
            self._prev_close = close
            self._prev_ama = ama_val
            return

        if self._prev_close <= self._prev_ama and close > ama_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close >= self._prev_ama and close < ama_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ama = ama_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ama_trader_strategy()
